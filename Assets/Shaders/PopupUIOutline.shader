Shader "Custom/PopupUIOutline"
{
    Properties
    {
        _FaceTex("Face Texture", 2D) = "white" {}
        _FaceColor("Face Color", Color) = (1,1,1,1)

        _OutlineColorA("Outline Color A", Color) = (1,0.2,0.8,1)
        _OutlineColorB("Outline Color B", Color) = (0.2,1,1,1)
        _OutlineColorC("Outline Color C", Color) = (1,0.9,0.2,1)

        _OutlineWidth("Outline Width", Range(0,1)) = 0.2
        _OutlineSoftness("Outline Softness", Range(0.001,1)) = 0.05

        _GlowColor("Glow Color", Color) = (1,1,1,1)
        _GlowPower("Glow Power", Range(0,4)) = 0.4

        _FlowSpeed("Flow Speed", Float) = 2
        _FlowScale("Flow Scale", Float) = 8
        _Brightness("Brightness", Range(0,4)) = 1.2

        _MainTex("Font Atlas", 2D) = "white" {}
        _FaceDilate("Face Dilate", Range(-1,1)) = 0

        _StencilComp("Stencil Comparison", Float) = 8
        _Stencil("Stencil ID", Float) = 0
        _StencilOp("Stencil Operation", Float) = 0
        _StencilWriteMask("Stencil Write Mask", Float) = 255
        _StencilReadMask("Stencil Read Mask", Float) = 255

        _CullMode("Cull Mode", Float) = 0
        _ColorMask("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull [_CullMode]
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex VertShader
            #pragma fragment PixShader
            #pragma target 3.0
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex    : POSITION;
                fixed4 color     : COLOR;
                float2 texcoord0 : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex    : SV_POSITION;
                fixed4 faceColor : COLOR;
                float2 uv        : TEXCOORD0;
                float2 flowUV    : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            sampler2D _FaceTex;

            float4 _MainTex_ST;
            float4 _FaceTex_ST;

            fixed4 _FaceColor;
            fixed4 _OutlineColorA;
            fixed4 _OutlineColorB;
            fixed4 _OutlineColorC;
            fixed4 _GlowColor;

            float _OutlineWidth;
            float _OutlineSoftness;
            float _GlowPower;
            float _FlowSpeed;
            float _FlowScale;
            float _Brightness;
            float _FaceDilate;

            float4 GetAnimatedOutlineColor(float2 uv, float timeValue)
            {
                float flow = sin(uv.x * _FlowScale + timeValue * _FlowSpeed)
                           + sin((uv.y + uv.x * 0.35) * (_FlowScale * 0.8) - timeValue * _FlowSpeed * 1.1)
                           + sin((uv.x - uv.y) * (_FlowScale * 0.6) + timeValue * _FlowSpeed * 0.7);

                flow = flow / 3.0;
                flow = flow * 0.5 + 0.5;

                float3 ab = lerp(_OutlineColorA.rgb, _OutlineColorB.rgb, saturate(flow));
                float3 rgb = lerp(ab, _OutlineColorC.rgb, saturate(flow * flow));
                return float4(rgb, 1);
            }

            v2f VertShader(appdata_t input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = TRANSFORM_TEX(input.texcoord0, _MainTex);
                output.flowUV = output.uv;
                output.faceColor = input.color * _FaceColor;
                return output;
            }

            fixed4 PixShader(v2f input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float sdf = tex2D(_MainTex, input.uv).a;

                float center = 0.5 - (_FaceDilate * 0.5);
                float softness = max(_OutlineSoftness, 0.001);

                float faceAlpha = smoothstep(center - softness, center + softness, sdf);

                float outlineEdge = center - _OutlineWidth;
                float outlineAlpha = smoothstep(outlineEdge - softness, outlineEdge + softness, sdf);

                float outlineOnly = saturate(outlineAlpha - faceAlpha);

                float4 faceSample = tex2D(_FaceTex, TRANSFORM_TEX(input.uv, _FaceTex));
                float3 faceRgb = input.faceColor.rgb * faceSample.rgb;

                float fillEdge = 1.0 - faceAlpha;
                float3 glowRgb = _GlowColor.rgb * fillEdge * faceAlpha * _GlowPower;

                float4 animatedOutline = GetAnimatedOutlineColor(input.flowUV, _Time.y);

                float3 rgb = 0;
                rgb += animatedOutline.rgb * outlineOnly;
                rgb += (faceRgb + glowRgb) * faceAlpha;

                float alpha = saturate(outlineAlpha * input.faceColor.a);

                return fixed4(rgb * _Brightness, alpha);
            }
            ENDCG
        }
    }
}