Shader "SkyRacers/Exhaust"
{
    Properties { [HDR] _BaseColor("Flame", Color) = (.22,.55,2,.85) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings vert(Attributes v) { Varyings o; o.positionCS = TransformObjectToHClip(v.positionOS.xyz); o.uv = v.uv; return o; }
            half4 frag(Varyings i) : SV_Target
            {
                float diamonds = .65 + .35 * pow(saturate(sin(i.uv.y * 32 - _Time.y * 4)), 3);
                float flicker = .9 + .1 * sin(i.uv.x * 33 + i.uv.y * 55 - _Time.y * 45);
                float fade = pow(1 - i.uv.y, .7);
                return half4(_BaseColor.rgb * diamonds * flicker, _BaseColor.a * fade);
            }
            ENDHLSL
        }
    }
}
