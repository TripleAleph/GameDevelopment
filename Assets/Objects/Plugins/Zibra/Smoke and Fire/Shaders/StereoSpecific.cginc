#define VERTEX_SHADER_SETUP(input, output) \
    UNITY_SETUP_INSTANCE_ID(input); \
    UNITY_TRANSFER_INSTANCE_ID(input, output); \
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output); \
    output.inverse_vp = inverse(UNITY_MATRIX_VP);
#define PIXEL_SHADER_SETUP(input) \
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input) \
    float4x4 inverseVP = input.inverse_vp;
#define ADDITIONAL_VS_IN_DATA UNITY_VERTEX_INPUT_INSTANCE_ID
#define ADDITIONAL_VS_OUT_DATA \
    nointerpolation float4x4 inverse_vp : TEXCOORD1; \
    UNITY_VERTEX_INPUT_INSTANCE_ID \
    UNITY_VERTEX_OUTPUT_STEREO
