using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.SceneManagement;
using UnityEditor;
#endif

namespace com.zibra.smoke_and_fire.DataStructures
{
    /// <summary>
    ///     Component that contains volume material parameters.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         It doesn't execute anything by itself, it is used by <see cref="ZibraSmokeAndFire"/> instead.
    ///     </para>
    ///     <para>
    ///         It's separated so you can save and apply presets for this component separately.
    ///     </para>
    /// </remarks>
    [ExecuteInEditMode]
    public class ZibraSmokeAndFireMaterialParameters : MonoBehaviour
    {
#region Public Interface
        /// <summary>
        ///     Material that will be used to render volume.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         If you want to create your own material, you'll need to use default one as a reference.
        ///     </para>
        ///     <para>
        ///         This is the material that gets parameters defined in 
        ///         <see cref="ZibraSmokeAndFireMaterialParameters"/>
        ///     </para>
        ///     <para>
        ///         If you set it to null in Editor, it'll revert to default.
        ///     </para>
        /// </remarks>
        [Tooltip("Custom smoke material.")]
        public Material SmokeMaterial;

        /// <summary>
        ///     Material that will be used to upscale rendered smoke/fire.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Most users won't need to customize this material,
        ///         but if you want to create your own material, you'll need to use default one as a reference.
        ///     </para>
        ///     <para>
        ///         Has no effect unless you enable downscale in ZibraSmokeAndFire component.
        ///     </para>
        ///     <para>
        ///         If you set it to null in Editor, it'll revert to default.
        ///     </para>
        /// </remarks>
        [Tooltip("Custom upscale material. Not used if you don't enable downscale in Smoke & Fire instance.")]
        public Material UpscaleMaterial;

        /// <summary>
        ///     Custom shadow projection material.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Not used if you don't enable shadow projection in Smoke & Fire instance.
        ///     </para>
        /// </remarks>
        [Tooltip(
            "Custom shadow projection material. Not used if you don't enable shadow projection in Smoke & Fire instance.")]
        public Material ShadowProjectionMaterial;

        /// <summary>
        ///     Material that will be used to render SDF visualization.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This material is meant to only be used for debugging.
        ///     </para>
        ///     <para>
        ///         We don't expect that anyone will need to modify this,
        ///         but if you want to create your own material, you'll need to use default one as a reference.
        ///     </para>
        ///     <para>
        ///         Has no effect unless you enable VisualizeSceneSDF in ZibraSmokeAndFire component.
        ///     </para>
        ///     <para>
        ///         If you set it to null in Editor, it'll revert to default.
        ///     </para>
        /// </remarks>
        [HideInInspector]
        public Material SDFRenderMaterial;

        /// <summary>
        ///     Optical density of smoke.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///          Higher values correspond to more opaque smoke.
        ///     </para>
        ///     <para>
        ///         Note that emitters may emit both smoke and fuel, but they have separate density values.
        ///     </para>
        ///     <para>
        ///         Smoke is also created as a result of fuel burning.
        ///     </para>
        /// </remarks>
        [Tooltip("Optical density of smoke. Higher values correspond to more opaque smoke. Note that emitters may emit both smoke and fuel, but they have separate density values. Smoke is also created as a result of fuel burning.")]
        [Range(0.0f, 1000.0f)]
        public float SmokeDensity = 300.0f;

        /// <summary>
        ///     Optical density of fuel.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///          Higher values correspond to more opaque fuel.
        ///     </para>
        ///     <para>
        ///         Note that emitters may emit both smoke and fuel, but they have separate density values.
        ///     </para>
        ///     <para>
        ///         Only has an effect in the fire simulation mode.
        ///     </para>
        /// </remarks>
        [Tooltip("Optical density of fuel. Higher values correspond to more opaque fuel.  Note that emitters may emit both smoke and fuel, but they have separate density values. Only has an effect in the fire simulation mode.")]
        [Range(0.0f, 1000.0f)]
        public float FuelDensity = 10.0f;

