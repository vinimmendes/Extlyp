using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using CyberExtraction.Core;
using CyberExtraction.Combat;
using CyberExtraction.Match;

namespace CyberExtraction.Enemies
{
    [RequireComponent(typeof(NetworkObject), typeof(SpriteRenderer))]
    public class SporeCloud : NetworkBehaviour
    {
        public static readonly List<SporeCloud> All = new List<SporeCloud>();
        public NetworkVariable<Vector2> Center = new NetworkVariable<Vector2>();
        public NetworkVariable<Vector2> Wind = new NetworkVariable<Vector2>();
        public NetworkVariable<double> BornAt = new NetworkVariable<double>();
        SpriteRenderer visual;

        // Called on the server before Spawn, so initial values arrive with the spawn.
        public void ServerInitialize(Vector2 origin, Vector2 wind)
        {
            Center.Value = origin;
            Wind.Value = wind;
            BornAt.Value = Unity.Netcode.NetworkManager.Singleton.ServerTime.Time;
        }

        public override void OnNetworkSpawn()
        {
            All.Add(this);
            visual = GetComponent<SpriteRenderer>();
            SpriteFactory.Setup(visual, SpriteFactory.Circle, new Color(.42f, .43f, .17f, .32f), 3);
        }

        public override void OnNetworkDespawn() { All.Remove(this); }

        void FixedUpdate()
        {
            if (!IsSpawned || !IsServer) return;
            var match = MatchManager.Instance;
            if (match == null || match.CurrentState != MatchState.Running ||
                NetworkManager.ServerTime.Time - BornAt.Value >= SporeRules.CloudSeconds)
            {
                NetworkObject.Despawn();
                return;
            }
            Vector2 next = Center.Value + Wind.Value * Time.fixedDeltaTime;
            // The cloud cannot drift through a wall or contaminate across one.
            if (!Physics2D.Linecast(Center.Value, next, GameConfig.WallMask))
                Center.Value = next;
            else Wind.Value = Vector2.zero;
        }

        public bool Contains(Vector2 point) =>
            (point - Center.Value).sqrMagnitude <= SporeRules.CloudRadius * SporeRules.CloudRadius &&
            !Physics2D.Linecast(Center.Value, point, GameConfig.WallMask);

        void LateUpdate()
        {
            if (!IsSpawned || visual == null) return;
            transform.position = Center.Value;
            float diameter = SporeRules.CloudRadius * 2f;
            transform.localScale = new Vector3(diameter, diameter, 1);
            float age = (float)(NetworkManager.ServerTime.Time - BornAt.Value);
            float fade = Mathf.Clamp01(age / .3f) *
                Mathf.Clamp01((SporeRules.CloudSeconds - age) / 1f);
            visual.color = new Color(.42f, .43f, .17f, .32f * fade);
        }
    }
}
