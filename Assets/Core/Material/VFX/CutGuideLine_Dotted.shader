Shader "PixelForge/VFX/Cut Guide Line Dotted"
{
    Properties
    {
        _MainTex ("Tiling Control", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _DotRadius ("Dot Radius", Range(0.05, 0.5)) = 0.28
        _Softness ("Edge Softness", Range(0.001, 0.2)) = 0.04
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        ZTest LEqual

        Pass
        {
            Name "Unlit"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float4 _MainTex_ST;
                float _DotRadius;
                float _Softness;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv * _MainTex_ST.xy + _MainTex_ST.zw;
                output.color = input.color * _Color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 cellUv = frac(input.uv);
                float distanceFromCenter = distance(cellUv, float2(0.5, 0.5));
                float dotAlpha = smoothstep(_DotRadius + _Softness, _DotRadius - _Softness, distanceFromCenter);

                half4 color = input.color;
                color.a *= dotAlpha;
                return color;
            }
            ENDHLSL
        }
    }
}
