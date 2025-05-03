#if UNITY_PIPELINE_URP

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using com.zibra.smoke_and_fire.Solver;

namespace com.zibra.smoke_and_fire
{
    /// <summary>
    ///     Component responsible for rendering smoke and fire in URP.
    /// </summary>
    public class SmokeAndFireURPRenderComponent : ScriptableRendererFeature
    {
#region Public Interface
        /// <summary>
        ///     URP specific rendering settings.
        /// </summary>
        [System.Serializable]
        public class SmokeAndFireURPRenderSettings
        {
            /// <summary>
            ///     Globally defines whether simulation renders in URP.
            /// </summary>
            public bool IsEnabled = true;
            /// <summary>
            ///     Injection point where we will insert rendering.
            /// </summary>
            /// <remarks>
            ///     In case of URP, this parameter will be used instead of
            ///     <see cref="Solver::ZibraSmokeAndFire::CurrentInjectionPoint">ZibraSmokeAndFire.CurrentInjectionPoint</see>.
            /// </remarks>
            public RenderPassEvent InjectionPoint = RenderPassEvent.AfterRenderingTransparents;
        }

        /// <summary>
        ///     See <see cref="SmokeAndFireURPRenderSettings"/>.
        /// </summary>
        // Must be called exactly "settings" so Unity shows this as render feature settings in editor
        public SmokeAndFireURPRenderSettings settings = new SmokeAndFireURPRenderSettings();

        /// <summary>
        ///     Creates URP ScriptableRendererFeature.
        /// </summary>
        public override void Create()
        {
            handleSystem.Initialize(0, 0);
        }

        /// <summary>
        ///     Adds scriptable render passes.
        /// </summary>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!settings.IsEnabled)
            {
                return;
            }

            if (renderingData.cameraData.cameraType != CameraType.Game &&
                renderingData.cameraData.cameraType != CameraType.SceneView)
            {
                return;
            }

            Camera camera = renderingData.cameraData.camera;
            camera.depthTextureMode = DepthTextureMode.Depth;

            int simulationsToRenderCount = 0;
            int simulationsToUpscaleCount = 0;

            foreach (var instance in ZibraSmokeAndFire.AllInstances)
            {
                if (instance != null && instance.Initialized)
                {
                    simulationsToRenderCount++;
                    if (instance.EnableDownscale)
                    {
                        simulationsToUpscaleCount++;
                    }
                }
            }

            if (smokeAndFireURPPasses == null || smokeAndFireURPPasses.Length != simulationsToRenderCount)
            {
                smokeAndFireURPPasses = new SmokeAndFireURPRenderPass[simulationsToRenderCount];
                for (int i = 0; i < simulationsToRenderCount; ++i)
                {
                    smokeAndFireURPPasses[i] = new SmokeAndFireURPRenderPass(settings.InjectionPoint);
                }
            }

            if (upscalePasses == null || upscalePasses.Length != simulationsToUpscaleCount)
            {
                upscalePasses = new SmokeAndFireUpscaleURPRenderPass[simulationsToUpscaleCount];
                for (int i = 0; i < simulationsToUpscaleCount; ++i)
                {
                    upscalePasses[i] = new SmokeAndFireUpscaleURPRenderPass(settings.InjectionPoint);
                }
            }

            int currentSmokeAndFirePass = 0;
            int currentUpscalePass = 0;

            foreach (var instance in ZibraSmokeAndFire.AllInstances)
            {
                if (instance != null && instance.IsRenderingEnabled() &&
                    ((camera.cullingMask & (1 << instance.gameObject.layer)) != 0))
                {
                    smokeAndFireURPPasses[currentSmokeAndFirePass].smokeAndFire = instance;
                    smokeAndFireURPPasses[currentSmokeAndFirePass].handleSystem = handleSystem;
                    smokeAndFireURPPasses[currentSmokeAndFirePass].ConfigureInput(ScriptableRenderPassInput.Color |
                                                                                  ScriptableRenderPassInput.Depth);
                    smokeAndFireURPPasses[currentSmokeAndFirePass].renderPassEvent = settings.InjectionPoint;

                    renderer.EnqueuePass(smokeAndFireURPPasses[currentSmokeAndFirePass]);
                    currentSmokeAndFirePass++;
                    if (instance.EnableDownscale)
                    {
                        upscalePasses[currentUpscalePass].smokeAndFire = instance;
                        upscalePasses[currentUpscalePass].handleSystem = handleSystem;

                        upscalePasses[currentUpscalePass].renderPassEvent = settings.InjectionPoint;

                        renderer.EnqueuePass(upscalePasses[currentUpscalePass]);
                        currentUpscalePass++;
                    }
                }
            }
        }
#endregion
#region Implementation details

        private class SmokeAndFireURPRenderPass : ScriptableRenderPass
        {
            public ZibraSmokeAndFire smokeAndFire;
            public RTHandleSystem handleSystem;

