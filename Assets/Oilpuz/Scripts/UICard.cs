using UnityEngine;
using UnityEngine.UI;

namespace Oilpuz
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class UICard : MaskableGraphic
    {
        public float radius=24;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var rect=rectTransform.rect;float r=Mathf.Min(radius,Mathf.Min(rect.width,rect.height)*.5f);
            vh.AddVert(rect.center,color,Vector2.one*.5f);
            const int arc=8;
            for(int corner=0;corner<4;corner++)for(int i=0;i<=arc;i++)
            {
                float angle=(corner*90+i*90f/arc)*Mathf.Deg2Rad;
                float cx=corner==0||corner==3?rect.xMax-r:rect.xMin+r;
                float cy=corner<2?rect.yMax-r:rect.yMin+r;
                vh.AddVert(new Vector3(cx+Mathf.Cos(angle)*r,cy+Mathf.Sin(angle)*r),color,Vector2.zero);
            }
            int n=4*(arc+1);for(int i=0;i<n;i++)vh.AddTriangle(0,i+1,(i+1)%n+1);
        }
    }
}
