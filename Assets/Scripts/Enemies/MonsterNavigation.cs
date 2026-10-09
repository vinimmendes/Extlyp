using System.Collections.Generic;
using UnityEngine;
using CyberExtraction.Core;

namespace CyberExtraction.Enemies
{
    // A* no mapa fixo do MVP. Apenas paredes/caixas (WallMask) bloqueiam caminhos.
    // A grade considera o tamanho do monstro; cada aresta tambem e validada
    // com CircleCast, impedindo que diagonais cortem quinas.
    public sealed class MonsterNavigation
    {
        const float Cell = 0.5f;
        const float RepathInterval = 0.5f;
        Vector2 Min;
        const int Width = 61, Height = 61;
        bool initialized;

        readonly float radius;
        readonly bool[] walkable = new bool[Width * Height];
        readonly float[] costs = new float[Width * Height];
        readonly int[] parents = new int[Width * Height];
        readonly bool[] closed = new bool[Width * Height];
        readonly List<Vector2> path = new List<Vector2>();
        readonly List<Candidate> heap = new List<Candidate>();
        readonly Dictionary<long, bool> edges = new Dictionary<long, bool>();
        int waypoint;
        Vector2 progressPosition;
        float nextRepath, progressAt;

        struct Candidate
        {
            public int node;
            public float priority;
            public Candidate(int node, float priority) { this.node = node; this.priority = priority; }
        }

        public MonsterNavigation(float worldRadius)
        {
            radius = worldRadius + 0.04f;
        }

        public bool TryGetWaypoint(Vector2 from, Vector2 goal, out Vector2 next)
        {
            next = from;
            if (ClearSegment(from, goal))
            {
                path.Clear();
                waypoint = 0;
                next = goal;
                progressPosition = from;
                progressAt = Time.time;
                return true;
            }

            // Um corpo bloqueado por outro jogador pode nao progredir mesmo
            // com rota valida. Recalcula sem rodar A* a cada passo da fisica.
            if ((from - progressPosition).sqrMagnitude > 0.04f)
            {
                progressPosition = from;
                progressAt = Time.time;
            }
            else if (Time.time - progressAt > 1f)
            {
                nextRepath = 0f;
                progressAt = Time.time;
            }

            if (Time.time >= nextRepath)
            {
                FindPath(from, goal);
                nextRepath = Time.time + RepathInterval;
            }

            if (waypoint >= path.Count) return false;
            // Avanca apenas por segmentos livres para o corpo inteiro.
            int lookAhead = Mathf.Min(path.Count - 1, waypoint + 8);
            for (int i = lookAhead; i > waypoint; i--)
            {
                if (!ClearSegment(from, path[i])) continue;
                waypoint = i;
                break;
            }
            if ((path[waypoint] - from).sqrMagnitude < 0.01f)
            {
                waypoint++;
                if (waypoint >= path.Count) return false;
            }
            if (!ClearSegment(from, path[waypoint])) return false;
            next = path[waypoint];
            return true;
        }

        bool ClearSegment(Vector2 from, Vector2 to)
        {
            Vector2 delta = to - from;
            float distance = delta.magnitude;
            if (distance < 0.001f)
                return Physics2D.OverlapCircle(from, radius, GameConfig.WallMask) == null;
            return Physics2D.CircleCast(from, radius, delta / distance, distance, GameConfig.WallMask).collider == null;
        }

        Vector2 Position(int i) => Min + new Vector2(i % Width, i / Width) * Cell;

        int NearestNode(Vector2 point, bool requireConnection)
        {
            int best = -1;
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < walkable.Length; i++)
            {
                if (!walkable[i]) continue;
                float distance = (Position(i) - point).sqrMagnitude;
                if (distance >= bestDistance) continue;
                if (requireConnection && !ClearSegment(point, Position(i))) continue;
                best = i;
                bestDistance = distance;
            }
            return best;
        }

