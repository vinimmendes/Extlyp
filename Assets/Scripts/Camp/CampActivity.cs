using System;
using UnityEngine;
using CyberExtraction.Items;
namespace CyberExtraction.Camp
{
    public enum CampCommandKind : byte { Sync, Action, BeginFishing, Reel, BeginCooking, Flip, FinishCooking, Cancel, TestSupply, TestReset, SelectFishingBait, CastFishing, HookFishing }
    [Serializable] public class CampCommand
    {
        public string Id=Guid.NewGuid().ToString("N");
        public CampCommandKind Kind;
        public CampAction Action;
        public int Slot;
        public bool Held;
        public bool UseBait;
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
        public FishingStage FishingStage;
        public float FishingBar=.5f, FishingAge, FishingDuration;
        public int FishingVisitor;
        public ItemId FishingItem;
        public bool FishingBait, FishingConsumed;
        public string FishingMessage;
    }
    // Only the server owns this state, random capture and elapsed time.
    public sealed class CampActivity
    {
        public int Phase, Slot, Side;
        public float Tension=.4f, Progress, HeatA, HeatB;
        public float Fish=.5f, Zone=.28f, CookMarker;
        public bool Nibble;
        readonly FishingGame fishing=new FishingGame();
        public FishingGame Fishing=>fishing;
        bool fishingCompleted;
        public float BarVelocity => fishing.Velocity;
        public double CookStartedAt { get; private set; }
        public int LastCookingQuality { get; private set; }=-1;
        public string CookingRejection { get; private set; }
        public string LastCookingStorage { get; private set; }
        public string FishingRejection { get; private set; }
        float started;
        public bool Busy=>Phase!=0;
        public void Cancel() { Phase=0; Nibble=false; fishing.Close(); }
        public bool BeginFishing(CampProgress profile,float now)
        {
            FishingRejection=null;
            if(Busy || !profile.PierRepaired || profile.ExpeditionState!=0) return false;
            fishing.Open(now); Phase=2; started=now; Progress=0; Nibble=false; fishingCompleted=false; return true;
        }
        public bool SelectFishingBait(CampProgress profile,bool value)
        {
            FishingRejection=null;
            if(Phase<2 || Phase>4 || value && profile.Count(ItemId.Worm)<1) { FishingRejection="Sem minhocas disponíveis na base."; return false; }
            return fishing.SelectBait(value);
        }
        public bool CastFishing(CampProgress profile,float now)
        {
            FishingRejection=null;
            if(Phase<2 || Phase>4 || !profile.PierRepaired || profile.ExpeditionState!=0) return false;
            if(fishing.Stage!=FishingStage.Ready && !fishing.Done) return false;
            if(fishing.UseBait && profile.Count(ItemId.Worm)<1) { FishingRejection="Sem minhocas. Selecione Sem isca para continuar."; return false; }
            foreach(var item in new[] { ItemId.FishCommon,ItemId.FishLarge,ItemId.FishRare })
                if(profile.Pack.FreeFor(new ItemStack(item,1))<1) { FishingRejection="Libere espaço na mochila para qualquer peixe da captura."; return false; }
            fishingCompleted=false; started=now;
            bool success=fishing.Cast(fishing.UseBait,now); if(success) AdvanceFishing(now,0); return success;
        }
        public void HookFishing(float now) { if(Phase>=2 && Phase<=4) { fishing.Hook(now); AdvanceFishing(now,0); } }
        public bool BeginCooking(CampProgress profile,int slot,float now,double? synchronizedNow=null)
        {
            if(Busy || slot<0 || slot>=profile.Pack.Slots.Length) return false;
            if(!profile.CanStoreCooking(slot)) { CookingRejection="Libere um espaco na mochila ou no bau para guardar o preparo."; return false; }
            var fish=profile.Pack.Slots[slot]; if(fish.Empty || !(ItemCatalog.Fish(fish.Item) || fish.Item==ItemId.RawFish)) return false;
            Phase=6; Slot=slot; Side=0; HeatA=HeatB=CookMarker=0; started=now; CookStartedAt=synchronizedNow ?? now; LastCookingQuality=-1; LastCookingStorage=null; CookingRejection=null; return true;
        }
        public void Reel(bool value,float now) { if(Phase>=2 && Phase<=4) fishing.Input(value,now); }
        public bool Flip() => false;
        public void AdvanceFishing(float now,float dt)
        {
            if(Phase<2 || Phase>4) return;
            fishing.Tick(now,dt); Fish=fishing.Fish; Zone=fishing.Zone; Progress=fishing.Progress; Nibble=fishing.Nibble;
            Phase=fishing.Stage==FishingStage.Bite ? 3 : fishing.Stage==FishingStage.Capture ? 4 : 2;
        }
        public string Tick(CampProgress profile,float now,float dt,double? synchronizedNow=null)
        {
            if(!Busy) return null;
            if(Phase>=2 && Phase<=4)
            {
                AdvanceFishing(now,dt);
                if(fishing.Stage==FishingStage.Bite && fishing.UseBait && !fishing.BaitConsumed)
                {
                    if(!profile.Take(ItemId.Worm,1)) { fishing.Fail("A minhoca não está mais disponível. Escolha uma isca novamente.",now); Phase=2; return fishing.Notice; }
                    profile.Revision++; fishing.ConsumeBait();
                }
                if(fishing.Stage==FishingStage.Success && !fishingCompleted)
                {
                    fishingCompleted=true;
                    if(!profile.CompleteFishing(fishing.Item)) { fishing.Fail("Não foi possível guardar a captura: mochila cheia.",now); return fishing.Notice; }
                    return "Captura guardada: "+ItemCatalog.Name(fishing.Item);
                }
                return null;
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
