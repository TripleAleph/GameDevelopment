#include "UnityCG.cginc"

#ifdef USING_STEREO_MATRICES

#include "StereoSpecific.cginc"

#else

#define VERTEX_SHADER_SETUP(input, output)
#define PIXEL_SHADER_SETUP(input) float4x4 inverseVP = inverse(UNITY_MATRIX_VP);
#define ADDITIONAL_VS_IN_DATA
#define ADDITIONAL_VS_OUT_DATA

#endif