        bool EdgeIsClear(int a, int b)
        {
            int low = Mathf.Min(a, b), high = Mathf.Max(a, b);
            long key = (long)low * walkable.Length + high;
            if (!edges.TryGetValue(key, out bool clear))
            {
                clear = ClearSegment(Position(a), Position(b));
                edges[key] = clear;
            }
            return clear;
        }

        void FindPath(Vector2 from, Vector2 goal)
        {
            if (!initialized || from.x < Min.x + 5 || from.y < Min.y + 5 || from.x > Min.x + 25 || from.y > Min.y + 25)
            {
                Min = from - Vector2.one * 15;
                initialized = true; edges.Clear(); Physics2D.SyncTransforms();
                for (int i = 0; i < walkable.Length; i++)
                    walkable[i] = Physics2D.OverlapCircle(Position(i), radius, GameConfig.WallMask) == null;
            }
            path.Clear();
            waypoint = 0;
            heap.Clear();
            Vector2 localGoal = new Vector2(Mathf.Clamp(goal.x, Min.x + 1, Min.x + 29), Mathf.Clamp(goal.y, Min.y + 1, Min.y + 29));
            int start = NearestNode(from, true), end = NearestNode(localGoal, false);
            if (start < 0 || end < 0) return;

            for (int i = 0; i < costs.Length; i++)
            {
                costs[i] = float.PositiveInfinity;
                parents[i] = -1;
                closed[i] = false;
            }
            costs[start] = 0f;
            Push(start, Heuristic(start, end));
            while (heap.Count > 0)
            {
                int current = Pop();
                if (closed[current]) continue;
                if (current == end)
                {
                    for (int n = end; n >= 0; n = parents[n]) path.Add(Position(n));
                    path.Reverse();
                    // So inclui o destino exato se nao estiver dentro de um obstaculo.
                    if ((localGoal-goal).sqrMagnitude < .01f && ClearSegment(Position(end), goal)) path.Add(goal);
                    return;
                }
                closed[current] = true;
                int x = current % Width, y = current / Width;
                for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || ny < 0 || nx >= Width || ny >= Height) continue;
                    int neighbor = ny * Width + nx;
                    if (!walkable[neighbor] || closed[neighbor]) continue;
                    bool diagonal = dx != 0 && dy != 0;
                    if (diagonal && (!walkable[y * Width + nx] || !walkable[ny * Width + x])) continue;
                    float candidateCost = costs[current] + (diagonal ? 1.41421356f : 1f);
                    if (candidateCost >= costs[neighbor] || !EdgeIsClear(current, neighbor)) continue;
                    costs[neighbor] = candidateCost;
                    parents[neighbor] = current;
                    Push(neighbor, candidateCost + Heuristic(neighbor, end));
                }
            }
        }

        static float Heuristic(int a, int b)
        {
            int dx = Mathf.Abs(a % Width - b % Width), dy = Mathf.Abs(a / Width - b / Width);
            return dx + dy + (1.41421356f - 2f) * Mathf.Min(dx, dy);
        }

        void Push(int node, float priority)
        {
            var candidate = new Candidate(node, priority);
            int i = heap.Count;
            heap.Add(candidate);
            while (i > 0)
            {
                int parent = (i - 1) / 2;
                if (heap[parent].priority <= priority) break;
                heap[i] = heap[parent];
                i = parent;
            }
            heap[i] = candidate;
        }

        int Pop()
        {
            int result = heap[0].node;
            var last = heap[heap.Count - 1];
            heap.RemoveAt(heap.Count - 1);
            if (heap.Count == 0) return result;
            int i = 0;
            while (i * 2 + 1 < heap.Count)
            {
                int child = i * 2 + 1;
                if (child + 1 < heap.Count && heap[child + 1].priority < heap[child].priority) child++;
                if (last.priority <= heap[child].priority) break;
                heap[i] = heap[child];
                i = child;
            }
            heap[i] = last;
            return result;
        }

        public void DrawPath()
        {
            for (int i = waypoint; i + 1 < path.Count; i++)
                Gizmos.DrawLine(path[i], path[i + 1]);
        }
    }
}
