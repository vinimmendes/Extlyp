using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace CyberExtraction.Camp
{
    // C8/C9 wire contract. Apply/acknowledge only after all UTF-8 bytes and the
    // SHA-256 digest have been verified. Parts never represent partial profiles.
    internal static class CampSnapshotTransfer
    {
        public const string MessageName="ce-camp-snapshot-parts-v1";
        public const byte Version=1;
        public const int PartBytes=8192, MaxBytes=8*1024*1024, DigestBytes=32;
        public const int HeaderBytes=1+8+4+4+4+4+DigestBytes;
        public const int PartsPerTick=4;
        public const float SendInterval=.1f;
        public const float TransferTimeout=90f, PartTimeout=15f;
    }

    internal sealed class CampSnapshotReceiver
    {
        ulong latest;
        byte[] bytes,digest;
        int nextPart;
        float started,lastPart;
        public bool TimedOut(float now) => bytes!=null &&
            (now-started>CampSnapshotTransfer.TransferTimeout || now-lastPart>CampSnapshotTransfer.PartTimeout);
        public void Reset() { Discard(); latest=0; }
        public void Discard() { bytes=null; digest=null; nextPart=0; }
        public string Receive(FastBufferReader reader,float now,bool replaceIncomplete=false)
        {
            // NGO's named-message reader may already be past its message-name hash.
            int length=reader.Length-reader.Position;
            if(length<CampSnapshotTransfer.HeaderBytes || length>CampSnapshotTransfer.HeaderBytes+CampSnapshotTransfer.PartBytes)
                throw new InvalidOperationException("Tamanho de parte invalido.");
            reader.ReadValueSafe(out byte version); reader.ReadValueSafe(out ulong id);
            reader.ReadValueSafe(out int total); reader.ReadValueSafe(out int index);
            reader.ReadValueSafe(out int parts); reader.ReadValueSafe(out int count);
            if(version!=CampSnapshotTransfer.Version || id==0 || total<=0 || total>CampSnapshotTransfer.MaxBytes ||
                parts!=(total+CampSnapshotTransfer.PartBytes-1)/CampSnapshotTransfer.PartBytes || index<0 || index>=parts ||
                count!=Math.Min(CampSnapshotTransfer.PartBytes,total-index*CampSnapshotTransfer.PartBytes) ||
                length!=CampSnapshotTransfer.HeaderBytes+count)
                throw new InvalidOperationException("Cabecalho de sincronizacao invalido.");
            var receivedDigest=new byte[CampSnapshotTransfer.DigestBytes];
            reader.ReadBytesSafe(ref receivedDigest,receivedDigest.Length);
            if(id<latest || id==latest && bytes==null) return null;
            if(id>latest)
            {
                if(index!=0 || bytes!=null && !replaceIncomplete) throw new InvalidOperationException("Transferencia incompleta ou fora de ordem.");
                latest=id; bytes=new byte[total]; digest=receivedDigest;
                nextPart=0; started=lastPart=now;
            }
            if(bytes.Length!=total || !SameDigest(digest,receivedDigest))
                throw new InvalidOperationException("Transferencia inconsistente.");
            if(index<nextPart) return null;
            if(index!=nextPart) throw new InvalidOperationException("Parte de sincronizacao ausente.");
            reader.ReadBytesSafe(ref bytes,count,index*CampSnapshotTransfer.PartBytes);
            nextPart++; lastPart=now;
            if(nextPart!=parts) return null;
            try
            {
                using(var hash=SHA256.Create())
                    if(!SameDigest(digest,hash.ComputeHash(bytes))) throw new InvalidOperationException("Integridade de sincronizacao invalida.");
                return new UTF8Encoding(false,true).GetString(bytes);
            }
            finally { Discard(); }
        }
        static bool SameDigest(byte[] a,byte[] b)
        {
            if(a.Length!=b.Length) return false;
            for(int i=0;i<a.Length;i++) if(a[i]!=b[i]) return false;
            return true;
        }
    }

    internal sealed class CampSnapshotSender
    {
        sealed class Transfer
        {
            public ulong Id;
            public byte[] Bytes,Digest;
            public CampSnapshot Snapshot;
            public int Part;
            public int Count => (Bytes.Length+CampSnapshotTransfer.PartBytes-1)/CampSnapshotTransfer.PartBytes;
        }
        sealed class Outbox { public ulong Sequence; public Transfer Current,Next; }
        readonly NetworkManager manager;
        readonly Dictionary<ulong,Outbox> outboxes=new Dictionary<ulong,Outbox>();
        readonly List<ulong> clients=new List<ulong>();
        float nextSend;

        public CampSnapshotSender(NetworkManager manager) { this.manager=manager; }

        public void Enqueue(ulong client,CampSnapshot snapshot)
        {
            if(!manager.IsServer) throw new InvalidOperationException("Somente o servidor envia o acampamento.");
            if(!outboxes.TryGetValue(client,out var box)) outboxes[client]=box=new Outbox();
            // At most one current transfer and one newer complete snapshot per client.
            // Activity updates may replace the newer snapshot, never an unsent ack.
            if(box.Next!=null)
            {
                var previous=box.Next.Snapshot;
                if(!string.IsNullOrEmpty(previous.Ack))
                {
                    if(!string.IsNullOrEmpty(snapshot.Ack) && previous.Ack!=snapshot.Ack)
                        throw new InvalidOperationException("Ha uma confirmacao de acampamento aguardando envio.");
                    if(string.IsNullOrEmpty(snapshot.Ack))
                    { snapshot.Ack=previous.Ack; snapshot.Message=previous.Message; }
                }
                else if(string.IsNullOrEmpty(snapshot.Message)) snapshot.Message=previous.Message;
            }
            string json=JsonUtility.ToJson(snapshot);
            int size=Encoding.UTF8.GetByteCount(json);
            if(size==0 || size>CampSnapshotTransfer.MaxBytes)
                throw new InvalidOperationException("Acampamento excede o limite de sincronizacao; nenhum item foi removido.");
            var bytes=Encoding.UTF8.GetBytes(json);
            byte[] digest;
            using(var hash=SHA256.Create()) digest=hash.ComputeHash(bytes);
            var transfer=new Transfer { Id=checked(++box.Sequence),Bytes=bytes,Digest=digest,Snapshot=snapshot };
            if(box.Current==null) box.Current=transfer;
            else box.Next=transfer;
        }

        public void Remove(ulong client) { outboxes.Remove(client); }
        public void Restart(ulong client)
        { if(outboxes.TryGetValue(client,out var box)) { box.Current=null; box.Next=null; } }
        public void Clear() { outboxes.Clear(); clients.Clear(); nextSend=0; }

        public void Tick(Action<ulong,Exception> failed)
        {
            if(!manager.IsServer || Time.realtimeSinceStartup<nextSend) return;
            nextSend=Time.realtimeSinceStartup+CampSnapshotTransfer.SendInterval;
            clients.Clear(); clients.AddRange(outboxes.Keys);
            foreach(var client in clients)
            {
                if(!outboxes.TryGetValue(client,out var box)) continue;
                if(!manager.ConnectedClients.ContainsKey(client)) { Remove(client); continue; }
                try
                {
                    for(int sent=0;sent<CampSnapshotTransfer.PartsPerTick && box.Current!=null;sent++)
                    {
                        var transfer=box.Current;
                        int offset=transfer.Part*CampSnapshotTransfer.PartBytes;
                        int count=Math.Min(CampSnapshotTransfer.PartBytes,transfer.Bytes.Length-offset);
                        using(var writer=new FastBufferWriter(CampSnapshotTransfer.HeaderBytes+count,Allocator.Temp))
                        {
                            writer.WriteValueSafe(CampSnapshotTransfer.Version);
                            writer.WriteValueSafe(transfer.Id);
                            writer.WriteValueSafe(transfer.Bytes.Length);
                            writer.WriteValueSafe(transfer.Part);
                            writer.WriteValueSafe(transfer.Count);
                            writer.WriteValueSafe(count);
                            writer.WriteBytesSafe(transfer.Digest);
                            writer.WriteBytesSafe(transfer.Bytes,count,offset);
                            manager.CustomMessagingManager.SendNamedMessage(CampSnapshotTransfer.MessageName,client,writer,NetworkDelivery.ReliableFragmentedSequenced);
                        }
                        transfer.Part++;
                        if(transfer.Part==transfer.Count) { box.Current=box.Next; box.Next=null; }
                    }
                }
                catch(Exception error) { Remove(client); failed(client,error); }
            }
        }
    }
}
