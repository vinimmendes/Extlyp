using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using CyberExtraction.Camp;
using CyberExtraction.UI;

public static class CampInteractionChecks
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static int checks;
    static void Check(bool ok,string label) { if(!ok) throw new Exception("CAMP INTERACTION FAIL: "+label); checks++; }
    public static void BuildClient() { Run(); ServerBuild.BuildClient(); }
    [MenuItem("Cyber Extraction/Avançado/Diagnósticos/Interações do acampamento")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Saia do Play para executar a verificacao isolada.");
        checks=0;
        bool synchronized=CampSession.Synchronized,pending=CampSession.Pending; string message=CampSession.Message;
        var profile=CampSession.Progress;
        var campObject=new GameObject("CampInteractionFixture"); var bagObject=new GameObject("CampInventoryFixture");
        var camp=campObject.AddComponent<CampController>(); var bag=bagObject.AddComponent<InventoryWindow>();
        try
        {
            typeof(InventoryWindow).GetMethod("Awake",Private).Invoke(bag,null);
            typeof(CampController).GetMethod("Awake",Private).Invoke(camp,null);
            var avatar=(Rigidbody2D)typeof(CampController).GetField("avatar",Private).GetValue(camp);
            var interact=typeof(CampController).GetMethod("Interact",Private);
            var sites=new[] {CabinScenery.Depot+Vector2.down*1.3f,CabinScenery.Fishing,
                CabinScenery.Stove+Vector2.down*1.3f,CabinScenery.Bench+Vector2.down*1.3f};
            var names=new[] {"bau","pier","fogueira","bancada"};
            for(int state=0;state<2;state++) for(int i=0;i<sites.Length;i++)
            {
                CampSession.Synchronized=state==1; CampSession.Pending=state==1;
                camp.ExpeditionPanelOpen=false; avatar.position=new Vector2(2000,0)+sites[i];
                interact.Invoke(camp,null);
                Check(!camp.ExpeditionPanelOpen,names[i]+(state==0 ? " desconectado nao abre expedicao" : " pendente nao abre expedicao"));
                Check(!InventoryWindow.IsOpen && !camp.HasActivity,names[i]+" aguarda sincronizacao sem executar acao");
            }
            CampSession.Synchronized=false; CampSession.Pending=false;
            avatar.position=new Vector2(2000,0)+CabinScenery.Gate+Vector2.up; camp.ExpeditionPanelOpen=false; interact.Invoke(camp,null);
            Check(camp.ExpeditionPanelOpen,"Portao permite abrir conexao/expedicao sem sincronizacao");
            CampSession.Synchronized=true; CampSession.Pending=false;
            camp.ExpeditionPanelOpen=false; avatar.position=new Vector2(2000,0)+sites[0]; interact.Invoke(camp,null);
            Check(InventoryWindow.IsOpen && !camp.ExpeditionPanelOpen,"Bau sincronizado abre inventario seguro"); bag.Close();
            avatar.position=new Vector2(2000,0)+sites[2]; interact.Invoke(camp,null);
            Check(camp.HasActivity && (int)typeof(CampController).GetField("activity",Private).GetValue(camp)==5 && !camp.ExpeditionPanelOpen,"Fogueira sincronizada abre selecao de peixe");
            typeof(CampController).GetField("activity",Private).SetValue(camp,0);
            avatar.position=new Vector2(2000,0)+sites[3]; interact.Invoke(camp,null);
            Check(camp.HasActivity && (int)typeof(CampController).GetField("activity",Private).GetValue(camp)==1 && !camp.ExpeditionPanelOpen,"Bancada sincronizada abre fabricacao");
            Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/camp-interaction-checks.json","{\"passed\":"+checks+"}");
            Debug.Log("CAMP INTERACTION CHECKS PASS: "+checks);
        }
        finally
        {
            var world=typeof(CampController).GetField("root",Private).GetValue(camp) as GameObject;
            if(world!=null) UnityEngine.Object.DestroyImmediate(world);
            UnityEngine.Object.DestroyImmediate(campObject); UnityEngine.Object.DestroyImmediate(bagObject);
            CampSession.Synchronized=synchronized; CampSession.Pending=pending; CampSession.Message=message; CampSession.Progress=profile;
        }
    }
}
