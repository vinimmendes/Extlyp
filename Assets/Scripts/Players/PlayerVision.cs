using UnityEngine;
using System.Collections.Generic;
using CyberExtraction.Core;
using CyberExtraction.Combat;
using CyberExtraction.Match;

namespace CyberExtraction.Players
{
    // Local presentation only. Physics queries remain the source of gameplay sight.
    public class PlayerVision : MonoBehaviour
    {
        const float OuterRadius=40f, DarknessAlpha=.92f;
        const float LookupInterval=.08f, MovementThreshold=.08f;
        // Show only the one-cell contour actually drawn as the wall. Extending
        // through deeper masonry creates a lit patch past its visible rear edge.
        // This is cosmetic only, never a gameplay sight allowance.
        const int DungeonWallFaceCells=1;
        PlayerController pc;
        PlayerInventory inv;
        PlayerShooter shooter;
        Camera viewCamera;
        GameObject darkness;
        Mesh mesh;
        MeshRenderer fogRenderer;
        Material fogMaterial;
        Texture2D visibilityLookup;
        Texture2D wallFaceMap;
        Color32[] lookupPixels;
        readonly Dictionary<Collider2D,byte> lookupSurfaces=new Dictionary<Collider2D,byte>();
        Vector2 lookupOrigin;
        float nextLookupTime,lookupRange;
        bool lookupReady;
        int rayCount;
        LineRenderer lineL,lineR;
        readonly Vector3[] vertices=new Vector3[4];

        private void Awake()
        {
            pc=GetComponent<PlayerController>();
            inv=GetComponent<PlayerInventory>();
            shooter=GetComponent<PlayerShooter>();
            viewCamera=Camera.main;
            var shader=Resources.Load<Shader>("VisionSurface");
            if(shader==null)
            {
                Debug.LogError("VisionSurface shader ausente.");
                enabled=false;
                return;
            }
            rayCount=Application.isMobilePlatform ? 128 : 192;
            lookupPixels=new Color32[rayCount];
            visibilityLookup=new Texture2D(rayCount,1,TextureFormat.RGBA32,false,true);
            visibilityLookup.name="PlayerVisionLookup";
            visibilityLookup.filterMode=FilterMode.Bilinear;
            visibilityLookup.wrapModeU=TextureWrapMode.Repeat;
            visibilityLookup.wrapModeV=TextureWrapMode.Clamp;
            fogMaterial=new Material(shader);
            fogMaterial.SetTexture("_VisibilityLookup",visibilityLookup);
            fogMaterial.SetFloat("_RayCount",rayCount);
            fogMaterial.SetFloat("_VisionRange",GameConfig.VisionRange);
            fogMaterial.SetFloat("_NearRadius",GameConfig.NearRadius);
            fogMaterial.SetFloat("_VisionHalfAngle",inv.VisionAngle*.5f*Mathf.Deg2Rad);
            fogMaterial.SetFloat("_DarknessAlpha",DarknessAlpha);
            darkness=new GameObject("Darkness");
            var filter=darkness.AddComponent<MeshFilter>();
            fogRenderer=darkness.AddComponent<MeshRenderer>();
            fogRenderer.sharedMaterial=fogMaterial;
            // Capture Default world layers before this overlay samples their color.
            fogRenderer.sortingLayerName="VisionOverlay";
            fogRenderer.sortingOrder=100;
            mesh=new Mesh();
            mesh.MarkDynamic();
            mesh.vertices=vertices;
            mesh.triangles=new[]{0,1,2,0,2,3};
            filter.sharedMesh=mesh;
            lineL=MakeLine("AimL");
            lineR=MakeLine("AimR");
        }

        LineRenderer MakeLine(string name)
        {
            var line=new GameObject(name).AddComponent<LineRenderer>();
            line.useWorldSpace=true;
            line.positionCount=2;
            line.startWidth=line.endWidth=.05f;
            line.startColor=line.endColor=new Color(1,1,1,.5f);
            line.sortingLayerName="VisionOverlay";
            line.sortingOrder=101;
            line.sharedMaterial=SpriteFactory.Unlit;
            return line;
        }

