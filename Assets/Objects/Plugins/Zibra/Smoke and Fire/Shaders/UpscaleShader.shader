Shader "Hidden/ZibraEffects/SmokeAndFires/UpscaleShader"
{
    SubShader
    {
        Pass
        {
            // Premultiplied alpha blending
            Blend One OneMinusSrcAlpha
            Cull Off
            ZWrite Off
            ZTest Always

            HLSLPROGRAM
            #pragma vertex VSMain
            #pragma fragment PSMain
            #pragma target 3.0
            #include "RenderingUtils.cginc"
            #include "UnityCG.cginc"
            #include "StereoSupport.cginc"

            struct VSIn
            {
                uint vertexID : SV_VertexID;
                ADDITIONAL_VS_IN_DATA
            };

            struct VSOut
            {
                float4 position : POSITION;
                float2 uv : TEXCOORD0;
                ADDITIONAL_VS_OUT_DATA
            };

            struct PSOut
            {
                float4 color : COLOR;
            };
            float2 TextureScale;
            UNITY_DECLARE_SCREENSPACE_TEXTURE(RenderedVolume);
            
            float2 GetFlippedUV(float2 uv)
            {
                if (_ProjectionParams.x > 0)
                    return float2(uv.x, 1 - uv.y);
                return uv;
            }

            VSOut VSMain(VSIn input)
            {
                VSOut output;
                VERTEX_SHADER_SETUP(input, output)
                
                float2 vertexBuffer[4] = {
                    float2(0.0f, 0.0f),
                    float2(0.0f, 1.0f),
                    float2(1.0f, 0.0f),
                    float2(1.0f, 1.0f),
                    };
                uint indexBuffer[6] = {0, 1, 2, 2, 1, 3};
                uint indexID = indexBuffer[input.vertexID];

                float2 uv = vertexBuffer[indexID];
                float2 flippedUV = GetFlippedUV(uv);

                output.position = float4(2 * flippedUV.x - 1, 1 - 2 * flippedUV.y, 0.5, 1.0);
                output.uv = uv;

                return output;
            }

            PSOut PSMain(VSOut input)
            {
                PSOut output;
                PIXEL_SHADER_SETUP(input);
                output.color = UNITY_SAMPLE_SCREENSPACE_TEXTURE(RenderedVolume, input.uv * TextureScale);
				if (output.color.a == 0.0f)
                {
                    discard;
                }

                return output;
            }
            ENDHLSL
        }
    }
}
