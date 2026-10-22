using Unity.Netcode;
using UnityEngine;
using CyberExtraction.Players;

namespace CyberExtraction.Match
{
    // Local extraction animation; preparation and banking are always authoritative.
    public sealed class DungeonPresentation : MonoBehaviour
    {
        SpriteRenderer platform,boat;
        GameObject grade,chain,bridge;
        Collider2D[] bridgeBlockers;
        Vector3 boatOrigin,platformOrigin;
        PlayerInventory local;
        NetworkObject localObject;
        int mask=-1,lastExit=-1;
        bool shortcut,wasAlive;
        float departure=-1;
        DungeonExtractionArt art;
        public void SetArt(DungeonExtractionArt value)
        {
            art=value;
            // The source sprite's bow faces right. Rotate the complete carrier
            // after attaching its art so nonuniform placeholder scale cannot
            // distort a separately rotated child. The boarding trigger stays fixed.
            if(boat!=null && DungeonLayout.CanalDeparture.sqrMagnitude>0)
                boat.transform.rotation=Quaternion.Euler(0,0,
                    Vector2.SignedAngle(Vector2.right,DungeonLayout.CanalDeparture));
        }
        public void Initialize(SpriteRenderer lift,SpriteRenderer vessel,GameObject gate,GameObject tether,GameObject crossing,Collider2D[] waterColliders)
        {
            platform=lift; boat=vessel; grade=gate; chain=tether; bridge=crossing; bridgeBlockers=waterColliders;
            boatOrigin=boat.transform.position; platformOrigin=platform.transform.position;
        }
        void Update()
        {
            var mm=MatchManager.Instance; if(mm==null || platform==null) return;
            if(mask!=mm.ActiveExtractions.Value || shortcut!=mm.DungeonShortcut.Value)
            {
                mask=mm.ActiveExtractions.Value; shortcut=mm.DungeonShortcut.Value;
                grade.SetActive((mask&1)==0); chain.SetActive((mask&4)==0); bridge.SetActive(shortcut);
                // Permanent river colliders are never disabled by the shortcut.
                foreach(var collider in bridgeBlockers) if(collider!=null) collider.enabled=!shortcut;
                DungeonLayout.SetShortcutOpen(shortcut);
            }
            var nm=NetworkManager.Singleton;
            var player=nm!=null && nm.SpawnManager!=null ? nm.SpawnManager.GetLocalPlayerObject() : null;
            if(player!=localObject) { localObject=player; local=player!=null ? player.GetComponent<PlayerInventory>() : null; wasAlive=false; lastExit=-1; departure=-1; }
            float progress=0; int exit=-1;
            if(local!=null)
            {
                if(!wasAlive && local.IsAlive) { departure=-1; lastExit=-1; }
                progress=local.ExtractionProgress.Value; exit=local.CurrentExtractionExit.Value;
                if(progress>0 && exit>=0) lastExit=exit;
                if(wasAlive && local.CurrentStatus==PlayerStatus.Extracted && lastExit>=0) departure=Time.unscaledTime;
                wasAlive=local.IsAlive;
            }
            if(departure>=0 && Time.unscaledTime-departure>2.5f) { departure=-1; lastExit=-1; }
            float departing=departure>=0 ? Mathf.Clamp01((Time.unscaledTime-departure)/1.5f) : 0;
            float lift=exit==1 ? progress : lastExit==1 && departure>=0 ? departing : 0;
            platform.transform.position=platformOrigin+Vector3.up*lift*2;
            platform.transform.localScale=new Vector3(2.5f,2.5f,1)*(1-lift*.35f);
            // Stay moored throughout the server countdown; depart only after extraction.
            float sailing=lastExit==2 && departure>=0 ? departing : 0;
            boat.transform.position=boatOrigin+(Vector3)DungeonLayout.CanalDeparture*sailing;
            boat.color=new Color(.37f,.25f,.16f,1-sailing*.85f);
            if(art!=null) art.Apply(mask,shortcut,sailing);
        }
    }
}
