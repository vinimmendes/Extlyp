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
        const float ConeStep = 2f;
        const float NearStep = 10f;
        const float DarknessAlpha = 0.92f;

        PlayerController pc;
        PlayerInventory inv;
        GameObject darkness;
        Mesh mesh;
        LineRenderer lineL, lineR;

        readonly List<float> angles = new List<float>();
        readonly List<float> dists = new List<float>();
        Vector3[] verts;
        Color[] cols;
        int[] tris;

        private void Awake()
        {
            pc = GetComponent<PlayerController>();
            inv = GetComponent<PlayerInventory>();

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
            if (darkness != null) Destroy(darkness);
            if (lineL != null) Destroy(lineL.gameObject);
            if (lineR != null) Destroy(lineR.gameObject);
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
            angles.Clear();
            dists.Clear();

            int ck = Mathf.CeilToInt(GameConfig.VisionAngle / ConeStep);
            for (int i = 0; i <= ck; i++)
            {
                float a = facing - half + GameConfig.VisionAngle * i / ck;
                angles.Add(a);
                dists.Add(WallDistance(o, a, GameConfig.VisionRange));
            }

            float span = 360f - GameConfig.VisionAngle;
            int nk = Mathf.CeilToInt(span / NearStep);
            for (int i = 0; i <= nk; i++)
            {
                float a = facing + half + span * i / nk;
                angles.Add(a);
                dists.Add(WallDistance(o, a, GameConfig.NearRadius));
            }

            int m = angles.Count;
            if (verts == null || verts.Length != m * 2)
            {
                verts = new Vector3[m * 2];
                cols = new Color[m * 2];
                tris = new int[m * 6];
                var c = new Color(0f, 0f, 0f, DarknessAlpha);
                for (int i = 0; i < cols.Length; i++) cols[i] = c;
                for (int j = 0; j < m; j++)
                {
                    int n = (j + 1) % m;
                    int k = j * 6;
                    tris[k] = 2 * j; tris[k + 1] = 2 * j + 1; tris[k + 2] = 2 * n;
                    tris[k + 3] = 2 * n; tris[k + 4] = 2 * j + 1; tris[k + 5] = 2 * n + 1;
                }
                mesh.Clear();
                mesh.vertices = verts;
                mesh.colors = cols;
                mesh.triangles = tris;
            }

            for (int j = 0; j < m; j++)
            {
                float r = angles[j] * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(r), Mathf.Sin(r));
                verts[2 * j] = o + dir * dists[j];
                verts[2 * j + 1] = o + dir * OuterRadius;
            }
            mesh.vertices = verts;
            mesh.bounds = new Bounds(o, new Vector3(200f, 200f, 10f));
        }

        static float WallDistance(Vector2 o, float angleDeg, float maxDist)
        {
            float r = angleDeg * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(r), Mathf.Sin(r));
            var hit = Physics2D.Raycast(o, dir, maxDist, GameConfig.WallMask);
            if (hit.collider == null) return maxDist;
            return Mathf.Min(maxDist, hit.distance + 0.35f);
        }

        void UpdateAimLines(Vector2 o, float facing)
        {
            var mm = MatchManager.Instance;
            bool show = mm != null && mm.CurrentState == MatchState.Running && inv != null && inv.IsAlive;
            lineL.enabled = lineR.enabled = show;
            if (!show) return;

            float h = Mathf.Lerp(GameConfig.AimHalfAngleStill, GameConfig.AimHalfAngleMoving, pc.Speed01);
            SetLine(lineL, o, facing - h);
            SetLine(lineR, o, facing + h);
        }

        static void SetLine(LineRenderer lr, Vector2 o, float angleDeg)
        {
            float r = angleDeg * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(r), Mathf.Sin(r));
            var hit = Physics2D.Raycast(o, dir, GameConfig.AimRange, GameConfig.WallMask);
            float d = hit.collider != null ? hit.distance : GameConfig.AimRange;
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
                bool inView = d <= GameConfig.NearRadius;
                if (!inView && d <= GameConfig.VisionRange)
                {
                    float ang = Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg;
                    inView = Mathf.Abs(Mathf.DeltaAngle(facing, ang)) <= half;
                }
                if (inView && Physics2D.Linecast(o, p, GameConfig.WallMask)) inView = false;
                t.SetVisible(inView);
            }
        }
    }
}
