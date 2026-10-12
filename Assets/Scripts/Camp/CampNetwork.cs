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
        const string CommandName="ce-camp-command", SnapshotName="ce-camp-snapshot";
        readonly NetworkManager manager;
        readonly NetworkBootstrap bootstrap;
        readonly Dictionary<ulong,CampActivity> activities=new Dictionary<ulong,CampActivity>();
        readonly Dictionary<ulong,CampProgress> testProfiles=new Dictionary<ulong,CampProgress>();
        bool IsTest(ulong client) { var join=bootstrap.RequestFor(client); return join!=null && join.CampOnly && join.CampSandbox && CyberExtraction.Core.GameConfig.AllowQuickTest; }
        CampProgress Profile(ulong client)
        {
            if(!IsTest(client)) return CampRepository.Get(bootstrap.RequestFor(client).Session);
            if(!testProfiles.TryGetValue(client,out var profile)) testProfiles[client]=profile=CampTestResources.Create();
            return profile;
        }
        void Save(ulong client,CampProgress profile)
        { if(IsTest(client)) testProfiles[client]=profile; else CampRepository.Save(bootstrap.RequestFor(client).Session,profile); }
        sealed class InputBudget { public float At, Tokens=8; }
        readonly Dictionary<ulong,InputBudget> nextInput=new Dictionary<ulong,InputBudget>();
        string pendingId;
        readonly List<ulong> clients=new List<ulong>();
        float nextTick, lastTick;
        public CampSnapshot Snapshot { get; private set; }=new CampSnapshot();
        public CampNetwork(NetworkManager manager,NetworkBootstrap bootstrap) { this.manager=manager; this.bootstrap=bootstrap; }
        public void Register()
        {
            manager.CustomMessagingManager.RegisterNamedMessageHandler(CommandName,ReceiveCommand);
            manager.CustomMessagingManager.RegisterNamedMessageHandler(SnapshotName,ReceiveSnapshot);
            lastTick=Time.realtimeSinceStartup;
        }
        static void Send(NetworkManager manager,string name,ulong target,string json)
        {
            using(var writer=new FastBufferWriter(json.Length*2+8,Allocator.Temp))
            { writer.WriteValueSafe(json); manager.CustomMessagingManager.SendNamedMessage(name,target,writer,NetworkDelivery.ReliableFragmentedSequenced); }
        }
        public bool Request(CampCommand command)
        {
            if(!bootstrap.CampConnection || !manager.IsConnectedClient || (command.Kind!=CampCommandKind.Reel && CampSession.Pending)) return false;
            if(command.Kind!=CampCommandKind.Reel) { CampSession.Pending=true; pendingId=command.Id; }
            Send(manager,CommandName,NetworkManager.ServerClientId,JsonUtility.ToJson(command)); return true;
        }
        public void Sync(ulong client,string ack=null,string message=null)
        {
            var join=bootstrap.RequestFor(client); if(join==null || !join.CampOnly) return;
            if(!activities.TryGetValue(client,out var activity)) activities[client]=activity=new CampActivity();
            var snapshot=new CampSnapshot { Profile=JsonUtility.ToJson(Profile(client)),Ack=ack,Message=message,TestMode=IsTest(client),
                Activity=activity.Phase,Side=activity.Side,Tension=activity.Tension,Progress=activity.Progress,HeatA=activity.HeatA,HeatB=activity.HeatB,
                Fish=activity.Fish,Zone=activity.Zone,Nibble=activity.Nibble,CookMarker=activity.CookMarker,BarVelocity=activity.BarVelocity,CookStartedAt=activity.CookStartedAt };
            Send(manager,SnapshotName,client,JsonUtility.ToJson(snapshot));
        }
        void ReceiveSnapshot(ulong sender,FastBufferReader reader)
        {
            if(!manager.IsClient || sender!=NetworkManager.ServerClientId || reader.Length>32768) return;
            reader.ReadValueSafe(out string json); var snapshot=JsonUtility.FromJson<CampSnapshot>(json); if(snapshot==null) return;
            if(!string.IsNullOrEmpty(snapshot.Profile)) CampSession.Accept(snapshot.Profile);
            Snapshot=snapshot;
            if(snapshot.Ack==pendingId || snapshot.Ack=="error") { CampSession.Pending=false; pendingId=null; }
            if(!string.IsNullOrEmpty(snapshot.Message)) CampSession.Message=snapshot.Message;
        }
        void ReceiveCommand(ulong client,FastBufferReader reader)
        {
            if(!manager.IsServer || reader.Length>8192 || !bootstrap.IsCampClient(client)) return;
            float now=Time.realtimeSinceStartup;
            try
            {
                reader.ReadValueSafe(out string json); var command=JsonUtility.FromJson<CampCommand>(json);
                if(command==null || !Guid.TryParseExact(command.Id,"N",out _) || !Enum.IsDefined(typeof(CampCommandKind),command.Kind)) return;
                if(!nextInput.TryGetValue(client,out var budget)) nextInput[client]=budget=new InputBudget { At=now };
                budget.Tokens=Mathf.Min(8,budget.Tokens+(now-budget.At)*20); budget.At=now;
                if(budget.Tokens<1)
                { if(command.Kind!=CampCommandKind.Reel) Sync(client,command.Id,"Aguarde um instante e tente novamente."); return; }
                budget.Tokens--;
                var join=bootstrap.RequestFor(client);
                if(!activities.TryGetValue(client,out var a)) activities[client]=a=new CampActivity();
                var profile=Profile(client); bool success=false;
                string completionMessage=null;
                switch(command.Kind)
                {
                    case CampCommandKind.Sync: success=true; break;
                    case CampCommandKind.Action:
                        if(!a.Busy) success=IsTest(client) ? profile.Apply(command.Action) : CampRepository.Edit(join.Session,p=>p.Apply(command.Action)); break;
                    case CampCommandKind.BeginFishing: success=a.BeginFishing(profile,now); break;
                    case CampCommandKind.Reel: a.Reel(command.Held,now); return;
                    case CampCommandKind.BeginCooking: success=a.BeginCooking(profile,command.Slot,now,manager.ServerTime.Time); break;
                    case CampCommandKind.Flip: success=a.Flip(); break;
                    case CampCommandKind.FinishCooking:
                        double serverNow=manager.ServerTime.Time;
                        if(!double.IsNaN(command.CookingPressedAt) && !double.IsInfinity(command.CookingPressedAt) &&
                            command.CookingPressedAt>=serverNow-.35 && command.CookingPressedAt<=serverNow+.05)
                            success=a.FinishCooking(profile,now,(float)(command.CookingPressedAt-a.CookStartedAt),serverNow);
                        completionMessage= a.CookingRejection ?? "O clique chegou fora da janela de sincronizacao. Tente novamente.";
                        if(success)
                        {
                            Save(client,profile);
                            completionMessage=a.LastCookingQuality==3 ? "PERFEITO! Peixe preparado com qualidade maxima." :
                                a.LastCookingQuality==2 ? "BOM! Peixe cozido." : a.LastCookingQuality==0 ? "Errou a faixa: o peixe ficou cru." : "Errou a faixa: o peixe queimou.";
                            completionMessage+=" Guardado em: "+a.LastCookingStorage+".";
                        }
                        break;
                    case CampCommandKind.Cancel: a.Cancel(); success=true; break;
                    case CampCommandKind.TestSupply:
                        if(IsTest(client)) { a.Cancel(); CampTestResources.Supply(profile); success=true; } break;
                    case CampCommandKind.TestReset:
                        if(IsTest(client)) { a.Cancel(); var reset=CampTestResources.Create(); reset.Revision=profile.Revision+1; testProfiles[client]=reset; success=true; } break;
                }
                Sync(client,command.Id,success ? completionMessage ?? "Acampamento atualizado." : completionMessage ?? "Acao recusada: verifique recursos, espaco e estado da atividade.");
            }
            catch(Exception error) { Debug.LogError("Comando de acampamento: "+error.Message); Sync(client,"error","Nao foi possivel salvar. Tente novamente."); }
        }
        public void Remove(ulong client) { activities.Remove(client); nextInput.Remove(client); testProfiles.Remove(client); }
        public void Tick()
        {
            if(!manager.IsServer) return;
            float now=Time.realtimeSinceStartup,dt=Mathf.Min(.1f,now-lastTick); lastTick=now;
            bool publish=now>=nextTick;
            if(publish) nextTick=now+.1f;
            clients.Clear(); clients.AddRange(activities.Keys);
            foreach(var client in clients)
            {
                if(!bootstrap.IsCampClient(client)) { Remove(client); continue; }
                var a=activities[client]; if(!a.Busy) continue;
                int previousPhase=a.Phase;
                a.AdvanceFishing(now,dt);
                if(!publish && previousPhase==a.Phase) continue;
                var join=bootstrap.RequestFor(client);
                try
                {
                    var p=Profile(client); long revision=p.Revision;
                    string result=a.Tick(p,now,0,manager.ServerTime.Time);
                    if(p.Revision!=revision) Save(client,p);
                    if(publish || previousPhase!=a.Phase || result!=null) Sync(client,null,result);
                }
                catch(Exception error) { a.Cancel(); Debug.LogError("Atividade do acampamento: "+error.Message); Sync(client,"error","Falha ao salvar atividade."); }
            }
        }
    }
}
