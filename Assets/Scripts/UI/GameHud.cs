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
        PlayerController movement;
        float extractionAngle,extractionAngularVelocity;
        bool extractionDirectionReady;
        void Update()
        {
            if(UguiGameHud.Active) return;
            var local=InventoryWindow.Local();
            if(player!=local)
            {
                player=local; health=player!=null ? player.GetComponent<Health>() : null;
                contamination=player!=null ? player.GetComponent<SporeContamination>() : null;
                movement=player!=null ? player.GetComponent<PlayerController>() : null;
                extractionDirectionReady=false; extractionAngularVelocity=0;
            }
            var match=MatchManager.Instance;
            if(player==null || match==null || match.CurrentState!=MatchState.Running) { extractionDirectionReady=false; extractionAngularVelocity=0; return; }
            var d=ExtractionDestination()-(Vector2)player.transform.position;
            if(d.sqrMagnitude<.01f) return;
            float target=90-Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg;
            if(!extractionDirectionReady) { extractionAngle=target; extractionDirectionReady=true; }
            else extractionAngle=Mathf.SmoothDampAngle(extractionAngle,target,ref extractionAngularVelocity,.16f,Mathf.Infinity,Time.unscaledDeltaTime);
        }
        Vector2 ExtractionDestination()
        {
            int exit=player.SelectedExit.Value;
            return exit>=0 && exit<MapBuilder.ExitCount ? MapBuilder.ExitPosition(exit) :
                MapBuilder.Current==ExpeditionMap.Test ? MapBuilder.ExtractionPoints[0] : (Vector2)player.transform.position;
        }
        void OnGUI()
        {
            if(UguiGameHud.Active) return;
            if(HudLayoutEditor.IsOpen || ExpeditionMenu.IsOpen || Camp.CampController.Active || Application.isBatchMode) return;
            var nm=NetworkManager.Singleton; var mm=MatchManager.Instance;
            if(nm==null || !nm.IsConnectedClient || mm==null) return;
            using var frame=UiLayout.Begin();
            if(movement!=null && player.IsAlive && mm.CurrentState==MatchState.Running)
            {
                HudArt.Panel(HudLayout.Bounds(HudControl.Stamina));
                GUI.Label(HudLayout.Detail(HudControl.Stamina,12,3,206,20),"STAMINA   "+Mathf.CeilToInt(movement.Stamina.Value)+" / "+Mathf.CeilToInt(movement.MaxStamina.Value),HudTheme.Label(HudLayout.TextSize(HudControl.Stamina,14)));
                HudTheme.Meter(HudLayout.Detail(HudControl.Stamina,12,24,206,10),movement.Stamina.Value/Mathf.Max(1,movement.MaxStamina.Value),HudTheme.Accent);
            }
            if(health!=null)
            {
                var box=HudLayout.Bounds(HudControl.Health); HudArt.Panel(box);
                GUI.Label(HudLayout.Detail(HudControl.Health,12,5,206,18),"VIDA   "+health.Current.Value+" / "+health.Maximum.Value,HudTheme.Label(HudLayout.TextSize(HudControl.Health,14)));
                var bar=HudLayout.Detail(HudControl.Health,12,24,206,10);
                HudTheme.Fill(bar,new Color(.57f,.52f,.42f,.65f));
                HudTheme.Meter(new Rect(bar.x+1,bar.y+1,bar.width-2,bar.height-2),(float)health.Current.Value/Mathf.Max(1,health.Maximum.Value),health.Current.Value<health.Maximum.Value*.3f ? HudTheme.Danger : HudTheme.Good);
                if(player!=null && player.WellFed.Value) Status(HudControl.Food,HudSymbol.Food,"Bem alimentado",HudTheme.Good);
                if(contamination!=null && contamination.Level.Value>0) Status(HudControl.Spores,HudSymbol.Spores,"Esporos  "+Mathf.CeilToInt(contamination.Level.Value)+"%",HudTheme.Somber ? new Color(.72f,.63f,.77f) : HudTheme.Text);
            }
            var centerX=UiLayout.Width*.5f;
            int sec=mm.SecondsLeft.Value;
            var clock=HudLayout.Bounds(HudControl.Clock); HudArt.Panel(clock);
            GUI.Label(HudLayout.Detail(HudControl.Clock,5,4,122,30),mm.Sandbox ? "LABORATÓRIO" : (sec/60).ToString("00")+":"+(sec%60).ToString("00"),HudTheme.Label(HudLayout.TextSize(HudControl.Clock,mm.Sandbox ? 14 : 26),TextAnchor.MiddleCenter));
            if(player!=null && mm.CurrentState==MatchState.Running)
            {
                int exit=player.SelectedExit.Value;
                Vector2 destination=ExtractionDestination();
                Vector2 d=destination-(Vector2)player.transform.position;
                string name=MapBuilder.ExitName(exit);
                string knowledge=MapBuilder.Current!=ExpeditionMap.Test && exit>=0 ? player.ExitKnowledge(exit) : "Confirmada";
                GUI.Label(HudLayout.Detail(HudControl.Clock,9,35,57,18),"Extração",HudTheme.Label(HudLayout.TextSize(HudControl.Clock,12)));
                HudArt.Icon(HudLayout.Detail(HudControl.Clock,64,35,15,15),HudSymbol.Arrow,HudTheme.Text,extractionAngle);
                GUI.Label(HudLayout.Detail(HudControl.Clock,82,35,46,18),Mathf.RoundToInt(d.magnitude)+" u",HudTheme.Label(HudLayout.TextSize(HudControl.Clock,12)));
                // The full exit knowledge stays available in the expanded map; uncertainty is also visible here.
                if(knowledge!="Confirmada") GUI.Label(new Rect(clock.center.x-115,clock.yMax+3,230,17),name+" · "+knowledge,HudTheme.Label(11,TextAnchor.MiddleCenter));
                if(player.ExtractionProgress.Value>0)
                {
                    int extracting=player.CurrentExtractionExit.Value;
                    string action=MapBuilder.Current==ExpeditionMap.Dungeon ? "Saindo: "+MapBuilder.ExitName(extracting) : MapBuilder.ExitTransport(extracting) ? "Embarcando" : "Deixando a floresta";
                    GUI.Label(new Rect(clock.center.x-110,clock.yMax+23,220,21),action,HudTheme.Label(14,TextAnchor.MiddleCenter));
                    HudTheme.Meter(new Rect(clock.center.x-65,clock.yMax+47,130,5),player.ExtractionProgress.Value,HudTheme.Good);
                }
            }
            if(mm.Sandbox) GUI.Label(new Rect(14,UiLayout.Height-24,300,20),"Sala "+mm.RoomNumber.Value+" · "+MapBuilder.MapName(MapBuilder.Current),HudTheme.Label(11));
            if(GameOptionsMenu.IsOpen || TestLabPanel.IsOpen || LockpickWindow.IsOpen || InventoryWindow.IsOpen) return;
            string message=null;
            if(mm.CurrentState==MatchState.Waiting) message=mm.QuickTest.Value ? "Preparando laboratorio · "+mm.QuickBots.Value+" bots" : "Procurando jogadores "+mm.HumanCount.Value+"/"+MapBuilder.Capacity+"\nInicio em "+mm.WaitingSeconds.Value+" s";
            else if(player!=null && !player.IsAlive) message=player.CurrentStatus==PlayerStatus.Extracted ? (mm.Sandbox ? "Extracao de teste concluida" : "Extracao concluida") : player.CurrentStatus==PlayerStatus.Dead ? (health!=null ? health.DeathMessage : "Você morreu") : "Tempo esgotado";
            if(mm.CurrentState==MatchState.Ended) message=(message ?? "Partida encerrada")+"\nRetorno em "+mm.ResultsLeft.Value+" s";
            if(message==null) return;
            var panel=new Rect(centerX-230,180,460,205); GUI.Box(panel,"");
            GUI.Label(new Rect(panel.x+16,panel.y+18,panel.width-32,125),message,HudTheme.Label(23,TextAnchor.MiddleCenter));
            if(player!=null && !player.IsAlive && GUI.Button(new Rect(panel.x+30,panel.yMax-52,panel.width-60,40),"Voltar ao acampamento"))
            { nm.Shutdown(); SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
        }
        static void Status(HudControl control,HudSymbol icon,string text,Color color)
        {
            var r=HudLayout.Bounds(control); HudArt.Panel(r);
            var basis=HudLayout.DefaultSize(control);
            HudArt.Icon(HudLayout.Detail(control,7,3.5f,18,18),icon,color);
            var old=GUI.color; GUI.color=color;
            GUI.Label(HudLayout.Detail(control,31,3,basis.x-35,19),text,HudTheme.Label(HudLayout.TextSize(control,12),TextAnchor.MiddleLeft)); GUI.color=old;
        }
    }
}
