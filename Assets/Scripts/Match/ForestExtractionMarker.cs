using System.Collections.Generic;
using UnityEngine;
using CyberExtraction.Core;

namespace CyberExtraction.Match
{
    public sealed class ForestExtractionMarker : MonoBehaviour
    {
        public static readonly List<ForestExtractionMarker> All=new List<ForestExtractionMarker>();
        public int ExitId { get; private set; }
        GameObject props;
        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }
        public void Initialize(int id) { ExitId=id; Apply(); }
        public void Apply()
        {
            if(props!=null) { props.SetActive(false); Destroy(props); }
            props=new GameObject("ExitPresentation"); props.transform.SetParent(transform,false);
            bool active=MatchManager.Instance!=null && (MatchManager.Instance.ActiveExtractions.Value&(1<<ExitId))!=0;
            var exit=LargeMapPlan.Exits[ExitId];
            ForestProps.Part(props.transform,"WornGround",Vector2.zero,new Vector2(6,5),new Color(.23f,.22f,.15f),false).sortingOrder=-8;
            if(exit.Transport)
            {
                if(active)
                {
                    if(ExitId==6 || ExitId==7) ForestProps.Boat(props.transform,ExitId==7);
                    else if(ExitId==2) { ForestProps.Vehicle(props.transform,Vector2.zero,false); foreach(float y in new[]{-2f,2f}) ForestProps.Part(props.transform,"Rail",new Vector2(0,y),new Vector2(7,.12f),Color.gray,false); }
                    else ForestProps.Vehicle(props.transform,Vector2.zero,ExitId==1);
                }
                else foreach(float y in new[]{-.8f,.8f}) ForestProps.Part(props.transform,"OldTracks",new Vector2(0,y),new Vector2(5,.1f),new Color(.13f,.15f,.10f),false);
            }
            else
            {
                if(ExitId==4) for(int i=0;i<5;i++) ForestProps.Part(props.transform,"BridgeStone",new Vector2(-2+i,2.6f),new Vector2(.85f,3),new Color(.36f,.37f,.30f),false);
                ForestProps.Part(props.transform,"LeftPost",new Vector2(-3,2.5f),new Vector2(.5f,.5f),new Color(.35f,.28f,.19f),true);
                ForestProps.Part(props.transform,"RightPost",new Vector2(3,2.5f),new Vector2(.5f,.5f),new Color(.35f,.28f,.19f),true);
                if(!active)
                {
                    var blocked=ForestProps.Part(props.transform,"BlockedExit",new Vector2(0,3),new Vector2(5.5f,.7f),new Color(.32f,.28f,.20f),true);
                    if(ExitId==3) blocked.gameObject.layer=GameConfig.WallLayer;
                }
            }
        }
        public bool Inspectable(Vector2 origin,float facing)
        {
            if(!CyberExtraction.Bots.BotController.InVision(origin,facing,transform.position)) return false;
            var hit=Physics2D.Linecast(origin,transform.position,GameConfig.VisionMask).collider;
            return hit==null || hit.transform.IsChildOf(transform);
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset() { All.Clear(); }
    }
}
