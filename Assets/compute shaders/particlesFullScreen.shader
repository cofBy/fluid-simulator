Shader "Custom/particlesFullScreen"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "black" {}
        _color("color", Color) = (1, 1, 1, 1)
        _defultColor("back ground color", Color) = (0,0,0,1)
        _radius("radius", Float) = 3
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _color;
                float4 _defultColor;
                float _radius;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float width;
                float height;
                _BaseMap.GetDimensions(width, height);

                float neighborValue = 0;
                float self = 0;
                float radInt = (int)_radius;
                for(int x = -radInt; x <= radInt; x++)
                {
                    for(int y = -radInt; y <= radInt; y++)
                    {
                        float2 offset = float2(x, y);
                        float dist = length(offset);

                        if (dist > _radius) continue;

                        float2 neighbor = clamp(IN.uv + offset / float2(width, height), 0, 1);
                        float sampleVal = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, neighbor).r;

                        float falloff = 1.0 - saturate(dist / _radius);
                        neighborValue = max(neighborValue, sampleVal * falloff);
                    }
                }
                self = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).r;
                float value = min(self + neighborValue, 1);

                return lerp(_defultColor, _color, value);
            }
            ENDHLSL
        }
    }
}
