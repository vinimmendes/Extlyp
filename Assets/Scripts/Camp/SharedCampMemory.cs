using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System.IO.MemoryMappedFiles;
using System.Threading;
#endif

namespace CyberExtraction.Camp
{
    [Serializable]
    public class CampRecord
    {
        public string Session, Lease, PublicKey;
        public long LeaseExpires, LastLoginTicks;
        public CampProgress Progress = new CampProgress();
    }

    [Serializable]
    class CampRecords { public int Version = 2; public CampRecord[] Records; }

    // RAM compartilhada entre as salas Windows. Nada e salvo em banco/arquivo.
    // Dois buffers preservam o ultimo estado completo se uma sala cair durante a escrita.
    public sealed class SharedCampMemory : IDisposable
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        const int SlotSize = 2 * 1024 * 1024;
        readonly MemoryMappedFile memory;
        readonly MemoryMappedViewAccessor view;
        readonly Mutex mutex;
        readonly string savePath;

        public SharedCampMemory(string cluster, string directory = null)
        {
            if (string.IsNullOrEmpty(cluster) || cluster.Length > 64) throw new ArgumentException("Cluster invalido.");
            foreach (char c in cluster)
                if (!(c >= 'a' && c <= 'z') && !(c >= 'A' && c <= 'Z') && !(c >= '0' && c <= '9') && c != '-' && c != '_')
                    throw new ArgumentException("Cluster invalido.");
            string name = "Local\\CyberExtractionCamp_" + cluster;
            string folder = directory ?? System.IO.Path.Combine(Application.persistentDataPath,"Camps");
            System.IO.Directory.CreateDirectory(folder);
            savePath=System.IO.Path.Combine(folder,cluster+".json");
            mutex = new Mutex(false, name + "_lock");
            memory = MemoryMappedFile.CreateOrOpen(name, 16 + SlotSize * 2, MemoryMappedFileAccess.ReadWrite);
            view = memory.CreateViewAccessor();
        }

        public T Transaction<T>(Func<Dictionary<string, CampRecord>, T> operation, bool persist = true)
        {
            bool acquired = false;
            try
            {
                try { acquired = mutex.WaitOne(3000); }
                catch (AbandonedMutexException) { acquired = true; }
                if (!acquired) throw new TimeoutException("Acampamento ocupado. Tente novamente.");
                int active = view.ReadInt32(0);
                if (active != 0 && active != 1) throw new InvalidOperationException("Estado compartilhado invalido.");
                long offset = 16 + active * SlotSize;
                int length = view.ReadInt32(offset);
                var records = new Dictionary<string, CampRecord>();
                if (length < 0 || length > SlotSize - 4) throw new InvalidOperationException("Acampamento compartilhado invalido.");
                if (length > 0)
                {
                    var bytes = new byte[length];
                    view.ReadArray(offset + 4, bytes, 0, length);
                    var data = JsonUtility.FromJson<CampRecords>(Encoding.UTF8.GetString(bytes));
                    if (data == null || data.Records == null) throw new InvalidOperationException("Acampamento compartilhado invalido.");
                    foreach (var record in data.Records) records.Add(record.Session, record);
                }
                else if (System.IO.File.Exists(savePath))
                {
                    CampRecords saved;
                    try { saved=JsonUtility.FromJson<CampRecords>(System.IO.File.ReadAllText(savePath)); }
                    catch(Exception) { if(!System.IO.File.Exists(savePath+".bak")) throw; saved=JsonUtility.FromJson<CampRecords>(System.IO.File.ReadAllText(savePath+".bak")); }
                    if(saved==null || saved.Records==null || saved.Version!=2) throw new InvalidOperationException("Arquivo de progresso invalido.");
                    foreach(var record in saved.Records)
                    {
                        record.Lease=null; record.LeaseExpires=0; record.Progress.Ensure();
                        if(record.Progress.ExpeditionState!=0) CampRepository.Recover(record.Progress);
                        records.Add(record.Session,record);
                    }
                    persist=true;
                }
                T result = operation(records);
                var output = new CampRecord[records.Count]; records.Values.CopyTo(output, 0);
                var encoded = Encoding.UTF8.GetBytes(JsonUtility.ToJson(new CampRecords { Records = output }));
                if (encoded.Length > SlotSize - 4) throw new InvalidOperationException("Limite de acampamentos desta sessao atingido.");
                // Durable write precedes the shared-memory commit; failure leaves the previous state intact.
                if(persist)
                {
                    string pending=savePath+"."+Guid.NewGuid().ToString("N")+".tmp";
                    try
                    {
                        using(var stream=new System.IO.FileStream(pending,System.IO.FileMode.CreateNew,System.IO.FileAccess.Write,System.IO.FileShare.None)) { stream.Write(encoded,0,encoded.Length); stream.Flush(true); }
                        if(System.IO.File.Exists(savePath)) System.IO.File.Replace(pending,savePath,savePath+".bak");
                        else System.IO.File.Move(pending,savePath);
                    }
                    finally { if(System.IO.File.Exists(pending)) System.IO.File.Delete(pending); }
                }
                int next = 1 - active;
                long nextOffset = 16 + next * SlotSize;
                view.WriteArray(nextOffset + 4, encoded, 0, encoded.Length);
                view.Write(nextOffset, encoded.Length);
                Thread.MemoryBarrier();
                view.Write(0, next);
                return result;
            }
            finally { if (acquired) mutex.ReleaseMutex(); }
        }
        public void Dispose() { view.Dispose(); memory.Dispose(); mutex.Dispose(); }
#else
        public SharedCampMemory(string cluster, string directory = null) { throw new PlatformNotSupportedException("As salas dedicadas usam Windows."); }
        public T Transaction<T>(Func<Dictionary<string, CampRecord>, T> operation, bool persist = true) { throw new PlatformNotSupportedException(); }
        public void Dispose() { }
#endif
    }
}
