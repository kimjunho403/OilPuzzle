Shader "Oilpuz/OilSurface"
{
    Properties
    {
        _MainTex ("Density", 2D)="black" {}
        _OilColor ("Oil", Color)=(1,.48,.045,1)
        _EdgeColor ("Edge", Color)=(.76,.25,.018,1)
        _Highlight ("Highlight", Color)=(1,.94,.61,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            ZTest Always ZWrite Off Cull Off Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex; float4 _MainTex_TexelSize;
            float4 _OilColor, _EdgeColor, _Highlight;
            struct input { float4 vertex:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct output { float4 position:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            output vert(input v) { output o; o.position=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color; return o; }
            float4 frag(output i):SV_Target
            {
                float f=tex2D(_MainTex,i.uv).r;
                float coverage=smoothstep(.69,.80,f);
                clip(coverage-.003);
                float2 px=_MainTex_TexelSize.xy*1.6;
                float2 gradient=float2(tex2D(_MainTex,i.uv+float2(px.x,0)).r-tex2D(_MainTex,i.uv-float2(px.x,0)).r,
                    tex2D(_MainTex,i.uv+float2(0,px.y)).r-tex2D(_MainTex,i.uv-float2(0,px.y)).r);
                float2 normal=-normalize(gradient+float2(.00001,.00001));
                float light=saturate(dot(normal,normalize(float2(-.6,.8))));
                // A thin translucent slick: nearly uniform interior and a very
                // narrow meniscus, not a shaded convex jelly bead.
                float meniscus=exp(-pow((f-.84)*11,2));
                float film=saturate((f-.8)*.38);
                float3 col=lerp(_OilColor.rgb,_EdgeColor.rgb,meniscus*.32);
                float glint=meniscus*pow(light,12)*.52;
                col=lerp(col,_Highlight.rgb,glint);
                float drift=sin(i.uv.x*19+i.uv.y*13+_Time.y*.18)*.5+.5;
                col+=float3(.015,.006,-.004)*drift*film;
                return float4(col,coverage*(.53+film*.07+meniscus*.15))*i.color;
            }
            ENDCG
        }
    }
}
