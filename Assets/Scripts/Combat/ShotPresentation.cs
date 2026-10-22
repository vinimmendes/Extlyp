using UnityEngine;
using CyberExtraction.Core;
using CyberExtraction.Items;
namespace CyberExtraction.Combat
{
    // A bounded pool belongs to the scene, not to a shooter that may despawn when hidden.
    public class ShotPresentation : MonoBehaviour
    {
        const int Capacity=16;
        static ShotPresentation instance;
        AudioSource[] voices;
        LineRenderer[] lines;
        float[] until;
        int index;
        void Awake()
        {
            instance=this; voices=new AudioSource[Capacity]; lines=new LineRenderer[Capacity]; until=new float[Capacity];
            for(int i=0;i<Capacity;i++)
            {
                var go=new GameObject("ShotVoice"); go.transform.SetParent(transform); var source=go.AddComponent<AudioSource>(); voices[i]=source;
                source.playOnAwake=false; source.volume=.8f; source.spatialBlend=1; source.dopplerLevel=0; source.rolloffMode=AudioRolloffMode.Linear; source.minDistance=2; source.maxDistance=GameConfig.ShotNoiseRadius;
                var lr=go.AddComponent<LineRenderer>(); lines[i]=lr; lr.useWorldSpace=true; lr.positionCount=2; lr.startWidth=lr.endWidth=.06f;
                lr.startColor=lr.endColor=new Color(1,.9f,.5f,.9f); lr.sortingOrder=8; lr.sharedMaterial=SpriteFactory.Unlit; lr.enabled=false;
            }
        }
        public static void Play(Vector2 from,Vector2 to,bool tracer,bool sound=true,float noiseRadius=GameConfig.ShotNoiseRadius,ItemId weapon=ItemId.ImprovisedHandCrossbow)
        {
            if(Application.isBatchMode || Camera.main==null || weapon!=ItemId.ImprovisedHandCrossbow) return;
            if(instance==null) new GameObject("ShotPresentation").AddComponent<ShotPresentation>();
            var p=instance; int i=p.index++%Capacity; var camera=Camera.main;
            p.voices[i].transform.position=new Vector3(from.x,from.y,camera.transform.position.z);
            p.voices[i].maxDistance=noiseRadius;
            bool crossbow=weapon==ItemId.ImprovisedHandCrossbow;
            AudioClip shotClip=null; // Dedicated crossbow sound is not supplied yet.
            p.lines[i].startWidth=p.lines[i].endWidth=crossbow ? .035f : .06f;
            p.lines[i].startColor=p.lines[i].endColor=crossbow ? new Color(.7f,.61f,.43f,.8f) : new Color(1,.9f,.5f,.9f);
            if(sound && shotClip!=null && Vector2.Distance(from,camera.transform.position)<noiseRadius)
            { p.voices[i].clip=shotClip; p.voices[i].Play(); }
            p.lines[i].enabled=tracer; if(tracer) { p.lines[i].SetPosition(0,from); p.lines[i].SetPosition(1,to); p.until[i]=Time.time+.06f; }
        }
        void Update() { for(int i=0;i<Capacity;i++) if(lines[i].enabled && Time.time>=until[i]) lines[i].enabled=false; }
        void OnDestroy() { if(instance==this) instance=null; }
    }
}
