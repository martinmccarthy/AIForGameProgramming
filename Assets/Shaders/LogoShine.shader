Shader "Custom/LogoShine"
{
    Properties
    {
        _MainTex("Logo Texture", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1,1,1,1)

        _ShineColor("Shine Color", Color) = (1,1,1,1)
        _ShineIntensity("Shine Intensity", Range(0,5)) = 1.5
        _ShineWidth("Shine Width", Range(0.01,1)) = 0.18
        _ShineSoftness("Shine Softness", Range(0.001,1)) = 0.12

        _ShineAngle("Shine Angle (Degrees)", Range(0,360)) = 45
        _ShinePosition("Shine Position", Range(-2,2)) = -1
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
        ZWrite Off
        Cull Off

        Pass
        {
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
                float4 _BaseColor;
                float4 _ShineColor;
                float _ShineIntensity;
                float _ShineWidth;
                float _ShineSoftness;
                float _ShineAngle;
                float _ShinePosition;
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

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                float2 uv = IN.uv;

                half4 baseTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv) * _BaseColor;

                // 🔥 Convert degrees → radians
                float angleRad = radians(_ShineAngle);

                float2 centered = uv - 0.5;
                float2 dir = float2(cos(angleRad), sin(angleRad));

                float projected = dot(centered, dir);

                float dist = abs(projected - _ShinePosition);
                float shine = 1.0 - smoothstep(_ShineWidth, _ShineWidth + _ShineSoftness, dist);

                half3 finalRgb = baseTex.rgb + (_ShineColor.rgb * shine * _ShineIntensity * baseTex.a);
                half finalA = baseTex.a;

                return half4(finalRgb, finalA);
            }
            ENDHLSL
        }
    }
}