using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using CyberExtraction.Core;
using CyberExtraction.Items;
using CyberExtraction.Players;
using CyberExtraction.UI;
namespace CyberExtraction.Camp
{
    public class CampController : MonoBehaviour
    {
        public static CampController Instance { get; private set; }
        public static bool Active => ClientEnvironment.Chosen && Instance!=null && NetworkManager.Singleton!=null && (!NetworkManager.Singleton.IsListening || (NetworkBootstrap.Instance!=null && NetworkBootstrap.Instance.CampConnection));
        public bool ExpeditionPanelOpen;
        public bool HasActivity => activity!=0;
        public bool CanMove => Active && !ExpeditionMenu.IsOpen && !GameOptionsMenu.IsOpen && !CampTestPanel.IsOpen && !ExpeditionPanelOpen && !InventoryWindow.IsOpen && !HasActivity;
        public static Rect InteractionBounds => new Rect((UiLayout.Width-350)/2,UiLayout.Height-74,350,50);
        readonly Vector2 origin=new Vector2(2000,0), depot=CabinScenery.Depot, gate=CabinScenery.Gate,
            pier=CabinScenery.Fishing, bench=CabinScenery.Bench, fire=CabinScenery.Stove;
        CabinScenery scenery;
        Transform avatarArt;
        class GroundView { public string Id; public GameObject Object; public float ReadyAt; }
        readonly List<GroundView> groundViews=new List<GroundView>();
        GameObject root; Rigidbody2D avatar;
        int activity;
        readonly FishingPresentation fishing=new FishingPresentation();
        readonly CookingPresentation cooking=new CookingPresentation();
        string message="";
        void Awake()
        {
            Instance=this; root=new GameObject("PersonalCabin"); root.transform.position=origin;
            scenery=new CabinScenery(root.transform);
            var player=Block("CampPlayer",CabinScenery.Spawn,Vector2.one,new Color(.2f,.75f,.65f),5);
            player.GetComponent<SpriteRenderer>().sprite=SpriteFactory.Circle; player.AddComponent<CircleCollider2D>().radius=CabinScenery.PlayerRadius;
            avatar=player.AddComponent<Rigidbody2D>(); avatar.gravityScale=0; avatar.freezeRotation=true; avatar.interpolation=RigidbodyInterpolation2D.Interpolate;
            var art=new GameObject("CampCharacter"); art.transform.SetParent(player.transform,false);
            // Match the cabin's larger collider without changing movement/collision rules.
            art.transform.localScale=Vector3.one*1.3f;
            art.AddComponent<PlayerCharacterVisual>(); avatarArt=art.transform;
            player.GetComponent<SpriteRenderer>().enabled=false;
            FocusCamera(); CampSession.Progress.Ensure(); foreach(var drop in CampSession.Progress.Ground) SpawnDrop(drop.Id,drop.Stack);
        }
        public void FocusCamera()
        {
            var camera=Camera.main; if(camera==null) return;
            camera.orthographic=true; camera.orthographicSize=GameConfig.CampCameraSize; camera.transform.position=new Vector3(avatar.position.x,avatar.position.y,-10);
            var follow=camera.GetComponent<CameraFollow>() ?? camera.gameObject.AddComponent<CameraFollow>(); follow.SetTarget(avatar.transform);
            follow.SetBounds(new Rect(origin-CabinScenery.Size*.5f,CabinScenery.Size));
        }
        GameObject Block(string name,Vector2 position,Vector2 size,Color color,int order,bool solid=false)
        {
            var go=new GameObject(name); go.transform.SetParent(root.transform); go.transform.position=origin+position; go.transform.localScale=new Vector3(size.x,size.y,1);
            SpriteFactory.Setup(go.AddComponent<SpriteRenderer>(),SpriteFactory.Square,color,order); if(solid) { go.layer=GameConfig.WallLayer; go.AddComponent<BoxCollider2D>(); } return go;
        }
        void Update()
        {
            root.SetActive(Active); if(!Active) { fishing.Reset(); cooking.Reset(); return; }
            UpdateFishing();
            UpdateCooking();
            if(GameOptionsMenu.IsOpen || CampTestPanel.IsOpen || GameOptionsMenu.ConsumedBack) return;
            scenery.Refresh(CampSession.Progress.PierRepaired);
            foreach(var drop in CampSession.Progress.Ground) SpawnDrop(drop.Id,drop.Stack);
            for(int i=groundViews.Count-1;i>=0;i--)
            {
                var drop=groundViews[i];
                if(!CampSession.Progress.Ground.Exists(item=>item.Id==drop.Id)) { Destroy(drop.Object); groundViews.RemoveAt(i); continue; }
                if(CampSession.Synchronized && !CampSession.Pending && Time.time>=drop.ReadyAt && Vector2.Distance(avatar.position,drop.Object.transform.position)<1)
                    CampSession.Act(new CampAction { Kind=CampActionKind.RecoverDrop,Reference=drop.Id });
            }
            var view=NetworkBootstrap.Instance.CampView;
            if(view.Activity!=0) activity=view.Activity;
            else if(activity>=2 && activity<=4 || activity==6) activity=activity==6 ? 5 : 0;
            if(GameInput.BackPressed)
            { ExpeditionPanelOpen=false; if(view.Activity!=0) NetworkBootstrap.Instance.SendCamp(new CampCommand { Kind=CampCommandKind.Cancel }); activity=0; InventoryWindow.Instance.Close(); }
            if(CanMove && GameInput.InteractPressed) Interact();
        }
        void UpdateFishing()
        {
            var network=NetworkBootstrap.Instance;
            if(network==null) { fishing.Reset(); return; }
            var view=network.CampView;
            if(view.Activity<2 || view.Activity>4) { fishing.Reset(); return; }
            fishing.ReadInput(!GameOptionsMenu.IsOpen && !CampTestPanel.IsOpen && !InventoryWindow.IsOpen && !ExpeditionPanelOpen);
            if(fishing.NeedsSend(Time.unscaledTime) && network.SendCamp(new CampCommand { Kind=CampCommandKind.Reel,Held=fishing.Held })) fishing.Sent(Time.unscaledTime);
            fishing.Tick(view.Activity-1,view.Tension,view.Fish,view.Zone,view.Progress,view.BarVelocity,Time.unscaledDeltaTime);
        }
        void UpdateCooking()
        {
            var network=NetworkBootstrap.Instance;
            if(network==null || NetworkManager.Singleton==null) { cooking.Reset(); return; }
            cooking.Tick(network.CampView,CampSession.Pending,NetworkManager.Singleton.ServerTime.Time,Time.unscaledDeltaTime);
            bool enabled=!GameOptionsMenu.IsOpen && !CampTestPanel.IsOpen && !InventoryWindow.IsOpen && !ExpeditionPanelOpen && !CampSession.Pending && !GameOptionsMenu.ConsumedBack;
            if(cooking.RetirePressed(enabled) && network.SendCamp(new CampCommand { Kind=CampCommandKind.FinishCooking,CookingPressedAt=cooking.ServerTime })) cooking.Submit();
        }
        void FixedUpdate()
        {
            if(!CanMove) { avatar.linearVelocity=Vector2.zero; return; }
            Vector2 movement=Vector2.zero; var touch=TouchControls.Instance;
            if(touch!=null && touch.IsTouch) movement=touch.Move;
            else movement=GameInput.Move;
            avatar.linearVelocity=Vector2.ClampMagnitude(movement,1)*GameConfig.PlayerSpeed;
            if(avatarArt!=null && movement.sqrMagnitude>.01f) avatarArt.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(movement.y,movement.x)*Mathf.Rad2Deg);
            avatar.position=new Vector2(Mathf.Clamp(avatar.position.x,origin.x-20,origin.x+20),Mathf.Clamp(avatar.position.y,-10.7f,10.7f));
        }
        int Nearby()
        {
            var local=avatar.position-origin;
            if(Vector2.Distance(local,bench)<2.5f) return 4;
            if(Vector2.Distance(local,fire)<2.5f) return 5;
            if(Vector2.Distance(local,depot)<2.8f) return 1;
            if(Vector2.Distance(local,gate)<2.4f) return 2;
            if(Vector2.Distance(local,pier)<2.5f) return 3; return 0;
        }
        void Interact()
        {
            int near=Nearby();
            if(near==2) { ExpeditionPanelOpen=true; return; }
            if(near==0) return;
            if(!CampSession.Synchronized) { message=CampSession.Message="Acampamento nao sincronizado. Conecte pelo portao de expedicao e aguarde."; return; }
            if(CampSession.Pending) { message=CampSession.Message="Aguarde a confirmacao da acao anterior no acampamento."; return; }
            if(near==1) InventoryWindow.Instance.OpenSafe();
            else if(near==3)
            {
                if(!CampSession.Progress.PierRepaired) message=CampSession.Act(new CampAction { Kind=CampActionKind.Repair }) ? "Pesca habilitada!" : "Precisa de M 20 e P 10.";
                else NetworkBootstrap.Instance.SendCamp(new CampCommand { Kind=CampCommandKind.BeginFishing });
            }
            else if(near==4) activity=1; else if(near==5) activity=5;
        }
        public void SpawnDrop(string id,ItemStack stack)
        {
            if(groundViews.Exists(drop=>drop.Id==id)) return;
            var obj=Block("Dropped_"+ItemCatalog.Icon(stack.Item),avatar.position-origin+Vector2.down*1.5f,new Vector2(.6f,.6f),ItemCatalog.Tint(stack.Item),4);
            ItemIconLibrary.ApplyWorld(obj,stack.Item);
            groundViews.Add(new GroundView { Id=id,Object=obj,ReadyAt=Time.time+2 });
        }
        void Bar(Rect rect,float value,Color color,string text)
        {
            GUI.Box(rect,""); var old=GUI.color; GUI.color=color; GUI.DrawTexture(new Rect(rect.x+2,rect.y+2,(rect.width-4)*Mathf.Clamp01(value),rect.height-4),Texture2D.whiteTexture); GUI.color=old; GUI.Label(rect,text);
        }
        void OnGUI()
        {
            if(ExpeditionMenu.IsOpen) return;
            if(GameOptionsMenu.IsOpen) return;
            using var themeFrame=UiLayout.Begin();
            if(!Active || ExpeditionPanelOpen || InventoryWindow.IsOpen || CampTestPanel.IsOpen) return;
            var old=GUI.matrix; GUI.matrix=UiLayout.Matrix;
            int font=Mathf.Clamp((int)(UiLayout.Height/28),18,32);
            var label=new GUIStyle(GUI.skin.label) { fontSize=font,alignment=TextAnchor.MiddleCenter,wordWrap=true };
            var button=new GUIStyle(GUI.skin.button) { fontSize=font,wordWrap=true };
            var profile=CampSession.Progress;
            if(fishing.Phase!=0)
            {
                if(MinigameView.FishingPanel(fishing,NetworkBootstrap.Instance.CampView.Nibble))
                    NetworkBootstrap.Instance.SendCamp(new CampCommand { Kind=CampCommandKind.Cancel });
                GUI.matrix=old;
                return;
            }
            if(cooking.Active)
            {
                if(MinigameView.CookingPanel(cooking) && !CampSession.Pending)
                    NetworkBootstrap.Instance.SendCamp(new CampCommand { Kind=CampCommandKind.Cancel });
                GUI.matrix=old;
                return;
            }
            GUI.Box(new Rect(14,14,260,78),"");
            GUI.Label(new Rect(26,22,236,54),"M "+profile.Wood+"   P "+profile.Nails+"   B "+profile.Bandages+(profile.WellFed ? "\nBem alimentado pronto" : "\nMochila e bau pessoais"),HudTheme.Label(17));
            GUI.Label(new Rect(UiLayout.Width/2-160,16,320,38),"CABANA",HudTheme.Label(26,TextAnchor.MiddleCenter));
            Rect panel=new Rect(UiLayout.Width*.12f,90,UiLayout.Width*.76f,390);
            if(activity!=0)
            {
                GUI.Box(panel,"");
                if(activity==1)
                {
                    GUI.Label(new Rect(panel.x+10,panel.y+10,panel.width-20,60),"BANCADA DE MUNICOES",label);
                    var ammoTypes=new[] { ItemId.PistolAmmo,ItemId.RifleAmmo,ItemId.ShotgunShell };
                    for(int i=0;i<ammoTypes.Length;i++)
                        if(ItemIconGui.Button(new Rect(panel.x+15+i*(panel.width-30)/3,panel.y+75,(panel.width-30)/3-5,65),ammoTypes[i],"x"+WeaponCatalog.RecipeOutput(ammoTypes[i])+"\nFabricar",button))
                            CampSession.Act(new CampAction { Kind=CampActionKind.CraftAmmo,Result=(int)ammoTypes[i] });
                    GUI.Label(new Rect(panel.x+10,panel.y+140,panel.width-20,30),"Custo: 3 metal + 1 polvora + 1 componente",new GUIStyle(label) { fontSize=18 });
                    if(GUI.Button(new Rect(panel.x+15,panel.y+155,panel.width-30,65),"Reciclador nivel "+profile.RecyclerLevel+" -> melhorar ("+((profile.RecyclerLevel+1)*10)+" metais)",button)) message=CampSession.Act(new CampAction { Kind=CampActionKind.UpgradeRecycler }) ? "Reciclador melhorado." : "Nivel maximo ou metal insuficiente.";
                    if(GUI.Button(new Rect(panel.x+15,panel.y+230,panel.width-30,55),"Adquirir 1 componente por 2 metais",button)) message=CampSession.Act(new CampAction { Kind=CampActionKind.BuyComponent }) ? "Componente adquirido." : "Metal insuficiente ou slots cheios.";
                }
                else if(activity==5)
                {
                    GUI.Label(new Rect(panel.x+10,panel.y+10,panel.width-20,45),"Escolha um peixe da mochila",label);
                    for(int i=0;i<9;i++)
                    {
                        var stack=profile.Pack.Slots[i];
                        if(!stack.Empty && (ItemCatalog.Fish(stack.Item) || stack.Item==ItemId.RawFish || stack.Item==ItemId.CookedFish))
                            if(ItemIconGui.Button(new Rect(panel.x+15+(i%3)*panel.width/3,panel.y+70+(i/3)*65,panel.width/3-25,55),stack.Item,ItemCatalog.Name(stack.Item)+" x"+stack.Quantity,button))
                            {
                                if(stack.Item==ItemId.CookedFish) message=CampSession.Act(new CampAction { Kind=CampActionKind.Eat,Source=i }) ? "Bem alimentado preparado." : "Nao foi possivel comer.";
                                else NetworkBootstrap.Instance.SendCamp(new CampCommand { Kind=CampCommandKind.BeginCooking,Slot=i });
                            }
                    }
                }
                if(GUI.Button(new Rect(panel.x+20,panel.yMax-55,panel.width-40,45),"Voltar",button)) { if(viewActivity()) NetworkBootstrap.Instance.SendCamp(new CampCommand { Kind=CampCommandKind.Cancel }); activity=0; }
            }
            else
            {
                int near=Nearby(); string text=near==1 ? "Abrir bau [E]" : near==2 ? "Preparar expedicao [E]" : near==3 ? (profile.PierRepaired ? "Pescar [E]" : "Habilitar pesca: M 20 + P 10 [E]") : near==4 ? "Bancada de municoes [E]" : near==5 ? "Fogao a lenha [E]" : "WASD / joystick: caminhar pela cabana";
                bool enabled=GUI.enabled; GUI.enabled=near!=0; if(GUI.Button(InteractionBounds,text,button)) Interact(); GUI.enabled=enabled;
            }
            message=CampSession.Message;
            if(!string.IsNullOrEmpty(message)) GUI.Label(new Rect(20,activity!=0 ? panel.yMax+6 : UiLayout.Height*.74f,UiLayout.Width-40,UiLayout.Height*.07f),message,label);
            GUI.matrix=old;
        }
        bool viewActivity()=>NetworkBootstrap.Instance.CampView.Activity!=0;
        void OnDisable()
        {
            if(fishing.Phase!=0 && NetworkBootstrap.Instance!=null)
                NetworkBootstrap.Instance.SendCamp(new CampCommand { Kind=CampCommandKind.Reel,Held=false });
            fishing.Reset();
            cooking.Reset();
        }
        void OnDestroy() { if(root!=null) Destroy(root); if(Instance==this) Instance=null; }
    }
}
