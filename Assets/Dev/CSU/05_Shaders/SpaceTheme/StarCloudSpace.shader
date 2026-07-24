Shader "CSU/Space Theme/Star Cloud"
{
    Properties
    {
        [PerRendererData] [NoScaleOffset]
        _MainTex ("Sprite Texture", 2D) = "white" {}
        [HideInInspector] _EffectEnabled ("Effect Enabled", Float) = 1
        [HideInInspector] _CloudTint ("Cloud Tint", Color) =
            (0.78, 0.9, 1, 1)
        [HideInInspector] _EmissionColor ("Emission Color", Color) =
            (0.1, 0.65, 1.2, 1)
        [HideInInspector] _EmissionStrength ("Emission Strength", Float) =
            0.28
        [HideInInspector] _FlowSpeed ("Flow Speed", Vector) =
            (0.008, 0.003, 0, 0)
        [HideInInspector] _DistortionStrength ("Distortion Strength", Float) =
            0.002
        [HideInInspector] _NoiseScale ("Noise Scale", Float) = 3.5
        [HideInInspector] _OverallOpacity ("Overall Opacity", Float) = 0.95
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
                half4 _CloudTint;
                half4 _EmissionColor;
                float4 _FlowSpeed;
                float _EffectEnabled;
                float _EmissionStrength;
                float _DistortionStrength;
                float _NoiseScale;
                float _OverallOpacity;
            CBUFFER_END

            float Hash21(float2 value)
            {
                value = frac(value * float2(123.34, 456.21));
                value += dot(value, value + 45.32);
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
                float2 movingNoiseUv =
                    input.uv * max(_NoiseScale, 0.001)
                    + _Time.y * _FlowSpeed.xy;
                float primaryNoise = ValueNoise(movingNoiseUv);
                float secondaryNoise =
                    ValueNoise(movingNoiseUv + float2(17.2, 9.1));
                float2 distortion =
                    (float2(primaryNoise, secondaryNoise) - 0.5)
                    * _DistortionStrength
                    * effectAmount;
                float2 sampleUv = saturate(input.uv + distortion);
                half4 textureColor =
                    SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, sampleUv);

                half3 tintedColor = textureColor.rgb * lerp(
                    half3(1.0, 1.0, 1.0),
                    _CloudTint.rgb,
                    effectAmount);
                half luminance = max(
                    tintedColor.r,
                    max(tintedColor.g, tintedColor.b));
                half emissionMask =
                    smoothstep(0.25, 0.9, luminance)
                    * (0.75 + 0.25 * primaryNoise);
                half3 emission =
                    _EmissionColor.rgb
                    * _EmissionStrength
                    * emissionMask
                    * textureColor.a
                    * effectAmount;

                half4 outputColor;
                outputColor.rgb =
                    tintedColor * input.color.rgb + emission;
                outputColor.a =
                    textureColor.a
                    * input.color.a
                    * lerp(1.0, saturate(_OverallOpacity), effectAmount);
                return outputColor;
            }
            ENDHLSL
        }
    }
}
