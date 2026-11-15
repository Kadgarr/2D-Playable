// Dance Tap: one shader for the beat-reactive 2D layers (SRP Batcher compatible: all material params in UnityPerMaterial).
//  - stage: neon from a low-res glow mask flashes on every beat (_GlowBoost)
//  - crowd: vertices bounce on the beat as a wave travelling across the crowd (_BeatAmp), bottom row stays fixed
//  - beams: same shader with additive blending
// _BeatPhase (0..1 inside the beat) is a global set once per frame by BeatClock - no per-object scripts.
Shader "DanceTap/Sprite Beat"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _GlowMask ("Glow Mask (R)", 2D) = "black" {}
        _GlowBoost ("Glow Boost On Beat", Range(0, 2)) = 0
        _BeatAmp ("Vertex Bounce (units)", Range(0, 0.5)) = 0
        _WaveFreq ("Bounce Wave Frequency", Float) = 1.3
        _WaveLag ("Bounce Wave Lag (beats per unit)", Float) = 0.06
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Blend [_SrcBlend] [_DstBlend]
        Cull Off ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);  SAMPLER(sampler_MainTex);
            TEXTURE2D(_GlowMask); SAMPLER(sampler_GlowMask);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _GlowMask_ST;
                half _GlowBoost;
                half _BeatAmp;
                half _WaveFreq;
                half _WaveLag;
                half _SrcBlend;
                half _DstBlend;
            CBUFFER_END

            float _BeatPhase;

            struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionCS : SV_POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; half pulse : TEXCOORD1; };

            Varyings vert (Attributes v)
            {
                Varyings o;
                float3 ws = TransformObjectToWorld(v.positionOS.xyz);
                half k = 1.0h - (half)_BeatPhase;
                o.pulse = k * k;                                        // sharp on the beat, fades through it
                float p = frac(_BeatPhase - ws.x * _WaveLag);           // phase lags across x -> travelling wave
                float kb = 1.0 - p; kb *= kb;
                float wave = 0.6 + 0.4 * sin(ws.x * _WaveFreq * 6.2831853);
                ws.y += _BeatAmp * kb * wave * smoothstep(0.15, 0.9, v.uv.y);
                o.positionCS = TransformWorldToHClip(ws);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * i.color;
                half m = SAMPLE_TEXTURE2D(_GlowMask, sampler_GlowMask, i.uv).r;
                c.rgb += c.rgb * m * _GlowBoost * i.pulse;
                return c;
            }
            ENDHLSL
        }
    }
    Fallback Off
}
