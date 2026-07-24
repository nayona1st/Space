Shader "CSU/Visual Effects/Selective Sprite Emission"
{
    Properties
    {
        [PerRendererData] [NoScaleOffset]
        _MainTex ("Sprite Texture", 2D) = "white" {}
        [HideInInspector] _EffectEnabled ("Effect Enabled", Float) = 1
        [HideInInspector] _BaseColor ("Base Color", Color) =
            (1, 1, 1, 1)
        [HDR] _EmissionColor ("Emission Color", Color) =
            (0.4, 0.8, 1, 1)
        _EmissionStrength ("Aura Emission Strength", Float) = 0
        _BodyEmissionStrength ("Body Emission Strength", Float) = 0
        _OverallOpacity ("Overall Opacity", Range(0, 1)) = 1
        [HideInInspector] _MaskMode ("Mask Mode", Float) = 0
        _BrightnessThreshold ("Brightness Threshold", Range(0, 1)) = 0.55
        _ThresholdSoftness ("Threshold Softness", Range(0.001, 0.5)) =
            0.12
        _OutputClamp ("Output Clamp", Range(1, 10)) = 4
        [HideInInspector] _BodyCenter ("Body Center", Vector) =
            (0.5, 0.55, 0, 0)
        [HideInInspector] _BodyHalfSize ("Body Half Size", Vector) =
            (0.3, 0.24, 0, 0)
        [HideInInspector] _PulseSpeed ("Pulse Speed", Float) = 0
        [HideInInspector] _PulseAmount ("Pulse Amount", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Name "Universal2D"
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag

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

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _EmissionColor;
                float4 _BodyCenter;
                float4 _BodyHalfSize;
                float _EffectEnabled;
                float _EmissionStrength;
                float _BodyEmissionStrength;
                float _OverallOpacity;
                float _MaskMode;
                float _BrightnessThreshold;
                float _ThresholdSoftness;
                float _OutputClamp;
                float _PulseSpeed;
                float _PulseAmount;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS =
                    TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 textureColor =
                    SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float effectAmount = saturate(_EffectEnabled);
                half3 baseCorrection = lerp(
                    half3(1.0, 1.0, 1.0),
                    _BaseColor.rgb,
                    effectAmount);
                half3 baseRgb =
                    textureColor.rgb
                    * input.color.rgb
                    * baseCorrection;

                half sourceBrightness = max(
                    textureColor.r,
                    max(textureColor.g, textureColor.b));
                float softness = max(_ThresholdSoftness, 0.001);
                half brightnessMask = smoothstep(
                    _BrightnessThreshold - softness,
                    _BrightnessThreshold + softness,
                    sourceBrightness);

                float2 safeHalfSize =
                    max(_BodyHalfSize.xy, float2(0.001, 0.001));
                float bodyDistance = length(
                    (input.uv - _BodyCenter.xy) / safeHalfSize);
                half bodyMask =
                    1.0 - smoothstep(0.85, 1.05, bodyDistance);
                half auraMask = 1.0 - bodyMask;
                half ufoMask =
                    bodyMask * brightnessMask
                    + auraMask * (0.25 + 0.75 * brightnessMask);
                half selectedMask = lerp(
                    brightnessMask,
                    saturate(ufoMask),
                    saturate(_MaskMode));

                float pulseWave =
                    0.5 + 0.5 * sin(_Time.y * _PulseSpeed);
                float auraPulse = lerp(
                    1.0,
                    0.85 + 0.15 * pulseWave,
                    saturate(_PulseAmount));
                float selectedStrength = lerp(
                    _EmissionStrength,
                    lerp(
                        _EmissionStrength * auraPulse,
                        _BodyEmissionStrength,
                        bodyMask),
                    saturate(_MaskMode));
                half3 emission =
                    _EmissionColor.rgb
                    * selectedStrength
                    * selectedMask
                    * textureColor.a
                    * effectAmount;

                half3 finalRgb = min(
                    baseRgb + emission,
                    max(_OutputClamp, 1.0));
                half finalAlpha =
                    textureColor.a
                    * input.color.a
                    * lerp(
                        1.0,
                        saturate(_OverallOpacity),
                        effectAmount);
                return half4(finalRgb, finalAlpha);
            }
            ENDHLSL
        }
    }
}
