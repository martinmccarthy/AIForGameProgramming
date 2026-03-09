Shader "Custom/ToonShader"
{
    Properties
    {
        [HDR]_Color("Albedo", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}

        [Header(Cel Shading)]
        _ShadowColor ("Shadow Color", Color) = (0.15, 0.15, 0.25, 1)
        _ShadowThreshold ("Shadow Threshold", Range(-1,1)) = 0.0
        _ShadowSoftness ("Shadow Softness", Range(0,0.5)) = 0.05

        _MidtoneColor ("Midtone Color", Color) = (0.6, 0.6, 0.65, 1)
        _MidtoneThreshold ("Midtone Threshold", Range(-1,1)) = 0.4
        _MidtoneSoftness ("Midtone Softness", Range(0,0.5)) = 0.05

        [Header(Specular)]
        _SpecularColor ("Specular Color", Color) = (1,1,1,1)
        _SpecularSize ("Specular Size", Range(0,1)) = 0.1
        _SpecularSoftness ("Specular Softness", Range(0,0.2)) = 0.02
        _Glossiness ("Glossiness", Range(1,256)) = 64

        [Header(Rim Light)]
        _RimColor ("Rim Color", Color) = (0.4, 0.6, 1.0, 1)
        _RimThreshold ("Rim Threshold", Range(0,1)) = 0.5
        _RimSoftness ("Rim Softness", Range(0,0.3)) = 0.1
        _RimStrength ("Rim Strength", Range(0,2)) = 1.0

        [Header(Stencil)]
        _Stencil ("Stencil ID [0;255]", Float) = 0
        _ReadMask ("ReadMask [0;255]", Int) = 255
        _WriteMask ("WriteMask [0;255]", Int) = 255
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("Stencil Comparison", Int) = 0
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilOp ("Stencil Operation", Int) = 0
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilFail ("Stencil Fail", Int) = 0
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilZFail ("Stencil ZFail", Int) = 0

        [Header(Rendering)]
        _Offset("Offset", float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Culling ("Cull Mode", Int) = 2
        [Enum(Off,0,On,1)] _ZWrite("ZWrite", Int) = 1
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Int) = 4
        [Enum(None,0,Alpha,1,Red,8,Green,4,Blue,2,RGB,14,RGBA,15)] _ColorMask("Color Mask", Int) = 15
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Stencil
        {
            Ref       [_Stencil]
            ReadMask  [_ReadMask]
            WriteMask [_WriteMask]
            Comp      [_StencilComp]
            Pass      [_StencilOp]
            Fail      [_StencilFail]
            ZFail     [_StencilZFail]
        }

        // -------------------------------------------------------
        // Forward Lit Pass
        // -------------------------------------------------------
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull      [_Culling]
            Offset    [_Offset], [_Offset]
            ZWrite    [_ZWrite]
            ZTest     [_ZTest]
            ColorMask [_ColorMask]

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            // URP keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma multi_compile _ STEREO_INSTANCING_ON  // VR single-pass instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // ---- Properties ----
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4  _Color;
                half4  _ShadowColor;
                float  _ShadowThreshold;
                float  _ShadowSoftness;
                half4  _MidtoneColor;
                float  _MidtoneThreshold;
                float  _MidtoneSoftness;
                half4  _SpecularColor;
                float  _SpecularSize;
                float  _SpecularSoftness;
                float  _Glossiness;
                half4  _RimColor;
                float  _RimThreshold;
                float  _RimSoftness;
                float  _RimStrength;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            // ---- Structs ----
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                float3 normalWS    : TEXCOORD2;
                float  fogFactor   : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // ---- Cel step helper ----
            half CelStep(float val, float threshold, float softness)
            {
                return smoothstep(threshold - softness, threshold + softness, val);
            }

            // ---- Vertex ----
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   nrmInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;
                OUT.normalWS   = nrmInputs.normalWS;
                OUT.uv         = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.fogFactor  = ComputeFogFactor(posInputs.positionCS.z);
                return OUT;
            }

            // ---- Fragment ----
            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                // Base color
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * _Color;

                float3 N = normalize(IN.normalWS);
                float3 V = normalize(GetWorldSpaceViewDir(IN.positionWS));

                // ---- Main directional light ----
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(IN.positionWS));
                float3 L = normalize(mainLight.direction);
                float3 H = normalize(L + V);

                float shadow   = mainLight.shadowAttenuation;
                float3 lightCol = mainLight.color;

                float NdotL  = dot(N, L) * 0.5 + 0.5;
                float lightVal = NdotL * shadow;

                // Three-tone banding from main light
                half s = CelStep(lightVal, _ShadowThreshold  + 0.5, _ShadowSoftness);
                half m = CelStep(lightVal, _MidtoneThreshold + 0.5, _MidtoneSoftness);

                half4 diffuse = lerp(_ShadowColor, _MidtoneColor, s);
                diffuse       = lerp(diffuse, half4(1,1,1,1), m);
                diffuse.rgb  *= lightCol;
                diffuse      *= texColor;

                // Main light toon specular
                float NdotH    = saturate(dot(N, H));
                float spec     = pow(NdotH, _Glossiness * _Glossiness);
                float specToon = CelStep(spec, _SpecularSize, _SpecularSoftness) * shadow;
                half4 specular = specToon * _SpecularColor;

                // ---- Additional lights (point lights, spot lights) ----
                half3 additionalDiffuse  = half3(0,0,0);
                half3 additionalSpecular = half3(0,0,0);

                int additionalLightCount = GetAdditionalLightsCount();
                for (int i = 0; i < additionalLightCount; i++)
                {
                    Light light = GetAdditionalLight(i, IN.positionWS);

                    float3 AL = normalize(light.direction);
                    float3 AH = normalize(AL + V);

                    // Attenuated NdotL — use raw (not hemisphere-wrapped) so point
                    // lights fall off naturally, then cel-snap just the lit/unlit boundary
                    float ANdotL   = saturate(dot(N, AL));
                    float atten    = light.distanceAttenuation * light.shadowAttenuation;
                    float aLightVal = ANdotL * atten;

                    // Single hard step: lit or not — keeps extra lights punchy not mushy
                    half aStep = CelStep(aLightVal, 0.1, 0.05);

                    additionalDiffuse  += aStep * light.color * texColor.rgb;

                    // Toon specular for this light
                    float ANdotH    = saturate(dot(N, AH));
                    float aSpec     = pow(ANdotH, _Glossiness * _Glossiness);
                    float aSpecToon = CelStep(aSpec, _SpecularSize, _SpecularSoftness) * atten;
                    additionalSpecular += aSpecToon * _SpecularColor.rgb * light.color;
                }

                // ---- Rim light ----
                float rim     = 1.0 - saturate(dot(V, N));
                float rimToon = CelStep(rim, _RimThreshold, _RimSoftness);
                half4 rimLight = rimToon * _RimColor * _RimStrength;

                // ---- Combine ----
                half4 col;
                col.rgb = diffuse.rgb + specular.rgb
                        + additionalDiffuse + additionalSpecular
                        + rimLight.rgb;
                col.rgb = MixFog(col.rgb, IN.fogFactor);
                col.a   = texColor.a;
                return col;
            }

            ENDHLSL
        }

        // -------------------------------------------------------
        // Shadow Caster Pass
        // -------------------------------------------------------
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest  LEqual
            Cull   [_Culling]

            HLSLPROGRAM
            #pragma vertex   vertShadow
            #pragma fragment fragShadow
            #pragma multi_compile_instancing
            #pragma multi_compile _ STEREO_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        // -------------------------------------------------------
        // Depth Only Pass (required by URP for depth prepass / SSAO)
        // -------------------------------------------------------
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite    On
            ColorMask 0
            Cull      [_Culling]

            HLSLPROGRAM
            #pragma vertex   DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ STEREO_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }
    }
}