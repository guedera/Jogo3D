Shader "SkyRacers/Canyon Rock"
{
    Properties { _BaseMap("Texture", 2D) = "white" {} _BaseColor("Color", Color) = (1,1,1,1) _Cutoff("Cutoff", Float) = .5 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; half3 normalWS : TEXCOORD1; half4 color : COLOR; half fog : TEXCOORD2; };
            Varyings vert(Attributes v)
            {
                Varyings o; o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS); o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.color = v.color; o.fog = ComputeFogFactor(o.positionCS.z); return o;
            }
            half4 frag(Varyings i) : SV_Target
            {
                Light light = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half diffuse = saturate(dot(normalize(i.normalWS), light.direction));
                float strata = .95 + .05 * sin(i.positionWS.y * .31);
                half3 color = i.color.rgb * strata * (.38 + diffuse * .72 * light.shadowAttenuation) * light.color;
                return half4(MixFog(color, i.fog), 1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
