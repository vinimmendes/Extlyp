using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using CyberExtraction.Items;
using CyberExtraction.Match;
using CyberExtraction.Players;
using CyberExtraction.Camp;
using CyberExtraction.Core;
using Text=TMPro.TextMeshProUGUI;
namespace CyberExtraction.UI
{
    public sealed partial class UguiGameHud
    {
        float menuLayoutWidth=-1;
        bool menuCompact;
        int pendingMenuFocus=-1;
        ScrollRect menuHelpScroll;
        void Choice(string text,Action action)
        {
            int index=menuActions.Count; if(index>=menuButtons.Count) return;
            menuActions.Add(action); var b=menuButtons[index]; b.gameObject.SetActive(true); var label=b.GetComponentInChildren<Text>(true); label.text=text; label.color=HudTheme.Text;
            Place((RectTransform)b.transform,new Rect(28,122+index*56,menu.sizeDelta.x-56,48));
            UguiMenuStyle.Button(b);
            label.enableAutoSizing=true; label.fontSizeMin=16; label.fontSizeMax=20;
        }
        void RefreshMenu(bool style)
        {
            int screen=GameOptionsMenu.MenuScreen; bool visible=screen!=0 && !HudLayoutEditor.IsOpen;
            menu.gameObject.SetActive(visible);
            if(menuDimmer==null)
            {
                var scrim=new GameObject("MenuScrim",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
                scrim.transform.SetParent(safe,false); menuDimmer=scrim.GetComponent<Image>();
                scrim.transform.SetSiblingIndex(menu.GetSiblingIndex());
            }
            menuDimmer.gameObject.SetActive(visible);
            menuDimmer.color=new Color(.015f,.02f,.015f,UguiBaseHub.Active ? .72f : .86f);
            float scale=UiLayout.Scale; var safeScreen=UiLayout.SafeScreen;
            Place(menuDimmer.rectTransform,new Rect(-safeScreen.x/scale,-(Screen.height-safeScreen.yMax)/scale,Screen.width/scale,Screen.height/scale));
            int count=screen==2 ? 3 : screen==5 ? 7 : screen==6 ? 4 : screen==3 ? 1 : screen==4 ? 2 : 4+(TestLabPanel.Available ? 1 : 0)+(CampTestPanel.Available ? 1 : 0);
            float height=screen==2 ? 340 : screen==5 ? 550 : screen==6 ? 390 : screen==3 ? 490 : screen==4 ? 302 : 146+count*56;
            float width=Mathf.Min(screen==5 || screen==6 ? 620 : 540,UiLayout.Width-40);
            bool compact=UguiBaseHub.Compact;
            if(compact) { width=Mathf.Min(900,UiLayout.Width-40); height=screen==3 ? 560 : screen==4 ? 380 : 480; }
            style|=menuLayoutWidth!=width || menuCompact!=compact;
            menuLayoutWidth=width; menuCompact=compact;
            Place(menu,new Rect((UiLayout.Width-width)/2,(600-height)/2,width,height));
            if(screen==0) { lastMenu=0; return; }
            if(lastMenu==screen && !style) return; lastMenu=screen;
            if(menuHelpScroll!=null)
            {
                menuBody.transform.SetParent(menu,false);
                menuHelpScroll.gameObject.SetActive(false);
            }
            BaseMenuStyle.Scope(menu.gameObject,true);
            menu.GetComponent<HudFrameGraphic>().ApplyTheme(); menuTitle.color=menuBody.color=HudTheme.Text;
            Place(menuTitle.rectTransform,new Rect(28,26,width-56,40)); Place(menuBody.rectTransform,new Rect(28,76,width-56,44));
            menuTitle.font=UiFontAssets.Title; menuTitle.fontSize=26; menuTitle.enableAutoSizing=true; menuTitle.fontSizeMin=20; menuTitle.fontSizeMax=26;
            menuTitle.alignment=TMPro.TextAlignmentOptions.MidlineLeft;
            menuBody.font=UiFontAssets.Regular; menuBody.fontSize=18; menuBody.color=BaseMenuStyle.Secondary;
            menuBody.enableAutoSizing=true; menuBody.fontSizeMin=16; menuBody.fontSizeMax=18;
            menuBody.overflowMode=TMPro.TextOverflowModes.Ellipsis;
            menuBody.alignment=TMPro.TextAlignmentOptions.TopLeft; menuBody.textWrappingMode=TMPro.TextWrappingModes.Normal;
            foreach(var button in menuButtons) { button.gameObject.SetActive(false); button.GetComponentInChildren<Text>(true).color=HudTheme.Text; }
            menuActions.Clear(); menuBody.text="";
            if(screen==2)
            {
                menuTitle.text="OPÇÕES"; menuBody.text="Escolha uma categoria. Preferências salvas neste aparelho.";
                Choice("Jogabilidade",GameOptionsMenu.ShowGameplayOptions);
                Choice("Gráficos",GameOptionsMenu.ShowGraphicsOptions);
                Choice("Voltar",GameOptionsMenu.BackToMenu);
            }
            else if(screen==5)
            {
                menuTitle.text="OPÇÕES · JOGABILIDADE"; menuBody.text="Controles de toque e organização dos botões.";
                Choice(TouchAimSettings.Label,()=> { TouchAimSettings.Toggle(); pendingMenuFocus=0; lastMenu=-1; });
                Choice(TouchAimSettings.SwapLabel,()=> { TouchAimSettings.ToggleSwap(); pendingMenuFocus=1; lastMenu=-1; });
                Choice(TouchAimSettings.AutoFireLabel,()=> { TouchAimSettings.ToggleAutoFire(); pendingMenuFocus=2; lastMenu=-1; });
                Choice(TouchAimSettings.ScreenAimLabel,()=> { TouchAimSettings.ToggleScreenAim(); pendingMenuFocus=3; lastMenu=-1; });
                Choice(TouchAimSettings.AutoReloadLabel,()=> { TouchAimSettings.ToggleAutoReload(); pendingMenuFocus=4; lastMenu=-1; });
                Choice("Organizar botões e tamanhos",()=> { GameOptionsMenu.CloseMenu(); HudLayoutEditor.Show(); });
                Choice("Voltar às categorias",()=> { GameOptionsMenu.BackToOptions(); pendingMenuFocus=0; });
            }
            else if(screen==6)
            {
                menuTitle.text="OPÇÕES · GRÁFICOS"; menuBody.text="Tema da interface e aparência do cenário.";
                Choice("A · SIMPLES\n"+(HudTheme.Somber ? "Superfícies limpas" : "Selecionado"),()=>HudTheme.Select(HudAppearance.Simple));
                Choice("B · SOMBRIA\n"+(HudTheme.Somber ? "Selecionado" : "Ferro, couro e bronze"),()=>HudTheme.Select(HudAppearance.Somber));
                Choice("Gráficos do cenário: "+SceneGraphics.Name+" · alternar",()=> { SceneGraphics.Toggle(); pendingMenuFocus=2; lastMenu=-1; });
                Choice("Voltar às categorias",()=> { GameOptionsMenu.BackToOptions(); pendingMenuFocus=1; });
                float card=(width-68)/2;
                Place((RectTransform)menuButtons[0].transform,new Rect(28,134,card,88));
                Place((RectTransform)menuButtons[1].transform,new Rect(40+card,134,card,88));
                Place((RectTransform)menuButtons[2].transform,new Rect(28,238,width-56,48));
                Place((RectTransform)menuButtons[3].transform,new Rect(28,302,width-56,48));
            }
            else if(screen==3)
            {
                menuTitle.text="CONTROLES"; menuBody.text=GameHelp.Controls; Place(menuBody.rectTransform,new Rect(28,82,width-56,310));
                Choice("Voltar",GameOptionsMenu.BackToMenu); Place((RectTransform)menuButtons[0].transform,new Rect(28,414,width-56,48));
            }
            else if(screen==4)
            {
                menuTitle.text="SAIR DA PARTIDA?";
                menuBody.text=CampController.Active ? "Seu progresso permanece salvo." : NetworkBootstrap.Instance!=null && NetworkBootstrap.Instance.IsTestEnvironment ? "Itens desta expedição são temporários." : "Sair perde os itens da mochila. O baú seguro permanece na base.";
                Choice("Continuar jogando",GameOptionsMenu.BackToMenu); Choice("Confirmar saída",GameOptionsMenu.ConfirmLeave);
            }
            else
            {
                menuTitle.text="MENU"; menuBody.text="Partidas online continuam com o menu aberto.";
                Choice("Continuar",GameOptionsMenu.CloseMenu); Choice("Opções",GameOptionsMenu.ShowOptions); Choice("Ajuda e controles",GameOptionsMenu.ShowHelp);
                if(TestLabPanel.Available) Choice("Laboratório",()=> { GameOptionsMenu.CloseMenu(); TestLabPanel.Show(); });
                if(CampTestPanel.Available) Choice("Ferramentas de teste",()=> { GameOptionsMenu.CloseMenu(); CampTestPanel.Show(); });
                Choice("Sair para a tela inicial",GameOptionsMenu.AskLeave);
            }
            if(compact) LayoutCompactMenu(screen,width,height);
            for(int i=0;i<menuActions.Count;i++)
            {
                var navigation=new Navigation {mode=Navigation.Mode.Explicit,
                    selectOnUp=menuButtons[Mathf.Max(0,i-1)],selectOnDown=menuButtons[Mathf.Min(menuActions.Count-1,i+1)]};
                if(compact && (screen==1 || screen==5))
                {
                    navigation.selectOnUp=menuButtons[Mathf.Max(0,i-2)];
                    navigation.selectOnDown=menuButtons[Mathf.Min(menuActions.Count-1,i+2)];
                    navigation.selectOnLeft=menuButtons[i%2==1 ? i-1 : i];
                    navigation.selectOnRight=menuButtons[i%2==0 && i+1<menuActions.Count ? i+1 : i];
                }
                if(screen==6 && i<2)
                {
                    navigation.selectOnUp=menuButtons[i]; navigation.selectOnDown=menuButtons[2];
                    navigation.selectOnLeft=menuButtons[0]; navigation.selectOnRight=menuButtons[1];
                }
                if(screen==6 && i==2) navigation.selectOnUp=menuButtons[HudTheme.Somber ? 1 : 0];
                menuButtons[i].navigation=navigation;
            }
            if(EventSystem.current!=null && menuActions.Count>0)
            {
                int selected=pendingMenuFocus>=0 && pendingMenuFocus<menuActions.Count ? pendingMenuFocus : screen==6 && HudTheme.Somber ? 1 : 0;
                EventSystem.current.SetSelectedGameObject(menuButtons[selected].gameObject);
            }
            pendingMenuFocus=-1;
        }
        void LayoutCompactMenu(int screen,float width,float height)
        {
            menuTitle.enableAutoSizing=false; menuTitle.fontSize=30;
            menuBody.enableAutoSizing=false; menuBody.fontSize=24;
            menuBody.overflowMode=TMPro.TextOverflowModes.Ellipsis;
            Place(menuTitle.rectTransform,new Rect(28,20,width-56,44));
            Place(menuBody.rectTransform,new Rect(28,80,width-56,64));
            for(int i=0;i<menuActions.Count;i++)
            {
                var text=menuButtons[i].GetComponentInChildren<Text>(true);
                text.enableAutoSizing=false; text.fontSize=26;
                text.textWrappingMode=TMPro.TextWrappingModes.Normal;
                text.overflowMode=TMPro.TextOverflowModes.Ellipsis;
                float half=(width-68)/2;
                var bounds=new Rect(28+i%2*(half+12),164+i/2*76,half,64);
                if(screen==2) bounds=new Rect(28,164+i*76,width-56,64);
                else if(screen==5 && i==menuActions.Count-1) bounds=new Rect(28,height-88,width-56,64);
                else if(screen==6)
                {
                    if(i<2) bounds=new Rect(28+i*(half+12),156,half,104);
                    else bounds=new Rect(28,272+(i-2)*76,width-56,64);
                }
                else if(screen==3) bounds=new Rect(28,height-84,width-56,64);
                else if(screen==4) bounds=new Rect(28+i*(half+12),height-92,half,64);
                Place((RectTransform)menuButtons[i].transform,bounds);
            }
            if(screen==4) Place(menuBody.rectTransform,new Rect(28,84,width-56,160));
            if(screen!=3) return;
            if(menuHelpScroll==null) menuHelpScroll=UguiGeometry.CreateVerticalScroll(menu,"CompactControlsHelp");
            menuHelpScroll.gameObject.SetActive(true);
            float inner=width-56,available=height-188;
            Place((RectTransform)menuHelpScroll.transform,new Rect(28,80,inner,available));
            Place(menuHelpScroll.viewport,new Rect(0,0,inner,available));
            menuBody.transform.SetParent(menuHelpScroll.content,false);
            menuBody.raycastTarget=false; menuBody.overflowMode=TMPro.TextOverflowModes.Overflow;
            float contentHeight=Mathf.Max(available,menuBody.GetPreferredValues(menuBody.text,inner,float.PositiveInfinity).y);
            Place(menuHelpScroll.content,new Rect(0,0,inner,contentHeight));
            Place(menuBody.rectTransform,new Rect(0,0,inner,contentHeight));
            menuHelpScroll.StopMovement(); menuHelpScroll.verticalNormalizedPosition=1;
        }
        void RefreshResults(bool show)
        {
            string message=null;
            if(show && match!=null)
            {
                if(match.CurrentState==MatchState.Waiting) message=match.QuickTest.Value ? "Preparando laboratório" : "Procurando jogadores "+match.HumanCount.Value+"/"+MapBuilder.Capacity+"\nInício em "+match.WaitingSeconds.Value+" s";
                else if(player!=null && !player.IsAlive) message=player.CurrentStatus==PlayerStatus.Extracted ? "Extração concluída" : player.CurrentStatus==PlayerStatus.Dead ? (health!=null ? health.DeathMessage : "Você morreu") : "Tempo esgotado";
                if(match.CurrentState==MatchState.Ended) message=(message ?? "Partida encerrada")+"\nRetorno em "+match.ResultsLeft.Value+" s";
            }
            results.gameObject.SetActive(message!=null); if(message==null) return;
            if(player!=null && !player.IsAlive && match!=null && !match.Sandbox)
                message+="\nXP confirmado: +"+player.LastConfirmedExperience.Value+" · Nível "+player.CharacterLevel.Value+" · Pontos "+player.AvailableAttributePoints.Value;
            Place(results,new Rect((UiLayout.Width-460)/2,180,460,205)); results.GetComponent<HudFrameGraphic>().ApplyTheme(); resultText.text=message; resultText.color=HudTheme.Text;
            Place(resultText.rectTransform,new Rect(20,15,420,125)); resultText.enableAutoSizing=true; resultText.fontSizeMin=14; resultText.fontSizeMax=23;
            Place((RectTransform)resultBack.transform,new Rect(30,155,400,38));
            resultBack.gameObject.SetActive(player!=null && !player.IsAlive);
            resultBack.GetComponent<HudFrameGraphic>().ApplyTheme(); resultBack.GetComponentInChildren<Text>(true).color=HudTheme.Text;
            if(UguiBaseHub.Compact)
            {
                float width=Mathf.Min(800,UiLayout.Width-40);
                Place(results,new Rect((UiLayout.Width-width)/2,150,width,300));
                Place(resultText.rectTransform,new Rect(24,20,width-48,172));
                resultText.enableAutoSizing=false; resultText.fontSize=26;
                Place((RectTransform)resultBack.transform,new Rect(24,212,width-48,64));
                var label=resultBack.GetComponentInChildren<Text>(true); label.enableAutoSizing=false; label.fontSize=26;
            }
        }
    }
}
