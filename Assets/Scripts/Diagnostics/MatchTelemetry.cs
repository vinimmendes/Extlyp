using System;
using System.IO;
using UnityEngine;
using Unity.Profiling;
namespace CyberExtraction.Diagnostics
{
    // Opt-in observations; no device identity, key or inventory contents are logged.
    public class MatchTelemetry : MonoBehaviour
    {
        static string path;
        ProfilerRecorder cpu, gc;
        float nextSample;
        [Serializable] class Sample { public string Event; public double Time; public long CpuNs, GcBytes; public bool CpuAvailable, GcAvailable; public ulong Object; public int Value; }
        void Awake()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-metrics")<0) { enabled=false; return; }
            Directory.CreateDirectory("Logs"); path=Path.GetFullPath("Logs/gameplay-metrics.jsonl");
            cpu=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread",1);
            gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame",1);
        }
        void Update()
        {
            if(Time.unscaledTime<nextSample) return; nextSample=Time.unscaledTime+1;
            File.AppendAllText(path,JsonUtility.ToJson(new Sample { Event="frame",Time=Time.realtimeSinceStartupAsDouble,CpuNs=cpu.Valid ? cpu.LastValue : 0,GcBytes=gc.Valid ? gc.LastValue : 0,CpuAvailable=cpu.Valid,GcAvailable=gc.Valid })+Environment.NewLine);
        }
        public static void Record(string name,ulong obj=0,int value=0)
        { if(path!=null) File.AppendAllText(path,JsonUtility.ToJson(new Sample { Event=name,Time=Time.realtimeSinceStartupAsDouble,Object=obj,Value=value })+Environment.NewLine); }
        void OnDestroy() { cpu.Dispose(); gc.Dispose(); path=null; }
    }
}
