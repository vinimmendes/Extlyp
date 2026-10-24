using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using CyberExtraction.Items;

namespace CyberExtraction.Camp
{
    public static class PlayerNameRules
    {
        public static readonly long RenameDelay=TimeSpan.FromDays(7).Ticks;
        public static bool TryNormalize(string value,out string name,out string error)
        {
            name=""; error="Use de 3 a 20 caracteres: letras, números, espaços, hífen ou sublinhado.";
            if(string.IsNullOrEmpty(value) || value.Length>160) return false;
            string text;
            try { text=value.Normalize(NormalizationForm.FormC); } catch(ArgumentException) { return false; }
            var result=new StringBuilder(); bool space=false;
            foreach(char c in text)
            {
                if(c==' ') { space=result.Length>0; continue; }
                var category=CharUnicodeInfo.GetUnicodeCategory(c);
                bool combining=category==UnicodeCategory.NonSpacingMark || category==UnicodeCategory.SpacingCombiningMark;
                if(combining && (result.Length==0 || space || result[result.Length-1]=='-' || result[result.Length-1]=='_')) return false;
                if(!char.IsLetterOrDigit(c) && category!=UnicodeCategory.NonSpacingMark && category!=UnicodeCategory.SpacingCombiningMark && c!='-' && c!='_') return false;
                if(space) result.Append(' '); space=false; result.Append(c);
            }
            text=result.ToString(); int count=new StringInfo(text).LengthInTextElements;
            if(count<3 || count>20 || Encoding.UTF8.GetByteCount(text)>120) return false;
            name=text; error=null; return true;
        }
    }
    public enum ProfileSpecies : byte
    {
        Unknown=0, Human=1, Bot=2, Environment=3, Charger=4, Listener=5, Shell=6, Weaver=7, Herald=8, Mimic=9, Acid=10, Stalker=11, Devourer=12,
        Spore=13, PlantMother=14, AttackBulb=15, HealingBulb=16, PlantMinion=17
    }
    public enum ExpeditionOutcome : byte { Active=0, Extracted=1, Dead=2, TimeUp=3, Interrupted=4 }
    [Serializable] public sealed class SpeciesStatistics
    {
        public ProfileSpecies Species;
        public long Kills,Assists,Deaths;
        public bool HasActivity=>Kills>0 || Assists>0 || Deaths>0;
    }
    [Serializable] public sealed class WeaponStatistics
    { public ItemId Weapon; public long Kills,LastKillTicks; }
    [Serializable] public sealed class MapStatistics
    { public byte Map; public long Started,Extracted; }
    [Serializable] public sealed class ExpeditionStatistics
    {
        public string Id;
        public byte Map;
        public long StartedTicks,EndedTicks;
        public double Seconds;
        public ExpeditionOutcome Outcome;
        public long CreatureKills,HumanKills,BotKills,Assists,ConfirmedXp;
        public long DamageCreatures,DamageParticipants,DamageReceived,Healing;
        public ProfileSpecies DeathSource;
        public string KillerName;
        public bool Fatal;
        public int Exit=-1;
        // Only the active expedition needs event deduplication. Completed history never stores this list.
        public List<string> Events=new List<string>();
    }
    [Serializable] public sealed class PlayerStatistics
    {
        public long SinceTicks,Started,Extracted,Deaths,TimeUps,Interrupted,Streak,BestStreak;
        public long CreatureKills,HumanKills,BotKills,Assists,DamageCreatures,DamageParticipants,DamageReceived,Healing;
        public long CombatXp,ConfirmedXp;
        public double Seconds,LongestSeconds;
        public List<SpeciesStatistics> Species=new List<SpeciesStatistics>();
        public List<WeaponStatistics> Weapons=new List<WeaponStatistics>();
        public List<MapStatistics> Maps=new List<MapStatistics>();
        public List<ExpeditionStatistics> History=new List<ExpeditionStatistics>();
        public ExpeditionStatistics Active;
        // JsonUtility can materialize a null inline class as an empty object.
        public ExpeditionStatistics Current=>Active!=null && !string.IsNullOrEmpty(Active.Id) ? Active : null;
        public long Ended=>Extracted+Deaths+TimeUps+Interrupted;
        public WeaponStatistics Favorite
        {
            get
            {
                WeaponStatistics best=null;
                foreach(var w in Weapons) if(w.Kills>0 && (best==null || w.Kills>best.Kills || w.Kills==best.Kills &&
                    (w.LastKillTicks>best.LastKillTicks || w.LastKillTicks==best.LastKillTicks && (byte)w.Weapon<(byte)best.Weapon))) best=w;
                return best;
            }
        }
        public SpeciesStatistics ForSpecies(ProfileSpecies species)
        { var row=Species.Find(r=>r.Species==species); if(row==null) { row=new SpeciesStatistics {Species=species}; Species.Add(row); } return row; }
        public MapStatistics ForMap(byte map)
        { var row=Maps.Find(r=>r.Map==map); if(row==null) { row=new MapStatistics {Map=map}; Maps.Add(row); } return row; }
        public void Begin(string id,byte map)
        {
            if(Active!=null && Active.Id==id) return;
            Active=new ExpeditionStatistics { Id=id,Map=map,StartedTicks=DateTime.UtcNow.Ticks };
            Started++; ForMap(map).Started++;
        }
        public void Finish(string id,ExpeditionOutcome outcome,long xp,int exit=-1)
        {
            var run=Current; if(run==null || run.Id!=id) return;
            if(run.Fatal) outcome=ExpeditionOutcome.Dead;
            run.Outcome=outcome; run.EndedTicks=DateTime.UtcNow.Ticks; run.ConfirmedXp=xp; run.Exit=exit;
            ConfirmedXp+=xp; LongestSeconds=Math.Max(LongestSeconds,run.Seconds);
            if(outcome==ExpeditionOutcome.Extracted) { Extracted++; Streak++; BestStreak=Math.Max(BestStreak,Streak); ForMap(run.Map).Extracted++; }
            else { Streak=0; if(outcome==ExpeditionOutcome.Dead) Deaths++; else if(outcome==ExpeditionOutcome.TimeUp) TimeUps++; else Interrupted++; }
            run.Events=null; History.Insert(0,run); if(History.Count>20) History.RemoveRange(20,History.Count-20); Active=null;
        }
        public void Validate()
        {
            if(SinceTicks<0 || SinceTicks>DateTime.MaxValue.Ticks || Started<0 || Extracted<0 || Deaths<0 || TimeUps<0 || Interrupted<0 || Streak<0 || BestStreak<0 ||
                CreatureKills<0 || HumanKills<0 || BotKills<0 || Assists<0 || DamageCreatures<0 || DamageParticipants<0 || DamageReceived<0 || Healing<0 ||
                CombatXp<0 || ConfirmedXp<0 || double.IsNaN(Seconds) || double.IsInfinity(Seconds) || Seconds<0 || LongestSeconds<0 ||
                double.IsNaN(LongestSeconds) || double.IsInfinity(LongestSeconds) || Species==null || Species.Count>18 || Weapons==null || Weapons.Count>128 ||
                Maps==null || Maps.Count>3 || History==null || History.Count>20) throw new InvalidOperationException("Estatísticas de perfil inválidas.");
            var species=new HashSet<ProfileSpecies>();
            foreach(var row in Species) if(row==null || !Enum.IsDefined(typeof(ProfileSpecies),row.Species) || !species.Add(row.Species) || row.Kills<0 || row.Assists<0 || row.Deaths<0) throw new InvalidOperationException("Estatística de criatura inválida.");
            var weapons=new HashSet<ItemId>();
            foreach(var row in Weapons) if(row==null || !ItemCatalog.Weapon(row.Weapon) || !weapons.Add(row.Weapon) || row.Kills<0 || row.LastKillTicks<0) throw new InvalidOperationException("Estatística de arma inválida.");
            var maps=new HashSet<byte>();
            foreach(var row in Maps) if(row==null || row.Map>2 || !maps.Add(row.Map) || row.Started<0 || row.Extracted<0) throw new InvalidOperationException("Estatística de mapa inválida.");
            foreach(var run in History) ValidateRun(run,false);
            if(Current!=null) ValidateRun(Current,true);
        }
        static void ValidateRun(ExpeditionStatistics run,bool active)
        {
            if(run==null || string.IsNullOrEmpty(run.Id) || run.Id.Length>96 || run.Map>2 || run.StartedTicks<0 || run.StartedTicks>DateTime.MaxValue.Ticks || run.EndedTicks<0 || run.EndedTicks>DateTime.MaxValue.Ticks ||
                !Enum.IsDefined(typeof(ExpeditionOutcome),run.Outcome) || active!=(run.Outcome==ExpeditionOutcome.Active) ||
                double.IsNaN(run.Seconds) || double.IsInfinity(run.Seconds) || run.Seconds<0 || run.CreatureKills<0 || run.HumanKills<0 || run.BotKills<0 || run.Assists<0 || run.ConfirmedXp<0 ||
                run.DamageCreatures<0 || run.DamageParticipants<0 || run.DamageReceived<0 || run.Healing<0 ||
                !Enum.IsDefined(typeof(ProfileSpecies),run.DeathSource) || run.KillerName!=null && (run.KillerName.Length>128 || Encoding.UTF8.GetByteCount(run.KillerName)>120) ||
                active && (run.Events==null || run.Events.Count>4096)) throw new InvalidOperationException("Histórico de expedição inválido.");
        }
    }
    public partial class CampProgress
    {
        public string PlayerName="";
        public long NameChangedTicks;
        public PlayerStatistics Statistics=new PlayerStatistics();
        public bool SetPlayerName(string value,out string message)
        {
            if(!PlayerNameRules.TryNormalize(value,out var name,out message)) return false;
            if(PlayerName==name) { message="Este já é o seu nome."; return true; }
            long now=DateTime.UtcNow.Ticks;
            if(!string.IsNullOrEmpty(PlayerName) && now-NameChangedTicks<PlayerNameRules.RenameDelay)
            { message="Você pode alterar o nome novamente em "+new DateTime(NameChangedTicks+PlayerNameRules.RenameDelay,DateTimeKind.Utc).ToLocalTime().ToString("dd/MM/yyyy HH:mm")+"."; return false; }
            PlayerName=name; NameChangedTicks=now; message="Nome salvo."; return true;
        }
        public void ValidateProfile()
        {
            if(NameChangedTicks<0 || NameChangedTicks>DateTime.MaxValue.Ticks-PlayerNameRules.RenameDelay || (!string.IsNullOrEmpty(PlayerName) && (!PlayerNameRules.TryNormalize(PlayerName,out var name,out _) || name!=PlayerName)) || Statistics==null)
                throw new InvalidOperationException("Nome ou perfil inválido.");
            Statistics.Validate();
        }
    }
}
