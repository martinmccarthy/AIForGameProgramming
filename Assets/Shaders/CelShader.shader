Shader "Custom/CelShader"
{
    Properties
    {
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1,1,1,1)

        _ShadowThreshold("Shadow Threshold", Range(0,1)) = 0.5
        _ShadowSmoothness("Shadow Smoothness", Range(0.001,0.2)) = 0.03

        _SpecColor("Specular Color", Color) = (1,1,1,1)
        _SpecThreshold("Specular Threshold", Range(0,1)) = 0.82
        _SpecSmoothness("Specular Smoothness", Range(0.001,0.2)) = 0.02
        _SpecPower("Specular Power", Range(1,128)) = 48

        _AmbientStrength("Ambient Strength", Range(0,1)) = 0.2
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _ShadowThreshold;
                float _ShadowSmoothness;
                float4 _SpecColor;
                float _SpecThreshold;
                float _SpecSmoothness;
                float _SpecPower;
                float _AmbientStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
                float fogCoord : TEXCOORD4;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionHCS = positionInputs.positionCS;
                OUT.positionWS = positionInputs.positionWS;
                OUT.normalWS = normalize(normalInputs.normalWS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.shadowCoord = GetShadowCoord(positionInputs);
                OUT.fogCoord = ComputeFogFactor(positionInputs.positionCS.z);

                return OUT;
            }

            float ToonStep(float value, float threshold, float smoothness)
            {
                return smoothstep(threshold - smoothness, threshold + smoothness, value);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 normalWS = normalize(IN.normalWS);
                float3 viewDirWS = normalize(GetWorldSpaceViewDir(IN.positionWS));

                float4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                float3 albedo = tex.rgb * _BaseColor.rgb;

                Light mainLight = GetMainLight(IN.shadowCoord);

                float3 mainLightDir = normalize(mainLight.direction);
                float NdotL = saturate(dot(normalWS, mainLightDir));
                float mainAtten = mainLight.shadowAttenuation;
                float toonLight = ToonStep(NdotL * mainAtten, _ShadowThreshold, _ShadowSmoothness);

                float3 color = albedo * (_AmbientStrength + toonLight * mainLight.color);

                float3 reflectDir = reflect(-mainLightDir, normalWS);
                float RdotV = saturate(dot(reflectDir, viewDirWS));
                float spec = pow(RdotV, _SpecPower);
                spec = ToonStep(spec * mainAtten, _SpecThreshold, _SpecSmoothness);
                spec *= toonLight;
                color += spec * _SpecColor.rgb * mainLight.color;

                #ifdef _ADDITIONAL_LIGHTS
                uint lightCount = GetAdditionalLightsCount();
                for (uint i = 0; i < lightCount; i++)
                {
                    Light light = GetAdditionalLight(i, IN.positionWS);

                    float3 lightDir = normalize(light.direction);
                    float atten = light.distanceAttenuation * light.shadowAttenuation;

                    float addNdotL = saturate(dot(normalWS, lightDir));
                    float addToon = ToonStep(addNdotL * atten, _ShadowThreshold, _ShadowSmoothness);
                    color += albedo * addToon * light.color;

                    float3 addReflectDir = reflect(-lightDir, normalWS);
                    float addRdotV = saturate(dot(addReflectDir, viewDirWS));
                    float addSpec = pow(addRdotV, _SpecPower);
                    addSpec = ToonStep(addSpec * atten, _SpecThreshold, _SpecSmoothness);
                    addSpec *= addToon;
                    color += addSpec * _SpecColor.rgb * light.color;
                }
                #endif

                color = MixFog(color, IN.fogCoord);

                return half4(color, tex.a * _BaseColor.a);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}