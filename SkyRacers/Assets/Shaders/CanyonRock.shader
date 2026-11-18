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
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
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
            float hash3(float3 p) { return frac(sin(dot(p,float3(127.1,311.7,74.7)))*43758.5453); }
            float stoneNoise(float3 p)
            {
                float3 i=floor(p), f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(lerp(hash3(i),hash3(i+float3(1,0,0)),f.x),lerp(hash3(i+float3(0,1,0)),hash3(i+float3(1,1,0)),f.x),f.y),
                    lerp(lerp(hash3(i+float3(0,0,1)),hash3(i+float3(1,0,1)),f.x),lerp(hash3(i+float3(0,1,1)),hash3(i+float3(1,1,1)),f.x),f.y),f.z);
            }
            half4 frag(Varyings i) : SV_Target
            {
                Light light = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half diffuse = saturate(dot(normalize(i.normalWS), light.direction));
                float strata = .8 + .14 * stoneNoise(i.positionWS * .17) + .10 * stoneNoise(i.positionWS * 1.7) + .05 * stoneNoise(i.positionWS * 7);
                if(i.color.b > i.color.g*1.04 && i.color.b > i.color.r*1.15) strata *= .92 + .08*sin(i.positionWS.y*1.6 + i.positionWS.x*.9 + _Time.y*9);
                half3 color = i.color.rgb * strata * (.38 + diffuse * .72 * light.shadowAttenuation) * light.color;
                return half4(MixFog(color, i.fog), 1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}
