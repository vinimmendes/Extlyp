// Focused proof of the real profile accounting code; storage and item lookup are fixtures.
using System;
using System.Collections.Generic;
using CyberExtraction.Camp;
using CyberExtraction.Items;

namespace CyberExtraction.Items
{
    public enum ItemId : byte { Pistol=0 }
    public static class ItemCatalog { public static bool Weapon(ItemId item)=>item==ItemId.Pistol; }
}
namespace CyberExtraction.Camp
{
    public partial class CampProgress { public int ExpeditionState=2; public string ExpeditionId="run"; public long Revision; }
    public class CampRecord
    {
        public string Lease="room",LeaseToken="token";
        public long LeaseExpires=DateTime.UtcNow.AddMinutes(10).Ticks;
        public CampProgress Progress=new CampProgress();
    }
    public static partial class CampRepository
    {
        sealed class LeaseClaim { public string Owner,Token; }
        static readonly Dictionary<string,LeaseClaim> heldLeases=new Dictionary<string,LeaseClaim>();
        static Dictionary<string,CampRecord> fixture;
        internal static PlayerStatistics BeginProbe()
        {
            pendingProfile.Clear(); heldLeases.Clear();
            fixture=new Dictionary<string,CampRecord> { ["player"]=new CampRecord() };
            heldLeases["player"]=new LeaseClaim {Owner="room",Token="token"};
            var stats=fixture["player"].Progress.Statistics;
            stats.Begin("run",0); return stats;
        }
        internal static void FlushProbe()
        { var batch=pendingProfile.ToArray(); ApplyPendingProfile(fixture,batch); CompletePendingProfile(batch); }
        internal static void ExpireProbe()=>fixture["player"].LeaseExpires=DateTime.UtcNow.AddMinutes(-1).Ticks;
        internal static void RevokeProbe()=>heldLeases.Clear();
        internal static void FinishProbe()
        { fixture["player"].Progress.Statistics.Finish("run",ExpeditionOutcome.Extracted,0); fixture["player"].Progress.ExpeditionState=0; }
    }
}
static class ProfileCounterProbe
{
    static int checks;
    static void Check(bool ok,string name)
    { if(!ok) throw new Exception(name); checks++; Console.WriteLine("PASS "+name); }
    static ProfileEvent Kill(string key,ProfileSpecies species=ProfileSpecies.Charger)=>
        new ProfileEvent {Session="player",Expedition="run",Key=key,Kill=true,Target=species,Weapon=ItemId.Pistol};
    static void Flush(ProfileEvent e,bool metric=false)
    { CampRepository.QueueProfile(e,metric); CampRepository.FlushProbe(); }
    static void Main()
    {
        Check(!new SpeciesStatistics().HasActivity,"zero species hidden");
        Check(new SpeciesStatistics {Kills=1}.HasActivity,"kill-only species visible");
        Check(new SpeciesStatistics {Assists=1}.HasActivity,"assist-only species visible");
        Check(new SpeciesStatistics {Deaths=1}.HasActivity,"death-only species visible");
        var s=CampRepository.BeginProbe();
        Flush(Kill("kill"));
        Check(s.CreatureKills==1 && s.Current.CreatureKills==1 && s.ForSpecies(ProfileSpecies.Charger).Kills==1,"official kill updates totals, expedition and species without XP");
        Flush(Kill("kill"));
        Check(s.CreatureKills==1,"replayed kill not counted twice");
        Flush(Kill("human",ProfileSpecies.Human)); Flush(Kill("bot",ProfileSpecies.Bot));
        Check(s.CreatureKills==1 && s.HumanKills==1 && s.BotKills==1,"humans and bots separated");
        var assist=new ProfileEvent {Session="player",Expedition="run",Key="assist",Assist=true,Target=ProfileSpecies.Listener};
        Flush(assist); Flush(assist);
        Check(s.Assists==1 && s.ForSpecies(ProfileSpecies.Listener).Assists==1,"assist counted once");
        Flush(new ProfileEvent {Session="player",Expedition="run",Key="death",Death=true,DeathSource=ProfileSpecies.Weaver});
        Check(s.Current.Fatal && s.ForSpecies(ProfileSpecies.Weaver).Deaths==1,"death source recorded");
        s=CampRepository.BeginProbe();
        for(int i=(int)ProfileSpecies.Charger;i<=(int)ProfileSpecies.PlantMinion;i++) Flush(Kill("species-"+i,(ProfileSpecies)i));
        Check(s.CreatureKills==14 && s.Species.Count==14,"all creature species counted");
        s.Validate(); Check(true,"result satisfies production statistics validation");
        CampRepository.FinishProbe(); Flush(Kill("after-end"));
        Check(s.CreatureKills==14 && s.History[0].CreatureKills==14 && s.Current==null,"ended expedition preserves totals and rejects later events");
        s=CampRepository.BeginProbe(); var wrong=Kill("wrong"); wrong.Expedition="another"; Flush(wrong);
        Check(s.CreatureKills==0,"other expedition rejected");
        CampRepository.ExpireProbe(); Flush(Kill("expired"));
        Check(s.CreatureKills==0,"expired lease rejected");
        s=CampRepository.BeginProbe(); CampRepository.RevokeProbe(); Flush(Kill("no-lease"));
        Check(s.CreatureKills==0,"no official lease rejected");
        s=CampRepository.BeginProbe(); var sandbox=Kill("sandbox"); sandbox.Expedition=null; Flush(sandbox);
        Check(s.CreatureKills==0,"test path without official expedition does not count");
        Flush(new ProfileEvent {Session="player",Expedition="run",Seconds=.02},true);
        Flush(new ProfileEvent {Session="player",Expedition="run",DamageCreatures=10},true);
        Check(s.Seconds==.02 && s.DamageCreatures==10 && s.CreatureKills==0,"metrics do not invent kills");
        Console.WriteLine("PASS total="+checks+"; real accounting source; no Unity, network or disk persistence validation");
    }
}