            RTHandle cameraColorTarget;

            static int upscaleColorTextureID = Shader.PropertyToID("ZibraSmokeAndFire_SmokeAndFireTempColorTexture");
            RTHandle upscaleColorTexture;

            public SmokeAndFireURPRenderPass(RenderPassEvent injectionPoint)
            {
                renderPassEvent = injectionPoint;
            }

            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
#if UNITY_PIPELINE_URP_13_1_OR_HIGHER
                cameraColorTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
#else
                cameraColorTarget = handleSystem.Alloc(renderingData.cameraData.renderer.cameraColorTarget);
#endif
            }

            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                if (smokeAndFire.EnableDownscale)
                {
                    RenderTextureDescriptor descriptor = cameraTextureDescriptor;

                    Vector2Int dimensions = new Vector2Int(descriptor.width, descriptor.height);
                    dimensions = smokeAndFire.ApplyDownscaleFactor(dimensions);
                    descriptor.width = dimensions.x;
                    descriptor.height = dimensions.y;

                    descriptor.msaaSamples = 1;

                    descriptor.colorFormat = RenderTextureFormat.ARGBHalf;
                    descriptor.depthBufferBits = 0;

                    cmd.GetTemporaryRT(upscaleColorTextureID, descriptor, FilterMode.Bilinear);

                    upscaleColorTexture = handleSystem.Alloc(new RenderTargetIdentifier(upscaleColorTextureID));
                    ConfigureTarget(upscaleColorTexture);
                    ConfigureClear(ClearFlag.All, new Color(0, 0, 0, 0));
                }
                else
                {
                    ConfigureTarget(cameraColorTarget);
                    // ConfigureClear seems to be persistent, so need to reset it
                    ConfigureClear(ClearFlag.None, new Color(0, 0, 0, 0));
                }
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                Camera camera = renderingData.cameraData.camera;
                camera.depthTextureMode = DepthTextureMode.Depth;
                CommandBuffer cmd = CommandBufferPool.Get("ZibraSmokeAndFire.EffectParticles.Render");

                smokeAndFire.RenderCallBack(renderingData.cameraData.camera, renderingData.cameraData.renderScale);

                if (!smokeAndFire.EnableDownscale)
                {
                    cmd.SetRenderTarget(cameraColorTarget, 0, CubemapFace.Unknown, -1);
                }
                smokeAndFire.RenderParticlesNative(cmd, camera);
                smokeAndFire.RenderSDFNative(cmd);
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);

                cmd = CommandBufferPool.Get("ZibraSmokeAndFire.Render");
                if (!smokeAndFire.EnableDownscale)
                {
                    cmd.SetRenderTarget(cameraColorTarget, 0, CubemapFace.Unknown, -1);
                }
                if (!smokeAndFire.EnableDownscale)
                {
                    smokeAndFire.RenderSmokeShadows(cmd, camera);
                }
                smokeAndFire.RenderSmokeAndFireMain(cmd, camera);
                if (smokeAndFire.VisualizeSceneSDF)
                {
                    smokeAndFire.RenderSDFVisualization(cmd, camera);
                }
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            public override void OnCameraCleanup(CommandBuffer cmd)
            {
                if (smokeAndFire.EnableDownscale)
                {
                    cmd.ReleaseTemporaryRT(upscaleColorTextureID);
                }
            }
        }

        private class SmokeAndFireUpscaleURPRenderPass : ScriptableRenderPass
        {
            public ZibraSmokeAndFire smokeAndFire;
            public RTHandleSystem handleSystem;

            static int upscaleColorTextureID = Shader.PropertyToID("ZibraSmokeAndFire_SmokeAndFireTempColorTexture");
            RenderTargetIdentifier upscaleColorTexture;

            public SmokeAndFireUpscaleURPRenderPass(RenderPassEvent injectionPoint)
            {
                renderPassEvent = injectionPoint;
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                Camera camera = renderingData.cameraData.camera;
                camera.depthTextureMode = DepthTextureMode.Depth;
                CommandBuffer cmd = CommandBufferPool.Get("ZibraSmokeAndFire.Render");

                upscaleColorTexture = new RenderTargetIdentifier(upscaleColorTextureID);
                smokeAndFire.RenderSmokeShadows(cmd, camera);
                smokeAndFire.UpscaleSmokeAndFireDirect(cmd, camera, upscaleColorTexture);

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
        }

        RTHandleSystem handleSystem = new RTHandleSystem();
        // 1 pass per rendered simulation
        SmokeAndFireURPRenderPass[] smokeAndFireURPPasses;
        // 1 pass per rendered simulation that have downscale enabled
        SmokeAndFireUpscaleURPRenderPass[] upscalePasses;
#endregion
    }
}

#endif // UNITY_PIPELINE_HDRP