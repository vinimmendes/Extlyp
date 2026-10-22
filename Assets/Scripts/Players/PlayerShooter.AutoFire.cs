using Unity.Netcode;
using UnityEngine;
using CyberExtraction.Combat;
using CyberExtraction.Core;
using CyberExtraction.Enemies;
using CyberExtraction.Match;

namespace CyberExtraction.Players
{
    public partial class PlayerShooter
    {
        readonly RaycastHit2D[] autoFireHits = new RaycastHit2D[64];

        // Apenas decide se o cliente deve solicitar um tiro normal. Nao corrige
        // a direcao nem resolve dano, municao ou cadencia fora do servidor.
        public bool HasAutomaticFireTarget(float angle)
        {
            if (!IsOwner || inv == null || !inv.IsAlive || inv.SwitchingWeapon || inv.ChangingEquipment ||
                inv.Lockpicking.Value || inv.DungeonAction.Value >= 0 || inv.FishingPhase.Value != 0 ||
                Weapon == null || Weapon.IsMelee || Magazine <= 0 || Reloading || CyclingBolt || MeleeAttacking ||
                MatchManager.Instance == null || MatchManager.Instance.CurrentState != MatchState.Running ||
                UI.UiLayout.BlocksGameplay || !GameInput.GameplayActive || float.IsNaN(angle) || float.IsInfinity(angle)) return false;

            Vector2 origin = MuzzlePosition;
            if (Physics2D.Linecast(transform.position, origin, GameConfig.WallMask).collider != null ||
                Physics2D.OverlapPoint(origin, GameConfig.WallMask) != null) return false;

            float radians = angle * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
            var filter = new ContactFilter2D { useTriggers = true };
            filter.SetLayerMask(Physics2D.DefaultRaycastLayers);
            int count = Physics2D.Raycast(origin, direction, filter, autoFireHits, Range);
            // Se o buffer lotar, nao decidir com uma lista potencialmente incompleta.
            if (count == autoFireHits.Length) return false;

            Health target = null;
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                var hit = autoFireHits[i];
                var collider = hit.collider;
                if (collider == null || collider.GetComponentInParent<NetworkObject>() == NetworkObject) continue;
                var monster = collider.GetComponentInParent<Monster>();
                var prop = collider.GetComponentInParent<DungeonBreakable>();
                bool breakable = prop != null && prop.IsSpawned && !prop.Broken.Value;
                if (!breakable && collider.isTrigger && (monster == null || !monster.Disguised)) continue;
                var health = collider.GetComponentInParent<Health>();
                if (!breakable && health != null && health.IsDead) continue;
                if (hit.distance >= nearest) continue;
                nearest = hit.distance;
                // Obstaculos e objetos quebraveis bloqueiam o tiro automatico.
                target = !breakable ? health : null;
            }

            if (target == null || !target.IsSpawned || target.IsDead) return false;
            var creature = target.GetComponent<Monster>();
            if (creature != null && creature.Disguised) return false;
            var visibility = target.GetComponent<VisibilityTarget>();
            return (visibility == null || !visibility.ForceHidden) &&
                ServerVisibility.CanSee(transform.position, angle, target.transform.position, inv);
        }
    }
}
