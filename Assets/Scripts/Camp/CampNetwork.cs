using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using CyberExtraction.UI;

namespace CyberExtraction.Camp
{
    public sealed class CampNetwork
    {
        const string CommandName="ce-camp-command";
        readonly NetworkManager manager;
        readonly NetworkBootstrap bootstrap;
        readonly CampSnapshotSender snapshotSender;
        readonly CampSnapshotReceiver snapshotReceiver=new CampSnapshotReceiver();
        float waitingSince=-1;
        bool recovering,clientActive;
        readonly Dictionary<ulong,CampActivity> activities=new Dictionary<ulong,CampActivity>();
        readonly Dictionary<ulong,CampProgress> testProfiles=new Dictionary<ulong,CampProgress>();
        bool IsTest(ulong client) { var join=bootstrap.RequestFor(client); return join!=null && join.CampOnly && join.CampSandbox && CyberExtraction.Core.GameConfig.AllowQuickTest; }
        CampProgress Profile(ulong client)
        {
            if(!IsTest(client)) return CampRepository.GetOwned(bootstrap.RequestFor(client).Session,bootstrap.LeaseOwner(client));
            if(!testProfiles.TryGetValue(client,out var profile))
            {
                profile=CampTestResources.Create();
                profile.Merchant=CampRepository.GetTestMerchant(bootstrap.RequestFor(client).Session,bootstrap.LeaseOwner(client));
                testProfiles[client]=profile;
            }
            return profile;
        }
        void Save(ulong client,CampProgress profile,long expectedRevision)
        { if(IsTest(client)) { CampRepository.SaveTestMerchant(bootstrap.RequestFor(client).Session,bootstrap.LeaseOwner(client),profile.Merchant); testProfiles[client]=profile; } else CampRepository.Save(bootstrap.RequestFor(client).Session,profile,expectedRevision,bootstrap.LeaseOwner(client)); }
        sealed class InputBudget { public float At, Tokens=8; }
        readonly Dictionary<ulong,InputBudget> nextInput=new Dictionary<ulong,InputBudget>();
        string pendingId;
        readonly List<ulong> clients=new List<ulong>();
        float nextTick, lastTick;
        public CampSnapshot Snapshot { get; private set; }=new CampSnapshot();
        public CampNetwork(NetworkManager manager,NetworkBootstrap bootstrap)
        { this.manager=manager; this.bootstrap=bootstrap; snapshotSender=new CampSnapshotSender(manager); }
        public void Register()
        {
            snapshotSender.Clear();
            ResetClient(); clientActive=false;
            manager.CustomMessagingManager.RegisterNamedMessageHandler(CommandName,ReceiveCommand);
            manager.CustomMessagingManager.RegisterNamedMessageHandler(CampSnapshotTransfer.MessageName,ReceiveSnapshot);
            lastTick=Time.realtimeSinceStartup;
        }
        static void Send(NetworkManager manager,string name,ulong target,string json)
        {
            using(var writer=new FastBufferWriter(json.Length*2+8,Allocator.Temp))
            { writer.WriteValueSafe(json); manager.CustomMessagingManager.SendNamedMessage(name,target,writer,NetworkDelivery.ReliableFragmentedSequenced); }
        }
        public bool Request(CampCommand command)
        {
            if(command==null || recovering || !bootstrap.CampConnection || !manager.IsConnectedClient ||
                (command.Kind!=CampCommandKind.Sync && !CampSession.Synchronized) || (command.Kind!=CampCommandKind.Reel && CampSession.Pending)) return false;
            if(command.Kind!=CampCommandKind.Reel) { CampSession.Pending=true; pendingId=command.Id; waitingSince=Time.realtimeSinceStartup; }
            try { Send(manager,CommandName,NetworkManager.ServerClientId,JsonUtility.ToJson(command)); return true; }
            catch(Exception error) { FailClient(error); return false; }
        }
        public void Sync(ulong client,string ack=null,string message=null)
        {
            var join=bootstrap.RequestFor(client); if(join==null || !join.CampOnly) return;
            try
            {
                if(!activities.TryGetValue(client,out var activity)) activities[client]=activity=new CampActivity();
                var snapshot=new CampSnapshot { Profile=JsonUtility.ToJson(Profile(client)),Ack=ack,Message=message,TestMode=IsTest(client),
                    Activity=activity.Phase,Side=activity.Side,Tension=activity.Tension,Progress=activity.Progress,HeatA=activity.HeatA,HeatB=activity.HeatB,
                    Fish=activity.Fish,Zone=activity.Zone,Nibble=activity.Nibble,CookMarker=activity.CookMarker,BarVelocity=activity.BarVelocity,CookStartedAt=activity.CookStartedAt };
                snapshotSender.Enqueue(client,snapshot);
            }
            catch(CampLeaseLostException) { bootstrap.DisconnectCampSession(client,"SESSION_LOST|Esta sala perdeu a sessao do acampamento. Conecte novamente."); }
            catch(Exception error)
            {
                Debug.LogError("Falha ao sincronizar acampamento: "+error);
                bootstrap.DisconnectCampSession(client,"SERVER_ERROR|Nao foi possivel carregar o acampamento. Conecte novamente.");
            }
        }
        void ReceiveSnapshot(ulong sender,FastBufferReader reader)
        {
            if(!manager.IsClient || !manager.IsConnectedClient || !bootstrap.CampConnection || sender!=NetworkManager.ServerClientId) return;
            try
            {
                string json=snapshotReceiver.Receive(reader,Time.realtimeSinceStartup,recovering);
                if(json==null) return;
                var snapshot=JsonUtility.FromJson<CampSnapshot>(json);
                if(snapshot==null || string.IsNullOrEmpty(snapshot.Profile)) throw new InvalidOperationException("Snapshot sem perfil completo.");
                if(recovering && snapshot.Ack!=pendingId) return;
                var profile=JsonUtility.FromJson<CampProgress>(snapshot.Profile);
                if(profile==null || profile.SchemaVersion!=CampProfileMigration.CurrentVersion)
                    throw new InvalidOperationException("Versao de perfil incompativel.");
                CampProfileMigration.ValidateOwnership(new[] { new CampRecord { Progress=profile } });
                if(CampSession.Synchronized && profile.Revision<CampSession.Progress.Revision)
                    throw new InvalidOperationException("Revisao de acampamento antiga.");
                profile.Ensure();
                CampSession.ApplySnapshot(profile,()=>
                {
                    Snapshot=snapshot;
                    if(pendingId!=null && snapshot.Ack==pendingId) { CampSession.Pending=false; pendingId=null; }
                    if(snapshot.Message!=null) CampSession.Message=snapshot.Message;
                    if(!CampSession.Pending) { waitingSince=-1; recovering=false; }
                });
            }
            catch(Exception error) { FailClient(error); }
        }
        void ResetClient()
        { snapshotReceiver.Reset(); pendingId=null; waitingSince=-1; recovering=false; Snapshot=new CampSnapshot(); CampSession.Disconnected(); }
        void FailClient(Exception error)
        {
            Debug.LogError("Falha ao receber acampamento: "+error);
            ResetClient(); manager.Shutdown();
            CampSession.Message="Nao foi possivel sincronizar o acampamento. Reconecte para carregar o estado salvo.";
        }
        void TickClient()
        {
            if(!bootstrap.CampConnection || !manager.IsConnectedClient)
            { if(clientActive) ResetClient(); clientActive=false; return; }
            clientActive=true;
            float now=Time.realtimeSinceStartup;
            bool waiting=!CampSession.Synchronized || CampSession.Pending;
            if(waiting && waitingSince<0) waitingSince=now;
            if(!snapshotReceiver.TimedOut(now) && (!waiting || now-waitingSince<CampSnapshotTransfer.TransferTimeout)) return;
            if(recovering) { FailClient(new TimeoutException("Tempo de sincronizacao excedido apos nova tentativa.")); return; }
            // Only request authoritative state again. Never replay the original operation.
            recovering=true; snapshotReceiver.Discard(); CampSession.Synchronized=false; CampSession.Pending=true;
            var command=new CampCommand { Kind=CampCommandKind.Sync };
            pendingId=command.Id; waitingSince=now;
            CampSession.Message="Recuperando a sincronizacao do acampamento. Aguarde.";
            try { Send(manager,CommandName,NetworkManager.ServerClientId,JsonUtility.ToJson(command)); }
            catch(Exception error) { FailClient(error); }
        }
        void ReceiveCommand(ulong client,FastBufferReader reader)
        {
            if(!manager.IsServer || reader.Length>8192 || !bootstrap.IsCampClient(client)) return;
            float now=Time.realtimeSinceStartup;
            string commandId=null;
            try
            {
                reader.ReadValueSafe(out string json); var command=JsonUtility.FromJson<CampCommand>(json);
                if(command==null || !Guid.TryParseExact(command.Id,"N",out _) || !Enum.IsDefined(typeof(CampCommandKind),command.Kind)) return;
                commandId=command.Id;
                if(!nextInput.TryGetValue(client,out var budget)) nextInput[client]=budget=new InputBudget { At=now };
                budget.Tokens=Mathf.Min(8,budget.Tokens+(now-budget.At)*20); budget.At=now;
                if(budget.Tokens<1)
                { if(command.Kind!=CampCommandKind.Reel) Sync(client,command.Id,"Aguarde um instante e tente novamente."); return; }
                budget.Tokens--;
                var join=bootstrap.RequestFor(client);
                if(!activities.TryGetValue(client,out var a)) activities[client]=a=new CampActivity();
                var profile=Profile(client); long revision=profile.Revision; bool success=false;
                string completionMessage=null;
                switch(command.Kind)
                {
                    case CampCommandKind.Sync: snapshotSender.Restart(client); success=true; break;
                    case CampCommandKind.Action:
                        if(!a.Busy)
                        {
                            if(IsTest(client)) { var copy=profile.Copy(); success=copy.Apply(command.Action); if(success) Save(client,copy,revision); }
                            else success=CampRepository.Edit(join.Session,p=>p.Apply(command.Action),bootstrap.LeaseOwner(client));
                        }
                        if(success) completionMessage=command.Action?.TradeFeedback;
                        else if(command.Action?.Kind==CampActionKind.Organize) completionMessage="Inventário ou estado mudou. Tente organizar novamente.";
                        break;
                    case CampCommandKind.BeginFishing: success=a.BeginFishing(profile,now); completionMessage=a.FishingRejection; break;
                    case CampCommandKind.Reel: a.Reel(command.Held,now); return;
                    case CampCommandKind.BeginCooking: success=a.BeginCooking(profile,command.Slot,now,manager.ServerTime.Time); completionMessage=a.CookingRejection; break;
                    case CampCommandKind.Flip: success=a.Flip(); break;
                    case CampCommandKind.FinishCooking:
                        double serverNow=manager.ServerTime.Time;
                        if(!double.IsNaN(command.CookingPressedAt) && !double.IsInfinity(command.CookingPressedAt) &&
                            command.CookingPressedAt>=serverNow-.35 && command.CookingPressedAt<=serverNow+.05)
                            success=a.FinishCooking(profile,now,(float)(command.CookingPressedAt-a.CookStartedAt),serverNow);
                        completionMessage= a.CookingRejection ?? "O clique chegou fora da janela de sincronizacao. Tente novamente.";
                        if(success)
                        {
                            Save(client,profile,revision);
                            completionMessage=a.LastCookingQuality==3 ? "PERFEITO! Peixe preparado com qualidade maxima." :
                                a.LastCookingQuality==2 ? "BOM! Peixe cozido." : a.LastCookingQuality==0 ? "Errou a faixa: o peixe ficou cru." : "Errou a faixa: o peixe queimou.";
                            completionMessage+=" Guardado em: "+a.LastCookingStorage+".";
                        }
                        break;
                    case CampCommandKind.Cancel: a.Cancel(); success=true; break;
                    case CampCommandKind.TestSupply:
                        if(IsTest(client)) { a.Cancel(); CampTestResources.Supply(profile); success=true; } break;
                    case CampCommandKind.TestReset:
                        if(IsTest(client)) { a.Cancel(); var reset=CampTestResources.Create(); reset.Revision=profile.Revision+1; Save(client,reset,profile.Revision); success=true; } break;
                }
                Sync(client,command.Id,success ? completionMessage ?? string.Empty : completionMessage ?? "Acao recusada: verifique recursos, espaco e estado da atividade.");
            }
            catch(CampLeaseLostException) { bootstrap.DisconnectCampSession(client,"SESSION_LOST|Esta sala perdeu a sessao do acampamento. Conecte novamente."); }
            catch(Exception error) { Debug.LogError("Comando de acampamento: "+error.Message); Sync(client,commandId,"Nao foi possivel salvar. Tente novamente."); }
        }
        public void Remove(ulong client) { if(activities.TryGetValue(client,out var activity)) activity.Cancel(); activities.Remove(client); nextInput.Remove(client); testProfiles.Remove(client); snapshotSender.Remove(client); if(client==manager.LocalClientId) ResetClient(); }
        public void Tick()
        {
            TickClient();
            if(!manager.IsServer) return;
            snapshotSender.Tick((client,error)=>
            {
                Debug.LogError("Falha ao enviar acampamento: "+error);
                bootstrap.DisconnectCampSession(client,"SERVER_ERROR|Nao foi possivel sincronizar o acampamento. Conecte novamente.");
            });
            float now=Time.realtimeSinceStartup,dt=Mathf.Min(.1f,now-lastTick); lastTick=now;
            bool publish=now>=nextTick;
            if(publish) nextTick=now+.1f;
            clients.Clear(); clients.AddRange(activities.Keys);
            foreach(var client in clients)
            {
                if(!bootstrap.IsCampClient(client)) { Remove(client); continue; }
                if(!activities.TryGetValue(client,out var a) || !a.Busy) continue;
                int previousPhase=a.Phase;
                a.AdvanceFishing(now,dt);
                if(!publish && previousPhase==a.Phase) continue;
                var join=bootstrap.RequestFor(client);
                try
                {
                    var p=Profile(client); long revision=p.Revision;
                    string result=a.Tick(p,now,0,manager.ServerTime.Time);
                    if(p.Revision!=revision) Save(client,p,revision);
                    if(publish || previousPhase!=a.Phase || result!=null) Sync(client,null,result);
                }
                catch(CampLeaseLostException) { bootstrap.DisconnectCampSession(client,"SESSION_LOST|Esta sala perdeu a sessao do acampamento. Conecte novamente."); }
                catch(Exception error) { a.Cancel(); Debug.LogError("Atividade do acampamento: "+error.Message); Sync(client,null,"Falha ao salvar atividade."); }
            }
        }
    }
}
