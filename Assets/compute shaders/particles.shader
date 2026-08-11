Shader "Custom/particles"
{
    Properties
    {
        _radius("point radius", Float) = 5.0
        _startColor("start color", Color) = (1,0,1,1)
        _endColor("end color", Color) = (0,1,0,1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            struct particle
            {
                float2 pos;
                float2 vel;
                float life;
            };
            StructuredBuffer<particle> particlesBuffer;

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                uint instanceID : SV_InstanceID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color : COLOR;
                float size : PSIZE;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float _radius;
                float4 _startColor;
                float4 _endColor;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                particle current = particlesBuffer[IN.instanceID];
                OUT.positionHCS = TransformObjectToHClip(float3(current.pos, 0));
                OUT.color = lerp(_endColor, _startColor, current.life / 5.0);
                OUT.size = _radius;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return IN.color;
            }
            ENDHLSL
        }
    }
}
