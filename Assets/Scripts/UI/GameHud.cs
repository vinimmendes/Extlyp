using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using CyberExtraction.Combat;
using CyberExtraction.Match;
using CyberExtraction.Players;
using CyberExtraction.Core;

namespace CyberExtraction.UI
{
    public class GameHud : MonoBehaviour
    {
        public static Rect ExitBounds=>HudLayout.Menu;
        PlayerInventory player;
        Health health;
        SporeContamination contamination;
        void Update()
        {
            var local=InventoryWindow.Local(); if(player==local) return;
            player=local; health=player!=null ? player.GetComponent<Health>() : null;
            contamination=player!=null ? player.GetComponent<SporeContamination>() : null;
        }
        void OnGUI()
        {
            if(ExpeditionMenu.IsOpen || Camp.CampController.Active || Application.isBatchMode) return;
            var nm=NetworkManager.Singleton; var mm=MatchManager.Instance;
            if(nm==null || !nm.IsConnectedClient || mm==null) return;
            using var frame=UiLayout.Begin();
            if(health!=null)
            {
                var box=new Rect(14,14,236,126); GUI.Box(box,"");
                GUI.Label(new Rect(26,22,212,25),"VIDA  "+health.Current.Value+" / "+health.Maximum.Value,HudTheme.Label(19));
                HudTheme.Meter(new Rect(26,53,212,7),(float)health.Current.Value/Mathf.Max(1,health.Maximum.Value),health.Current.Value<health.Maximum.Value*.3f ? HudTheme.Danger : HudTheme.Good);
                if(health.Maximum.Value>GameConfig.PlayerMaxHealth) GUI.Label(new Rect(26,69,212,23),"Bem alimentado",HudTheme.Label(16));
                if(contamination!=null && contamination.Level.Value>0) GUI.Label(new Rect(26,96,212,23),"Esporos "+Mathf.CeilToInt(contamination.Level.Value)+"%",HudTheme.Label(16));
            }
            var centerX=UiLayout.Width*.5f;
            int sec=mm.SecondsLeft.Value;
            GUI.Label(new Rect(centerX-140,14,280,39),mm.Sandbox ? "LABORATORIO" : sec/60+":"+(sec%60).ToString("00"),HudTheme.Label(29,TextAnchor.MiddleCenter));
            if(player!=null && mm.CurrentState==MatchState.Running)
            {
                int exit=player.SelectedExit.Value;
                Vector2 destination=MapBuilder.Current==ExpeditionMap.Large && exit>=0 && exit<LargeMapPlan.Exits.Length ? LargeMapPlan.Exits[exit].Position : MapBuilder.Current==ExpeditionMap.Test ? MapBuilder.ExtractionPoints[0] : (Vector2)player.transform.position;
                Vector2 d=destination-(Vector2)player.transform.position;
                string direction=Mathf.Abs(d.x)>Mathf.Abs(d.y) ? (d.x>0 ? "Leste" : "Oeste") : (d.y>0 ? "Norte" : "Sul");
                string name=MapBuilder.Current==ExpeditionMap.Large && exit>=0 && exit<LargeMapPlan.Exits.Length ? LargeMapPlan.Exits[exit].Name : "Extração";
                string knowledge=MapBuilder.Current==ExpeditionMap.Large && exit>=0 ? player.ExitKnowledge(exit) : "Confirmada";
                GUI.Label(new Rect(centerX-245,56,490,25),name+" · "+knowledge,HudTheme.Label(16,TextAnchor.MiddleCenter));
                GUI.Label(new Rect(centerX-170,81,340,25),Mathf.RoundToInt(d.magnitude)+" u · "+direction,HudTheme.Label(15,TextAnchor.MiddleCenter));
                if(player.ExtractionProgress.Value>0)
                {
                    int extracting=player.CurrentExtractionExit.Value;
                    string action=extracting>=0 && extracting<LargeMapPlan.Exits.Length && LargeMapPlan.Exits[extracting].Transport ? "Embarcando" : "Deixando a floresta";
                    GUI.Label(new Rect(centerX-170,109,340,24),action,HudTheme.Label(17,TextAnchor.MiddleCenter));
                    HudTheme.Meter(new Rect(centerX-100,137,200,7),player.ExtractionProgress.Value,HudTheme.Good);
                }
            }
            GUI.Label(new Rect(14,UiLayout.Height-40,300,24),"Sala "+mm.RoomNumber.Value+" · "+(MapBuilder.Current==ExpeditionMap.Test ? "Mapa de teste" : "Mapa grande"),HudTheme.Label(14));
            if(GameOptionsMenu.IsOpen || TestLabPanel.IsOpen || LockpickWindow.IsOpen || InventoryWindow.IsOpen) return;
            string message=null;
            if(mm.CurrentState==MatchState.Waiting) message=mm.QuickTest.Value ? "Preparando laboratorio · "+mm.QuickBots.Value+" bots" : "Procurando jogadores "+mm.HumanCount.Value+"/"+GameConfig.MatchCapacity+"\nInicio em "+mm.WaitingSeconds.Value+" s";
            else if(player!=null && !player.IsAlive) message=player.CurrentStatus==PlayerStatus.Extracted ? (mm.Sandbox ? "Extracao de teste concluida" : "Extracao concluida") : player.CurrentStatus==PlayerStatus.Dead ? "Voce morreu" : "Tempo esgotado";
            if(mm.CurrentState==MatchState.Ended) message=(message ?? "Partida encerrada")+"\nRetorno em "+mm.ResultsLeft.Value+" s";
            if(message==null) return;
            var panel=new Rect(centerX-230,200,460,165); GUI.Box(panel,"");
            GUI.Label(new Rect(panel.x+16,panel.y+18,panel.width-32,85),message,HudTheme.Label(23,TextAnchor.MiddleCenter));
            if(player!=null && !player.IsAlive && GUI.Button(new Rect(panel.x+30,panel.yMax-52,panel.width-60,40),"Voltar ao acampamento"))
            { nm.Shutdown(); SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
        }
    }
}
