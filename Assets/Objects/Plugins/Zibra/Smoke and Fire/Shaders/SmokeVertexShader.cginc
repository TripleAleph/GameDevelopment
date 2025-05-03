#include "UnityCG.cginc"
#include "StereoSupport.cginc"

struct VSIn
{
    uint vertexID : SV_VertexID;
    float4 position : POSITION;
    float2 uv : TEXCOORD0;
    ADDITIONAL_VS_IN_DATA
};

struct VSOut
{
    float4 position : POSITION;
    float2 uv : TEXCOORD0;
    float3 worldpos : COLOR0;
    ADDITIONAL_VS_OUT_DATA
};

#if defined(INPUT_2D_ARRAY) || defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
Texture2DArray _CameraDepthTexture;
#else
Texture2D _CameraDepthTexture;
#endif
float4 _CameraDepthTexture_TexelSize;

float LoadCameraDepth(float2 pos)
{
#ifdef FLIP_NATIVE_TEXTURES
    pos.y = _CameraDepthTexture_TexelSize.w - pos.y;
#endif
#if defined(INPUT_2D_ARRAY) || defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
    float sceneDepth = _CameraDepthTexture.Load(int4(pos, unity_StereoEyeIndex, 0)).x;
#else
    float sceneDepth = _CameraDepthTexture.Load(int3(pos, 0)).x;
#endif
#if !defined(UNITY_REVERSED_Z)
    sceneDepth = 1.0 - sceneDepth;
#endif
    return sceneDepth;
}

float4 ComputeClipSpacePosition(float2 positionNDC, float deviceDepth)
{
    float4 positionCS = float4(positionNDC * 2.0 - 1.0, deviceDepth, 1.0);
#if UNITY_UV_STARTS_AT_TOP
    positionCS.y = -positionCS.y;
#endif
    return positionCS;
}

float3 SampleWorldPositionFromDepth(float2 uv, float depth, float4x4 inverseVP)
{
    return ClipToWorld(ComputeClipSpacePosition(uv, depth), inverseVP);
}

VSOut VSMain(VSIn input)
{
    VSOut output;

    VERTEX_SHADER_SETUP(input, output)

    output.worldpos = mul(unity_ObjectToWorld, input.position).xyz;
#ifdef FULLSCREEN_QUAD
    float4 position = 2 * input.position;
    output.position = float4(position.xy, .5, 1.);
    output.uv = float2(input.position.x + 0.5, 1.0 - (input.position.y + 0.5));

    // degrade the unused triangles during fullscreen pass
    if (input.vertexID > 3)
    {
        output.position = 0;
        output.uv = 0;
    }
#else
    output.position = UnityObjectToClipPos(input.position);
    output.uv = float2(input.uv.x, 1 - input.uv.y);
#endif
    
#ifdef FLIP_NATIVE_TEXTURES
    output.uv.y = 1. - output.uv.y;
#endif

    return output;
}