        /// <summary>
        ///     Fade coefficient.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Intended to be used with <see cref="ZibraControlTrack"/>.
        ///     </para>
        ///     <para>
        ///         Effectively works as a multiplier for the smoke/fuel density.
        ///     </para>
        ///     <para>
        ///         1.0 corresponds to no fade, 0.0 corresponds to full fade.
        ///     </para>
        /// </remarks>
        [Range(0.0f, 1.0f)]
        public float FadeCoefficient = 1.0f;

        /// <summary>
        ///     Color of the light that can pass through the smoke.
        /// </summary>
        [ColorUsage(false, true)]
        [Tooltip("Color of the light that can pass through the smoke.")]
        public Color AbsorptionColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);

        /// <summary>
        ///     Color of the light that can scatter inside the smoke.
        /// </summary>
        [ColorUsage(false, true)]
        [Tooltip("Color of the light that can scatter inside the smoke.")]
        public Color ScatteringColor = new Color(0.27f, 0.27f, 0.27f, 1.0f);

        /// <summary>
        ///     The shadow absorption color. Defines color of the light for shadow render purposes.
        /// </summary>
        /// <remarks>
        ///     Only has effect when shadow projection is enabled.
        /// </remarks>
        [ColorUsage(false, true)]
        [Tooltip("The shadow absorption color. Defines color of the light for shadow render purposes.  Only has effect when shadow projection is enabled.")]
        public Color ShadowAbsorptionColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);

        /// <summary>
        ///     Controls the distance light can travel through the volume.
        /// </summary>
        /// <remarks>
        ///     Higher values correspond to a more localized illumination effect.
        /// </remarks>
        [Range(0.0f, 1.0f)]
        [Tooltip("Controls the distance light can travel through the volume.Higher values correspond to a more localized illumination effect.")]
        public float ScatteringAttenuation = 0.2f;

        /// <summary>
        ///     Determines the amount of scattered light that contributes to the final rendered image, affecting the overall brightness of scattered light. 
        /// </summary>
        [Range(0.0f, 1.0f)]
        [Tooltip("Determines the amount of scattered light that contributes to the final rendered image, affecting the overall brightness of scattered light. ")]
        public float ScatteringContribution = 0.2f;

        /// <summary>
        ///     When enabled, simulation volume renders shadow from the point of view of Primary Light on objects occluded by smoke. 
        /// </summary>
        [Tooltip("When enabled, simulation volume renders shadow from the point of view of Primary Light on objects occluded by smoke. ")]
        public bool ObjectPrimaryShadows = false;

        /// <summary>
        ///     When enabled, simulation volume renders shadow from the point of view of Additional Lights on objects occluded by smoke.
        /// </summary>
        [Tooltip("When enabled, simulation volume renders shadow from the point of view of Additional Lights on objects occluded by smoke.")]
        public bool ObjectIlluminationShadows = false;

        /// <summary>
        ///     Controls the overall brightness of the volume's illumination from point lights.
        /// </summary>
        /// <remarks>
        ///     Higher values correspond to more illuminated volume.
        /// </remarks>
        [Min(0.0f)]
        [Tooltip("Controls the overall brightness of the volume's illumination from point lights. Higher values correspond to more illuminated volume.")]
        public float IlluminationBrightness = 1.0f;

        /// <summary>
        ///     Determines the softness of the volume's illumination.
        /// </summary>
        /// <remarks>
        ///     Higher values resulting in a more diffuse and softer appearance.
        /// </remarks>
        [Range(0.0f, 1.0f)]
        [Tooltip("Determines the softness of the volume's illumination, with higher values resulting in a more diffuse and softer appearance.")]
        public float IlluminationSoftness = 0.6f;

        /// <summary>
        ///     Brightness of black body radiation.
        /// </summary>
        /// <remarks>
        ///     Black body radiation is light emitted based on the temperature of the object emitting light.
        /// </remarks>
        [Min(0.0f)]
        [Tooltip("Brightness of black body radiation. Black body radiation is light emitted based on the temperature of the object emitting light.")]
        public float BlackBodyBrightness = 4.0f;

        /// <summary>
        ///     Controls the overall brightness of the fire.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///          This is a multiplier for brightness.
        ///     </para>
        ///     <para>
        ///         Another factor that affects brightness is speed of fuel combustion reaction.
        ///     </para>
        ///     <para>
        ///         Only has an effect in the fire simulation mode.
        ///     </para>
        /// </remarks>
        [Min(0.0f)]
        [Tooltip("Controls the overall brightness of the fire. This is a multiplier for brightness. Another factor that affects brightness is speed of fuel combustion reaction. Only has an effect in the fire simulation mode.")]
        public float FireBrightness = 400.0f;

        /// <summary>
        ///     Controls the overall brightness of the fire.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///          This is a multiplier for brightness.
        ///     </para>
        ///     <para>
        ///         Another factor that affects brightness is speed of fuel combustion reaction.
        ///     </para>
        ///     <para>
        ///         Only has an effect in the fire simulation mode.
        ///     </para>
        /// </remarks>
        [ColorUsage(true, true)]
        [Tooltip("Determines the color of the fire in Fire simulation mode, allowing for a more customized appearance. This parameter changes the entire color of the fire and does not depend on temperature. Only has an effect in the fire simulation mode.")]
        public Color FireColor = new Color(1.0f, 0.0f, 0.0f, 1.0f);

        /// <summary>
        ///     Controls the optical density of the volume, decreasing with increasing temperature.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///          Higher values correspond to more transparent fire.
        ///     </para>
        ///     <para>
        ///         Only has an effect in the fire simulation mode.
        ///     </para>
        /// </remarks>
        [Range(-1.0f, 15.0f)]
        [Tooltip("Controls the optical density of the volume, decreasing with increasing temperature. Higher values correspond to more transparent fire. Only has an effect in the fire simulation mode.")]
        public float TemperatureDensityDependence = 6.0f;

        /// <summary>
        ///     Determines the intensity of the shadows cast by the volume on the objects in the scene. 
        /// </summary>
        /// <remarks>
        ///     Only has an effect when <see cref="EnableProjectedShadows"/> is set to true.
        /// </remarks>
        [Range(0.0f, 1.0f)]
        [Tooltip("Determines the intensity of the shadows cast by the volume on the objects in the scene. Only has an effect when EnableProjectedShadows is set to true.")]
        public float ObjectShadowIntensity = 0.75f;

        /// <summary>
        ///     Controls the rate at which the intensity of the shadows cast by the volume decreases with distance, affecting how far the shadows extend.
        /// </summary>
        /// <remarks>
        ///     Only has an effect when <see cref="EnableProjectedShadows"/> is set to true.
        /// </remarks>
        [Range(0.0f, 10.0f)]
        [Tooltip("Controls the rate at which the intensity of the shadows cast by the volume decreases with distance, affecting how far the shadows extend. Only has an effect when EnableProjectedShadows is set to true.")]
        public float ShadowDistanceDecay = 2.0f;

        /// <summary>
        ///     Intensity of shadowing effect.
        /// </summary>
        /// <remarks>
        ///     Only has an effect when <see cref="EnableProjectedShadows"/> is set to true.
        /// </remarks>
        [Range(0.0f, 1.0f)]
        [Tooltip("Intensity of shadowing effect. Only has an effect when EnableProjectedShadows is set to true.")]
        public float ShadowIntensity = 0.5f;

        /// <summary>
        ///     Determines whether the volume casts shadows in the scene.
        /// </summary>
        /// <remarks>
        ///     Currently experimental.
        /// </remarks>
        [Tooltip("Determines whether the volume casts shadows in the scene. Currently Experimental.")]
        public bool EnableProjectedShadows = true;

        /// <summary>
        ///     Quality of projected shadows.
        /// </summary>
        public enum ShadowProjectionQuality
        {
            Trilinear,
            Tricubic
        }

        /// <summary>
        ///     Sets the quality level of the projected shadows.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         “Tricubic” setting results in more detailed and accurate shadows but higher performance cost.
        ///     </para>
        ///     <para>
        ///         Only has an effect when <see cref="EnableProjectedShadows"/> is set to true.
        ///     </para>
        /// </remarks>
        [Tooltip("Sets the quality level of the projected shadows. “Tricubic” setting results in more detailed and accurate shadows but higher performance cost. Only has an effect when EnableProjectedShadows is set to true.")]
        public ShadowProjectionQuality ShadowProjectionQualityLevel = ShadowProjectionQuality.Tricubic;

        /// <summary>
        ///     Controls the amount of fading of the optical density at the edges of the simulation volume, creating a smoother transition between the volume and its surroundings.
        /// </summary>
        [Range(0.0f, 1.0f)]
        [Tooltip("Controls the amount of fading of the optical density at the edges of the simulation volume, creating a smoother transition between the volume and its surroundings.")]
        public float VolumeEdgeFadeoff = 0.008f;

        /// <summary>
        ///     Ray marching step size used for the volume rendering process.
        /// </summary>
        /// <remarks>
        ///     Smaller values correspond to more accurate rendering but higher performance cost.
        /// </remarks>
        [Range(0.5f, 10.0f)]
        [Tooltip("Ray marching step size used for the volume rendering process. Smaller values correspond to more accurate rendering but higher performance cost.")]
        public float RayMarchingStepSize = 2.5f;

        /// <summary>
        ///     The resolution of the primary shadow 3D texture relative to the simulation volume.
        /// </summary>
        /// <remarks>
        ///     Smaller values correspond to more accurate shadows but higher performance cost.
        /// </remarks>
        [Range(0.05f, 1.0f)]
        [Tooltip("The resolution of the primary shadow 3D texture relative to the simulation volume. Smaller values correspond to more accurate shadows but higher performance cost.")]
        public float ShadowResolution = 0.25f;

        /// <summary>
        ///     Ray marching step size that is used for calculating the primary shadows, affecting the accuracy and detail of the shadows.
        /// </summary>
        [Range(1.0f, 10.0f)]
        [Tooltip("Ray marching step size that is used for calculating the primary shadows, affecting the accuracy and detail of the shadows.")]
        public float ShadowStepSize = 1.5f;

        /// <summary>
        ///     Maximum number of ray marching steps used for calculating the primary shadows.
        /// </summary>
        /// <remarks>
        ///     Higher values correspond to more accurate shadows but higher performance cost.
        /// </remarks>
        [Range(8, 512)]
        [Tooltip("Maximum number of ray marching steps used for calculating the primary shadows. Higher values correspond to more accurate shadows but higher performance cost.")]
        public int ShadowMaxSteps = 256;

        /// <summary>
        ///     The resolution of the Additional Lights shadow 3D texture relative to the simulation volume.
        /// </summary>
        /// <remarks>
        ///     The lower the resolution the faster the shadows are computed.
        /// </remarks>
        [Range(0.05f, 1.0f)]
        [Tooltip("The resolution of the Additional Lights shadow 3D texture relative to the simulation volume. The lower the resolution the faster the shadows are computed.")]
        public float IlluminationResolution = 0.25f;

        /// <summary>
        ///     Ray marching step size used for illumination calculation.
        /// </summary>
        /// <remarks>
        ///     Smaller values correspond to more accurate illumination but higher performance cost.
        /// </remarks>
        [Range(1.0f, 10.0f)]
        [Tooltip("Ray marching step size used for illumination calculation. Smaller values correspond to more accurate illumination but higher performance cost.")]
        public float IlluminationStepSize = 1.5f;

        /// <summary>
        ///     Maximum number of ray marching steps used for illumination calculation.
        /// </summary>
        /// <remarks>
        ///     Higher values correspond to more accurate illumination but higher performance cost.
        /// </remarks>
        [Range(0, 512)]
        [Tooltip("Maximum number of ray marching steps used for illumination calculation. Higher values correspond to more accurate illumination but higher performance cost.")]
        public int IlluminationMaxSteps = 64;

        /// <summary>
        ///     Controls the maximum number of effect particles in the simulation.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         If you hit the limit, you won’t be able to emit more, until some of the particles get deleted.
        ///     </para>
        ///     <para>
        ///         Higher values correspond to potentially more effect particles but higher performance cost.
        ///     </para>
        /// </remarks>
        [Range(0, 8388608)]
        [Tooltip("Controls the maximum number of effect particles in the simulation. If you hit the limit, you won’t be able to emit more, until some of the particles get deleted. Higher values correspond to potentially more effect particles but higher performance cost.")]
        public int MaxEffectParticles = 32768;

        /// <summary>
        ///     Determines the lifetime of the effect particles in the simulation, affecting how long the particles exist before disappearing. 
        /// </summary>
        // Must fit in 12 bits
        [Range(0, 4095)]
        [Tooltip("Determines the lifetime of the effect particles in the simulation, affecting how long the particles exist before disappearing. ")]
        public int ParticleLifetime = 150;

        /// <summary>
        ///     Controls the resolution of the occlusion texture used in the effect particle rendering.
        /// </summary>
        /// <remarks>
        ///     Higher values correspond to more accurate effect particles occlusion but higher performance and memory cost.
        /// </remarks>
        [Range(0.05f, 1.0f)]
        [Tooltip("Controls the resolution of the occlusion texture used in the effect particle rendering. Higher values correspond to more accurate effect particles occlusion but higher performance and memory cost.")]
        public float ParticleOcclusionResolution = 0.25f;

        /// <summary>
        ///     Name for preset analytics
        /// </summary>
        [HideInInspector]
        public string PresetName = "";
