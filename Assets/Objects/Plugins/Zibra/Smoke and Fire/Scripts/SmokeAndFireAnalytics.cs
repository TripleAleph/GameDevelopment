#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using com.zibra.smoke_and_fire.Solver;
using com.zibra.smoke_and_fire.Manipulators;
using com.zibra.common.Utilities;
using com.zibra.common.SDFObjects;
using com.zibra.common.Analytics;
using static com.zibra.common.Editor.PluginManager;

namespace com.zibra.smoke_and_fire.Analytics
{
    [InitializeOnLoad]
    internal static class SmokeAndFireAnalytics
    {
#region Public Interface
        public static void SimulationCreated(ZibraSmokeAndFire smoke)
        {
            AnalyticsManagerInstance.TrackEvent(new AnalyticsManager.AnalyticsEvent
            {
                EventType = "SF_simulation_created",
                Properties = new Dictionary<string, object>
                    {
                        { "SF_simulation_id", smoke.SimulationGUID }
                    }
            });
        }

        public static void SimulationStart(ZibraSmokeAndFire smoke)
        {
            PurchasedAssetRunEvent(smoke);
            SimulationdRun(smoke);
            EmitterRun(smoke);
            TextureEmitterRun(smoke);
            ParticleEmitterRun(smoke);
            VoidRun(smoke);
            DetectorRun(smoke);
            ForceFieldRun(smoke);
        }
#endregion
#region Implementation details

        private static void PurchasedAssetRunEvent(ZibraSmokeAndFire smoke)
        {
            List<string> presetNames = GetPresetNames(smoke);

            AnalyticsManagerInstance.TrackEvent(new AnalyticsManager.AnalyticsEvent
            {
                EventType = "SF_purchased_asset_run",
                Properties = new Dictionary<string, object>
                {
                    { "SF_simulation_id", smoke.SimulationGUID },
                    { "Presets_used", presetNames },
                    { "Build_platform", EditorUserBuildSettings.activeBuildTarget.ToString() },
                    { "AppleARKit", PackageTracker.IsPackageInstalled("com.unity.xr.arkit") },
                    { "GoogleARCore", PackageTracker.IsPackageInstalled("com.unity.xr.arcore") },
                    { "MagicLeap", PackageTracker.IsPackageInstalled("com.unity.xr.magicleap") },
                    { "Oculus", PackageTracker.IsPackageInstalled("com.unity.xr.oculus") },
                    { "OpenXR", PackageTracker.IsPackageInstalled("com.unity.xr.openxr") }
                }
            });
        }

        private static void SimulationdRun(ZibraSmokeAndFire smoke)
        {
            List<string> presetNames = GetPresetNames(smoke);

            AnalyticsManagerInstance.TrackEvent(new AnalyticsManager.AnalyticsEvent
            {
                EventType = "SF_simulation_run",
                Properties = new Dictionary<string, object>
                {
                    { "Purchased_asset", presetNames.Count > 0 },
                    { "SF_simulation_id", smoke.SimulationGUID },
                    { "Simulation_mode", smoke.CurrentSimulationMode.ToString() },
                    { "Additional_lights_used", smoke.Lights.Count > 0 },
                    { "Effective_voxel_count", smoke.GridSize.x * smoke.GridSize.y * smoke.GridSize.z },
                    { "Emitter_count", CountManipulators(smoke, typeof(ZibraSmokeAndFireEmitter)) },
                    { "Texture_emiter_count", CountManipulators(smoke, typeof(ZibraSmokeAndFireTextureEmitter)) },
                    { "Particle_emitter_count", CountManipulators(smoke, typeof(ZibraParticleEmitter)) },
                    { "Void_count", CountManipulators(smoke, typeof(ZibraSmokeAndFireVoid)) },
                    { "Detector_count", CountManipulators(smoke, typeof(ZibraSmokeAndFireDetector)) },
                    { "Forcefield_count", CountManipulators(smoke, typeof(ZibraSmokeAndFireForceField)) },
                    { "Analytic_collider_count", CountManipulatorsWithSDFs(smoke, typeof(ZibraSmokeAndFireCollider), typeof(AnalyticSDF)) },
                    { "Neural_collider_count", CountManipulatorsWithSDFs(smoke, typeof(ZibraSmokeAndFireCollider), typeof(NeuralSDF)) },
                    { "Skinned_mesh_colider_count", CountManipulatorsWithSDFs(smoke, typeof(ZibraSmokeAndFireCollider), typeof(SkinnedMeshSDF)) },
                    { "Build_platform", EditorUserBuildSettings.activeBuildTarget.ToString() },
                    { "Render_pipeline", RenderPipelineDetector.GetRenderPipelineType().ToString() },
                    { "AppleARKit", PackageTracker.IsPackageInstalled("com.unity.xr.arkit") },
                    { "GoogleARCore", PackageTracker.IsPackageInstalled("com.unity.xr.arcore") },
                    { "MagicLeap", PackageTracker.IsPackageInstalled("com.unity.xr.magicleap") },
                    { "Oculus", PackageTracker.IsPackageInstalled("com.unity.xr.oculus") },
                    { "OpenXR", PackageTracker.IsPackageInstalled("com.unity.xr.openxr") }
                }
            });
        }

