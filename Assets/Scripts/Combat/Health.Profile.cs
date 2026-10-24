using System;
using System.Collections.Generic;
using CyberExtraction.Camp;
using CyberExtraction.Enemies;
using CyberExtraction.Items;
using CyberExtraction.Players;
using UnityEngine;

namespace CyberExtraction.Combat
{
    // Captured at attack creation/impact; does not read an attacker's later equipped weapon.
    public struct DamageOrigin
    {
        public string Session,Expedition,Name;
        public ProfileSpecies Species;
        public ItemId Weapon;
        public static DamageOrigin Player(PlayerInventory player,ItemId weapon)
        {
            if(player==null) return Creature(ProfileSpecies.Environment);
            return new DamageOrigin { Species=player.NetworkObject.IsPlayerObject ? ProfileSpecies.Human : ProfileSpecies.Bot,
                Name=player.DisplayName,Weapon=weapon,Session=player.ProgressionEligible ? player.ProgressionSession : null,
                Expedition=player.ProgressionEligible ? player.ProgressionExpedition : null };
        }
        public static DamageOrigin Creature(ProfileSpecies species)=>new DamageOrigin {Species=species,Weapon=(ItemId)255};
        public static ProfileSpecies Identify(GameObject target)
        {
            var player=target.GetComponent<PlayerInventory>();
            if(player!=null) return player.NetworkObject.IsPlayerObject ? ProfileSpecies.Human : ProfileSpecies.Bot;
            if(target.GetComponent<Esporifero>()!=null) return ProfileSpecies.Spore;
            var plant=target.GetComponent<PlantCreature>();
            if(plant!=null) return (ProfileSpecies)((int)ProfileSpecies.PlantMother+(int)plant.CurrentKind);
            var monster=target.GetComponent<Monster>();
            if(monster!=null) return (ProfileSpecies)((int)ProfileSpecies.Charger+(int)monster.Kind);
            return ProfileSpecies.Unknown;
        }
    }
    public partial class Health
    {
        struct ProfileContribution { public DamageOrigin Origin; public double At; }
        readonly List<ProfileContribution> profileContributions=new List<ProfileContribution>();
        bool profileDeathQueued;
        void TrackProfileDamage(DamageOrigin origin,int damage)
        {
            double now=Time.timeAsDouble;
            profileContributions.RemoveAll(c=>c.At<now-15);
            var target=DamageOrigin.Identify(gameObject);
            bool recognized=target>=ProfileSpecies.Charger || target==ProfileSpecies.Human || target==ProfileSpecies.Bot;
            if(!string.IsNullOrEmpty(origin.Session) && recognized && (armorReceiver==null || armorReceiver.ProgressionSession!=origin.Session))
            {
                profileContributions.Add(new ProfileContribution {Origin=origin,At=now});
                CampRepository.QueueProfile(new ProfileEvent {Session=origin.Session,Expedition=origin.Expedition,
                    DamageCreatures=target>=ProfileSpecies.Charger ? damage : 0,
                    DamageParticipants=target==ProfileSpecies.Human || target==ProfileSpecies.Bot ? damage : 0},true);
            }
            if(armorReceiver!=null && armorReceiver.ProgressionActive)
                CampRepository.QueueProfile(new ProfileEvent {Session=armorReceiver.ProgressionSession,Expedition=armorReceiver.ProgressionExpedition,DamageReceived=damage},true);
        }
        void RecordProfileDeath(DamageOrigin origin)
        {
            if(profileDeathQueued) return;
            var target=DamageOrigin.Identify(gameObject);
            if(target==ProfileSpecies.Unknown) return;
            bool self=armorReceiver!=null && origin.Session==armorReceiver.ProgressionSession;
            if(armorReceiver!=null && armorReceiver.ProgressionActive)
                CampRepository.QueueProfile(new ProfileEvent {Session=armorReceiver.ProgressionSession,Expedition=armorReceiver.ProgressionExpedition,
                    Key=experienceDeathId,Death=true,DeathSource=origin.Species,KillerName=origin.Name});
            if(!self && !string.IsNullOrEmpty(origin.Session))
                CampRepository.QueueProfile(new ProfileEvent {Session=origin.Session,Expedition=origin.Expedition,Key=experienceDeathId,
                    Kill=true,Target=target,Weapon=origin.Weapon});
            var assisted=new HashSet<string>();
            foreach(var hit in profileContributions)
            {
                var actor=hit.Origin;
                if(hit.At<Time.timeAsDouble-15 || actor.Session==origin.Session || !assisted.Add(actor.Session) ||
                    armorReceiver!=null && actor.Session==armorReceiver.ProgressionSession) continue;
                CampRepository.QueueProfile(new ProfileEvent {Session=actor.Session,Expedition=actor.Expedition,Key=experienceDeathId,Assist=true,Target=target});
            }
            profileDeathQueued=true;
            TryFlushProfile();
        }
        static void TryFlushProfile()
        {
            try { CampRepository.FlushPendingExperience(); }
            catch(Exception error) { Debug.LogError("Perfil enfileirado; gravação será repetida: "+error.Message); }
        }
    }
}
