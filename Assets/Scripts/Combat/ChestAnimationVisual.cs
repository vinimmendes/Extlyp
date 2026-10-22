using System.Collections;
using UnityEngine;
using CyberExtraction.Core;

namespace CyberExtraction.Combat
{
    // Presentation only: loot access, locks and opening duration remain server-owned.
    public sealed class ChestAnimationVisual : MonoBehaviour
    {
        LootPickup loot;
        SpriteRenderer visual;
        Coroutine transition;
        float openness;

        public void Configure(LootPickup owner, SpriteRenderer renderer)
        {
            loot=owner; visual=renderer;
            Subscribe(); Snap();
        }
        void OnEnable() { if(loot!=null) { Subscribe(); Snap(); } }
        void Subscribe()
        {
            loot.OpenState.OnValueChanged-=StateChanged;
            loot.OpenProgress.OnValueChanged-=ProgressChanged;
            loot.OpenState.OnValueChanged+=StateChanged;
            loot.OpenProgress.OnValueChanged+=ProgressChanged;
        }
        void OnDisable()
        {
            if(loot!=null)
            {
                loot.OpenState.OnValueChanged-=StateChanged;
                loot.OpenProgress.OnValueChanged-=ProgressChanged;
            }
            StopTransition();
        }
        void StopTransition()
        {
            if(transition!=null) StopCoroutine(transition);
            transition=null;
        }
        void Snap()
        {
            StopTransition();
            SetOpening(loot.OpenState.Value==2 ? 1 : loot.OpenState.Value==1 ? loot.OpenProgress.Value : 0);
        }
        void SetOpening(float progress)
        {
            openness=Mathf.Clamp01(progress);
            // Blender reaches its final pose at frame 116, then holds through 120.
            visual.sprite=ChestAnimationArt.Open[Mathf.Clamp(Mathf.RoundToInt(openness*15),0,19)];
        }
        void ProgressChanged(float previous,float current)
        {
            if(loot.OpenState.Value==1) SetOpening(current);
        }
        void StateChanged(byte previous,byte current)
        {
            StopTransition();
            if(current==1) SetOpening(loot.OpenProgress.Value);
            else if(current==2)
            {
                if(previous==1) SetOpening(1);
                else transition=StartCoroutine(Play(false)); // Lockpick success also opens the lid.
            }
            else if(openness>0) transition=StartCoroutine(Play(true));
            else SetOpening(0);
        }
        IEnumerator Play(bool closing)
        {
            var frames=closing ? ChestAnimationArt.Close : ChestAnimationArt.Open;
            float start=(closing ? 1-openness : openness)*15;
            float elapsed=0;
            while(start+elapsed*24<frames.Length)
            {
                float index=start+elapsed*24;
                visual.sprite=frames[Mathf.Clamp(Mathf.FloorToInt(index),0,frames.Length-1)];
                openness=closing ? 1-Mathf.Clamp01(index/15) : Mathf.Clamp01(index/15);
                yield return null;
                elapsed+=Time.deltaTime;
            }
            SetOpening(closing ? 0 : 1);
            transition=null;
        }
    }

    public static class ChestAnimationArt
    {
        static Sprite[] open,close;
        static bool attempted;
        public static Sprite[] Open => open;
        public static Sprite[] Close => close;
        public static bool TryBuild(Transform root)
        {
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null) return false;
            if(!attempted)
            {
                attempted=true;
                open=Load("Open"); close=Load("Close");
            }
            if(open==null || close==null) return false;
            var part=new GameObject("AnimatedChest"); part.transform.SetParent(root,false);
            // Closed Blender body is 194 x 128 pixels; preserve 1.5 x 1.2 world units.
            // All frames share the same canvas/pivot, including the extended open lid.
            part.transform.localScale=new Vector3(1,1.2f/(128f/(194f/1.5f)),1);
            var renderer=part.AddComponent<SpriteRenderer>();
            SpriteFactory.Setup(renderer,open[0],Color.white,5);
            var loot=root.parent.GetComponent<LootPickup>();
            if(loot!=null && loot.IsClient && loot.FixtureKind.Value==0)
                part.AddComponent<ChestAnimationVisual>().Configure(loot,renderer);
            return true;
        }
        static Sprite[] Load(string name)
        {
            var texture=Resources.Load<Texture2D>("Environment/Chest/"+name);
            if(texture==null || texture.width!=2048 || texture.height!=768)
            { Debug.LogWarning("Atlas de bau ausente/invalido: "+name); return null; }
            var frames=new Sprite[20];
            for(int i=0;i<frames.Length;i++)
            {
                frames[i]=Sprite.Create(texture,new Rect(i%8*256,texture.height-(i/8+1)*256,256,256),
                    new Vector2(.5f,129f/256),194f/1.5f,0,SpriteMeshType.FullRect);
                frames[i].name="Chest/"+name+"_"+i;
            }
            return frames;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            if(open!=null) foreach(var sprite in open) if(sprite!=null) Object.Destroy(sprite);
            if(close!=null) foreach(var sprite in close) if(sprite!=null) Object.Destroy(sprite);
            open=close=null; attempted=false;
        }
    }
}
