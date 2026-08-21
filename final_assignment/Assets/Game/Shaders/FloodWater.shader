Shader "Skyhook Ascent/Flood Water"
{
    Properties
    {
        _ShallowColor ("Shallow Color", Color) = (0.12, 0.72, 0.95, 0.82)
        _DeepColor ("Deep Color", Color) = (0.015, 0.12, 0.48, 0.9)
        _FoamColor ("Crest Color", Color) = (0.68, 0.95, 1, 1)
        _WaveScale ("Wave Scale", Range(0.1, 3)) = 0.65
        _WaveSpeed ("Wave Speed", Range(0, 4)) = 1.1
        _WaveStrength ("Wave Strength", Range(0, 0.25)) = 0.07
        _Smoothness ("Smoothness", Range(0, 1)) = 0.82
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "FloodWater"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor;
                half4 _DeepColor;
                half4 _FoamColor;
                float _WaveScale;
                float _WaveSpeed;
                float _WaveStrength;
                float _Smoothness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float topMask = saturate(input.normalOS.y * 8.0);
                float wave = sin(positionWS.x * _WaveScale + _Time.y * _WaveSpeed);
                wave += cos(positionWS.z * (_WaveScale * 0.83) - _Time.y * (_WaveSpeed * 1.17));
                positionWS.y += wave * 0.5 * _WaveStrength * topMask;

                output.positionWS = positionWS;
                output.positionHCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float rippleA = sin((input.positionWS.x + input.positionWS.z) * 0.48 + _Time.y * 1.3);
                float rippleB = cos((input.positionWS.x - input.positionWS.z) * 0.71 - _Time.y * 1.65);
                float ripple = saturate(rippleA * rippleB * 0.5 + 0.5);

                half3 viewDirection = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));
                half fresnel = pow(1.0h - saturate(dot(normalize(input.normalWS), viewDirection)), 3.0h);
                half3 waterColor = lerp(_DeepColor.rgb, _ShallowColor.rgb, saturate(ripple * 0.45 + fresnel));
                half crest = smoothstep(0.82h, 1.0h, ripple) * 0.28h;
                waterColor = lerp(waterColor, _FoamColor.rgb, crest);

                half alpha = lerp(_ShallowColor.a, _DeepColor.a, fresnel);
                alpha = lerp(alpha, 1.0h, _Smoothness * 0.08h);
                return half4(waterColor, alpha);
            }
            ENDHLSL
        }
    }
}
