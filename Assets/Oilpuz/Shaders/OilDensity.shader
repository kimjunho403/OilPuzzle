Shader "Oilpuz/OilDensity"
{
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        Pass
        {
            ZWrite Off ZTest Always Cull Off Blend One One
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct input { float4 vertex:POSITION; float2 uv:TEXCOORD0; float2 neck:TEXCOORD1; };
            struct output { float4 position:SV_POSITION; float2 uv:TEXCOORD0; float neck:TEXCOORD1; };
            output vert(input v)
            {
                output o; o.position=float4(v.vertex.xy*2-1,0,1);
                // RenderTexture UV origin differs on Direct3D/Metal versus OpenGL.
                // Keep the density field aligned with UI/touch coordinates on both.
                #if UNITY_UV_STARTS_AT_TOP
                    o.position.y=-o.position.y;
                #endif
                o.uv=v.uv; o.neck=v.neck.x; return o;
            }
            float4 frag(output i):SV_Target
            {
                float2 p=i.uv*2-1;
                p.x=max(abs(p.x)-i.neck,0)/max(1-i.neck,.001);
                float d=dot(p,p);
                return exp(-d*4.3)*step(d,1.0);
            }
            ENDCG
        }
    }
}