        private static void EmitterRun(ZibraSmokeAndFire smoke)
        {
            int totalCount = CountManipulators(smoke, typeof(ZibraSmokeAndFireEmitter));
            if (totalCount == 0)
            {
                return;
            }

            AnalyticsManagerInstance.TrackEvent(new AnalyticsManager.AnalyticsEvent
            {
                EventType = "SF_emitter_run",
                Properties = new Dictionary<string, object>
                {
                    { "SF_simulation_id", smoke.SimulationGUID },
                    { "SDF_analytic_count", CountManipulatorsWithSDFs(smoke, typeof(ZibraSmokeAndFireEmitter), typeof(AnalyticSDF)) },
                    { "SDF_neural_count", CountManipulatorsWithSDFs(smoke, typeof(ZibraSmokeAndFireEmitter), typeof(NeuralSDF)) },
                    { "SDF_skinned_mesh_count", CountManipulatorsWithSDFs(smoke, typeof(ZibraSmokeAndFireEmitter), typeof(SkinnedMeshSDF)) },
                    { "Total_count", totalCount }
                }
            });
        }

        private static void TextureEmitterRun(ZibraSmokeAndFire smoke)
        {
            int totalCount = CountManipulators(smoke, typeof(ZibraSmokeAndFireTextureEmitter));
            if (totalCount == 0)
            {
                return;
            }

            AnalyticsManagerInstance.TrackEvent(new AnalyticsManager.AnalyticsEvent
            {
                EventType = "SF_texture_emitter_run",
                Properties = new Dictionary<string, object>
                {
                    { "SF_simulation_id", smoke.SimulationGUID },
                    { "SDF_analytic_count", CountManipulatorsWithSDFs(smoke, typeof(ZibraSmokeAndFireTextureEmitter), typeof(AnalyticSDF)) },
                    { "SDF_neural_count", CountManipulatorsWithSDFs(smoke, typeof(ZibraSmokeAndFireTextureEmitter), typeof(NeuralSDF)) },
                    { "SDF_skinned_mesh_count", CountManipulatorsWithSDFs(smoke, typeof(ZibraSmokeAndFireTextureEmitter), typeof(SkinnedMeshSDF)) },
                    { "Total_count", totalCount }
                }
            });
        }

        private static void ParticleEmitterRun(ZibraSmokeAndFire smoke)
        {
            int totalCount = CountManipulators(smoke, typeof(ZibraParticleEmitter));
            if (totalCount == 0)
            {
                return;
            }

            AnalyticsManagerInstance.TrackEvent(new AnalyticsManager.AnalyticsEvent
            {
                EventType = "SF_particle_emitter_run",
                Properties = new Dictionary<string, object>
                {
                    { "SF_simulation_id", smoke.SimulationGUID },
                    { "SDF_analytic_count", CountManipulatorsWithSDFs(smoke, typeof(ZibraParticleEmitter), typeof(AnalyticSDF)) },
                    { "SDF_neural_count", CountManipulatorsWithSDFs(smoke, typeof(ZibraParticleEmitter), typeof(NeuralSDF)) },
                    { "SDF_skinned_mesh_count", CountManipulatorsWithSDFs(smoke, typeof(ZibraParticleEmitter), typeof(SkinnedMeshSDF)) },
                    { "Total_count", totalCount }
                }
            });
        }