        private void OnEnable()
        {
            SceneGraphics.Changed+=ApplySceneGraphics;
            ApplySceneGraphics();
        }

        void ApplySceneGraphics()
        {
            if(fogMaterial!=null) fogMaterial.SetFloat("_Pixelated",SceneGraphics.Mode==SceneGraphicsMode.Pixelated ? 1 : 0);
        }

        private void OnDisable()
        {
            SceneGraphics.Changed-=ApplySceneGraphics;
            if(fogRenderer!=null) fogRenderer.enabled=false;
            if(lineL!=null) lineL.enabled=false;
            if(lineR!=null) lineR.enabled=false;
            lookupReady=false;
        }

        private void OnDestroy()
        {
            if(mesh!=null) Destroy(mesh);
            if(fogMaterial!=null) Destroy(fogMaterial);
            if(visibilityLookup!=null) Destroy(visibilityLookup);
            if(wallFaceMap!=null) Destroy(wallFaceMap);
            if(darkness!=null) Destroy(darkness);
            if(lineL!=null) Destroy(lineL.gameObject);
            if(lineR!=null) Destroy(lineR.gameObject);
        }

        private void LateUpdate()
        {
            var match=MatchManager.Instance;
            bool running=match!=null && match.CurrentState==MatchState.Running;
            fogRenderer.enabled=running;
            if(!running)
            {
                lineL.enabled=lineR.enabled=false;
                lookupReady=false;
                return;
            }
            Vector2 origin=transform.position;
            float facing=transform.eulerAngles.z;
            if(!lookupReady || lookupRange!=GameConfig.VisionRange || Time.unscaledTime>=nextLookupTime ||
                (origin-lookupOrigin).sqrMagnitude>=MovementThreshold*MovementThreshold)
                RefreshLookup(origin);
            BuildOverlay(origin,facing);
            UpdateAimLines(origin,facing);
            UpdateTargets(origin,facing,inv.VisionAngle*.5f);
        }

        void RefreshLookup(Vector2 origin)
        {
            bool dungeon=MapBuilder.Current==ExpeditionMap.Dungeon;
            fogMaterial.SetFloat("_WallFaceMapEnabled",dungeon ? 1 : 0);
            if(dungeon) PrepareWallFaceMap();
            // World-fixed angles prevent rotating the flashlight from rotating the
            // sampling grid over a stationary wall. Reuse the same pixels and texture.
            lookupSurfaces.Clear();
            for(int i=0;i<rayCount;i++)
            {
                float angle=i*Mathf.PI*2/rayCount;
                Vector2 direction=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
                var hit=Physics2D.Raycast(origin,direction,GameConfig.VisionRange,GameConfig.VisionMask);
                float distance=GameConfig.VisionRange;
                float faceDistance=distance;
                byte surfaceId=0;
                if(hit.collider!=null)
                {
                    // Gameplay uses first-hit linecasts, never these fog pixels.
                    if(!lookupSurfaces.TryGetValue(hit.collider,out surfaceId))
                    {
                        surfaceId=(byte)(lookupSurfaces.Count+1); // At most rayCount <= 192.
                        lookupSurfaces.Add(hit.collider,surfaceId);
                    }
                    // Keep the first face for all floor pixels. Dungeon masonry
                    // gets a separate distance, applied ONLY to solid wall pixels.
                    distance=dungeon ? hit.distance :
                        Mathf.Clamp(WallExitDistance(hit.collider,origin,direction),hit.distance,GameConfig.VisionRange);
                    faceDistance=dungeon ? DungeonWallFaceDistance(origin,direction,hit.distance,hit.normal) : distance;
                }
                byte value=(byte)Mathf.RoundToInt(Mathf.Clamp01(distance/GameConfig.VisionRange)*255);
                byte faceValue=(byte)Mathf.RoundToInt(Mathf.Clamp01(faceDistance/GameConfig.VisionRange)*255);
                lookupPixels[i]=new Color32(value,faceValue,surfaceId,255);
            }
            visibilityLookup.SetPixels32(lookupPixels);
            visibilityLookup.Apply(false,false);
            lookupOrigin=origin;
            lookupRange=GameConfig.VisionRange;
            nextLookupTime=Time.unscaledTime+LookupInterval;
            lookupReady=true;
        }

