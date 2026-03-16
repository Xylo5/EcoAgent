Shader "Custom/BasicWaterShader"
{
    Properties
    {
        _Color ("Background Color", Color) = (0.1, 0.4, 0.8, 0.8)
        _TextureColor ("Texture Color", Color) = (1, 1, 1, 1)
        _MainTex ("Water Texture", 2D) = "white" {}
        _WaveSpeed ("Wave Speed", Float) = 0.5
        _WaveStrength ("Wave Strength", Range(0, 0.1)) = 0.01
        _WaveAmount ("Wave Amount", Float) = 0.1
        _WaveFrequency ("Wave Frequency", Float) = 1
        _TextureDistortion ("Texture Distortion", Range(0, 1)) = 0.5
        _CartoonFactor ("Cartoon Factor", Range(0, 1)) = 0.5
        _TransparencySpeed ("Transparency Animation Speed", Float) = 1.0
        _TransparencyStrength ("Transparency Strength", Range(0, 1)) = 0.5
        _FoamColor ("Foam Color", Color) = (1, 1, 1, 1)
        _FoamAmount ("Foam Amount", Range(0, 1)) = 0.1
        _FoamCutoff ("Foam Cutoff", Range(0, 1)) = 0.5
        _FoamSpeed ("Foam Speed", Float) = 0.1
        _FoamNoiseScale ("Foam Noise Scale", Float) = 20
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _TextureColor;
                half4 _FoamColor;
                float4 _MainTex_ST;
                float _WaveSpeed;
                float _WaveStrength;
                float _WaveAmount;
                float _WaveFrequency;
                float _TextureDistortion;
                float _CartoonFactor;
                float _TransparencySpeed;
                float _TransparencyStrength;
                float _FoamAmount;
                float _FoamCutoff;
                float _FoamSpeed;
                float _FoamNoiseScale;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float4 screenPos : TEXCOORD2;
                float fogFactor : TEXCOORD3;
                float3 normalWS : TEXCOORD4;
            };

            // Gradient noise helpers
            float2 random2(float2 st)
            {
                st = float2(dot(st, float2(127.1, 311.7)),
                            dot(st, float2(269.5, 183.3)));
                return -1.0 + 2.0 * frac(sin(st) * 43758.5453123);
            }

            float gradientNoise(float2 st)
            {
                float2 i = floor(st);
                float2 f = frac(st);
                float2 u = f * f * (3.0 - 2.0 * f);

                return lerp(
                    lerp(dot(random2(i + float2(0.0, 0.0)), f - float2(0.0, 0.0)),
                         dot(random2(i + float2(1.0, 0.0)), f - float2(1.0, 0.0)), u.x),
                    lerp(dot(random2(i + float2(0.0, 1.0)), f - float2(0.0, 1.0)),
                         dot(random2(i + float2(1.0, 1.0)), f - float2(1.0, 1.0)), u.x), u.y);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.screenPos = ComputeScreenPos(posInputs.positionCS);
                OUT.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                OUT.normalWS = normInputs.normalWS;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;

                // Wave distortion
                float2 waveOffset = float2(
                    gradientNoise(uv * _WaveFrequency + _Time.y * _WaveSpeed),
                    gradientNoise(uv * _WaveFrequency * 1.2 + _Time.y * _WaveSpeed * 1.1)
                ) * _WaveAmount;

                float2 distortedUV = uv + waveOffset * _WaveStrength * _TextureDistortion;

                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, distortedUV);
                half4 cOrig = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                c = lerp(cOrig, c, _TextureDistortion);
                c *= _TextureColor;

                // Pulsating transparency
                float transparencyPulse = (sin(_Time.y * _TransparencySpeed) + 1) * 0.5;
                float textureTransparency = lerp(1, transparencyPulse, _TransparencyStrength);

                // Depth-based foam
                float2 foamUV = IN.positionWS.xz * _FoamNoiseScale + _Time.y * _FoamSpeed;
                float foamNoise = gradientNoise(foamUV);

                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                float rawDepth = SampleSceneDepth(screenUV);
                float sceneEyeDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                float surfaceEyeDepth = IN.screenPos.w;
                float foamLine = 1 - saturate(_FoamAmount * (sceneEyeDepth - surfaceEyeDepth));

                float foam = saturate(foamNoise + foamLine);
                foam = smoothstep(_FoamCutoff, 1, foam);

                // Simple Lambert lighting
                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(IN.normalWS, mainLight.direction));
                half3 lighting = mainLight.color * NdotL + half3(0.1, 0.1, 0.1);

                // Final color
                half3 finalColor = lerp(_Color.rgb, c.rgb, c.a * textureTransparency);
                finalColor = lerp(finalColor, _FoamColor.rgb, foam);
                finalColor *= lighting;

                float alpha = lerp(_Color.a, c.a * _TextureColor.a, c.a * textureTransparency);

                finalColor = MixFog(finalColor, IN.fogFactor);
                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
