Shader "AudioBugs/PrototypeReveal"
{
    Properties
    {
        _BaseColor("Color", Color) = (1,1,1,1)
        _Visibility("Visibility", Range(0,1)) = 1
        _RoundMask("Round Mask", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            float4 _BaseColor;
            float _Visibility;
            float _RoundMask;
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float alpha = _BaseColor.a * _Visibility;
                if (_RoundMask > .5) alpha *= 1 - smoothstep(.42, .5, length(input.uv - .5));
                return half4(_BaseColor.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
