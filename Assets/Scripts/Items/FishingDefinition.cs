using UnityEngine;

namespace CyberExtraction.Items
{
    [CreateAssetMenu(menuName="Cyber Extraction/Fishing",fileName="FishingDefinition")]
    public sealed class FishingDefinition : ScriptableObject
    {
        [System.Serializable] public sealed class Profile
        {
            [Range(.1f,.6f)] public float Zone=.28f;
            public float Frequency=1.25f, Speed=.45f, Gain=.19f;
        }
        public float CastSeconds=.6f, WaitMin=1, WaitMax=2, ApproachSeconds=1, InvestigateMin=.35f, InvestigateMax=.75f, LeaveSeconds=.65f;
        public float HookSeconds=1.6f, CaptureSeconds=20, AttemptSeconds=90, Loss=.12f;
        [Range(0,100)] public int BitePercent=70, RarePercent=10, BaitRarePercent=25, LargePercent=15;
        public Profile Common=new Profile();
        public Profile Large=new Profile { Zone=.23f,Frequency=1.4f,Speed=.62f,Gain=.11f };
        public Profile Rare=new Profile { Zone=.20f,Frequency=2.2f,Speed=.60f,Gain=.105f };
        static FishingDefinition loaded;
        public static FishingDefinition Current
        {
            get
            {
                if(loaded==null)
                {
                    loaded=Resources.Load<FishingDefinition>("Fishing/Definition");
                    if(loaded==null) { loaded=CreateInstance<FishingDefinition>(); loaded.hideFlags=HideFlags.HideAndDontSave; }
                }
                return loaded;
            }
        }
        public Profile For(ItemId item)=>item==ItemId.FishRare ? Rare : item==ItemId.FishLarge ? Large : Common;
        public ItemId Roll(bool bait)
        {
            int roll=Random.Range(0,100), rare=Mathf.Clamp(bait ? BaitRarePercent : RarePercent,0,100);
            return roll<rare ? ItemId.FishRare : roll<rare+Mathf.Clamp(LargePercent,0,100-rare) ? ItemId.FishLarge : ItemId.FishCommon;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset()=>loaded=null;
    }
    public enum FishingStage : byte { None, Ready, Cast, Waiting, Approaching, Investigating, Leaving, Bite, Capture, Success, Failed }
}
