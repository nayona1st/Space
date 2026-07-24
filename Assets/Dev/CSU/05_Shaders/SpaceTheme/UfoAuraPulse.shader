Shader "CSU/Space Theme/UFO Aura Pulse"
{
    Properties
    {
        [PerRendererData] [NoScaleOffset]
        _MainTex ("Sprite Texture", 2D) = "white" {}
        [HideInInspector] _EffectEnabled ("Effect Enabled", Float) = 1
        [HideInInspector] _AuraColor ("Aura Color", Color) =
            (0.15, 1.15, 0.75, 1)
        [HideInInspector] _EmissionStrength ("Emission Strength", Float) =
            0.35
        [HideInInspector] _PulseSpeed ("Pulse Speed", Float) = 1.1
        [HideInInspector] _PulseAmount ("Pulse Amount", Float) = 0.18
        [HideInInspector] _MinimumBrightness ("Minimum Brightness", Float) =
            0.82
        [HideInInspector] _FlowSpeed ("Flow Speed", Float) = 0.035
        [HideInInspector] _DistortionStrength ("Distortion Strength", Float) =
            0.002
        [HideInInspector] _NoiseScale ("Noise Scale", Float) = 4.5
        [HideInInspector] _EdgeSoftness ("Edge Softness", Float) = 0.12
        [HideInInspector] _OverallOpacity ("Overall Opacity", Float) = 0.92
        [HideInInspector] _BodyCenter ("Body Center", Vector) =
            (0.5, 0.55, 0, 0)
        [HideInInspector] _BodyHalfSize ("Body Half Size", Vector) =
            (0.3, 0.24, 0, 0)
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
                half4 _AuraColor;
                float4 _BodyCenter;
                float4 _BodyHalfSize;
                float _EffectEnabled;
                float _EmissionStrength;
                float _PulseSpeed;
                float _PulseAmount;
                float _MinimumBrightness;
                float _FlowSpeed;
                float _DistortionStrength;
                float _NoiseScale;
                float _EdgeSoftness;
                float _OverallOpacity;
            CBUFFER_END

            float Hash21(float2 value)
            {
                value = frac(value * float2(127.1, 311.7));
                value += dot(value, value + 19.19);
                return frac(value.x * value.y);
            }

            float ValueNoise(float2 value)
            {
                float2 cell = floor(value);
                float2 fraction = frac(value);
                fraction = fraction * fraction * (3.0 - 2.0 * fraction);

                float bottom = lerp(
                    Hash21(cell),
                    Hash21(cell + float2(1.0, 0.0)),
                    fraction.x);
                float top = lerp(
                    Hash21(cell + float2(0.0, 1.0)),
                    Hash21(cell + float2(1.0, 1.0)),
                    fraction.x);
                return lerp(bottom, top, fraction.y);
            }

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
                float effectAmount = saturate(_EffectEnabled);
                float2 protectedHalfSize =
                    max(_BodyHalfSize.xy, float2(0.01, 0.01));
                float bodyDistance = length(
                    (input.uv - _BodyCenter.xy) / protectedHalfSize);
                float bodyMask =
                    1.0 - smoothstep(0.82, 1.12, bodyDistance);
                float auraMask = 1.0 - bodyMask;
                float auraEffect = auraMask * effectAmount;

                float2 movingNoiseUv =
                    input.uv * max(_NoiseScale, 0.001)
                    + float2(_Time.y * _FlowSpeed, 0.0);
                float primaryNoise = ValueNoise(movingNoiseUv);
                float secondaryNoise =
                    ValueNoise(movingNoiseUv + float2(13.4, 7.7));
                float2 distortion =
                    (float2(primaryNoise, secondaryNoise) - 0.5)
                    * _DistortionStrength
                    * auraEffect;

                half4 originalColor =
                    SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half4 auraTexture = SAMPLE_TEXTURE2D(
                    _MainTex,
                    sampler_MainTex,
                    saturate(input.uv + distortion));

                float pulseWave =
                    0.5 + 0.5 * sin(_Time.y * _PulseSpeed);
                float pulseBrightness = lerp(
                    saturate(_MinimumBrightness),
                    1.0,
                    pulseWave);
                float finalBrightness = lerp(
                    1.0,
                    pulseBrightness,
                    saturate(_PulseAmount));
                float edgeFade = smoothstep(
                    0.0,
                    max(_EdgeSoftness, 0.001),
                    auraTexture.a);

                half3 auraBase = auraTexture.rgb * finalBrightness;
                half3 auraEmission =
                    _AuraColor.rgb
                    * _EmissionStrength
                    * (0.7 + 0.3 * primaryNoise)
                    * auraTexture.a;
                half3 finalRgb = lerp(
                    originalColor.rgb,
                    auraBase + auraEmission,
                    auraEffect);
                half auraAlpha =
                    auraTexture.a
                    * edgeFade
                    * saturate(_OverallOpacity);

                half4 outputColor;
                outputColor.rgb = finalRgb * input.color.rgb;
                outputColor.a = lerp(
                    originalColor.a,
                    auraAlpha,
                    auraEffect) * input.color.a;
                return outputColor;
            }
            ENDHLSL
        }
    }
}
