Shader "Custom/PopupSpriteOutline"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Tint("Tint", Color) = (1,1,1,1)

        _StrokeColorA("Stroke Color A", Color) = (1,0.2,0.8,1)
        _StrokeColorB("Stroke Color B", Color) = (0.2,1,1,1)
        _StrokeColorC("Stroke Color C", Color) = (1,0.9,0.2,1)

        _StrokeThickness("Stroke Thickness", Range(0, 8)) = 1.5
        _FlowSpeed("Flow Speed", Float) = 2.0
        _FlowScale("Flow Scale", Float) = 8.0

        _FillGlowColor("Fill Glow Color", Color) = (1,1,1,1)
        _FillGlowStrength("Fill Glow Strength", Range(0, 2)) = 0.35

        _Brightness("Brightness", Range(0, 4)) = 1.2
        _AlphaClip("Alpha Clip", Range(0,1)) = 0.05
        _Softness("Stroke Softness", Range(0.001, 1)) = 0.15

        _WaveAmplitudeX("Wave Amplitude X", Range(0, 0.1)) = 0.01
        _WaveFrequencyX("Wave Frequency X", Float) = 12
        _WaveSpeedX("Wave Speed X", Float) = 4

        _WaveAmplitudeY("Wave Amplitude Y", Range(0, 0.1)) = 0.0
        _WaveFrequencyY("Wave Frequency Y", Float) = 12
        _WaveSpeedY("Wave Speed Y", Float) = 4
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _MainTex_TexelSize;
                float4 _Tint;

                float4 _StrokeColorA;
                float4 _StrokeColorB;
                float4 _StrokeColorC;

                float _StrokeThickness;
                float _FlowSpeed;
                float _FlowScale;

                float4 _FillGlowColor;
                float _FillGlowStrength;

                float _Brightness;
                float _AlphaClip;
                float _Softness;

                float _WaveAmplitudeX;
                float _WaveFrequencyX;
                float _WaveSpeedX;

                float _WaveAmplitudeY;
                float _WaveFrequencyY;
                float _WaveSpeedY;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            float2 WarpUV(float2 uv)
            {
                float t = _Time.y;

                uv.y += sin(uv.x * _WaveFrequencyX + t * _WaveSpeedX) * _WaveAmplitudeX;
                uv.x += sin(uv.y * _WaveFrequencyY + t * _WaveSpeedY) * _WaveAmplitudeY;

                return uv;
            }

            float SampleAlpha(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                float2 warpedUV = WarpUV(IN.uv);

                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, warpedUV) * _Tint;
                float alpha = tex.a;

                float2 texel = _MainTex_TexelSize.xy * _StrokeThickness;

                float a1 = SampleAlpha(warpedUV + float2( texel.x, 0));
                float a2 = SampleAlpha(warpedUV + float2(-texel.x, 0));
                float a3 = SampleAlpha(warpedUV + float2(0,  texel.y));
                float a4 = SampleAlpha(warpedUV + float2(0, -texel.y));
                float a5 = SampleAlpha(warpedUV + float2( texel.x,  texel.y));
                float a6 = SampleAlpha(warpedUV + float2(-texel.x,  texel.y));
                float a7 = SampleAlpha(warpedUV + float2( texel.x, -texel.y));
                float a8 = SampleAlpha(warpedUV + float2(-texel.x, -texel.y));

                float maxNeighbor = max(max(max(a1, a2), max(a3, a4)), max(max(a5, a6), max(a7, a8)));

                float outside = 1.0 - step(_AlphaClip, alpha);
                float nearShape = step(_AlphaClip, maxNeighbor);
                float strokeMask = outside * nearShape;

                float softStroke = saturate((maxNeighbor - _AlphaClip) / max(_Softness, 0.0001)) * outside;

                float flowTime = _Time.y * _FlowSpeed;
                float flow = sin(warpedUV.x * _FlowScale + flowTime)
                           + sin((warpedUV.y + warpedUV.x * 0.35) * (_FlowScale * 0.8) - flowTime * 1.1)
                           + sin((warpedUV.x - warpedUV.y) * (_FlowScale * 0.6) + flowTime * 0.7);

                flow = flow / 3.0;
                flow = flow * 0.5 + 0.5;

                float3 gradAB = lerp(_StrokeColorA.rgb, _StrokeColorB.rgb, saturate(flow));
                float3 strokeColor = lerp(gradAB, _StrokeColorC.rgb, saturate(flow * flow));

                float fillEdge = saturate((alpha - _AlphaClip) / max(_Softness, 0.0001));
                float innerGlow = (1.0 - fillEdge) * alpha * _FillGlowStrength;

                float3 finalRgb = tex.rgb;
                finalRgb += _FillGlowColor.rgb * innerGlow;
                finalRgb = lerp(finalRgb, strokeColor, saturate(strokeMask + softStroke));

                float finalAlpha = max(alpha, saturate(strokeMask + softStroke));

                return half4(finalRgb * _Brightness, finalAlpha);
            }
            ENDHLSL
        }
    }
}