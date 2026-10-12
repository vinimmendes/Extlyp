using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using CyberExtraction.Camp;
using CyberExtraction.Items;
using CyberExtraction.Bots;
using CyberExtraction.Core;
using CyberExtraction.Enemies;
using CyberExtraction.UI;

public static class ImplementationChecks
{
    [Serializable] class Result { public int Passed; public string[] Checks; }
    static readonly List<string> passed=new List<string>();
    static void Verify(bool ok,string name) { if(!ok) throw new Exception("Implementation check failed: "+name); passed.Add(name); }
    [MenuItem("Cyber Extraction/Verificar melhorias da auditoria")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Saia do Play antes de executar os checks de editor.");
        passed.Clear(); CampRepository.Close();
        try
        {
            Rules(); Activities(); Identity(); Persistence(); Navigation();
            Verify(GameplayDefinition.Current.Valid && GameplayDefinition.Current.Magazine==12 && GameplayDefinition.Current.ReloadSeconds==1.5f,"Asset de balanceamento preserva pistola");
            Verify(UiLayout.Region(.12f,.15f,.76f,.65f).yMax<=UiLayout.Height,"Painel cabe na altura virtual da area segura");
            var result=new Result { Passed=passed.Count,Checks=passed.ToArray() };
            Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/implementation-checks.json",JsonUtility.ToJson(result,true));
            Debug.Log("IMPLEMENTATION CHECKS PASS: "+passed.Count);
            PerformanceBaseline.Run();
        }
        finally { CampRepository.Close(); }
    }
    static void Rules()
    {
        var p=new CampProgress { PierRepaired=true };
        Verify(!p.Apply(new CampAction { Sequence=1,Kind=CampActionKind.CatchFish,Duration=4 }),"Captura declarada pelo cliente rejeitada");
        p.Pack.Add(new ItemStack(ItemId.FishCommon,1));
        int fish=Array.FindIndex(p.Pack.Slots,x=>!x.Empty && x.Item==ItemId.FishCommon);
        Verify(!p.Apply(new CampAction { Sequence=1,Kind=CampActionKind.CookFish,Source=fish,Duration=16,Result=3 }),"Cozimento declarado pelo cliente rejeitado");
        p.Deposit(2,0,0);
        var command=new CampAction { Kind=CampActionKind.Transfer,Sequence=1,Source=0,Target=4 };
        Verify(p.Apply(command),"Transferencia valida e sequenciada");
        Verify(!p.Apply(command),"Repeticao imediata rejeitada");
        for(int i=0;i<256;i++) Verify(p.Apply(new CampAction { Kind=CampActionKind.Transfer,Sequence=p.Revision+1,Source=i%2==0 ? 4 : 0,Target=i%2==0 ? 0 : 4 }),"Transferencia historico "+i);
        Verify(!p.Apply(command),"Repeticao antiga rejeitada apos expirar historico");
        Verify(!p.Apply(new CampAction { Sequence=p.Revision+1,Kind=CampActionKind.Transfer,SourceGrid=9,Source=0,Target=1 }),"Grid invalida rejeitada");
        var full=new InventoryGrid(); for(int i=0;i<9;i++) full.Slots[i]=new ItemStack(ItemId.Wood,20);
        Verify(!full.Add(new ItemStack(ItemId.Metal,1)) && full.Count(ItemId.Wood)==180,"Mochila cheia conserva itens");
        Verify(!full.Remove(ItemId.Wood,-1),"Quantidade negativa rejeitada");
        var craft=new CampProgress { Pack=full.Copy(),Chest=full.Copy() };
        craft.Chest.Slots[0]=new ItemStack(ItemId.Metal,20); craft.Chest.Slots[1]=new ItemStack(ItemId.Gunpowder,20); craft.Chest.Slots[2]=new ItemStack(ItemId.AmmoComponent,20);
        Verify(!craft.CraftAmmo() && craft.Count(ItemId.Metal)==20 && craft.Count(ItemId.Gunpowder)==20,"Fabricacao sem espaco reverte custos");
        var started=InventoryGrid.Starter(); var other=new InventoryGrid();
        Verify(started.MoveTo(0,other,0) && other.Slots[0].Data==12,"Transferencia preserva tiros da pistola");
        Verify(!BotDecision.CanExtract(61) && BotDecision.CanExtract(60),"Bot nao conclui extracao antecipada");
        Verify(BotDecision.Select(new BotSituation { Active=true,HealthRatio=1,SecondsLeft=110,ExtractionTravelSeconds=105 },BotProfile.For(BotDifficulty.Medium))==BotState.Extract,"Bot planeja deslocamento para saida distante");
        Verify(BotDecision.Select(new BotSituation { Active=true,HealthRatio=1,SecondsLeft=180,KnownLoot=false },BotProfile.For(BotDifficulty.Medium))!=BotState.Collect,"Bot sem loot coletavel nao coleta");
    }
    public static void Activities()
    {
        var p=new CampProgress { PierRepaired=true,Pack=new InventoryGrid() };
        var activity=new CampActivity();
        Verify(activity.BeginFishing(p,0),"Pesca iniciada no servidor");
        activity.Reel(true,.1f); activity.Tick(p,.1f,.1f);
        Verify(!activity.Busy && p.Revision==0,"Fisgada antes da mordida nao entrega peixe");
        Verify(!new CampActivity().BeginFishing(new CampProgress(),0),"Pier quebrado impede pesca");
        var game=new FishingGame(); UnityEngine.Random.InitState(42); game.Begin(ItemId.FishCommon,0);
        float now=0,lastBar=game.Bar;
        for(int i=0;i<1200 && game.Phase<4;i++)
        {
            now+=.05f;
            float speed=(game.Bar-lastBar)/.05f; lastBar=game.Bar;
            game.Input(game.Phase==2 || game.Phase==3 && game.Fish>game.Bar+speed*.2f,now);
            game.Tick(now,.05f);
        }
        Verify(game.Phase==4,"Barra de pesca pode completar captura por entradas");
        p=new CampProgress { Pack=new InventoryGrid() }; p.Pack.Add(new ItemStack(ItemId.FishCommon,1)); activity=new CampActivity();
        Verify(activity.BeginCooking(p,0,0) && !activity.FinishCooking(p,.05f),"QTE rejeita entrada instantanea ao iniciar");
        Verify(Mathf.Abs(CookingGame.Marker(CookingGame.TraverseSeconds)-1)<.001f && CookingGame.Marker(CookingGame.TraverseSeconds*2)==0,"Cursor vai e volta no trilho");
        float perfectAt=(CookingGame.PerfectStart+CookingGame.PerfectEnd)*.5f*CookingGame.TraverseSeconds;
        Verify(CookingGame.Quality(perfectAt)==3 && CookingGame.Quality(CookingGame.TraverseSeconds*2-perfectAt)==3,"Faixa perfeita funciona na ida e na volta");
        Verify(!activity.FinishCooking(p,1,-1) && !activity.FinishCooking(p,1,2),"QTE rejeita tempos fora da janela do servidor");
        activity.Tick(p,perfectAt,.1f);
        Verify(activity.FinishCooking(p,perfectAt+.1f,perfectAt) && p.Pack.Slots[0].Quality==3,"Servidor avalia o instante do clique com atraso limitado");
        Verify(!activity.FinishCooking(p,perfectAt+.2f),"Conclusao de cozimento nao repete recompensa");
        Verify(p.Apply(new CampAction { Sequence=p.Revision+1,Kind=CampActionKind.Eat,Source=0 }) && p.ConsumeFoodBuff()==25 && p.ConsumeFoodBuff()==0,"Refeicao concede buff uma vez");
        p=new CampProgress { Pack=new InventoryGrid() }; p.Pack.Add(new ItemStack(ItemId.FishCommon,1)); activity=new CampActivity();
        activity.BeginCooking(p,0,0); activity.Tick(p,8.1f,.1f);
        Verify(!activity.Busy && p.Pack.Count(ItemId.BurntFish)==1,"Passar do fim da barra queima peixe");
    }
    static void Identity()
    {
        var join=new CampJoinRequest { CampOnly=true }; CampIdentity.Sign(join);
        Verify(CampIdentity.Verify(join,DateTime.UtcNow.Ticks),"Assinatura valida aceita");
        join.Map=1; Verify(!CampIdentity.Verify(join,DateTime.UtcNow.Ticks),"Alterar dados assinados invalida identidade");
        CampIdentity.Sign(join); Verify(CampRepository.Authenticate(join) && !CampRepository.Authenticate(join),"Login assinado nao pode ser repetido");
        join.Session=Guid.NewGuid().ToString("N"); Verify(!CampIdentity.Verify(join,DateTime.UtcNow.Ticks),"ID alheio sem chave rejeitado");
    }
    static void Persistence()
    {
        string directory=Path.GetFullPath("Logs/test-camps"),cluster="test-"+Guid.NewGuid().ToString("N"),session=Guid.NewGuid().ToString("N");
        CampRepository.EnableShared(cluster,directory);
        CampRepository.Edit(session,p=> { p.Deposit(20,10,0); p.WellFed=true; p.FoodHealth=25; return 0; });
        CampRepository.Close(); CampRepository.EnableShared(cluster,directory);
        Verify(CampRepository.Get(session).Wood==20,"Progresso sobrevive ao fechamento do store");
        var equipment=CampRepository.Begin(session,"one",out int food); Verify(food==25 && equipment.Count(ItemId.Pistol)==1,"Reserva de equipamento e alimento");
        CampRepository.Rollback(session,"one"); Verify(CampRepository.Get(session).Pack.Count(ItemId.Pistol)==1 && CampRepository.Get(session).WellFed,"Falha de inicio restaura equipamento e buff");
        CampRepository.Begin(session,"two",out food); CampRepository.Commit(session,"two");
        Verify(CampRepository.Bank(session,"two",equipment) && !CampRepository.Bank(session,"two",equipment),"Extracao contabilizada uma unica vez");
        CampRepository.Begin(session,"three",out food); CampRepository.Commit(session,"three"); CampRepository.Lose(session,"three");
        Verify(CampRepository.Get(session).ExpeditionState==0 && CampRepository.Get(session).Pack.Count(ItemId.Pistol)==0,"Morte nao devolve equipamento perdido");
        CampRepository.Edit(session,p=> { p.Pack=InventoryGrid.Starter(); return 0; });
        CampRepository.Begin(session,"crash",out food); CampRepository.Commit(session,"crash"); CampRepository.Close(); CampRepository.EnableShared(cluster,directory);
        Verify(CampRepository.Get(session).ExpeditionState==0 && CampRepository.Get(session).Pack.Count(ItemId.Pistol)==1,"Queda completa recupera reserva nao liquidada");
        var previous=CampRepository.Get(session);
        try { CampRepository.Edit<int>(session,p=> { p.Pack=new InventoryGrid(); throw new InvalidOperationException("injected"); }); } catch(InvalidOperationException) { }
        Verify(CampRepository.Get(session).Pack.Count(ItemId.Pistol)==previous.Pack.Count(ItemId.Pistol),"Excecao em transacao preserva ultimo estado");
        string save=Path.Combine(directory,cluster+".json"); CampRepository.Close(); File.WriteAllText(save,"invalid json"); CampRepository.EnableShared(cluster,directory);
        Verify(CampRepository.Get(session)!=null,"Backup recupera arquivo corrompido");
    }
    static void Navigation()
    {
        var wall=new GameObject("NavigationTestWall"); wall.layer=GameConfig.WallLayer; wall.transform.position=new Vector3(100,100,0); var collider=wall.AddComponent<BoxCollider2D>(); collider.size=new Vector2(1,8);
        try
        {
            Physics2D.SyncTransforms(); var nav=new MonsterNavigation(.4f);
            Verify(nav.TryGetWaypoint(new Vector2(97,100),new Vector2(103,100),out var waypoint),"A* contorna parede na Unity");
            Verify(Physics2D.CircleCast(new Vector2(97,100),.44f,(waypoint-new Vector2(97,100)).normalized,Vector2.Distance(waypoint,new Vector2(97,100)),GameConfig.WallMask).collider==null,"Waypoint respeita largura do corpo");
            collider.size=new Vector2(1,90); Physics2D.SyncTransforms(); nav=new MonsterNavigation(.4f);
            Verify(nav.TryGetWaypoint(new Vector2(99,100),new Vector2(101,100),out waypoint),"Desvio de parede maior que janela local");
        }
        finally { UnityEngine.Object.DestroyImmediate(wall); Physics2D.SyncTransforms(); }
    }
}
