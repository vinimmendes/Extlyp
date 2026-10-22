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
    public sealed class CampLeaseLostException : InvalidOperationException
    {
        public CampLeaseLostException() : base("Esta sala nao possui mais a sessao do acampamento. Reconecte para continuar.") { }
    }
    public static partial class CampRepository
    {
        static readonly Dictionary<string,CampRecord> records=new Dictionary<string,CampRecord>();
        sealed class LeaseClaim { public string Owner,Token; }
        static readonly Dictionary<string,LeaseClaim> heldLeases=new Dictionary<string,LeaseClaim>();
        static SharedCampMemory shared;
        public static void EnableShared(string cluster,string directory=null) { Close(); shared=new SharedCampMemory(cluster,directory); }
        public static void Close()
        {
            try { FlushPendingExperience(); }
            catch(Exception error) { UnityEngine.Debug.LogError("Nao foi possivel persistir XP ao encerrar: "+error.Message); }
            ClearPendingExperience(); shared?.Dispose(); shared=null; records.Clear(); heldLeases.Clear();
        }
        static CampRecord Record(Dictionary<string,CampRecord> data,string session)
        { if(!data.TryGetValue(session,out var record)) data[session]=record=new CampRecord { Session=session,Progress=CampProfileMigration.Create() }; return record; }
        static T Transaction<T>(Func<Dictionary<string,CampRecord>,T> operation,bool persist=true)
        {
            var batch=pendingRewards.ToArray();
            if(shared!=null)
            {
                T committed=shared.Transaction(data=> { ApplyPendingExperience(data,batch); return operation(data); },persist || batch.Length>0);
                CompletePendingExperience(batch); return committed;
            }
            var copy=new Dictionary<string,CampRecord>();
            foreach(var pair in records) copy[pair.Key]=UnityEngine.JsonUtility.FromJson<CampRecord>(UnityEngine.JsonUtility.ToJson(pair.Value));
            foreach(var record in copy.Values) CampProfileMigration.Upgrade(record.Progress);
            ApplyPendingExperience(copy,batch);
            T result=operation(copy); CampProfileMigration.ValidateOwnership(copy.Values);
            records.Clear(); foreach(var pair in copy) records[pair.Key]=pair.Value;
            CompletePendingExperience(batch); return result;
        }
        // Called inside the repository transaction (and shared-store mutex), never before it.
        static CampRecord OwnedRecord(Dictionary<string,CampRecord> data,string session,string expectedOwner=null)
        {
            if(string.IsNullOrEmpty(session) || !data.TryGetValue(session,out var record) ||
                !heldLeases.TryGetValue(session,out var claim) || record.Lease!=claim.Owner ||
                string.IsNullOrEmpty(claim.Token) || record.LeaseToken!=claim.Token || record.LeaseExpires<=DateTime.UtcNow.Ticks ||
                expectedOwner!=null && claim.Owner!=expectedOwner)
                throw new CampLeaseLostException();
            return record;
        }
        static bool MatchesLease(CampRecord record,string session,string owner) =>
            !string.IsNullOrEmpty(owner) && heldLeases.TryGetValue(session,out var claim) && claim.Owner==owner &&
            record.Lease==owner && !string.IsNullOrEmpty(claim.Token) && record.LeaseToken==claim.Token && record.LeaseExpires>DateTime.UtcNow.Ticks;
        public static T Edit<T>(string session,Func<CampProgress,T> operation,string expectedOwner=null)=>Transaction(data=>operation(OwnedRecord(data,session,expectedOwner).Progress));
        public static CampProgress Get(string session)=>Transaction(data=>Record(data,session).Progress.Copy(),false);
        internal static CampProgress GetOwned(string session,string owner)=>Transaction(data=>
        {
            if(string.IsNullOrEmpty(owner)) throw new CampLeaseLostException();
            return OwnedRecord(data,session,owner).Progress.Copy();
        },false);
        internal static void Save(string session,CampProgress profile,long expectedRevision,string expectedOwner=null)=>Transaction(data=>
        {
            var record=OwnedRecord(data,session,expectedOwner);
            if(record.Progress.Revision!=expectedRevision || profile.Revision<=expectedRevision)
                throw new InvalidOperationException("O acampamento mudou. Sincronize antes de salvar a atividade.");
            CampProfileMigration.ValidateStructure(profile);
            var saved=profile.Copy(); CampProfileMigration.Upgrade(saved); record.Progress=saved; return 0;
        });
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
        public static bool Claim(string session,string owner)
        {
            if(string.IsNullOrEmpty(session) || string.IsNullOrEmpty(owner)) return false;
            LeaseClaim acquired=null;
            bool success=Transaction(data=>
            {
                var record=Record(data,session); long now=DateTime.UtcNow.Ticks;
                if(!string.IsNullOrEmpty(record.Lease) && record.LeaseExpires>now)
                {
                    if(!MatchesLease(record,session,owner)) return false;
                    acquired=heldLeases[session];
                }
                else
                {
                    if(record.Progress.ExpeditionState!=0) Recover(record.Progress);
                    acquired=new LeaseClaim { Owner=owner,Token=Guid.NewGuid().ToString("N") };
                    // Invalidate snapshots obtained before this acquisition, even in camp.
                    record.Progress.Revision++;
                }
                record.Lease=owner; record.LeaseToken=acquired.Token;
                record.LeaseExpires=now+TimeSpan.FromSeconds(90).Ticks; return true;
            });
            // A failed durable transaction must not grant local authority.
            if(success) heldLeases[session]=acquired;
            return success;
        }
        public static bool Refresh(string session,string owner)=>Transaction(data=>
        {
            if(string.IsNullOrEmpty(session) || !data.TryGetValue(session,out var record) || !MatchesLease(record,session,owner)) return false;
            record.LeaseExpires=DateTime.UtcNow.Ticks+TimeSpan.FromSeconds(90).Ticks; return true;
        },false);
        public static bool Release(string session,string owner)
        {
            bool released=Transaction(data=>
            {
                if(string.IsNullOrEmpty(session) || !data.TryGetValue(session,out var record) || !MatchesLease(record,session,owner)) return false;
                record.Lease=null; record.LeaseToken=null; record.LeaseExpires=0; return true;
            },false);
            if(!string.IsNullOrEmpty(session) && heldLeases.TryGetValue(session,out var claim) && claim.Owner==owner) heldLeases.Remove(session);
            return released;
        }
        // Settle only the departing connection's acquisition, under the same mutex as release.
        public static bool EndConnection(string session,string owner,bool settleExpedition)
        {
            try
            {
                return Transaction(data=>
                {
                    if(string.IsNullOrEmpty(session) || !data.TryGetValue(session,out var record) || !MatchesLease(record,session,owner)) return false;
                    var p=record.Progress;
                    if(settleExpedition && p.ExpeditionState==2)
                    { ConfirmExperience(p,false); Clear(p); p.Revision++; p.Sync(); }
                    else if(settleExpedition && p.ExpeditionState==1) Recover(p,true);
                    record.Lease=null; record.LeaseToken=null; record.LeaseExpires=0;
                    return true;
                });
            }
            finally
            {
                // Do not erase a newer local connection's credential during a late callback.
                if(!string.IsNullOrEmpty(session) && heldLeases.TryGetValue(session,out var claim) && claim.Owner==owner) heldLeases.Remove(session);
            }
        }
        public static void Apply(CampJoinRequest request) { } // Login never executes economic commands.
        public static InventoryGrid Begin(string session,string expedition,out int food)
        { return Begin(session,expedition,out food,out _); }
        public static InventoryGrid Begin(string session,string expedition,out int food,out EquipmentLoadout loadout)
        {
            int buff=0;
            EquipmentLoadout reserved=null;
            var equipment=Edit(session,p=>
            {
                p.Ensure(); if(p.ExpeditionState!=0) throw new InvalidOperationException("Expedicao anterior ativa.");
                p.PrepareRecoveryEquipment();
                p.Equipment.FillWeapons(p.Pack);
                p.Escrow=p.Pack.Copy(); p.EscrowEquipment=p.Equipment.Copy(); reserved=p.Equipment.Copy();
                p.EscrowFood=p.ConsumeFoodBuff(); buff=p.EscrowFood;
                p.ExpeditionExperience=0; p.ExpeditionExplorationExperience=p.ExpeditionPvpExperience=0; p.ExpeditionRewardKeys.Clear();
                p.ExpeditionId=expedition; p.ExpeditionState=1; p.Pack=new InventoryGrid(ItemCatalog.BackpackSlots); p.Equipment=new EquipmentLoadout(); p.Revision++; p.Sync(); return p.Escrow.Copy();
            }); food=buff; loadout=reserved; return equipment;
        }
        public static void Commit(string session,string expedition)=>Edit(session,p=>
        { if(p.ExpeditionId!=expedition || (p.ExpeditionState!=1 && p.ExpeditionState!=2)) throw new InvalidOperationException("Reserva invalida."); p.ExpeditionState=2; return 0; });
        public static void Rollback(string session,string expedition)=>Edit(session,p=>
        { if(p.ExpeditionId==expedition && p.ExpeditionState!=0) Recover(p,true); return 0; });
        internal static void Recover(CampProgress p,bool rollback=false)
        {
            if(p.ExpeditionState==2 && !rollback) { ConfirmExperience(p,false); Clear(p); p.Revision++; p.Sync(); return; }
            if(p.Escrow!=null) p.Pack=p.Escrow.Copy(); if(p.EscrowEquipment!=null) p.Equipment=p.EscrowEquipment.Copy();
            if(p.EscrowFood>0) { p.WellFed=true; p.FoodHealth=p.EscrowFood; }
            if(rollback && p.ExpeditionState==2) p.ExpeditionState=1;
            Clear(p); p.Revision++; p.Sync();
        }
        static void Clear(CampProgress p) { if(p.ExpeditionState==2) p.RestockMerchant(p.ExpeditionId); p.ExpeditionState=0; p.ExpeditionId=null; p.Escrow=null; p.EscrowEquipment=null; p.EscrowFood=0; p.ExpeditionExperience=0; p.ExpeditionExplorationExperience=p.ExpeditionPvpExperience=0; p.ExpeditionRewardKeys.Clear(); p.DeliverLegacyItems(); }
        public static bool Bank(string session,string expedition,InventoryGrid returned)=>Bank(session,expedition,returned,new EquipmentLoadout());
        public static bool Bank(string session,string expedition,InventoryGrid returned,EquipmentLoadout loadout)=>Edit(session,p=>
        {
            if(p.ExpeditionState!=2 || p.ExpeditionId!=expedition) return false;
            p.Pack=returned.Copy(); p.Equipment=loadout.Copy();
            for(int i=0;i<p.Pack.Slots.Length;i++) p.Pack.Slots[i]=ItemCatalog.ExpeditionOnly(p.Pack.Slots[i].Item) ? default : RecoveryRules.Extracted(p.Pack.Slots[i]);
            for(int i=0;i<p.Equipment.Slots.Length;i++) p.Equipment.Slots[i]=RecoveryRules.Extracted(p.Equipment.Slots[i]);
            ConfirmExperience(p,true); Clear(p); p.Revision++; p.Sync(); return true;
        });
        public static void Lose(string session,string expedition)=>Edit(session,p=>
        { if(p.ExpeditionId==expedition && p.ExpeditionState!=0) { if(p.ExpeditionState==2) ConfirmExperience(p,false); Clear(p); p.Revision++; p.Sync(); } return 0; });
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
        internal static void ApplySnapshot(CampProgress profile,Action applyEnvelope)
        {
            Progress=profile; Synchronized=true; applyEnvelope();
            // Observers see the profile, activity and acknowledgement together.
            // A UI listener failure cannot invalidate a successfully applied transfer.
            if(Changed==null) return;
            foreach(Action listener in Changed.GetInvocationList())
                try { listener(); } catch(Exception error) { UnityEngine.Debug.LogException(error); }
        }
        public static void Disconnected() { Synchronized=false; Pending=false; }
    }
}
