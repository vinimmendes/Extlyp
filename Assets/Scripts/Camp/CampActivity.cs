using System;
using UnityEngine;
using CyberExtraction.Items;
namespace CyberExtraction.Camp
{
    public enum CampCommandKind : byte { Sync, Action, BeginFishing, Reel, BeginCooking, Flip, FinishCooking, Cancel, TestSupply, TestReset }
    [Serializable] public class CampCommand
    {
        public string Id=Guid.NewGuid().ToString("N");
        public CampCommandKind Kind;
        public CampAction Action;
        public int Slot;
        public bool Held;
        public double CookingPressedAt;
    }
    [Serializable] public class CampSnapshot
    {
        public string Profile, Ack, Message;
        public int Activity, Side;
        public float Tension, Progress, HeatA, HeatB;
        public float Fish=.5f, Zone=.28f, CookMarker, BarVelocity;
        public bool Nibble;
        public bool TestMode;
        public double CookStartedAt;
    }
    // Only the server owns this state, random capture and elapsed time.
    public sealed class CampActivity
    {
        public int Phase, Slot, Side;
        public float Tension=.4f, Progress, HeatA, HeatB;
        public float Fish=.5f, Zone=.28f, CookMarker;
        public bool Nibble;
        readonly FishingGame fishing=new FishingGame();
        public float BarVelocity => fishing.Velocity;
        public double CookStartedAt { get; private set; }
        public int LastCookingQuality { get; private set; }=-1;
        public string CookingRejection { get; private set; }
        public string LastCookingStorage { get; private set; }
        public string FishingRejection { get; private set; }
        float started;
        ItemId catchItem;
        public bool Busy=>Phase!=0;
        public void Cancel() { Phase=0; Nibble=false; }
        public bool BeginFishing(CampProgress profile,float now)
        {
            FishingRejection=null;
            if(Busy || !profile.PierRepaired) return false;
            catchItem=CampProgress.CatchItem(Guid.NewGuid().ToString("N"));
            if(profile.Pack.FreeFor(new ItemStack(catchItem,1))<1)
            { FishingRejection="Mochila cheia. Libere espaco para pescar."; return false; }
            Phase=2; started=now; Progress=0; Tension=.5f; Nibble=false; fishing.Begin(catchItem,now); return true;
        }
        public bool BeginCooking(CampProgress profile,int slot,float now,double? synchronizedNow=null)
        {
            if(Busy || slot<0 || slot>=profile.Pack.Slots.Length) return false;
            if(!profile.CanStoreCooking(slot)) { CookingRejection="Libere um espaco na mochila ou no bau para guardar o preparo."; return false; }
            var fish=profile.Pack.Slots[slot]; if(fish.Empty || !(ItemCatalog.Fish(fish.Item) || fish.Item==ItemId.RawFish)) return false;
            Phase=6; Slot=slot; Side=0; HeatA=HeatB=CookMarker=0; started=now; CookStartedAt=synchronizedNow ?? now; LastCookingQuality=-1; LastCookingStorage=null; CookingRejection=null; return true;
        }
        public void Reel(bool value,float now) { fishing.Input(value,now); }
        public bool Flip() => false;
        public void AdvanceFishing(float now,float dt)
        {
            if(Phase<2 || Phase>4) return;
            fishing.Tick(now,dt); Tension=fishing.Bar; Fish=fishing.Fish; Zone=fishing.Zone; Progress=fishing.Progress; Nibble=fishing.Nibble;
            if(fishing.Phase<=3) Phase=fishing.Phase+1;
        }
        public string Tick(CampProgress profile,float now,float dt,double? synchronizedNow=null)
        {
            if(!Busy) return null;
            if(now-started>90) { Cancel(); return "Atividade encerrada por tempo limite."; }
            if(Phase>=2 && Phase<=4)
            {
                AdvanceFishing(now,dt);
                if(fishing.Phase==5) { Cancel(); return "O peixe escapou. Espere a boia afundar e mantenha o peixe na faixa."; }
                if(fishing.Phase==4) { Cancel(); return profile.CompleteFishing(catchItem) ? "Captura guardada: "+ItemCatalog.Name(catchItem) : "Mochila cheia."; }
                Phase=fishing.Phase+1;
            }
            if(Phase==6)
            {
                float elapsed=synchronizedNow.HasValue ? (float)(synchronizedNow.Value-CookStartedAt) : now-started;
                CookMarker=CookingGame.Marker(elapsed);
                if(elapsed>=CookingGame.Seconds && CompleteCooking(profile,1,elapsed)) return "Tempo esgotado: o peixe queimou. Guardado em: "+LastCookingStorage+".";
            }
            return null;
        }
        public bool FinishCooking(CampProgress profile,float now,float? elapsedAtClick=null,double? synchronizedNow=null)
        {
            CookingRejection=null;
            // Both elapsed values must use the clock that supplied CookStartedAt.
            float elapsed=synchronizedNow.HasValue ? (float)(synchronizedNow.Value-CookStartedAt) : now-started;
            float evaluated=elapsedAtClick ?? elapsed;
            if(Phase!=6) { CookingRejection="O preparo ja foi encerrado."; return false; }
            if(elapsed>=CookingGame.Seconds) { CookingRejection="O tempo do preparo esgotou."; return false; }
            if(elapsed<CookingGame.MinimumInputSeconds || float.IsNaN(evaluated) || float.IsInfinity(evaluated) || evaluated<CookingGame.MinimumInputSeconds ||
                evaluated>elapsed+.05f || evaluated<Mathf.Max(0,elapsed-.35f))
            { CookingRejection="O clique chegou fora da janela de sincronizacao. Tente novamente."; return false; }
            bool success=CompleteCooking(profile,CookingGame.Quality(evaluated),evaluated);
            if(!success) CookingRejection="Nao foi possivel guardar o preparo. Verifique o peixe e o espaco na mochila.";
            return success;
        }
        bool CompleteCooking(CampProgress profile,int quality,float duration)
        {
            var action=new CampAction { Kind=CampActionKind.CookFish,Sequence=profile.Revision+1,Source=Slot,Result=quality,Duration=duration };
            bool success=profile.CompleteCooking(action);
            if(success) { LastCookingQuality=quality; LastCookingStorage=action.CookingStorage; Cancel(); } return success;
        }
    }
}
