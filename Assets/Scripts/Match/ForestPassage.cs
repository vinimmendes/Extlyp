using System.Collections.Generic;
using UnityEngine;
using CyberExtraction.Players;

namespace CyberExtraction.Match
{
    public sealed class ForestPassage : MonoBehaviour
    {
        public static readonly List<ForestPassage> All=new List<ForestPassage>();
        public int RouteId { get; private set; }
        GameObject barrier;
        GameObject remnants;
        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }
        public void Initialize(int route,GameObject obstacle)
        {
            RouteId=route; barrier=obstacle;
            remnants=new GameObject("OpenedRemnants"); remnants.transform.SetParent(transform,false);
            float span=Route.Width+15;
            foreach(float side in new[]{-1f,1f})
            {
                var piece=ForestProps.Part(remnants.transform,"BrokenEdge",new Vector2(0,side*span*.45f),new Vector2(.4f,2),new Color(.43f,.40f,.29f),false);
                piece.transform.localRotation=Quaternion.Euler(0,0,side*35);
            }
            Apply();
        }
        public bool Open => MatchManager.Instance!=null && (MatchManager.Instance.OpenedPassages.Value&(1u<<RouteId))!=0;
        public ForestRoute Route => RouteId>=0 && RouteId<LargeMapPlan.Routes.Count ? LargeMapPlan.Routes[RouteId] : null;
        public void Apply()
        {
            if(barrier!=null) barrier.SetActive(!Open);
            if(remnants!=null) remnants.SetActive(Open);
            foreach(Transform child in transform) if(child.name=="WireMesh" || child.name=="BarrierDetail") child.gameObject.SetActive(!Open);
        }
        public static ForestPassage Nearby(PlayerInventory player)
        {
            if(player==null) return null;
            ForestPassage best=null; float distance=3;
            foreach(var gate in All)
            {
                if(gate==null || gate.Open || gate.Route==null || player.Count(gate.Route.RequiredItem)<1) continue;
                float d=Vector2.Distance(player.transform.position,gate.transform.position);
                if(d<distance) { distance=d; best=gate; }
            }
            return best;
        }
        public bool CanOperate(PlayerInventory player)
        {
            if(player==null || Route==null || Open || player.Count(Route.RequiredItem)<1 || Vector2.Distance(player.transform.position,transform.position)>3) return false;
            var hit=Physics2D.Linecast(player.transform.position,transform.position,CyberExtraction.Core.GameConfig.WallMask).collider;
            return hit==null || hit.transform.IsChildOf(transform);
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset() { All.Clear(); }
    }
}