        private static void VoidRun(ZibraSmokeAndFire smoke)
        {
            int totalCount = CountManipulators(smoke, typeof(ZibraSmokeAndFireVoid));
            if (totalCount == 0)
            {
                return;
            }

            AnalyticsManagerInstance.TrackEvent(new AnalyticsManager.AnalyticsEvent
            {
                EventType = "SF_void_run",
                Properties = new Dictionary<string, object>
                {
                    { "SF_simulation_id", smoke.SimulationGUID },
                    { "SDF_analytic_count", CountManipulatorsWithSDFs(smoke, typeof(ZibraSmokeAndFireVoid), typeof(AnalyticSDF)) },
                    { "SDF_neural_count", CountManipulatorsWithSDFs(smoke, typeof(ZibraSmokeAndFireVoid), typeof(NeuralSDF)) },
                    { "SDF_skinned_mesh_count", CountManipulatorsWithSDFs(smoke, typeof(ZibraSmokeAndFireVoid), typeof(SkinnedMeshSDF)) },
                    { "Total_count", totalCount }
                }
            });
        }

        private static void DetectorRun(ZibraSmokeAndFire smoke)
        {
            int totalCount = CountManipulators(smoke, typeof(ZibraSmokeAndFireDetector));
            if (totalCount == 0)
            {
                return;
            }

            AnalyticsManagerInstance.TrackEvent(new AnalyticsManager.AnalyticsEvent
            {
                EventType = "SF_detector_run",
                Properties = new Dictionary<string, object>
                {
                    { "SF_simulation_id", smoke.SimulationGUID },
                    { "SDF_analytic_count", CountManipulatorsWithSDFs(smoke, typeof(ZibraSmokeAndFireDetector), typeof(AnalyticSDF)) },
                    { "SDF_neural_count", CountManipulatorsWithSDFs(smoke, typeof(ZibraSmokeAndFireDetector), typeof(NeuralSDF)) },
                    { "SDF_skinned_mesh_count", CountManipulatorsWithSDFs(smoke, typeof(ZibraSmokeAndFireDetector), typeof(SkinnedMeshSDF)) },
                    { "Total_count", totalCount }
                }
            });
        }

        private static void ForceFieldRun(ZibraSmokeAndFire smoke)
        {
            int totalCount = CountManipulators(smoke, typeof(ZibraSmokeAndFireForceField));
            if (totalCount == 0)
            {
                return;
            }

            AnalyticsManagerInstance.TrackEvent(new AnalyticsManager.AnalyticsEvent
            {
                EventType = "SF_forcefield_run",
                Properties = new Dictionary<string, object>
                {
                    { "SF_simulation_id", smoke.SimulationGUID },
                    { "SDF_analytic_count", CountManipulatorsWithSDFs(smoke, typeof(ZibraSmokeAndFireForceField), typeof(AnalyticSDF)) },
                    { "SDF_neural_count", CountManipulatorsWithSDFs(smoke, typeof(ZibraSmokeAndFireForceField), typeof(NeuralSDF)) },
                    { "SDF_skinned_mesh_count", CountManipulatorsWithSDFs(smoke, typeof(ZibraSmokeAndFireForceField), typeof(SkinnedMeshSDF)) },
                    { "Total_count", totalCount }
                }
            });
        }

        private static int CountManipulators(ZibraSmokeAndFire smoke, Type type)
        {
            int result = 0;
            foreach (var manipuilator in smoke.GetManipulatorList())
            {
                if (manipuilator != null && manipuilator.GetType() == type)
                {
                    result++;
                }
            }
            return result;
        }

        private static int CountManipulatorsWithSDFs(ZibraSmokeAndFire smoke, Type manipType, Type SDFType)
        {
            int result = 0;
            foreach (var manipuilator in smoke.GetManipulatorList())
            {
                if (manipuilator == null)
                    continue;
                SDFObject sdf = manipuilator.GetComponent<SDFObject>();
                if (sdf != null && manipuilator.GetType() == manipType && sdf.GetType() == SDFType)
                {
                    result++;
                }
            }
            return result;
        }

        private static List<string> GetPresetNames(ZibraSmokeAndFire smoke)
        {
            List<string> result = new List<string> { smoke.SolverParameters.PresetName, smoke.MaterialParameters.PresetName };
            result.RemoveAll(s => s == "");
            return result;
        }

        private static AnalyticsManager AnalyticsManagerInstance = AnalyticsManager.GetInstance(Effect.Smoke);
#endregion
    }
}

#endif
