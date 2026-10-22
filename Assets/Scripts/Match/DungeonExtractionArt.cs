using System;
using System.Collections.Generic;
using UnityEngine;
using CyberExtraction.Core;

namespace CyberExtraction.Match
{
    // Receives presentation state only. Never opens a route or completes an extraction.
    public sealed class DungeonExtractionArt : MonoBehaviour
    {
        [Serializable] sealed class Manifest { public Entry[] assets; }
        [Serializable] sealed class Entry { public string id,resource; public float x,y,width,height,pivotX,pivotY; }
        readonly Dictionary<string,Sprite> sprites=new Dictionary<string,Sprite>();
        readonly List<Sprite> owned=new List<Sprite>();
        readonly List<GameObject> pieces=new List<GameObject>();
        readonly List<SpriteRenderer> originals=new List<SpriteRenderer>();
        readonly Sprite[] gateFrames=new Sprite[8],lockFrames=new Sprite[8];
        readonly Sprite[] chainPoses=new Sprite[2];
        SpriteRenderer gate,latch,tether,boatDeck;
        GameObject stowed,installedCrank;
        float gateFrame,lockFrame;
        bool synchronized;

        public static DungeonExtractionArt Build(Transform world)
        {
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null) return null;
            var root=new GameObject("DungeonExtractionArt"); root.transform.SetParent(world,false);
            var art=root.AddComponent<DungeonExtractionArt>();
            try { art.Load(); art.BuildPieces(); art.Commit(); return art; }
            catch(Exception exception)
            { foreach(var piece in art.pieces) if(piece!=null) piece.SetActive(false); root.SetActive(false); Destroy(root); Debug.LogWarning("Catacumbas: extracoes anteriores preservadas. "+exception.Message); return null; }
        }
        static string Id(int family,int variant=1) => "CAT-E"+family.ToString("00")+"_v"+variant.ToString("00");
        void Load()
        {
            var json=Resources.Load<TextAsset>("Environment/Dungeon/Exits/ExtractionsArt");
            if(json==null) throw new InvalidOperationException("Manifesto de extracoes ausente.");
            var manifest=JsonUtility.FromJson<Manifest>(json.text);
            if(manifest==null || manifest.assets==null) throw new InvalidOperationException("Manifesto de extracoes invalido.");
            foreach(var entry in manifest.assets)
            {
                var texture=Resources.Load<Texture2D>(entry.resource);
                if(texture==null) throw new InvalidOperationException("Asset ausente: "+entry.id);
                int x=Mathf.Clamp(Mathf.FloorToInt(entry.x*texture.width),0,texture.width-1);
                int y=Mathf.Clamp(Mathf.FloorToInt(entry.y*texture.height),0,texture.height-1);
                int w=Mathf.Clamp(Mathf.CeilToInt(entry.width*texture.width),1,texture.width-x);
                int h=Mathf.Clamp(Mathf.CeilToInt(entry.height*texture.height),1,texture.height-y);
                var sprite=Sprite.Create(texture,new Rect(x,y,w,h),new Vector2(entry.pivotX,entry.pivotY),128,0,SpriteMeshType.FullRect);
                owned.Add(sprite); sprites.Add(entry.id,sprite);
            }
            for(int i=0;i<8;i++)
            { gateFrames[i]=sprites[Id(2)+"_f"+(i+1).ToString("00")]; lockFrames[i]=sprites[Id(11)+"_f"+(i+1).ToString("00")]; }
            chainPoses[0]=sprites[Id(10)]; chainPoses[1]=sprites[Id(10,2)];
            originals.AddRange(transform.parent.GetComponentsInChildren<SpriteRenderer>(true));
        }
        SpriteRenderer Old(string name)
        {
            foreach(var renderer in originals) if(renderer.name==name) return renderer;
            throw new InvalidOperationException("Suporte de extracao ausente: "+name);
        }
        SpriteRenderer Place(string id,Vector2 point,Vector2 size,int order)
        {
            var go=new GameObject(id); go.transform.SetParent(transform,false); go.transform.position=point;
            pieces.Add(go);
            var renderer=go.AddComponent<SpriteRenderer>(); SpriteFactory.Setup(renderer,sprites[id],Color.white,order);
            Fit(renderer,sprites[id],size); return renderer;
        }
        static void Fit(SpriteRenderer renderer,Sprite sprite,Vector2 size)
        {
            renderer.sprite=sprite;
            renderer.transform.localScale=new Vector3(size.x/sprite.bounds.size.x,size.y/sprite.bounds.size.y,1);
        }
        SpriteRenderer Attach(string id,SpriteRenderer carrier,Vector2 size,int order)
        {
            var renderer=Place(id,carrier.transform.position,size,order);
            // Preserve world size when becoming a child of the scaled placeholder. Movement stays in DungeonPresentation.
            renderer.transform.SetParent(carrier.transform,true); return renderer;
        }
        void Dock(Vector2 centre)
        {
            var source=sprites[Id(8)]; var rect=source.rect;
            // The source's left pier and lower edge form an L-shaped quay; the original channel stays visible.
            var left=Sprite.Create(source.texture,new Rect(rect.x,rect.y,rect.width*.18f,rect.height),new Vector2(.5f,.5f),128,0,SpriteMeshType.FullRect);
            var lower=Sprite.Create(source.texture,new Rect(rect.x+rect.width*.18f,rect.y,rect.width*.82f,rect.height*.18f),new Vector2(.5f,.5f),128,0,SpriteMeshType.FullRect);
            owned.Add(left); owned.Add(lower); sprites.Add("DockPier",left); sprites.Add("DockEdge",lower);
            Place("DockPier",centre+Vector2.left*(2.5f*DungeonLayout.CellSize),new Vector2(1,6)*DungeonLayout.CellSize,-6);
            Place("DockEdge",centre+new Vector2(-3,-2)*DungeonLayout.CellSize,new Vector2(2,.5f)*DungeonLayout.CellSize,-6);
        }
        void BuildPieces()
        {
            var stair=DungeonLayout.ExitPositions[0]; var well=DungeonLayout.ExitPositions[1]; var canal=DungeonLayout.ExitPositions[2];
            Place(Id(1),stair+Vector2.up,new Vector2(3,3),-6);
            gate=Place(Id(2)+"_f01",stair,new Vector2(3,.3f),-3);
            Place(Id(3),DungeonContent.Mechanism(0),Vector2.one*.8f,-5);
            Place(Id(4),well,Vector2.one*4,-5);
            Attach(Id(5),Old("CoveirosPlatform"),Vector2.one*2.5f,-6);
            Place(Id(6),DungeonContent.Mechanism(1),Vector2.one*.8f,-5);
            installedCrank=Place(Id(13),DungeonContent.Mechanism(1)+new Vector2(.64f,.23f),Vector2.one*.55f,-4).gameObject;
            installedCrank.SetActive(false);
            // Ropes follow the two existing well edges, outside the central lift deck.
            Place(Id(7,1),well+Vector2.left*1.8f,new Vector2(.4f,3.18f),-4);
            Place(Id(7,2),well+Vector2.right*1.8f,new Vector2(.4f,3.18f),-4);
            StoreCounterweight();
            Dock(canal);
            boatDeck=Attach(Id(9),Old("FuneralBoat"),new Vector2(4,2)*DungeonLayout.CellSize,-6);
            tether=Place(Id(10),DungeonContent.Mechanism(2)+Vector2.right*.7f,new Vector2(2,.16f),-3);
            // The canal's tether is itself its physical release mechanism; its old marker becomes a simple mooring fitting.
            Place(Id(3),DungeonContent.Mechanism(2),Vector2.one*.8f,-5);
            latch=Place(Id(11)+"_f01",DungeonContent.Mechanism(3),LockSize(),-5);
            var crossing=Old("OpenedCanalCrossing");
            Attach(Id(12,2),crossing,DungeonLayout.WorldRect(DungeonLayout.CanalBridge).size,-6);
            // Retracted deck is on the northern landing, beside its control.
            stowed=Place(Id(12),crossing.transform.position+Vector3.up*(5*DungeonLayout.CellSize),new Vector2(4,2)*DungeonLayout.CellSize,-6).gameObject;
            for(int exit=0;exit<3;exit++) Waymark(exit);
        }
        void StoreCounterweight()
        {
            var r=DungeonLayout.Rooms[18].Cells;
            var occupied=transform.parent.GetComponentsInChildren<SpriteRenderer>();
            for(int side=0;side<2;side++) for(int x=r.xMin;x<r.xMax;x++)
            {
                var cells=new RectInt(x,side==0 ? r.yMax : r.yMin-3,1,3); bool solid=true;
                foreach(var c in cells.allPositionsWithin)
                    if(DungeonLayout.Index(c.x,c.y)<0 || DungeonLayout.IsBaseFloor(c.x,c.y) || DungeonLayout.IsWater(c.x,c.y) || DungeonLayout.IsLowCover(c.x,c.y)) { solid=false; break; }
                if(!solid) continue;
                var rect=DungeonLayout.WorldRect(cells); var bounds=new Bounds(rect.center,rect.size+Vector2.one*.08f);
                foreach(var renderer in occupied)
                    if(renderer.name.StartsWith("CAT-",StringComparison.Ordinal) && renderer.bounds.Intersects(bounds)) { solid=false; break; }
                if(!solid) continue;
                Place(Id(7,3),rect.center,rect.size,-4); return;
            }
        }
        void Waymark(int exit)
        {
            var origin=DungeonLayout.ExitPositions[exit];
            var offsets=new[]{Vector2.down*2.5f,Vector2.right*2.5f,Vector2.left*2.5f};
            foreach(var offset in offsets)
            {
                var point=origin+offset; int index=DungeonLayout.CellAt(point);
                if(index<0) continue;
                int x=index%DungeonLayout.GridWidth,y=index/DungeonLayout.GridWidth;
                point=DungeonLayout.ToWorld(new Vector2(x+.5f,y+.5f));
                if(!DungeonLayout.IsBaseFloor(x,y) || DungeonLayout.IsLowCover(x,y) || DungeonLayout.IsWater(x,y)) continue;
                bool clear=true;
                for(int i=0;i<4;i++) if((point-DungeonContent.Mechanism(i)).sqrMagnitude<2.25f) clear=false;
                foreach(var loot in DungeonContent.Loot) if((point-loot.Position).sqrMagnitude<2.25f) clear=false;
                if(!clear) continue;
                Place(Id(17,exit+1),point,Vector2.one*DungeonLayout.CellSize,-7); return;
            }
        }
        void Commit()
        {
            foreach(var renderer in originals)
                if(renderer.name=="CortejoStep" || renderer.name=="CoveirosPlatform" || renderer.name=="FuneralBoat" ||
                   renderer.name=="CortejoGrade" || renderer.name=="FuneralChain" || renderer.name=="OpenedCanalCrossing" || renderer.name.StartsWith("DungeonMechanism_",StringComparison.Ordinal)) renderer.enabled=false;
        }
        public void Apply(int readyMask,bool shortcut,float sailing)
        {
            float targetGate=(readyMask&1)!=0 ? 7 : 0,targetLock=shortcut ? 7 : 0;
            if(!synchronized) { gateFrame=targetGate; lockFrame=targetLock; synchronized=true; }
            else { gateFrame=Mathf.MoveTowards(gateFrame,targetGate,Time.unscaledDeltaTime*12); lockFrame=Mathf.MoveTowards(lockFrame,targetLock,Time.unscaledDeltaTime*12); }
            var gateSprite=gateFrames[Mathf.RoundToInt(gateFrame)];
            if(gate.sprite!=gateSprite) Fit(gate,gateSprite,new Vector2(3,.3f));
            var lockSprite=lockFrames[Mathf.RoundToInt(lockFrame)];
            if(latch.sprite!=lockSprite) Fit(latch,lockSprite,LockSize());
            var chainSprite=chainPoses[(readyMask&4)!=0 ? 1 : 0];
            if(tether.sprite!=chainSprite) Fit(tether,chainSprite,new Vector2(2,.16f));
            installedCrank.SetActive((readyMask&2)!=0); stowed.SetActive(!shortcut);
            boatDeck.color=new Color(1,1,1,1-sailing*.85f);
        }
        Vector2 LockSize() => new Vector2(.8f,.8f*lockFrames[0].rect.height/lockFrames[0].rect.width);
        void OnDestroy()
        {
            // Some visual children belong to the moving carriers, outside this root.
            foreach(var piece in pieces) if(piece!=null) Destroy(piece);
            foreach(var sprite in owned) if(sprite!=null) Destroy(sprite);
        }
    }
}
