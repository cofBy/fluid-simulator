Shader "Custom/particlesFullScreen"
{
    Properties
    {
        _particleColor("particle color", Color) = (1, 1, 1, 1)
        _defultColor("back ground color", Color) = (0, 0, 0, 1)
        _radius("ball radius", Float) = 0.5
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct particle
            {
                float2 pos;
                float2 vel;
                float life;
            };
            StructuredBuffer<particle> particlesBuffer;

            float aspectRatio;
            float cameraSize;

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
                half4 _BaseColor;
                float4 _BaseMap_ST;
                float4 _particleColor;
                float4 _defultColor;
                float _radius;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = (IN.uv - 0.5) * float2(aspectRatio * cameraSize * 2, cameraSize * 2);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                bool inParticale = false;
                uint particleAmount;
                uint dummystride;
                particlesBuffer.GetDimensions(particleAmount, dummystride);
                for (int i = 0; i < int(particleAmount); i++)
                {
                    float2 pos = particlesBuffer[i].pos;
                    if (distance(pos, IN.uv) < _radius)
                    {
                        inParticale = true;
                        break;
                    }
                }
                return inParticale == true ? _particleColor : _defultColor;
            }
            ENDHLSL
        }
    }
}
