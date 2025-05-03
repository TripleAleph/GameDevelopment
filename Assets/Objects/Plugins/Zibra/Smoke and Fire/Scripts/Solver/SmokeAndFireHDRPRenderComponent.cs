#if UNITY_PIPELINE_HDRP

using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using com.zibra.smoke_and_fire.Solver;

namespace com.zibra.smoke_and_fire
{
    internal class SmokeAndFireHDRPRenderComponent : CustomPassVolume
    {
        internal class FluidHDRPRender : CustomPass
        {
            public ZibraSmokeAndFire smokeAndFire;

            protected override void Execute(CustomPassContext ctx)
            {
                if (smokeAndFire && smokeAndFire.IsRenderingEnabled())
                {
                    RTHandle cameraColor = ctx.cameraColorBuffer;
                    RTHandle cameraDepth = ctx.cameraDepthBuffer;

                    HDCamera hdCamera = ctx.hdCamera;
                    CommandBuffer cmd = ctx.cmd;

                    if ((hdCamera.camera.cullingMask & (1 << smokeAndFire.gameObject.layer)) ==
                        0) // fluid gameobject layer is not in the culling mask of the camera
                        return;

                    float scale = (float)(hdCamera.actualWidth) / hdCamera.camera.pixelWidth;

                    smokeAndFire.RenderCallBack(hdCamera.camera, scale);

                    Rect viewport = new Rect(0, 0, hdCamera.actualWidth, hdCamera.actualHeight);

                    var exposure = hdCamera.GetPreviousFrameRT((int)HDCameraFrameHistoryType.Exposure);
                    cmd.SetGlobalTexture("_CameraExposureTexture", exposure);

                    bool isTextureArray = cameraColor.rt.dimension == TextureDimension.Tex2DArray;
                    if (isTextureArray)
                    {
                        smokeAndFire.CameraResourcesMap[hdCamera.camera]
                            .SmokeAndFireMaterial.CurrentMaterial.EnableKeyword("INPUT_2D_ARRAY");
                        smokeAndFire.CameraResourcesMap[hdCamera.camera]
                            .SmokeShadowProjectionMaterial.CurrentMaterial.EnableKeyword("INPUT_2D_ARRAY");
                    }
                    else
                    {
                        smokeAndFire.CameraResourcesMap[hdCamera.camera]
                            .SmokeAndFireMaterial.CurrentMaterial.DisableKeyword("INPUT_2D_ARRAY");
                        smokeAndFire.CameraResourcesMap[hdCamera.camera]
                            .SmokeShadowProjectionMaterial.CurrentMaterial.DisableKeyword("INPUT_2D_ARRAY");
                    }

                    if (smokeAndFire.VisualizeSceneSDF)
                    {
                        Material sdfRenderMaterial = smokeAndFire.CameraResourcesMap[hdCamera.camera]
                            .SDFRenderMaterial.CurrentMaterial;
                        sdfRenderMaterial.EnableKeyword("HDRP");
                        sdfRenderMaterial.SetTexture(ZibraSmokeAndFire.ShaderParam.SmokeSDFVisualizationCameraDepth, cameraDepth);
                    }

                    smokeAndFire.RenderParticlesNative(cmd, hdCamera.camera, isTextureArray);
                    smokeAndFire.RenderSDFNative(cmd);
                    smokeAndFire.RenderFluid(cmd, hdCamera.camera, cameraColor, cameraDepth, viewport);
                }
            }
        }

        public FluidHDRPRender fluidPass;
    }
}

#endif // UNITY_PIPELINE_HDRP