        static bool DungeonMasonry(int x,int y) => DungeonLayout.Index(x,y)>=0 &&
            !DungeonLayout.IsBaseFloor(x,y) && !DungeonLayout.IsLowCover(x,y) && !DungeonLayout.IsWater(x,y);

        void PrepareWallFaceMap()
        {
            if(wallFaceMap!=null) return;
            int width=DungeonLayout.GridWidth,height=DungeonLayout.GridHeight;
            var pixels=new Color32[width*height];
            for(int y=0;y<height;y++) for(int x=0;x<width;x++)
                pixels[y*width+x]=DungeonMasonry(x,y) ? new Color32(255,255,255,255) : new Color32(0,0,0,255);
            wallFaceMap=new Texture2D(width,height,TextureFormat.RGBA32,false,true);
            wallFaceMap.name="DungeonWallFaces";
            wallFaceMap.filterMode=FilterMode.Point; wallFaceMap.wrapMode=TextureWrapMode.Clamp;
            wallFaceMap.SetPixels32(pixels); wallFaceMap.Apply(false,true);
            fogMaterial.SetTexture("_WallFaceMap",wallFaceMap);
            Vector2 min=DungeonLayout.ToWorld(Vector2.zero);
            fogMaterial.SetVector("_WallFaceBounds",new Vector4(min.x,min.y,
                width*DungeonLayout.CellSize,height*DungeonLayout.CellSize));
        }

        static float DungeonWallFaceDistance(Vector2 origin,Vector2 direction,float entry,Vector2 normal)
        {
            const float epsilon=.001f;
            Vector2 point=origin+direction*(entry+epsilon);
            float size=DungeonLayout.CellSize;
            int x=Mathf.FloorToInt(point.x/size+64),y=Mathf.FloorToInt(point.y/size+64);
            if(!DungeonMasonry(x,y)) return entry;
            float limit=Mathf.Min(GameConfig.VisionRange,entry+DungeonWallFaceCells*size/
                Mathf.Max(.001f,Mathf.Abs(Vector2.Dot(direction,normal))));
            int sx=direction.x>=0 ? 1 : -1,sy=direction.y>=0 ? 1 : -1;
            float dx=Mathf.Abs(direction.x)>.00001f ? size/Mathf.Abs(direction.x) : float.PositiveInfinity;
            float dy=Mathf.Abs(direction.y)>.00001f ? size/Mathf.Abs(direction.y) : float.PositiveInfinity;
            Vector2 edge=DungeonLayout.ToWorld(new Vector2(x+(sx>0 ? 1 : 0),y+(sy>0 ? 1 : 0)));
            float tx=float.IsPositiveInfinity(dx) ? dx : (edge.x-origin.x)/direction.x;
            float ty=float.IsPositiveInfinity(dy) ? dy : (edge.y-origin.y)/direction.y;
            // Walk only the first contiguous masonry band. Stop at ANY floor,
            // water or low cover; never continue through a room to its far wall.
            for(int i=0;i<DungeonLayout.GridWidth+DungeonLayout.GridHeight;i++)
            {
                float next=Mathf.Min(tx,ty);
                if(next>=limit) return limit;
                bool crossX=tx<=ty,crossY=ty<=tx;
                if(crossX) { x+=sx; tx+=dx; }
                if(crossY) { y+=sy; ty+=dy; }
                if(!DungeonMasonry(x,y)) return Mathf.Clamp(next,entry,limit);
            }
            return entry;
        }

