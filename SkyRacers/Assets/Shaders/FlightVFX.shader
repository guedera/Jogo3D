Shader "SkyRacers/FlightVFX"
{
 Properties { _Ribbon("Ribbon mask", Range(0,1)) = 0 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
  Pass
  {
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off
   Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   CBUFFER_START(UnityPerMaterial)
   float _Ribbon;
   CBUFFER_END
   struct A { float4 vertex:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
   struct V { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
   V vert(A i) { V o;o.vertex=TransformObjectToHClip(i.vertex.xyz);o.uv=i.uv;o.color=i.color;return o; }
   half4 frag(V i):SV_Target
   {
    float radial = pow(saturate(1-length(i.uv-.5)*2),1.5);
    // A trail must fade across its width, not disappear at UV.x=0 (the wing).
    float ribbon = pow(saturate(1-abs(i.uv.y-.5)*2),.8);
    return half4(i.color.rgb,i.color.a*lerp(radial,ribbon,_Ribbon));
   }
   ENDHLSL
  }
 }
}
