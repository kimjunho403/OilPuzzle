using System;
using UnityEngine;

namespace Oilpuz
{
    public sealed class OilRenderer : IDisposable
    {
        public RenderTexture Texture { get; }
        public Material Surface { get; }
        readonly Material density;
        readonly Mesh mesh;
        Vector3[] vertices;
        Vector2[] uv;
        Vector2[] neck;
        int[] triangles;
        int count;
        public OilRenderer(Shader densityShader, Shader surfaceShader, SoupSkin skin, int resolution = 512)
        {
            var format = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RHalf) ? RenderTextureFormat.RHalf : RenderTextureFormat.ARGBHalf;
            Texture = new RenderTexture(resolution,resolution,0,format,RenderTextureReadWrite.Linear) { name="Oil density field", filterMode=FilterMode.Bilinear, wrapMode=TextureWrapMode.Clamp };
            Texture.Create();
            density = new Material(densityShader) { hideFlags=HideFlags.HideAndDontSave };
            Surface = new Material(surfaceShader) { name="Mala dynamic oil", hideFlags=HideFlags.HideAndDontSave };
            // CanvasRenderer binds a texture but does not reliably update texel-size
            // uniforms; bind it on the material too for correct surface normals.
            Surface.mainTexture=Texture;
            Surface.SetColor("_OilColor",skin.oil); Surface.SetColor("_EdgeColor",skin.oilEdge); Surface.SetColor("_Highlight",skin.oilHighlight);
            mesh = new Mesh { name="Local material density splats", hideFlags=HideFlags.HideAndDontSave }; mesh.MarkDynamic();
        }
        public void Draw(OilSimulation simulation)
        {
            int particleCount=simulation.Particles.Count;
            int n=particleCount+simulation.LinkCount;
            if(count!=n)
            {
                count=n;vertices=new Vector3[n*4];uv=new Vector2[n*4];neck=new Vector2[n*4];triangles=new int[n*6];
                for(int i=0;i<n;i++)
                {
                    int v=i*4,t=i*6; uv[v]=Vector2.zero;uv[v+1]=Vector2.right;uv[v+2]=Vector2.one;uv[v+3]=Vector2.up;
                    triangles[t]=v;triangles[t+1]=v+1;triangles[t+2]=v+2;triangles[t+3]=v;triangles[t+4]=v+2;triangles[t+5]=v+3;
                }
                mesh.Clear();
            }
            const float support=.108f*.5f;
            for(int i=0;i<particleCount;i++)
            {
                Vector2 p=simulation.Particles[i].position*.5f+Vector2.one*.5f;int v=i*4;
                vertices[v]=new Vector3(p.x-support,p.y-support);vertices[v+1]=new Vector3(p.x+support,p.y-support);
                vertices[v+2]=new Vector3(p.x+support,p.y+support);vertices[v+3]=new Vector3(p.x-support,p.y+support);
                for(int j=0;j<4;j++)neck[v+j]=Vector2.zero;
            }
            // Thin film follows actual surviving links, so a stretched neck stays
            // visually connected until the simulation tears it. Never bridge groups.
            for(int i=0;i<simulation.LinkCount;i++)
            {
                simulation.GetLink(i,out var a,out var b);
                Vector2 delta=b-a;float distance=delta.magnitude;
                int v=(particleCount+i)*4;
                if(distance<OilSimulation.Spacing*1.5f)
                {
                    for(int j=0;j<4;j++){vertices[v+j]=Vector3.zero;neck[v+j]=Vector2.zero;}
                    continue;
                }
                const float radius=.060f*.5f;
                Vector2 center=(a+b)*.25f+Vector2.one*.5f;
                Vector2 direction=delta/distance;
                float halfSegment=distance*.25f,halfLength=halfSegment+radius;
                Vector2 x=direction*halfLength,y=new Vector2(-direction.y,direction.x)*radius;
                vertices[v]=center-x-y;vertices[v+1]=center+x-y;
                vertices[v+2]=center+x+y;vertices[v+3]=center-x+y;
                for(int j=0;j<4;j++)neck[v+j]=new Vector2(halfSegment/halfLength,0);
            }
            mesh.vertices=vertices;mesh.uv=uv;mesh.uv2=neck;mesh.triangles=triangles;mesh.RecalculateBounds();
            RenderTexture previous=RenderTexture.active;
            Graphics.SetRenderTarget(Texture);GL.Clear(false,true,Color.clear);
            density.SetPass(0);Graphics.DrawMeshNow(mesh,Matrix4x4.identity);
            RenderTexture.active=previous;
        }
        public void Dispose()
        {
            Texture.Release();
            if(Application.isPlaying){UnityEngine.Object.Destroy(Texture);UnityEngine.Object.Destroy(Surface);UnityEngine.Object.Destroy(density);UnityEngine.Object.Destroy(mesh);}
            else{UnityEngine.Object.DestroyImmediate(Texture);UnityEngine.Object.DestroyImmediate(Surface);UnityEngine.Object.DestroyImmediate(density);UnityEngine.Object.DestroyImmediate(mesh);}
        }
    }
}