#endregion
#region Implementation details
        [HideInInspector]
        [SerializeField]
        internal ComputeShader NoOpCompute;

        [HideInInspector]
        [SerializeField]
        internal ComputeShader RendererCompute;

        [HideInInspector]
        [SerializeField]
        internal ComputeShader ClearResourceCompute;

        [HideInInspector]
        [SerializeField]
        internal Texture BlueNoise;

#if UNITY_EDITOR
        private static string DEFAULT_UPSCALE_MATERIAL_GUID = "5db2c81e302e40efb0419ec664a50f01";
        private static string DEFAULT_SMOKE_MATERIAL_GUID = "7246813b959848a28c439cc0e41ae98f";
        private static string NO_OP_COMPUTE_GUID = "82c4529b0f5984f10920878932a2435b";
        private static string RENDERER_COMPUTE_GUID = "5ce526a931bd4c559b5c9ba2ba56155c";
        private static string CLEAR_RESOURCE_COMPUTE_GUID = "7bef9cf412ed196488fd78b297412af6";
        private static string BLUE_NOISE_TEXTURE_GUID = "39bb69ae68e041cd8579a8abc5762e42";
        private static string DEFAULT_SHADOW_PROJECTION_MATERIAL_GUID = "4d5bfdd644c2696498171c3ad15d3e59";
        private static string DEFAULT_SDF_RENDER_MATERIAL_GUID = "95a617c615117f84499ff40ca43eca01";

        private void Reset()
        {
            string DefaultUpscaleMaterialPath = AssetDatabase.GUIDToAssetPath(DEFAULT_UPSCALE_MATERIAL_GUID);
            UpscaleMaterial = AssetDatabase.LoadAssetAtPath(DefaultUpscaleMaterialPath, typeof(Material)) as Material;
            string DefaultSmokeMaterialPath = AssetDatabase.GUIDToAssetPath(DEFAULT_SMOKE_MATERIAL_GUID);
            SmokeMaterial = AssetDatabase.LoadAssetAtPath(DefaultSmokeMaterialPath, typeof(Material)) as Material;
            string DefaultShadowProjectorMaterialPath =
                AssetDatabase.GUIDToAssetPath(DEFAULT_SHADOW_PROJECTION_MATERIAL_GUID);
            ShadowProjectionMaterial =
                AssetDatabase.LoadAssetAtPath(DefaultShadowProjectorMaterialPath, typeof(Material)) as Material;

            string NoOpComputePath = AssetDatabase.GUIDToAssetPath(NO_OP_COMPUTE_GUID);
            NoOpCompute = AssetDatabase.LoadAssetAtPath(NoOpComputePath, typeof(ComputeShader)) as ComputeShader;
            string RendererComputePath = AssetDatabase.GUIDToAssetPath(RENDERER_COMPUTE_GUID);
            RendererCompute = AssetDatabase.LoadAssetAtPath(RendererComputePath, typeof(ComputeShader)) as ComputeShader;
            string ClearResourceComputePath = AssetDatabase.GUIDToAssetPath(CLEAR_RESOURCE_COMPUTE_GUID);
            ClearResourceCompute = AssetDatabase.LoadAssetAtPath(ClearResourceComputePath, typeof(ComputeShader)) as ComputeShader;
            string BlueNoisePath = AssetDatabase.GUIDToAssetPath(BLUE_NOISE_TEXTURE_GUID);
            BlueNoise = AssetDatabase.LoadAssetAtPath(BlueNoisePath, typeof(Texture)) as Texture;
            string DefaultSDFRenderMaterialPath = AssetDatabase.GUIDToAssetPath(DEFAULT_SDF_RENDER_MATERIAL_GUID);
            SDFRenderMaterial =
                AssetDatabase.LoadAssetAtPath(DefaultSDFRenderMaterialPath, typeof(Material)) as Material;
        }

        private void OnValidate()
        {
            if (UpscaleMaterial == null)
            {
                string DefaultUpscaleMaterialPath = AssetDatabase.GUIDToAssetPath(DEFAULT_UPSCALE_MATERIAL_GUID);
                UpscaleMaterial =
                    AssetDatabase.LoadAssetAtPath(DefaultUpscaleMaterialPath, typeof(Material)) as Material;
            }
            if (SmokeMaterial == null)
            {
                string DefaultSmokeMaterialPath = AssetDatabase.GUIDToAssetPath(DEFAULT_SMOKE_MATERIAL_GUID);
                SmokeMaterial = AssetDatabase.LoadAssetAtPath(DefaultSmokeMaterialPath, typeof(Material)) as Material;
            }
            if (ShadowProjectionMaterial == null)
            {
                string DefaultShadowProjectorMaterialPath =
                    AssetDatabase.GUIDToAssetPath(DEFAULT_SHADOW_PROJECTION_MATERIAL_GUID);
                ShadowProjectionMaterial =
                    AssetDatabase.LoadAssetAtPath(DefaultShadowProjectorMaterialPath, typeof(Material)) as Material;
            }
            if (SDFRenderMaterial == null)
            {
                string DefaultSDFRenderMaterialPath = AssetDatabase.GUIDToAssetPath(DEFAULT_SDF_RENDER_MATERIAL_GUID);
                SDFRenderMaterial =
                    AssetDatabase.LoadAssetAtPath(DefaultSDFRenderMaterialPath, typeof(Material)) as Material;
            }

            string NoOpComputePath = AssetDatabase.GUIDToAssetPath(NO_OP_COMPUTE_GUID);
            NoOpCompute = AssetDatabase.LoadAssetAtPath(NoOpComputePath, typeof(ComputeShader)) as ComputeShader;
            string RendererComputePath = AssetDatabase.GUIDToAssetPath(RENDERER_COMPUTE_GUID);
            RendererCompute = AssetDatabase.LoadAssetAtPath(RendererComputePath, typeof(ComputeShader)) as ComputeShader;
            string ClearResourceComputePath = AssetDatabase.GUIDToAssetPath(CLEAR_RESOURCE_COMPUTE_GUID);
            ClearResourceCompute = AssetDatabase.LoadAssetAtPath(ClearResourceComputePath, typeof(ComputeShader)) as ComputeShader;
            string BlueNoisePath = AssetDatabase.GUIDToAssetPath(BLUE_NOISE_TEXTURE_GUID);
            BlueNoise = AssetDatabase.LoadAssetAtPath(BlueNoisePath, typeof(Texture)) as Texture;
        }
#endif
#endregion
    }
}
