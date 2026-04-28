Shader "Custom/TerrainEdgeOutline"
{
    Properties
    {
        [HideInInspector] _Control    ("Control (RGBA)", 2D)  = "red"   {}
        [HideInInspector] _Splat0     ("Layer 0 (R)",   2D)  = "white" {}
        [HideInInspector] _Splat1     ("Layer 1 (G)",   2D)  = "white" {}
        [HideInInspector] _Splat2     ("Layer 2 (B)",   2D)  = "white" {}
        [HideInInspector] _Splat3     ("Layer 3 (A)",   2D)  = "white" {}

        [Header(Edge Bands)]
        _Band1Color     ("Band 1 Color (inner)",  Color)          = (0.08, 0.05, 0.02, 1)
        _Band1Width     ("Band 1 Width",          Range(0, 0.5))  = 0.05
        _Band2Color     ("Band 2 Color (mid)",    Color)          = (0.35, 0.22, 0.08, 1)
        _Band2Width     ("Band 2 Width",          Range(0, 0.5))  = 0.12
        _Band3Color     ("Band 3 Color (outer)",  Color)          = (0.65, 0.50, 0.25, 1)
        _Band3Width     ("Band 3 Width",          Range(0, 0.5))  = 0.22
        _EdgeStrength   ("Edge Strength",         Range(0, 5))    = 1.5

        [Header(Cel Shading)]
        _CelBands       ("Light Bands",           Range(2, 8))    = 3
        _ShadowCutoff   ("Shadow Cutoff",         Range(0, 1))    = 0.15
        _ShadowColor    ("Shadow Tint",           Color)          = (0.25, 0.22, 0.35, 1)
        _SaturationBoost("Saturation Boost",      Range(0, 2))    = 1.3
    }

    SubShader
    {
        Tags
        {
            "RenderType"    = "Opaque"
            "RenderPipeline"= "UniversalPipeline"
            "Queue"         = "Geometry-100"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // ── Textures ────────────────────────────────────────────────────
            TEXTURE2D(_Control); SAMPLER(sampler_Control);
            TEXTURE2D(_Splat0);  SAMPLER(sampler_Splat0);
            TEXTURE2D(_Splat1);  SAMPLER(sampler_Splat1);
            TEXTURE2D(_Splat2);  SAMPLER(sampler_Splat2);
            TEXTURE2D(_Splat3);  SAMPLER(sampler_Splat3);

            CBUFFER_START(UnityPerMaterial)
                float4 _Control_ST;
                float4 _Splat0_ST;
                float4 _Splat1_ST;
                float4 _Splat2_ST;
                float4 _Splat3_ST;
                half4  _Band1Color;
                float  _Band1Width;
                half4  _Band2Color;
                float  _Band2Width;
                half4  _Band3Color;
                float  _Band3Width;
                float  _EdgeStrength;
                float  _CelBands;
                float  _ShadowCutoff;
                half4  _ShadowColor;
                float  _SaturationBoost;
            CBUFFER_END

            // ── Vertex ──────────────────────────────────────────────────────
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uvControl   : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 positionWS  : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = posInputs.positionCS;
                OUT.positionWS  = posInputs.positionWS;
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uvControl   = TRANSFORM_TEX(IN.uv, _Control);
                OUT.shadowCoord = GetShadowCoord(posInputs);
                return OUT;
            }

            // ── Edge margin ──────────────────────────────────────────────────
            // Returns margin = (top splatmap weight - second weight).
            // 0 = exactly on a layer boundary; higher = deeper inside one layer.
            // Sub-texel precision, resolution-independent.
            float EdgeMargin(float2 uv)
            {
                half4 ctrl = SAMPLE_TEXTURE2D(_Control, sampler_Control, uv);

                half a = ctrl.r, b = ctrl.g, c = ctrl.b, d = ctrl.a;
                half t;
                if (a < b) { t = a; a = b; b = t; }
                if (c < d) { t = c; c = d; d = t; }
                if (a < c) { t = a; a = c; c = t; }
                if (b < d) { t = b; b = d; d = t; }
                if (b < c) { t = b; b = c; c = t; }
                // a >= b >= c >= d

                return a - b;
            }

            // ── Cel helpers ──────────────────────────────────────────────────
            float CelStep(float value, float bands)
            {
                return floor(value * bands) / (bands - 1.0);
            }

            half3 BoostSaturation(half3 col, float amount)
            {
                half lum = dot(col, half3(0.299, 0.587, 0.114));
                return lerp(half3(lum, lum, lum), col, amount);
            }

            // ── Fragment ─────────────────────────────────────────────────────
            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                float2 uv = IN.uvControl;

                // Splatmap weights
                half4 ctrl = SAMPLE_TEXTURE2D(_Control, sampler_Control, uv);

                // Sample terrain layers at their own tiling
                half4 c0 = SAMPLE_TEXTURE2D(_Splat0, sampler_Splat0, uv * _Splat0_ST.xy + _Splat0_ST.zw);
                half4 c1 = SAMPLE_TEXTURE2D(_Splat1, sampler_Splat1, uv * _Splat1_ST.xy + _Splat1_ST.zw);
                half4 c2 = SAMPLE_TEXTURE2D(_Splat2, sampler_Splat2, uv * _Splat2_ST.xy + _Splat2_ST.zw);
                half4 c3 = SAMPLE_TEXTURE2D(_Splat3, sampler_Splat3, uv * _Splat3_ST.xy + _Splat3_ST.zw);

                half4 albedo = c0 * ctrl.r + c1 * ctrl.g + c2 * ctrl.b + c3 * ctrl.a;
                albedo.rgb = BoostSaturation(albedo.rgb, _SaturationBoost);

                // ── Cel lighting ─────────────────────────────────────────────
                float3 normalWS  = normalize(IN.normalWS);
                Light  mainLight = GetMainLight(IN.shadowCoord);
                float  NdotL     = saturate(dot(normalWS, mainLight.direction));

                float shadowMask = mainLight.shadowAttenuation > 0.5 ? 1.0 : 0.0;
                float litValue   = NdotL * shadowMask;
                float celLit     = CelStep(max(litValue, _ShadowCutoff), _CelBands);

                float shadowBlend = 1.0 - saturate((celLit - _ShadowCutoff) / (1.0 - _ShadowCutoff));
                half3 litColor    = mainLight.color * celLit;
                half3 ambient     = SampleSH(normalWS) * 0.4;
                half3 lighting    = lerp(litColor + ambient, _ShadowColor.rgb, shadowBlend * 0.6);
                albedo.rgb *= lighting;

                // ── Multi-band edge overlay ───────────────────────────────────
                // margin=0 is right on the boundary; each band occupies a
                // distinct non-overlapping ring in margin-space.
                // b1End = band1 width, b2End = band1+band2, b3End = all three.
                float margin = EdgeMargin(uv);

                float b1End = _Band1Width;
                float b2End = b1End + _Band2Width;
                float b3End = b2End + _Band3Width;

                // Build a single blended edge color by selecting the right ring.
                // Each lerp paints the next-inner band on top, so the inner ring
                // wins at margin=0 and the outer ring takes over further out.
                half3 edgeColor = _Band3Color.rgb;
                edgeColor = lerp(edgeColor, _Band2Color.rgb, 1.0 - smoothstep(b1End, b2End, margin));
                edgeColor = lerp(edgeColor, _Band1Color.rgb, 1.0 - smoothstep(0.0,   b1End, margin));

                // Fade the whole edge out past the outermost band
                float edgeAlpha = 1.0 - smoothstep(b2End, b3End, margin);
                albedo.rgb = lerp(albedo.rgb, edgeColor, saturate(edgeAlpha * _EdgeStrength));

                return half4(albedo.rgb, 1);
            }
            ENDHLSL
        }

        // Shadow caster pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex   ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
    }

    Fallback "Hidden/Universal Render Pipeline/FallbackError"
}
