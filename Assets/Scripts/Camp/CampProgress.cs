using System;
using System.Collections.Generic;
using CyberExtraction.Items;
namespace CyberExtraction.Camp
{
    [Serializable] public class CampJoinRequest
    {
        public string Session, PublicKey, Signature, Nonce;
        public long IssuedTicks;
        public bool CampOnly, RepairPier, Fish;
        public bool CampSandbox;
        public int BringBandages;
        public byte Map;
        public bool QuickTest;
        public int QuickBots=1;
        public CampAction[] Actions;
    }
    public static class CampRepository
    {
        static readonly Dictionary<string,CampRecord> records=new Dictionary<string,CampRecord>();
        static SharedCampMemory shared;
        public static void EnableShared(string cluster,string directory=null) { shared?.Dispose(); shared=new SharedCampMemory(cluster,directory); }
        public static void Close() { shared?.Dispose(); shared=null; records.Clear(); }
        static CampRecord Record(Dictionary<string,CampRecord> data,string session)
        { if(!data.TryGetValue(session,out var record)) data[session]=record=new CampRecord { Session=session }; return record; }
        static T Transaction<T>(Func<Dictionary<string,CampRecord>,T> operation,bool persist=true)
        {
            if(shared!=null) return shared.Transaction(operation,persist);
            var copy=new Dictionary<string,CampRecord>();
            foreach(var pair in records) copy[pair.Key]=UnityEngine.JsonUtility.FromJson<CampRecord>(UnityEngine.JsonUtility.ToJson(pair.Value));
            T result=operation(copy); records.Clear(); foreach(var pair in copy) records[pair.Key]=pair.Value; return result;
        }
        public static T Edit<T>(string session,Func<CampProgress,T> operation)=>Transaction(data=>operation(Record(data,session).Progress));
        public static CampProgress Get(string session)=>Transaction(data=>Record(data,session).Progress.Copy(),false);
        internal static void Save(string session,CampProgress profile)=>Transaction(data=> { Record(data,session).Progress=profile.Copy(); return 0; });
        public static bool Authenticate(CampJoinRequest join)
        {
            if(!CampIdentity.Verify(join,DateTime.UtcNow.Ticks)) return false;
            return Transaction(data=>
            {
                var record=Record(data,join.Session);
                if(record.LastLoginTicks>=join.IssuedTicks || (!string.IsNullOrEmpty(record.PublicKey) && record.PublicKey!=join.PublicKey)) return false;
                record.PublicKey=join.PublicKey; record.LastLoginTicks=join.IssuedTicks; return true;
            });
        }
        public static bool Claim(string session,string owner)=>Transaction(data=>
        {
            var record=Record(data,session); long now=DateTime.UtcNow.Ticks;
            if(!string.IsNullOrEmpty(record.Lease) && record.Lease!=owner && record.LeaseExpires>now) return false;
            if(record.Lease!=owner && record.Progress.ExpeditionState!=0) Recover(record.Progress);
            record.Lease=owner; record.LeaseExpires=now+TimeSpan.FromSeconds(90).Ticks; return true;
        });
        public static void Refresh(string session,string owner)=>Transaction(data=>
        { var record=Record(data,session); if(record.Lease==owner) record.LeaseExpires=DateTime.UtcNow.Ticks+TimeSpan.FromSeconds(90).Ticks; return 0; },false);
        public static void Release(string session,string owner)=>Transaction(data=>
        { var record=Record(data,session); if(record.Lease==owner) { record.Lease=null; record.LeaseExpires=0; } return 0; },false);
        public static void Apply(CampJoinRequest request) { } // Login never executes economic commands.
        public static InventoryGrid Begin(string session,string expedition,out int food)
        {
            int buff=0;
            var equipment=Edit(session,p=>
            {
                p.Ensure(); if(p.ExpeditionState!=0) throw new InvalidOperationException("Expedicao anterior ativa.");
                p.PrepareRecoveryEquipment();
                p.Escrow=p.Pack.Copy(); p.EscrowFood=p.ConsumeFoodBuff(); buff=p.EscrowFood;
                p.ExpeditionId=expedition; p.ExpeditionState=1; p.Pack=new InventoryGrid(); p.Revision++; p.Sync(); return p.Escrow.Copy();
            }); food=buff; return equipment;
        }
        public static void Commit(string session,string expedition)=>Edit(session,p=>
        { if(p.ExpeditionId!=expedition || p.ExpeditionState!=1) throw new InvalidOperationException("Reserva invalida."); p.ExpeditionState=2; return 0; });
        public static void Rollback(string session,string expedition)=>Edit(session,p=>
        { if(p.ExpeditionId==expedition && p.ExpeditionState!=0) Recover(p); return 0; });
        internal static void Recover(CampProgress p)
        { if(p.Escrow!=null) p.Pack=p.Escrow.Copy(); if(p.EscrowFood>0) { p.WellFed=true; p.FoodHealth=p.EscrowFood; } Clear(p); p.Revision++; p.Sync(); }
        static void Clear(CampProgress p) { p.ExpeditionState=0; p.ExpeditionId=null; p.Escrow=null; p.EscrowFood=0; }
        public static bool Bank(string session,string expedition,InventoryGrid returned)=>Edit(session,p=>
        { if(p.ExpeditionState!=2 || p.ExpeditionId!=expedition) return false; p.Pack=returned.Copy(); Clear(p); p.Revision++; p.Sync(); return true; });
        public static void Lose(string session,string expedition)=>Edit(session,p=>
        { if(p.ExpeditionId==expedition && p.ExpeditionState!=0) { Clear(p); p.Revision++; p.Sync(); } return 0; });
    }
    public static class CampSession
    {
        public static string Identity=>CampIdentity.Session;
        public static CampProgress Progress=new CampProgress();
        public static bool Synchronized, Pending;
        public static string Message="Conecte ao servidor para carregar seu acampamento.";
        public static event Action Changed;
        public static bool Act(CampAction action)
        {
            if(!Synchronized || Pending || action==null) { Message="Aguarde a sincronizacao do acampamento."; return false; }
            action.Sequence=Progress.Revision+1;
            return UI.NetworkBootstrap.Instance!=null && UI.NetworkBootstrap.Instance.SendCamp(new CampCommand { Kind=CampCommandKind.Action,Action=action });
        }
        public static CampJoinRequest Request()=>new CampJoinRequest { Session=Identity };
        public static void Accept(string json)
        { var p=UnityEngine.JsonUtility.FromJson<CampProgress>(json); if(p==null) return; p.Ensure(); Progress=p; Synchronized=true; Changed?.Invoke(); }
        public static void Disconnected() { Synchronized=false; Pending=false; }
    }
}
