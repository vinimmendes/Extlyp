using UnityEngine;

namespace CyberExtraction.UI
{
    public sealed class CapacitySegmentsGraphic : UnityEngine.UI.Image
    {
        public int Occupied;
        public override Texture mainTexture=>Texture2D.whiteTexture;
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
        {
            mesh.Clear(); var rect=GetPixelAdjustedRect();
            const int slots=12;
            float gap=3,cell=(rect.width-gap*(slots-1))/slots;
            if(cell<=0 || rect.height<=0) return;
            for(int i=0;i<slots;i++)
            {
                var tint=i<Occupied ? HudTheme.Accent : new Color(.19f,.20f,.17f,1);
                float x=rect.xMin+i*(cell+gap); int first=mesh.currentVertCount;
                mesh.AddVert(new Vector3(x,rect.yMin),tint,Vector2.zero);
                mesh.AddVert(new Vector3(x,rect.yMax),tint,Vector2.zero);
                mesh.AddVert(new Vector3(x+cell,rect.yMax),tint,Vector2.zero);
                mesh.AddVert(new Vector3(x+cell,rect.yMin),tint,Vector2.zero);
                mesh.AddTriangle(first,first+1,first+2); mesh.AddTriangle(first,first+2,first+3);
            }
        }
    }
}
