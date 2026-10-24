using System;
using System.Collections.Generic;
using CyberExtraction.Items;

namespace CyberExtraction.Camp
{
    public sealed class ProfileEvent
    {
        public string Session,Expedition,Key,KillerName;
        public ProfileSpecies Target,DeathSource;
        public ItemId Weapon=(ItemId)255;
        public long DamageCreatures,DamageParticipants,DamageReceived,Healing;
        public double Seconds;
        public bool Kill,Assist,Death;
        internal string Owner,Token;
        internal long Ticks;
    }
    public static partial class CampRepository
    {
        static readonly List<ProfileEvent> pendingProfile=new List<ProfileEvent>();
        internal static void QueueProfile(ProfileEvent item,bool metric=false)
        {
            if(item==null || string.IsNullOrEmpty(item.Session) || string.IsNullOrEmpty(item.Expedition) ||
                !heldLeases.TryGetValue(item.Session,out var owner)) return;
            if(metric)
            {
                var existing=pendingProfile.Find(e=>e.Session==item.Session && e.Expedition==item.Expedition && e.Owner==owner.Owner && e.Token==owner.Token && e.Key.StartsWith("metric:",StringComparison.Ordinal));
                if(existing!=null)
                { existing.DamageCreatures+=item.DamageCreatures; existing.DamageParticipants+=item.DamageParticipants; existing.DamageReceived+=item.DamageReceived; existing.Healing+=item.Healing; existing.Seconds+=item.Seconds; return; }
                item.Key="metric:"+Guid.NewGuid().ToString("N");
            }
            if(string.IsNullOrEmpty(item.Key) || item.Key.Length>96 || pendingProfile.Exists(e=>e.Session==item.Session && e.Expedition==item.Expedition && e.Key==item.Key)) return;
            item.Owner=owner.Owner; item.Token=owner.Token; item.Ticks=DateTime.UtcNow.Ticks; pendingProfile.Add(item);
        }
        static void ApplyPendingProfile(Dictionary<string,CampRecord> data,ProfileEvent[] batch)
        {
            foreach(var e in batch)
            {
                if(!data.TryGetValue(e.Session,out var record) || record.Lease!=e.Owner || record.LeaseToken!=e.Token || record.LeaseExpires<=DateTime.UtcNow.Ticks) continue;
                var p=record.Progress; var stats=p.Statistics; var run=stats?.Current;
                if(p.ExpeditionState!=2 || p.ExpeditionId!=e.Expedition || run==null || run.Id!=e.Expedition || run.Events.Contains(e.Key)) continue;
                run.Events.Add(e.Key);
                stats.DamageCreatures+=e.DamageCreatures; stats.DamageParticipants+=e.DamageParticipants;
                stats.DamageReceived+=e.DamageReceived; stats.Healing+=e.Healing; stats.Seconds+=e.Seconds; run.Seconds+=e.Seconds;
                run.DamageCreatures+=e.DamageCreatures; run.DamageParticipants+=e.DamageParticipants;
                run.DamageReceived+=e.DamageReceived; run.Healing+=e.Healing;
                if(e.Death) { run.Fatal=true; run.DeathSource=e.DeathSource; run.KillerName=e.KillerName; stats.ForSpecies(e.DeathSource).Deaths++; }
                if(e.Assist) { stats.Assists++; run.Assists++; stats.ForSpecies(e.Target).Assists++; }
                if(e.Kill)
                {
                    if(e.Target==ProfileSpecies.Human) { stats.HumanKills++; run.HumanKills++; }
                    else if(e.Target==ProfileSpecies.Bot) { stats.BotKills++; run.BotKills++; }
                    else { stats.CreatureKills++; run.CreatureKills++; }
                    stats.ForSpecies(e.Target).Kills++;
                    if(ItemCatalog.Weapon(e.Weapon))
                    {
                        var weapon=stats.Weapons.Find(w=>w.Weapon==e.Weapon);
                        if(weapon==null) { weapon=new WeaponStatistics {Weapon=e.Weapon}; stats.Weapons.Add(weapon); }
                        weapon.Kills++; weapon.LastKillTicks=e.Ticks;
                    }
                }
                p.Revision++;
            }
        }
        static void CompletePendingProfile(ProfileEvent[] batch) { foreach(var e in batch) pendingProfile.Remove(e); }
    }
}
