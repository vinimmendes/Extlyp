using System.Collections.Generic;
using UnityEngine;
using CyberExtraction.Core;
using CyberExtraction.Combat;
using CyberExtraction.Match;

namespace CyberExtraction.Players
{
    // So existe no jogador local. Faz tres coisas:
    // 1) escurece tudo fora do cone de visao (malha preta com buraco);
    // 2) desenha as duas linhas do cone de mira;
    // 3) esconde inimigos fora do cone.
    public class PlayerVision : MonoBehaviour
    {
        const float OuterRadius = 40f;
        const float DarknessAlpha = 0.92f;

        PlayerController pc;
        PlayerInventory inv;
        PlayerShooter shooter;
        Camera viewCamera;
        GameObject darkness;
        Mesh mesh;
        LineRenderer lineL, lineR;

        Vector3[] verts;
        Color[] cols;
        int[] tris;
        readonly Dictionary<SpriteRenderer,SpriteRenderer> visibleSurfaces=new Dictionary<SpriteRenderer,SpriteRenderer>();
        readonly HashSet<Collider2D> revealedColliders=new HashSet<Collider2D>();

        private void Awake()
        {
            pc = GetComponent<PlayerController>();
            inv = GetComponent<PlayerInventory>();
            shooter = GetComponent<PlayerShooter>();
            viewCamera=Camera.main;

            darkness = new GameObject("Darkness");
            var mf = darkness.AddComponent<MeshFilter>();
            var mr = darkness.AddComponent<MeshRenderer>();
            var m = SpriteFactory.Unlit;
            if (m != null) mr.sharedMaterial = m;
            mr.sortingOrder = 100;
            mesh = new Mesh();
            mesh.MarkDynamic();
            mf.sharedMesh = mesh;

            lineL = MakeLine("AimL");
            lineR = MakeLine("AimR");
        }

        LineRenderer MakeLine(string n)
        {
            var go = new GameObject(n);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = 2;
            lr.startWidth = lr.endWidth = 0.05f;
            lr.startColor = lr.endColor = new Color(1f, 1f, 1f, 0.5f);
            lr.sortingOrder = 101;
            var m = SpriteFactory.Unlit;
            if (m != null) lr.sharedMaterial = m;
            return lr;
        }

        private void OnDestroy()
        {
            if (mesh != null) Destroy(mesh);
            if (darkness != null) Destroy(darkness);
            if (lineL != null) Destroy(lineL.gameObject);
            if (lineR != null) Destroy(lineR.gameObject);
            foreach(var overlay in visibleSurfaces.Values) if(overlay!=null) Destroy(overlay.gameObject);
        }

        private void LateUpdate()
        {
            Vector2 o = transform.position;
            float facing = transform.eulerAngles.z;
            float half = GameConfig.VisionAngle * 0.5f;

            BuildDarkness(o, facing, half);
            UpdateAimLines(o, facing);
            UpdateTargets(o, facing, half);
        }

        void BuildDarkness(Vector2 o, float facing, float half)
        {
            foreach(var overlay in visibleSurfaces.Values) if(overlay!=null) overlay.enabled=false;
            revealedColliders.Clear();
            float outer=viewCamera!=null ? Mathf.Max(OuterRadius,viewCamera.orthographicSize*(Mathf.Sqrt(1+viewCamera.aspect*viewCamera.aspect)+1)) : OuterRadius;
            const int samples = 180, rings = 9;
            if (verts == null || verts.Length != samples * rings)
            {
                verts = new Vector3[samples * rings];
                cols = new Color[verts.Length];
                tris = new int[samples * (rings - 1) * 6];
                for (int j = 0; j < samples; j++)
                    for (int k = 0; k < rings - 1; k++)
                    {
                        int n = (j + 1) % samples;
                        int t = (j * (rings - 1) + k) * 6;
                        int v = j * rings + k, w = n * rings + k;
                        tris[t] = v; tris[t + 1] = v + 1; tris[t + 2] = w;
                        tris[t + 3] = w; tris[t + 4] = v + 1; tris[t + 5] = w + 1;
                    }
                mesh.Clear();
                mesh.vertices = verts;
                mesh.triangles = tris;
            }
            for (int j = 0; j < samples; j++)
            {
                float offset = -180f + 360f * j / samples;
                float angle = facing + offset;
                float r = angle * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(r), Mathf.Sin(r));
                float radius = WallDistance(o, angle, GameConfig.VisionRadiusAtAngle(offset));
                for (int k = 0; k < rings; k++)
                {
                    // Smooth inward falloff: no light is added past an occluding wall.
                    float t = k == rings - 1 ? 1f : (float)k / (rings - 2);
                    float distance = k == rings - 1 ? outer : radius * Mathf.Lerp(0.65f, 1f, t);
                    int v = j * rings + k;
                    verts[v] = o + dir * distance;
                    cols[v] = new Color(0f, 0f, 0f, DarknessAlpha * Mathf.SmoothStep(0f, 1f, t));
                }
            }
            mesh.vertices = verts;
            mesh.colors = cols;
            mesh.bounds = new Bounds(o, new Vector3(outer*2+2, outer*2+2, 10f));
        }

        float WallDistance(Vector2 o, float angleDeg, float maxDist)
        {
            float r = angleDeg * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(r), Mathf.Sin(r));
            var hit = Physics2D.Raycast(o, dir, maxDist, GameConfig.VisionMask);
            if (hit.collider == null) return maxDist;
            if(!revealedColliders.Add(hit.collider)) return Mathf.Min(maxDist,hit.distance);
            // Reveal the encountered surface above the darkness, never its surroundings.
            var fixture=hit.collider.GetComponent<FurnitureObstacle>();
            var surface=fixture!=null && fixture.Container!=null ? fixture.Container.gameObject : hit.collider.gameObject;
            foreach(var source in surface.GetComponentsInChildren<SpriteRenderer>())
            {
                if(!source.enabled || source.sprite==null || source.name=="VisionSurface") continue;
                if(!visibleSurfaces.TryGetValue(source,out var overlay) || overlay==null)
                {
                    var go=new GameObject("VisionSurface"); go.transform.SetParent(source.transform,false);
                    overlay=go.AddComponent<SpriteRenderer>(); visibleSurfaces[source]=overlay;
                }
                overlay.enabled=true; overlay.sprite=source.sprite; overlay.sharedMaterial=source.sharedMaterial;
                overlay.sortingLayerID=source.sortingLayerID; overlay.sortingOrder=102;
                overlay.color=source.color; overlay.flipX=source.flipX; overlay.flipY=source.flipY;
            }
            return Mathf.Min(maxDist, hit.distance);
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

            float h = shooter.Weapon != null ? shooter.Weapon.HalfAngle(pc.Speed01) : GameConfig.AimHalfAngleStill;
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
                bool inView = d <= GameConfig.VisionRadiusAtAngle(Mathf.DeltaAngle(facing, ang));
                if (inView && Physics2D.Linecast(o, p, GameConfig.VisionMask)) inView = false;
                t.SetVisible(inView);
            }
        }
    }
}