        static float WallExitDistance(Collider2D collider,Vector2 origin,Vector2 direction)
        {
            if(collider is BoxCollider2D box)
            {
                // Work in collider space. A rotated wall's world AABB includes empty
                // space past its real surface, producing apparent light through it.
                Vector2 localOrigin=box.transform.InverseTransformPoint(origin);
                Vector2 localDirection=box.transform.InverseTransformVector(direction);
                Vector2 min=box.offset-box.size*.5f, max=box.offset+box.size*.5f;
                float x=Mathf.Abs(localDirection.x)>.0001f ?
                    ((localDirection.x>0 ? max.x : min.x)-localOrigin.x)/localDirection.x : float.PositiveInfinity;
                float y=Mathf.Abs(localDirection.y)>.0001f ?
                    ((localDirection.y>0 ? max.y : min.y)-localOrigin.y)/localDirection.y : float.PositiveInfinity;
                return Mathf.Min(x,y);
            }
            var bounds=collider.bounds;
            float exitX=Mathf.Abs(direction.x)>.0001f ?
                ((direction.x>0 ? bounds.max.x : bounds.min.x)-origin.x)/direction.x : float.PositiveInfinity;
            float exitY=Mathf.Abs(direction.y)>.0001f ?
                ((direction.y>0 ? bounds.max.y : bounds.min.y)-origin.y)/direction.y : float.PositiveInfinity;
            return Mathf.Min(exitX,exitY);
        }

        void BuildOverlay(Vector2 origin,float facing)
        {
            if(viewCamera==null) viewCamera=Camera.main;
            float outer=viewCamera!=null ? Mathf.Max(OuterRadius,
                viewCamera.orthographicSize*(Mathf.Sqrt(1+viewCamera.aspect*viewCamera.aspect)+1)) : OuterRadius;
            vertices[0]=origin+new Vector2(-outer,-outer);
            vertices[1]=origin+new Vector2(outer,-outer);
            vertices[2]=origin+new Vector2(outer,outer);
            vertices[3]=origin+new Vector2(-outer,outer);
            mesh.vertices=vertices;
            mesh.bounds=new Bounds(origin,new Vector3(outer*2+2,outer*2+2,10));
            fogMaterial.SetVector("_VisionOrigin",new Vector4(origin.x,origin.y,facing*Mathf.Deg2Rad,0));
            fogMaterial.SetVector("_LookupOrigin",new Vector4(lookupOrigin.x,lookupOrigin.y,0,0));
            fogMaterial.SetFloat("_VisionRange",GameConfig.VisionRange);
            fogMaterial.SetFloat("_VisionHalfAngle",inv.VisionAngle*.5f*Mathf.Deg2Rad);
        }

        void UpdateAimLines(Vector2 o, float facing)
        {
            var mm = MatchManager.Instance;
            bool show = mm != null && mm.CurrentState == MatchState.Running && inv != null && inv.IsAlive &&
                shooter != null && shooter.Weapon != null;
            Vector2 muzzle = shooter != null ? shooter.MuzzlePosition : o;
            // Do not draw aim beyond a physical obstacle between the body and barrel.
            if (show && Physics2D.Linecast(o, muzzle, GameConfig.WallMask).collider != null) show = false;
            lineL.enabled = lineR.enabled = show;
            if (!show) return;

            float h = shooter.AimHalfAngle;
            float range = Mathf.Min(shooter.Range,GameConfig.VisionRange);
            SetLine(lineL, muzzle, facing - h,range);
            SetLine(lineR, muzzle, facing + h,range);
        }

        static void SetLine(LineRenderer lr, Vector2 o, float angleDeg,float range)
        {
            float r = angleDeg * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(r), Mathf.Sin(r));
            var hit = Physics2D.Raycast(o, dir, range, GameConfig.WallMask);
            float d = hit.collider != null ? hit.distance : range;
            lr.SetPosition(0, o);
            lr.SetPosition(1, o + dir * d);
        }

        void UpdateTargets(Vector2 o, float facing, float half)
        {
            foreach (var t in VisibilityTarget.All)
            {
                if (t == null) continue;
                if (t.transform == transform) { t.SetVisible(true); continue; }

                Vector2 p = t.transform.position;
                Vector2 to = p - o;
                float d = to.magnitude;
                float ang = Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg;
                bool inView = d <= inv.VisionRadiusAtAngle(Mathf.DeltaAngle(facing, ang));
                if (inView && Physics2D.Linecast(o, p, GameConfig.VisionMask)) inView = false;
                t.SetVisible(inView);
            }
        }
    }
}
