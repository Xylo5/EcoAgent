Shader "Custom/BasicToonWaterShader"
{
    Properties
    {
        _Color ("Water Color", Color) = (0.1, 0.4, 0.8, 0.8)
        _MainTex ("Water Texture", 2D) = "white" {}
        _WaveSpeed ("Wave Speed", Float) = 0.5
        _WaveStrength ("Wave Strength", Range(0, 0.1)) = 0.01
        _WaveAmount ("Wave Amount", Float) = 0.1
        _WaveFrequency ("Wave Frequency", Float) = 1
        _TextureDistortion ("Texture Distortion", Range(0, 1)) = 0.5
        _CartoonFactor ("Cartoon Factor", Range(0, 1)) = 0.5
        _ColorSteps ("Color Steps", Range(2, 10)) = 4
        _EdgeThreshold ("Edge Threshold", Range(0, 1)) = 0.2
        _EdgeColor ("Edge Color", Color) = (0, 0, 0, 1)
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineWidth ("Outline Width", Range(0, 0.1)) = 0.01
        _FoamColor ("Foam Color", Color) = (1, 1, 1, 1)
        _FoamAmount ("Foam Amount", Range(0, 1)) = 0.1
        _FoamCutoff ("Foam Cutoff", Range(0, 1)) = 0.5
        _FoamSpeed ("Foam Speed", Float) = 0.1
        _FoamNoiseScale ("Foam Noise Scale", Float) = 20
        _FoamSmoothness ("Foam Smoothness", Range(0, 0.5)) = 0.1
        _FoamEdgeSize ("Foam Edge Size", Range(0, 1)) = 0.2
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        // Pass 0: Outline (Cull Front)
        Pass
        {
            Name "Outline"
            Tags { "LightMode"="SRPDefaultUnlit" }

            Cull Front
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _EdgeColor;
                half4 _FoamColor;
                half4 _OutlineColor;
                float4 _MainTex_ST;
                float _WaveSpeed;
                float _WaveStrength;
                float _WaveAmount;
                float _WaveFrequency;
                float _TextureDistortion;
                float _CartoonFactor;
                float _ColorSteps;
                float _EdgeThreshold;
                float _OutlineWidth;
                float _FoamAmount;
                float _FoamCutoff;
                float _FoamSpeed;
                float _FoamNoiseScale;
                float _FoamSmoothness;
                float _FoamEdgeSize;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = posInputs.positionCS;

                float3 normalVS = normalize(mul((float3x3)UNITY_MATRIX_IT_MV, IN.normalOS));
                float2 offset = TransformViewToProjection(normalVS.xy);
                OUT.positionCS.xy += offset * _OutlineWidth * OUT.positionCS.z;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }

        // Pass 1: Main water surface with toon ramp lighting
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
                half4 _EdgeColor;
                half4 _FoamColor;
                half4 _OutlineColor;
                float4 _MainTex_ST;
                float _WaveSpeed;
                float _WaveStrength;
                float _WaveAmount;
                float _WaveFrequency;
                float _TextureDistortion;
                float _CartoonFactor;
                float _ColorSteps;
                float _EdgeThreshold;
                float _OutlineWidth;
                float _FoamAmount;
                float _FoamCutoff;
                float _FoamSpeed;
                float _FoamNoiseScale;
                float _FoamSmoothness;
                float _FoamEdgeSize;
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
                float3 normalWS : TEXCOORD3;
                float3 viewDirWS : TEXCOORD4;
                float fogFactor : TEXCOORD5;
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

            // Quintic Hermite (smootherstep)
            float smootherstep(float edge0, float edge1, float x)
            {
                x = saturate((x - edge0) / (edge1 - edge0));
                return x * x * x * (x * (x * 6 - 15) + 10);
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
                OUT.normalWS = normInputs.normalWS;
                OUT.viewDirWS = GetWorldSpaceNormalizeViewDir(posInputs.positionWS);
                OUT.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
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

                // View-dependent edge detection
                float3 normal = normalize(IN.normalWS);
                float edge = 1 - saturate(dot(normalize(IN.viewDirWS), normal));

                // Depth-based foam
                float2 foamUV = IN.positionWS.xz * _FoamNoiseScale + _Time.y * _FoamSpeed;
                float foamNoise = gradientNoise(foamUV);

                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                float rawDepth = SampleSceneDepth(screenUV);
                float sceneEyeDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                float surfaceEyeDepth = IN.screenPos.w;
                float foamLine = 1 - saturate(_FoamAmount * (sceneEyeDepth - surfaceEyeDepth));

                float foamGradient = smootherstep(_FoamCutoff - _FoamSmoothness, _FoamCutoff + _FoamSmoothness, foamLine + foamNoise);
                float foam = foamGradient * _FoamEdgeSize;

                // Toon ramp lighting
                Light mainLight = GetMainLight();
                float NdotL = dot(normal, mainLight.direction);
                float h = NdotL * 0.5 + 0.5;
                float ramp = floor(h * _ColorSteps) / _ColorSteps;
                ramp = lerp(h, ramp, _CartoonFactor);

                half3 finalColor;
                if (edge > _EdgeThreshold)
                    finalColor = lerp(c.rgb * _Color.rgb, _EdgeColor.rgb, _CartoonFactor);
                else
                    finalColor = c.rgb * _Color.rgb;

                // Apply foam
                finalColor = lerp(finalColor, _FoamColor.rgb, foam);

                // Apply toon lighting
                finalColor *= mainLight.color * ramp + half3(0.1, 0.1, 0.1);

                float alpha = c.a * _Color.a;

                finalColor = MixFog(finalColor, IN.fogFactor);
                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
