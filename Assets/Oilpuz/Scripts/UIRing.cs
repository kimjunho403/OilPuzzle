using UnityEngine;
using UnityEngine.UI;

namespace Oilpuz
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class UIRing : MaskableGraphic
    {
        public float thickness=3, progress=1;
        public int dashCount=24;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();float r=Mathf.Min(rectTransform.rect.width,rectTransform.rect.height)*.5f;int segments=144;
            for(int i=0;i<segments*progress;i++)
            {
                if(dashCount>0 && (i*dashCount/segments)%2==1)continue;
                float a=i*Mathf.PI*2/segments,b=(i+1)*Mathf.PI*2/segments;
                int s=vh.currentVertCount;
                vh.AddVert(new Vector3(Mathf.Sin(a)*r,Mathf.Cos(a)*r),color,Vector2.zero);
                vh.AddVert(new Vector3(Mathf.Sin(b)*r,Mathf.Cos(b)*r),color,Vector2.zero);
                vh.AddVert(new Vector3(Mathf.Sin(b)*(r-thickness),Mathf.Cos(b)*(r-thickness)),color,Vector2.zero);
                vh.AddVert(new Vector3(Mathf.Sin(a)*(r-thickness),Mathf.Cos(a)*(r-thickness)),color,Vector2.zero);
                vh.AddTriangle(s,s+1,s+2);vh.AddTriangle(s,s+2,s+3);
            }
        }
    }
}
