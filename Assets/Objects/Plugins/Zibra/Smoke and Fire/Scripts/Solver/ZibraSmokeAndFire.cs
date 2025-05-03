using com.zibra.smoke_and_fire.DataStructures;
using com.zibra.smoke_and_fire.Manipulators;
using com.zibra.common.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEngine.Experimental.Rendering;
using com.zibra.smoke_and_fire.Utilities;
using com.zibra.smoke_and_fire.Bridge;
using com.zibra.common.SDFObjects;
using com.zibra.common.Solver;
using com.zibra.common;
using com.zibra.common.Timeline;

#if UNITY_EDITOR
using com.zibra.smoke_and_fire.Analytics;
using com.zibra.common.Editor.SDFObjects;
using com.zibra.common.Editor;
using com.zibra.common.Editor.Licensing;
#endif

#if UNITY_PIPELINE_HDRP
using UnityEngine.Rendering.HighDefinition;
#endif // UNITY_PIPELINE_HDRP

namespace com.zibra.smoke_and_fire.Solver
{
    /// <summary>
    ///     Main Smoke & Fire solver component
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Each Smoke & Fire component corresponds to one instance of simulation.
    ///         Different instances of simulation can't interact with each other.
    ///     </para>
    ///     <para>
    ///         Some parameters can't be after simulation has started and we created GPU buffers.
    ///         Normally, simulation starts in playmode in OnEnable and stops in OnDisable.
    ///         To change those parameters in runtime you want to have this component disabled,
    ///         and after setting them, enable this component.
    ///     </para>
    ///     <para>
    ///         OnEnable will allocate GPU buffers, which may cause stuttering.
    ///         Consider enabling simulation volume on level load, but with simulation/render paused,
    ///         to not pay the cost of fluid initialization during gameplay.
    ///     </para>
    ///     <para>
    ///         Disabling simulation volume will free GPU buffers.
    ///         This means that Smoke&Fire state will be lost.
    ///     </para>
    ///     <para>
    ///         Various parameters of the simulation volume are spread throught multiple components.
    ///         This is done so you can use Unity's Preset system to only change part of parameters.
    ///     </para>
    /// </remarks>
    [AddComponentMenu(Effects.SmokeAndFireComponentMenuPath + "Zibra Smoke & Fire")]
    [RequireComponent(typeof(ZibraSmokeAndFireMaterialParameters))]
    [RequireComponent(typeof(ZibraSmokeAndFireSolverParameters))]
    [RequireComponent(typeof(ZibraManipulatorManager))]
    [ExecuteInEditMode]
    public class ZibraSmokeAndFire : PlaybackControl, StatReporter
    {
#region Public Interface
#region Properties
        /// <summary>
        ///     A list of all instances of the Smoke & Fire solver
        /// </summary>
        public static List<ZibraSmokeAndFire> AllInstances = new List<ZibraSmokeAndFire>();

        /// <summary>
        ///     See <see cref="CurrentSimulationMode"/>.
        /// </summary>
        public enum SimulationMode
        {
            Smoke,
            ColoredSmoke,
            Fire,
        }

        /// <summary>
        ///     Simulation GUID.
        /// </summary>
        /// <remarks>
        ///     Randomly generated when <see cref="ZibraLiquid"/> is created.
        ///     So when ZibraLiquid component is copied GUID will be copied as well and so may not be unique.
        /// </remarks>
        public string SimulationGUID
        {
            get { return _SimulationGUID; }
        }

        /// <summary>
        ///     Setting that determines the type of simulation being performed, with options including Smoke, Colored
        ///     Smoke, and Fire.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Smoke mode simulates a single colored smoke/fog/etc.
        ///     </para>
        ///     <para>
        ///         Colored Smoke mode allows emitting smoke of a given color.
        ///     </para>
        ///     <para>
        ///         Fire mode simulates smoke, fuel, and temperature components, allowing control of burning fuel to
        ///         produce fire.
        ///     </para>
        ///     <para>
        ///         Depending on this parameter, simulation will use different parameters defined in various other
        ///         classes.
        ///     </para>
        ///     <para>
        ///         Changing this parameter during simulation has no effect. See <see cref="ActiveSimulationMode"/>.
        ///     </para>
        /// </remarks>
        [Tooltip("Setting that determines the type of simulation being performed.")]
        public SimulationMode CurrentSimulationMode = SimulationMode.Fire;

        /// <summary>
        ///     Simulation mode currently used by the simulation. Will not be changed if <see cref="CurrentSimulationMode"/>
        ///     is changed after simulation has started.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Only valid when simulation is initialized.
        ///     </para>
        ///     <para>
        ///         <see cref="CurrentSimulationMode"/> is copied to this member on simulation initialization,
        ///         and it can't change until simulation deinitialization.
        ///     </para>
        /// </remarks>
        public SimulationMode ActiveSimulationMode { get; private set; }

        /// <summary>
        ///     Last used timestep.
        /// </summary>
        public float LastTimestep { get; private set; } = 0.0f;

        /// <summary>
        ///     Simulation time passed (in simulation time units)
        /// </summary>
        public float SimulationInternalTime { get; private set; } = 0.0f;

        /// <summary>
        ///     Number of simulation iterations done so far
        /// </summary>
        public int SimulationInternalFrame { get; private set; } = 0;

        /// <summary>
        ///     The grid size of the simulation
        /// </summary>
        public Vector3Int GridSize { get; private set; }

        /// <summary>
        ///     Directional light that will be used for Smoke & Fire lighting.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Must be set, otherwise simulation will not start.
        ///     </para>
        ///     <para>
        ///         Can be freely modified at runtime.
        ///     </para>
        /// </remarks>
        [Tooltip(
            "Directional light that will be used for Smoke & Fire lighting. Must be set, otherwise simulation will not start. Can be freely modified at runtime.")]
        [FormerlySerializedAs("mainLight")]
        public Light MainLight;

        /// <summary>
        ///     List of point lights that contribute to Smoke & Fire lighting.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         You can add up to 16 lights to that list.
        ///     </para>
        ///     <para>
        ///         Can be freely modified at runtime.
        ///     </para>
        /// </remarks>
        [Tooltip(
            "List of point lights that contribute to Smoke & Fire lighting. Can be freely modified at runtime. You can add up to 16 lights to that list.")]
        [FormerlySerializedAs("lights")]
        public List<Light> Lights;

        /// <summary>
        ///     Timestep used in each simulation iteration.
        /// </summary>
        [Tooltip("Timestep used in each simulation iteration.")]
        [Range(0.0f, 3.0f)]
        [FormerlySerializedAs("TimeStep")]
        [FormerlySerializedAs("timeStep")]
        public float Timestep = 1.00f;

        /// <summary>
        ///     Maximum allowed number of frames queued to render.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Only used when <c>QualitySettings.maxQueuedFrames</c> is not available or invalid.
        ///     </para>
        ///     <para>
        ///         Defines number of frames we'll wait between submitting simulation workload
        ///         and reading back simulation information back to the CPU.
        ///         Higher values correspond to more delay for simulation info readback,
        ///         while lower values can potentially decreasing framerate.
        ///     </para>
        /// </remarks>
        [Tooltip("Fallback max frame latency. Used when it isn't possible to retrieve Unity's max frame latency.")]
        [Range(2, 16)]
        public UInt32 MaxFramesInFlight = 3;

        /// <summary>
        ///     Number of simulation iterations per simulation frame.
        /// </summary>
        /// <remarks>
        ///     The simulation does 1/3 of the smoke simulation per iteration,
        ///     and extrapolates the smoke movement in time for higher performance while keeping smooth movement.
        ///     To do a full simulation per frame you can set it to 3 iterations,
        ///     which may be beneficial in cases where the simulation interacts with quickly moving objects.
        /// </remarks>
        [Tooltip("Number of simulation iterations per simulation frame.")]
        [Range(1, 10)]
        public int SimulationIterations = 3;

        /// <summary>
        ///     Size of single simulation node.
        /// </summary>
        public float CellSize { get; private set; }

        /// <summary>
        ///     Size of the simulation grid.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This is the most important parameter for performance adjustment.
        ///     </para>
        ///     <para>
        ///         Higher size of the grid corresponds to a higher quality simulation,
        ///         but results in higher VRAM usage and a higher performance cost.
        ///     </para>
        /// </remarks>
        [Tooltip("Sets the resolution of the largest side of the grids container equal to this value")]
        [Min(16)]
        [FormerlySerializedAs("gridResolution")]
        public int GridResolution = 128;

        /// <summary>
        ///     Freezes simulation when disabled.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Also decreases performance cost when disabled, since simulation won�t run.
        ///     </para>
        ///     <para>
        ///         Disabling this option does not prevent simulation from rendering.
        ///     </para>
        /// </remarks>
        [Tooltip(
            "Freezes simulation when disabled. Also decreases performance cost when disabled, since simulation won't run. Disabling this option does not prevent simulation from rendering.")]
        [FormerlySerializedAs("runSimulation")]
        public bool RunSimulation = true;

        /// <summary>
        ///     Enables rendering of the smoke/fire.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Disabling rendering decreases performance cost.
        ///     </para>
        ///     <para>
        ///         Disabling this option does not prevent simulation from running.
        ///     </para>
        /// </remarks>
        [Tooltip(
            "Enables rendering of the smoke/fire. Disabling rendering decreases performance cost. Disabling this option does not prevent simulation from running.")]
        [FormerlySerializedAs("runRendering")]
        public bool RunRendering = true;

        /// <summary>
        ///     When enabled, moving simulation volume will not disturb simulation. When disabled, smoke/fire will try
        ///     to stay in place in world space.
        /// </summary>
        /// <remarks>
        ///     If you want to move the simulation around the scene, you want to disable this option.
        /// </remarks>
        [Tooltip(
            "When enabled, moving simulation volume will not disturb simulation. When disabled, smoke/fire will try to stay in place in world space. If you want to move the simulation around the scene, you want to disable this option.")]
        [FormerlySerializedAs("fixVolumeWorldPosition")]
        public bool FixVolumeWorldPosition = true;

        /// <summary>
        ///     Whether to render visualised SDFs.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Has no effect when liquid is not initialized.
        ///     </para>
        ///     <para>
        ///         This option is only meant for debugging purposes.
        ///         It's strongly recommended to not enable it in final builds.
        ///     </para>
        /// </remarks>
        [FormerlySerializedAs("visualizeSceneSDF")]
        [Tooltip("Whether to render visualized SDFs")]
        public bool VisualizeSceneSDF = false;

        /// <summary>
        ///     Is simulation initialized
        /// </summary>
        public bool Initialized { get; private set; } = false;

        /// <summary>
        ///     Allows you to render Smoke & Fire in lower resolution.
        /// </summary>
        [Tooltip("Allows you to render Smoke & Fire in lower resolution.")]
        public bool EnableDownscale = false;

        /// <summary>
        ///     Scale width/height of smoke & fire render.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Pixel count is decreased by factor of DownscaleFactor * DownscaleFactor.
        ///     </para>
        ///     <para>
        ///         Doesn't have any effect unless EnableDownscale is set to true.
        ///     </para>
        /// </remarks>
        [Range(0.2f, 0.99f)]
        [Tooltip("Scale width/height of smoke & fire render.")]
        public float DownscaleFactor = 0.5f;

        /// <summary>
        ///     Size of the simulation volume.
        /// </summary>
        [Tooltip("Size of the simulation volume.")]
        [FormerlySerializedAs("containerSize")]
        public Vector3 ContainerSize = new Vector3(5, 5, 5);

        /// <summary>
        ///     Render target containing Visualize SDF data.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Format:
        ///         * xyz - Normal
        ///         * w - Depth
        ///     </para>
        ///     <para>
        ///         May be null if <see cref="VisualizeSceneSDF"/> is false.
        ///     </para>
        /// </remarks>
        [NonSerialized]
        public RenderTexture VisualizeSDFTarget;

        /// <summary>
        ///     Last simulated container position
        /// </summary>
        public Vector3 SimulationContainerPosition;

        /// <summary>
        ///     Injection point used for BRP render.
        /// </summary>
        [Tooltip("Injection point used for BRP render")]
        public CameraEvent CurrentInjectionPoint = CameraEvent.BeforeForwardAlpha;

        /// <summary>
        ///     Whether to limit maximum number of smoke simulation iterations per second.
        /// </summary>
        [Tooltip("Whether to limit maximum number of smoke simulation iterations per second.")]
        public bool LimitFramerate = true;

        /// <summary>
        ///     Maximum simulation iterations per second.
        /// </summary>
        /// <remarks>
        ///     Has no effect if <see cref="LimitFramerate"/> is set to false.
        /// </remarks>
        [Min(0.0f)]
        public float MaximumFramerate = 60.0f;

        /// <summary>
        ///     Reference to
        ///     <see cref="DataStructures::ZibraSmokeAndFireSolverParameters">ZibraSmokeAndFireSolverParameters</see>
        ///     corresponding to this object.
        /// </summary>
        public ZibraSmokeAndFireSolverParameters SolverParameters
        {
            get
            {
                if (SolverParametersInternal == null)
                {
                    SolverParametersInternal = gameObject.GetComponent<ZibraSmokeAndFireSolverParameters>();
                    if (SolverParametersInternal == null)
                    {
                        SolverParametersInternal = gameObject.AddComponent<ZibraSmokeAndFireSolverParameters>();
#if UNITY_EDITOR
                        UnityEditor.EditorUtility.SetDirty(this);
#endif
                    }
                }
                return SolverParametersInternal;
            }
        }

        /// <summary>
        ///     Reference to
        ///     <see cref="DataStructures::ZibraSmokeAndFireMaterialParameters">ZibraSmokeAndFireMaterialParameters</see>
        ///     corresponding to this object.
        /// </summary>
        public ZibraSmokeAndFireMaterialParameters MaterialParameters
        {
            get
            {
                if (MaterialParametersInternal == null)
                {
                    MaterialParametersInternal = gameObject.GetComponent<ZibraSmokeAndFireMaterialParameters>();
                    if (MaterialParametersInternal == null)
                    {
                        MaterialParametersInternal = gameObject.AddComponent<ZibraSmokeAndFireMaterialParameters>();
#if UNITY_EDITOR
                        UnityEditor.EditorUtility.SetDirty(this);
#endif
                    }
                }
                return MaterialParametersInternal;
            }
        }

        private ZibraSmokeAndFireSolverParameters SolverParametersInternal;
        private ZibraSmokeAndFireMaterialParameters MaterialParametersInternal;
#endregion

#region Methods
        /// <summary>
        ///     Simulation needs to do some loading before simulation can start.
        ///     Loading starts during initialization of engine.
        ///     If it takes too long and you start simulation too early
        ///     it can stall engine until loading finishes.
        ///     You can use this method to show loading screen to wait for loading to end
        ///     and prevent stalling.
        /// </summary>
        /// <returns>
        ///     true - if starting simulation won't trigger stall
        ///     false - if starting simulation will trigger stall
        /// </returns>
        public bool IsLoaded()
        {
            return SmokeAndFireBridge.ZibraSmokeAndFire_IsLoaded() != 0;
        }

        /// <summary>
        ///     Stalls engine until all loading is finished
        ///     See <see cref="IsLoaded"/>
        /// </summary>
        public void WaitLoad()
        {
            SmokeAndFireBridge.ZibraSmokeAndFire_WaitLoad();
        }

        /// <summary>
        ///     Initializes simulation resources.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This is automatically called in <c>OnEnable()</c> if not in edit mode.
        ///         To run simulation in edit mode, you need to call it manually.
        ///     </para>
        ///     <para>
        ///         On success, sets <see cref="Initialize"/> to true.
        ///     </para>
        ///     <para>
        ///         On fail, cleans up simulation resources and throws an <c>Exception</c>.
        ///     </para>
        ///     <para>
        ///         Initialization allocates GPU resources,
        ///         so calling this at runtime may cause stutter.
        ///         Prefer to initialize on scene load.
        ///     </para>
        ///     <para>
        ///         Has no effect if is already initialized.
        ///     </para>
        /// </remarks>
        private void InitializeSimulation()
        {
            if (Initialized)
            {
                return;
            }

            if (MainLight == null)
            {
                throw new Exception("The main light isn't assigned. SmokeAndFire was disabled.");
            }

            bool isDeviceSupported = SmokeAndFireBridge.ZibraSmokeAndFire_IsHardwareSupported();
            if (!isDeviceSupported)
            {
                throw new Exception("Zibra Smoke & Fire doesn't support this hardware. SmokeAndFire was disabled.");
            }

            try
            {
#if !ZIBRA_EFFECTS_NO_LICENSE_CHECK && UNITY_EDITOR
                if (!LicensingManager.Instance.IsLicenseVerified(PluginManager.Effect.Smoke))
                {
                    string errorMessage =
                        $"License wasn't verified. {LicensingManager.Instance.GetErrorMessage(PluginManager.Effect.Smoke)} Smoke & Fire won't run in editor.";
                    throw new Exception(errorMessage);
                }
#endif

#if UNITY_PIPELINE_HDRP
                if (RenderPipelineDetector.GetRenderPipelineType() == RenderPipelineDetector.RenderPipeline.HDRP)
                {
                    bool missingRequiredParameter = false;

                    if (MainLight == null)
                    {
                        Debug.LogError("No Custom Light set in Zibra Smoke & Fire.");
                        missingRequiredParameter = true;
                    }

                    if (missingRequiredParameter)
                    {
                        throw new Exception("Smoke & Fire creation failed due to missing parameter.");
                    }
                }
#endif

                ValidateManipulators();

                bool haveEmitter = false;
                foreach (var manipulator in Manipulators)
                {
                    if ((manipulator.GetManipulatorType() == Manipulator.ManipulatorType.Emitter ||
                         manipulator.GetManipulatorType() == Manipulator.ManipulatorType.TextureEmitter||
                         manipulator.GetManipulatorType() == Manipulator.ManipulatorType.EffectParticleEmitter) &&
                        manipulator.GetComponent<SDFObject>() != null)
                    {
                        haveEmitter = true;
                        break;
                    }
                }

                if (!haveEmitter)
                {
                    throw new Exception(
                        "Smoke & Fire creation failed. Simulation has no emitters, or all emitters are missing SDF component.");
                }

                Camera.onPreRender += RenderCallBackWrapper;

                solverCommandBuffer = new CommandBuffer { name = "ZibraSmokeAndFire.Solver" };
                ActiveSimulationMode = CurrentSimulationMode;

                CurrentInstanceID = ms_NextInstanceId++;

                ForceCloseCommandEncoder(solverCommandBuffer);
                SmokeAndFireBridge.SubmitInstanceEvent(solverCommandBuffer, CurrentInstanceID,
                                                       SmokeAndFireBridge.EventID.CreateFluidInstance);
                Graphics.ExecuteCommandBuffer(solverCommandBuffer);
                solverCommandBuffer.Clear();

                InitializeSolver();

                var initializeGPUReadbackParamsBridgeParams = new InitializeGPUReadbackParams();
                UInt32 manipSize = (UInt32)ManipulatorManager.Elements * STATISTICS_PER_MANIPULATOR * sizeof(Int32);

                initializeGPUReadbackParamsBridgeParams.readbackBufferSize = manipSize;
                switch (SystemInfo.graphicsDeviceType)
                {
                    case GraphicsDeviceType.Direct3D11:
                    case GraphicsDeviceType.XboxOne:
                    case GraphicsDeviceType.Switch:
                    case GraphicsDeviceType.Direct3D12:
                    case GraphicsDeviceType.XboxOneD3D12:
                        initializeGPUReadbackParamsBridgeParams.maxFramesInFlight = QualitySettings.maxQueuedFrames + 1;
                        break;
                    default:
                        initializeGPUReadbackParamsBridgeParams.maxFramesInFlight = (int)this.MaxFramesInFlight;
                        break;
                }

                IntPtr nativeCreateInstanceBridgeParams =
                    Marshal.AllocHGlobal(Marshal.SizeOf(initializeGPUReadbackParamsBridgeParams));
                Marshal.StructureToPtr(initializeGPUReadbackParamsBridgeParams, nativeCreateInstanceBridgeParams, true);

                solverCommandBuffer.Clear();
                SmokeAndFireBridge.SubmitInstanceEvent(solverCommandBuffer, CurrentInstanceID,
                                                       SmokeAndFireBridge.EventID.InitializeGpuReadback,
                                                       nativeCreateInstanceBridgeParams);
                Graphics.ExecuteCommandBuffer(solverCommandBuffer);
                solverCommandBuffer.Clear();
                toFreeOnExit.Add(nativeCreateInstanceBridgeParams);

                Initialized = true;
                // hack to make editor -> play mode transition work when the simulation is initialized
                forceTextureUpdate = true;

#if UNITY_EDITOR
                SmokeAndFireAnalytics.SimulationStart(this);
#endif
            }
            catch (Exception)
            {
                ClearRendering();
                ClearSolver();

                Initialized = false;

                throw;
            }
        }


        /// <summary>
        ///     Releases simulation resources.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This is automatically called in <c>OnDisable()</c>.
        ///         When running simulation in edit mode,
        ///         you may want to call it manually.
        ///     </para>
        ///     <para>
        ///         Sets <see cref="Initialize"/> to false.
        ///     </para>
        ///     <para>
        ///         Releases GPU resources and so frees up VRAM.
        ///     </para>
        ///     <para>
        ///         Has no effect if simulation is not initialized.
        ///     </para>
        /// </remarks>
        public void ReleaseSimulation()
        {
            if (!Initialized)
            {
                return;
            }

            Initialized = false;
            ClearRendering();
            ClearSolver();

            // If ZibraSmokeAndFire object gets disabled/destroyed
            // We still may need to do cleanup few frames later
            // So we create new gameobject which allows us to run cleanup code
            ZibraSmokeAndFireGPUGarbageCollector.CreateGarbageCollector();
        }

        /// <summary>
        ///     Removes manipulator from the simulation.
        /// </summary>
        /// <remarks>
        ///     Can only be used if simulation is not initialized yet,
        ///     e.g. when component is disabled.
        /// </remarks>
        public void RemoveManipulator(Manipulator manipulator)
        {
            if (Initialized)
            {
                Debug.LogWarning("We don't yet support changing number of manipulators/colliders at runtime.");
                return;
            }

            if (Manipulators.Contains(manipulator))
            {
                Manipulators.Remove(manipulator);
                Manipulators.Sort(new ManipulatorCompare());
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
        }

        /// <summary>
        ///     Returns read-only list of manipulators.
        /// </summary>
        public ReadOnlyCollection<Manipulator> GetManipulatorList()
        {
            return Manipulators.AsReadOnly();
        }

        /// <summary>
        ///     Checks whether manipulator list has specified manipulator.
        /// </summary>
        public bool HasManipulator(Manipulator manipulator)
        {
            return Manipulators.Contains(manipulator);
        }

        /// <summary>
        ///     Adds manipulator to the simulation.
        /// </summary>
        /// <remarks>
        ///     Can only be used if simulation is not initialized yet,
        ///     e.g. when component is disabled.
        /// </remarks>
        public void AddManipulator(Manipulator manipulator)
        {
            if (Initialized)
            {
                Debug.LogWarning("We don't yet support changing number of manipulators/colliders at runtime.");
                return;
            }

            if (!Manipulators.Contains(manipulator))
            {
                Manipulators.Add(manipulator);
                Manipulators.Sort(new ManipulatorCompare());
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
        }

        /// <summary>
        ///     Checks if simulation has at least one emitter manipulator.
        /// </summary>
        /// <remarks>
        ///     Smoke & Fire simulation must have at least one emitter,
        ///     otherwise it won't be able to generate any non empty state.
        /// </remarks>
        public bool HasEmitter()
        {
            foreach (var manipulator in Manipulators)
            {
                if (manipulator.GetManipulatorType() == Manipulator.ManipulatorType.Emitter ||
                    manipulator.GetManipulatorType() == Manipulator.ManipulatorType.TextureEmitter ||
                    manipulator.GetManipulatorType() == Manipulator.ManipulatorType.EffectParticleEmitter)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Updates values of some constants based on <see cref="ContainerSize"/> and
        ///     <see cref="GridResolution"/>.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Update values of <see cref="CellSize"/> and <see cref="GridSize"/>.
        ///     </para>
        ///     <para>
        ///         Has no effect when simulation is initialized, since you can't modify
        ///         aforementioned parameters in this case.
        ///     </para>
        /// </remarks>
        public void UpdateGridSize()
        {
            if (Initialized)
            {
                return;
            }

            CellSize = Math.Max(ContainerSize.x, Math.Max(ContainerSize.y, ContainerSize.z)) / GridResolution;
            GridSize = 8 * Vector3Int.CeilToInt(ContainerSize / (8.0f * CellSize));
            NumNodes = GridSize[0] * GridSize[1] * GridSize[2];
            GridDownscale = (int)Mathf.Ceil(
                1.0f / Mathf.Max(MaterialParameters.ShadowResolution, MaterialParameters.IlluminationResolution));
            GridSizeLOD = LODGridSize(GridSize, GridDownscale);
        }
        public List<string> GetStats()
        {
            string SimulationType = "";
            switch (CurrentSimulationMode)
            {
                case SimulationMode.Smoke:
                    SimulationType = "Smoke";
                    break;
                case SimulationMode.ColoredSmoke:
                    SimulationType = "Colored Smoke";
                    break;
                case SimulationMode.Fire:
                    SimulationType = "Fire";
                    break;
            }
            float ResolutionScale = EnableDownscale ? DownscaleFactor : 1.0f;
            float PixelCountScale = ResolutionScale * ResolutionScale;
            return new List<string> {
                $"{SimulationType} Simulation",
                $"Instance: {name}",
                $"Grid size: {GridSize}",
                $"Render resolution: {ResolutionScale * 100.0f}%",
                $"Render pixel count: {PixelCountScale * 100.0f}%"
            };
        }

#if UNITY_EDITOR
        /// <summary>
        ///     (Editor only) Event that is triggered when state of manipulator changes
        ///     to trigger update of custom editor.
        /// </summary>
        /// <remarks>
        ///     This is only intended to update custom editors,
        ///     You can trigger it when you change some state to update custom editor.
        ///     But using it for anything else is a bad idea.
        /// </remarks>
        public event Action OnChanged;

        /// <summary>
        ///     (Editor only) Triggers custom editor update.
        /// </summary>
        /// <remarks>
        ///     Just triggers <see cref="OnChanged"/>.
        /// </remarks>
        public void NotifyChange()
        {
            if (OnChanged != null)
            {
                OnChanged.Invoke();
            }
        }
#endif
#endregion

#endregion
#region Deprecated
        /// @cond SHOW_DEPRECATED

        
#region Properties
        /// @deprecated
        /// Only used for backwards compatibility
        [Obsolete("solverParameters is deprecated. Please use SolverParameters.", true)]
        [NonSerialized]
        public ZibraSmokeAndFireSolverParameters solverParameters;

        /// @deprecated
        /// Only used for backwards compatibility
        [Obsolete("materialParameters is deprecated. Please use MaterialParameters.", true)]
        [NonSerialized]
        public ZibraSmokeAndFireMaterialParameters materialParameters;
#endregion
#region Methods
        /// @deprecated
        /// Only used for backwards compatibility
        [Obsolete("StopSolver is deprecated. Please use ReleaseSimulation.", true)]
        public void StopSolver()
        {
        }
#endregion

        /// @endcond
#endregion
#region Implementation details
#region Constants
        internal const int STATISTICS_PER_MANIPULATOR = 12;
        private const int WORKGROUP_SIZE_X = 8;
        private const int WORKGROUP_SIZE_Y = 8;
        private const int WORKGROUP_SIZE_Z = 6;
        private const int PARTICLE_WORKGROUP = 256;
        private const int DEPTH_COPY_WORKGROUP = 16;
        private const int TEXTURE3D_CLEAR_GROUPSIZE = 4;
        private const int MAX_LIGHT_COUNT = 16;
        private const int RANDOM_TEX_SIZE = 64;
        private const int EMITTER_GRADIENT_TEX_WIDTH = 48;
        private const int EMITTER_GRADIENT_ITEM_STRIDE = 2;         //How many rows of texture are used for each emitter
        private const int EMITTER_SPRITE_TEX_SIZE = 64;
        private const float EMITTER_PARTICLE_SIZE_SCALE = .1f;
#endregion

        #region Cached shader properties
        internal class ShaderParam
        {
            public static int AbsorptionColor = Shader.PropertyToID("AbsorptionColor");
            public static int BlackBodyBrightness = Shader.PropertyToID("BlackBodyBrightness");
            public static int BlueNoise = Shader.PropertyToID("BlueNoise");
            public static int Color = Shader.PropertyToID("Color");
            public static int ContainerMaxPoint = Shader.PropertyToID("ContainerMaxPoint");
            public static int ContainerMinPoint = Shader.PropertyToID("ContainerMinPoint");
            public static int ContainerPosition = Shader.PropertyToID("ContainerPosition");
            public static int ContainerScale = Shader.PropertyToID("ContainerScale");
            public static int DeltaT = Shader.PropertyToID("DeltaT");
            public static int Density = Shader.PropertyToID("Density");
            public static int DensityDownscale = Shader.PropertyToID("DensityDownscale");
            public static int DepthDest = Shader.PropertyToID("DepthDest");
            public static int DownscaleFactor = Shader.PropertyToID("DownscaleFactor");
            public static int EyeRayCameraCoeficients = Shader.PropertyToID("EyeRayCameraCoeficients");
            public static int FakeShadows = Shader.PropertyToID("FakeShadows");
            public static int FireBrightness = Shader.PropertyToID("FireBrightness");
            public static int FireColor = Shader.PropertyToID("FireColor");
            public static int FuelDensity = Shader.PropertyToID("FuelDensity");
            public static int GridSize = Shader.PropertyToID("GridSize");
            public static int Illumination = Shader.PropertyToID("Illumination");
            public static int IlluminationOUT = Shader.PropertyToID("IlluminationOUT");
            public static int IlluminationShadows = Shader.PropertyToID("IlluminationShadows");
            public static int IlluminationSoftness = Shader.PropertyToID("IlluminationSoftness");
            public static int LightColor = Shader.PropertyToID("LightColor");
            public static int LightColorArray = Shader.PropertyToID("LightColorArray");
            public static int LightCount = Shader.PropertyToID("LightCount");
            public static int LightDirWorld = Shader.PropertyToID("LightDirWorld");
            public static int LightDirection = Shader.PropertyToID("LightDirection");
            public static int LightGridSize = Shader.PropertyToID("LightGridSize");
            public static int LightPositionArray = Shader.PropertyToID("LightPositionArray");
            public static int Lightmap = Shader.PropertyToID("Lightmap");
            public static int LightmapOUT = Shader.PropertyToID("LightmapOUT");
            public static int MainLightMode = Shader.PropertyToID("MainLightMode");
            public static int OriginalCameraResolution = Shader.PropertyToID("OriginalCameraResolution");
            public static int ParticlesTex = Shader.PropertyToID("ParticlesTex");
            public static int PrimaryShadows = Shader.PropertyToID("PrimaryShadows");
            public static int ReactionSpeed = Shader.PropertyToID("ReactionSpeed");
            public static int RenderedVolume = Shader.PropertyToID("RenderedVolume");
            public static int Resolution = Shader.PropertyToID("Resolution");
            public static int SDFRenderSmoke = Shader.PropertyToID("SDFRenderSmoke");
            public static int ScatteringAttenuation = Shader.PropertyToID("ScatteringAttenuation");
            public static int ScatteringColor = Shader.PropertyToID("ScatteringColor");
            public static int ScatteringContribution = Shader.PropertyToID("ScatteringContribution");
            public static int ShadowColor = Shader.PropertyToID("ShadowColor");
            public static int ShadowDistanceDecay = Shader.PropertyToID("ShadowDistanceDecay");
            public static int ShadowGridSize = Shader.PropertyToID("ShadowGridSize");
            public static int ShadowIntensity = Shader.PropertyToID("ShadowIntensity");
            public static int ShadowMaxSteps = Shader.PropertyToID("ShadowMaxSteps");
            public static int ShadowStepSize = Shader.PropertyToID("ShadowStepSize");
            public static int Shadowmap = Shader.PropertyToID("Shadowmap");
            public static int ShadowmapOUT = Shader.PropertyToID("ShadowmapOUT");
            public static int SimulationMode = Shader.PropertyToID("SimulationMode");
            public static int SmokeDensity = Shader.PropertyToID("SmokeDensity");
            public static int SmokeSDFVisualizationCameraDepth = Shader.PropertyToID("SmokeSDFVisualizationCameraDepth");
            public static int StepScale = Shader.PropertyToID("StepScale");
            public static int StepSize = Shader.PropertyToID("StepSize");
            public static int TempThreshold = Shader.PropertyToID("TempThreshold");
            public static int TemperatureDensityDependence = Shader.PropertyToID("TemperatureDensityDependence");
            public static int Texture3DFloat = Shader.PropertyToID("Texture3DFloat");
            public static int Texture3DFloat2 = Shader.PropertyToID("Texture3DFloat2");
            public static int Texture3DFloat2Dimensions = Shader.PropertyToID("Texture3DFloat2Dimensions");
            public static int Texture3DFloat3 = Shader.PropertyToID("Texture3DFloat3");
            public static int Texture3DFloat3Dimensions = Shader.PropertyToID("Texture3DFloat3Dimensions");
            public static int Texture3DFloat4 = Shader.PropertyToID("Texture3DFloat4");
            public static int Texture3DFloat4Dimensions = Shader.PropertyToID("Texture3DFloat4Dimensions");
            public static int Texture3DFloatDimensions = Shader.PropertyToID("Texture3DFloatDimensions");
            public static int TextureScale = Shader.PropertyToID("TextureScale");
            public static int ViewProjectionInverse = Shader.PropertyToID("ViewProjectionInverse");

            public LocalKeyword SmokeShader_FLIP_NATIVE_TEXTURES;
            public LocalKeyword SmokeShader_FULLSCREEN_QUAD;
            public LocalKeyword SmokeShader_INPUT_2D_ARRAY;

            public LocalKeyword SmokeShadowProjectionShader_FLIP_NATIVE_TEXTURES;
            public LocalKeyword SmokeShadowProjectionShader_INPUT_2D_ARRAY;
            public LocalKeyword SmokeShadowProjectionShader_TRICUBIC;

            public LocalKeyword RenderCompute_INPUT_2D_ARRAY;
        }

        ShaderParam ShaderParamContainer = new ShaderParam();

        #endregion

#region Resources
        private RenderTexture UpscaleColor;
        private RenderTexture Shadowmap;
        private RenderTexture Lightmap;
        private RenderTexture CameraOcclusion;
        private RenderTexture RenderDensity;
        private RenderTexture RenderDensityLOD;
        private RenderTexture RenderColor;
        private RenderTexture RenderIllumination;
        private RenderTexture ColorTexture0;
        private RenderTexture VelocityTexture0;
        private RenderTexture ColorTexture1;
        private RenderTexture VelocityTexture1;
        private RenderTexture TmpSDFTexture;
        private RenderTexture Divergence;
        private RenderTexture ResidualLOD0;
        private RenderTexture Pressure0LOD0;
        private RenderTexture Pressure1LOD0;

#if !UNITY_ANDROID || UNITY_EDITOR
        private RenderTexture ResidualLOD1;
        private RenderTexture ResidualLOD2;
        private RenderTexture Pressure0LOD1;
        private RenderTexture Pressure0LOD2;
        private RenderTexture Pressure1LOD1;
        private RenderTexture Pressure1LOD2;
#endif
        private ComputeBuffer AtomicCounters;
        private ComputeBuffer EffectParticleData0;
        private ComputeBuffer EffectParticleData1;
        private ComputeBuffer EffectParticleEmissionColorData;
        private Texture3D RandomTexture;
        private RenderTexture DepthTexture;
        private RenderTexture ParticlesRT;
        private ComputeBuffer DynamicManipulatorData;
        private ComputeBuffer SDFObjectData;
        private ComputeBuffer ManipulatorStatistics;
        private Texture3D SDFGridTexture;
        private Texture3D EmbeddingsTexture;
        private Texture2D EmittersColorsTexture;
        private Texture2D EmittersColorsStagingTexture;
        private Texture3D EmittersSpriteTexture;

        private int ShadowmapID;
        private int LightmapID;
        private int IlluminationID;
        private int CopyDepthID;
        private int ClearTexture3DFloatID;
        private int ClearTexture3DFloat2ID;
        private int ClearTexture3DFloat3ID;
        private int ClearTexture3DFloat4ID;
        private Vector3Int WorkGroupsXYZ;
        private int MaxEffectParticleWorkgroups;
        private Vector3Int ShadowGridSize;
        private Vector3Int ShadowWorkGroupsXYZ;
        private Vector3Int LightGridSize;
        private Vector3Int LightWorkGroupsXYZ;
        private Vector3Int DownscaleXYZ;
        private Mesh renderQuad;
        private Mesh renderSimulationCube;
        private int CurrentInstanceID;
        private CommandBuffer solverCommandBuffer;

        private bool ForceRepaint = false;
        private bool isSimulationContainerPositionChanged;
        private float timeAccumulation = 0.0f;
        private bool forceTextureUpdate = false;
        private int GridDownscale = 1;
        internal int NumNodes;

        private Vector3Int GridSizeLOD;

        private CameraEvent ActiveInjectionPoint = CameraEvent.BeforeForwardAlpha;
        [SerializeField]
        [FormerlySerializedAs("manipulators")]
        private List<Manipulator> Manipulators = new List<Manipulator>();

        private TexState[] EmittersSpriteTextureCache;

        private ZibraManipulatorManager ManipulatorManagerInternal;
        internal ZibraManipulatorManager ManipulatorManager
        {
            get
            {
                if (ManipulatorManagerInternal == null)
                {
                    ManipulatorManagerInternal = gameObject.GetComponent<ZibraManipulatorManager>();
                    if (ManipulatorManagerInternal == null)
                    {
                        ManipulatorManagerInternal = gameObject.AddComponent<ZibraManipulatorManager>();
#if UNITY_EDITOR
                        UnityEditor.EditorUtility.SetDirty(this);
#endif
                    }
                }
                return ManipulatorManagerInternal;
            }
        }

        [SerializeField]
        [HideInInspector]
        private string _SimulationGUID = Guid.NewGuid().ToString();

        private static int ms_NextInstanceId = 0;

#if UNITY_PIPELINE_URP
        private static int upscaleColorTextureID = Shader.PropertyToID("Zibra_DownscaledSmokeAndFireColor");
        private static int upscaleDepthTextureID = Shader.PropertyToID("Zibra_DownscaledSmokeAndFireDepth");
#endif

#if UNITY_PIPELINE_HDRP
        private SmokeAndFireHDRPRenderComponent HDRPRenderer;
#endif // UNITY_PIPELINE_HDRP

#if ZIBRA_EFFECTS_DEBUG
        // We don't know exact number of DebugTimestampsItems returned from native plugin
        // because several events (like UpdateRenderParams) can be triggered many times
        // per frame. For our current needs 100 should be enough
        [NonSerialized]
        internal SmokeAndFireBridge.DebugTimestampItem[] DebugTimestampsItems =
            new SmokeAndFireBridge.DebugTimestampItem[100];
        [NonSerialized]
        internal uint DebugTimestampsItemsCount = 0;
#endif

#region NATIVE RESOURCES

        private RenderParams cameraRenderParams;
        private SimulationParams simulationParams;
        private IntPtr NativeManipData;
        private IntPtr NativeManipIndices;
        private IntPtr NativeSDFData;
        private IntPtr NativeSimulationData;
        private List<IntPtr> toFreeOnExit = new List<IntPtr>();
        private Vector2Int CurrentTextureResolution = new Vector2Int(0, 0);

        // List of all cameras we have added a command buffer to
        private readonly Dictionary<Camera, CommandBuffer> cameraCBs = new Dictionary<Camera, CommandBuffer>();
        internal Dictionary<Camera, CameraResources> CameraResourcesMap = new Dictionary<Camera, CameraResources>();
        private Dictionary<Camera, IntPtr> camNativeParams = new Dictionary<Camera, IntPtr>();
        private Dictionary<Camera, IntPtr> camMeshRenderParams = new Dictionary<Camera, IntPtr>();
        private Dictionary<Camera, Vector2Int> CamRenderResolutions = new Dictionary<Camera, Vector2Int>();
        private Dictionary<Camera, Vector2Int> camNativeResolutions = new Dictionary<Camera, Vector2Int>();

        // Each camera needs its own resources
        private List<Camera> cameras = new List<Camera>();
#endregion

#endregion

#region Solver

        internal bool IsSimulationEnabled()
        {
            // We need at least 2 simulation frames before we can start rendering
            // So we need to always simulate first 2 frames
            return Initialized && (RunSimulation || (SimulationInternalFrame <= 2));
        }

        internal bool IsRenderingEnabled()
        {
            // We need at least 2 simulation frames before we can start rendering
            return Initialized && RunRendering && (SimulationInternalFrame > 1);
        }

        private void UpdateReadback()
        {
            solverCommandBuffer.Clear();
            ForceCloseCommandEncoder(solverCommandBuffer);

            // This must be called at most ONCE PER FRAME
            // Otherwise you'll get deadlock
            SmokeAndFireBridge.SubmitInstanceEvent(solverCommandBuffer, CurrentInstanceID,
                                                   SmokeAndFireBridge.EventID.UpdateReadback);

            Graphics.ExecuteCommandBuffer(solverCommandBuffer);

            UpdateManipulatorStatistics();
        }

        private void UpdateSimulation()
        {
            if (!Initialized)
                return;

            LastTimestep = Timestep;

            if (RunSimulation)
                StepPhysics();

            Illumination();

#if UNITY_EDITOR
            NotifyChange();
#endif
        }

        private void UpdateInteropBuffers()
        {
            Marshal.StructureToPtr(simulationParams, NativeSimulationData, true);

            if (ManipulatorManager.Elements > 0)
            {
                SetInteropBuffer(NativeManipData, ManipulatorManager.ManipulatorParams);
            }

            if (ManipulatorManager.SDFObjectList.Count > 0)
            {
                SetInteropBuffer(NativeSDFData, ManipulatorManager.SDFObjectList);
            }

            SetInteropBuffer(NativeManipIndices, new List<ZibraManipulatorManager.ManipulatorIndices> { ManipulatorManager.indices });
        }

        private void UpdateSolverParameters()
        {
            // Update solver parameters
            SmokeAndFireBridge.SubmitInstanceEvent(solverCommandBuffer, CurrentInstanceID,
                                                   SmokeAndFireBridge.EventID.UpdateSolverParameters,
                                                   NativeSimulationData);

            if (ManipulatorManager.Elements > 0)
            {
                SmokeAndFireBridge.SubmitInstanceEvent(solverCommandBuffer, CurrentInstanceID,
                                                       SmokeAndFireBridge.EventID.UpdateManipulatorParameters,
                                                       NativeManipData);
            }

            if (ManipulatorManager.SDFObjectList.Count > 0)
            {
                SmokeAndFireBridge.SubmitInstanceEvent(solverCommandBuffer, CurrentInstanceID,
                                                       SmokeAndFireBridge.EventID.UpdateSDFObjects, NativeSDFData);
            }

            SmokeAndFireBridge.SubmitInstanceEvent(solverCommandBuffer, CurrentInstanceID,
                                                       SmokeAndFireBridge.EventID.UpdateManipulatorIndices,
                                                       NativeManipIndices);
        }

        private void StepPhysics()
        {
            solverCommandBuffer.Clear();

            ForceCloseCommandEncoder(solverCommandBuffer);

#if ZIBRA_EFFECTS_OTP_VERSION
            bool isStereo = false;
#else
            bool isStereo = cameras.Count > 0 ? cameras[0].stereoEnabled : false;
#endif
            SetSimulationParameters(isStereo);

            ManipulatorManager.UpdateDynamic(this, LastTimestep);

            UpdateInteropBuffers();
            UpdateSolverParameters();

            // execute simulation
            SmokeAndFireBridge.SubmitInstanceEvent(solverCommandBuffer, CurrentInstanceID,
                                                   SmokeAndFireBridge.EventID.StepPhysics);
            Graphics.ExecuteCommandBuffer(solverCommandBuffer);

            // the actual position of the container
            Vector3 prevPosition = SimulationContainerPosition;
            SimulationContainerPosition = SmokeAndFireBridge.GetSimulationContainerPosition(CurrentInstanceID);
            isSimulationContainerPositionChanged = prevPosition != SimulationContainerPosition;

            // update internal time
            SimulationInternalTime += LastTimestep;
            SimulationInternalFrame++;
        }

        private void UpdateManipulatorStatistics()
        {
            /// ManipulatorStatistics GPUReadback
            if (ManipulatorManager.Elements > 0)
            {
                UInt32 size = (UInt32)ManipulatorManager.Elements * STATISTICS_PER_MANIPULATOR;
                IntPtr readbackData =
                    SmokeAndFireBridge.ZibraSmokeAndFire_GPUReadbackGetData(CurrentInstanceID, size * sizeof(Int32));
                if (readbackData != IntPtr.Zero)
                {
                    Int32[] Stats = new Int32[size];
                    Marshal.Copy(readbackData, Stats, 0, (Int32)size);
                    ManipulatorManager.UpdateStatistics(Stats, Manipulators, SolverParameters, MaterialParameters);
                }
            }
        }

        private void SetSimulationParameters(bool isStereo = false)
        {
            simulationParams.GridSize = GridSize;
            simulationParams.NodeCount = NumNodes;

            simulationParams.ContainerScale = ContainerSize;
            simulationParams.MinimumVelocity = SolverParameters.MinimumVelocity;

            simulationParams.ContainerPos = transform.position;
            simulationParams.MaximumVelocity = SolverParameters.MaximumVelocity;

            simulationParams.TimeStep = LastTimestep;
            simulationParams.SimulationTime = SimulationInternalTime;
            simulationParams.SimulationFrame = SimulationInternalFrame;
            simulationParams.Sharpen = SolverParameters.Sharpen;
            simulationParams.SharpenThreshold = SolverParameters.SharpenThreshold;

            simulationParams.JacobiIterations = SolverParameters.PressureSolveIterations;
            simulationParams.ColorDecay = SolverParameters.ColorDecay;
            simulationParams.VelocityDecay = SolverParameters.VelocityDecay;
            simulationParams.PressureReuse = SolverParameters.PressureReuse;
            simulationParams.PressureReuseClamp = SolverParameters.PressureReuseClamp;
            simulationParams.PressureProjection = SolverParameters.PressureProjection;
            simulationParams.PressureClamp = SolverParameters.PressureClamp;

            simulationParams.Gravity = SolverParameters.Gravity;
            simulationParams.SmokeBuoyancy = SolverParameters.SmokeBuoyancy;

            simulationParams.LOD0Iterations = SolverParameters.LOD0Iterations;
            simulationParams.LOD1Iterations = SolverParameters.LOD1Iterations;
            simulationParams.LOD2Iterations = SolverParameters.LOD2Iterations;
            simulationParams.PreIterations = SolverParameters.PreIterations;

            simulationParams.MainOverrelax = SolverParameters.MainOverrelax;
            simulationParams.EdgeOverrelax = SolverParameters.EdgeOverrelax;
            simulationParams.VolumeEdgeFadeoff = MaterialParameters.VolumeEdgeFadeoff;
            simulationParams.SimulationIterations = SimulationIterations;

            simulationParams.SimulationMode = (int)ActiveSimulationMode;
            simulationParams.FixVolumeWorldPosition = FixVolumeWorldPosition ? 1 : 0;

            simulationParams.FuelDensity = MaterialParameters.FuelDensity * MaterialParameters.FadeCoefficient;
            simulationParams.SmokeDensity = MaterialParameters.SmokeDensity * MaterialParameters.FadeCoefficient;
            simulationParams.TemperatureDensityDependence = MaterialParameters.TemperatureDensityDependence;
            simulationParams.FireBrightness =
                MaterialParameters.FireBrightness + MaterialParameters.BlackBodyBrightness;

            simulationParams.TempThreshold = SolverParameters.TempThreshold;
            simulationParams.HeatEmission = SolverParameters.HeatEmission;
            simulationParams.ReactionSpeed = SolverParameters.ReactionSpeed;
            simulationParams.HeatBuoyancy = SolverParameters.HeatBuoyancy;

            simulationParams.MaxEffectParticleCount = MaterialParameters.MaxEffectParticles;
            simulationParams.ParticleLifetime = MaterialParameters.ParticleLifetime;
            simulationParams.SimulateEffectParticles = (!isStereo && (ManipulatorManager.indices.EffectParticleEmitterEnd - ManipulatorManager.indices.EffectParticleEmitterBegin > 0)) ? 1 : 0;
            simulationParams.RenderEffectParticles = (!isStereo && (ManipulatorManager.indices.EffectParticleEmitterEnd - ManipulatorManager.indices.EffectParticleEmitterBegin > 0)) ? 1 : 0;

            simulationParams.GridSizeLOD = GridSizeLOD;
            simulationParams.GridDownscale = GridDownscale;
        }

        private void RefreshEmitterColorsTexture()
        {
            var emitters = Manipulators.FindAll(manip => manip is ZibraParticleEmitter);

            var textureFormat = GraphicsFormat.R8G8B8A8_UNorm;
            var textureFlags = TextureCreationFlags.None;

            emitters.Sort(new ManipulatorCompare());

            if (EmittersSpriteTexture == null)
            {
                int[] dimensions = new int[] { 1, 1, Mathf.Max(1, emitters.Count) };
                if (emitters.Find(emitter => (emitter as ZibraParticleEmitter).RenderMode ==
                                             ZibraParticleEmitter.RenderingMode.Sprite))
                {
                    dimensions[0] = dimensions[1] = EMITTER_SPRITE_TEX_SIZE;
                }
                EmittersSpriteTexture =
                    new Texture3D(dimensions[0], dimensions[1], dimensions[2], textureFormat, textureFlags);
                EmittersSpriteTextureCache = new TexState[emitters.Count];
            }

            float inv = 1f / (EMITTER_GRADIENT_TEX_WIDTH - 1);
            for (int y = 0; y < emitters.Count; y++)
            {
                var curEmitter = emitters[y] as ZibraParticleEmitter;
                for (int x = 0; x < EMITTER_GRADIENT_TEX_WIDTH; x++)
                {
                    var t = x * inv;
                    Color data1 = curEmitter.ParticleColor.Evaluate(t);
                    // Can't do SetPixel/Apply on texture passed to native plugin
                    // That will invalidate texture on Metal and crash!
                    EmittersColorsStagingTexture.SetPixel(x, y * EMITTER_GRADIENT_ITEM_STRIDE, data1);

                    Color data2 = Color.black;
                    data2.a = curEmitter.ParticleSize.Evaluate(t) * EMITTER_PARTICLE_SIZE_SCALE;
                    if (curEmitter.EmissionEnabled)
                    {
                        data2.r = curEmitter.SmokeDensity.Evaluate(t);
                        data2.g = curEmitter.Temperature.Evaluate(t);
                        data2.b = curEmitter.Fuel.Evaluate(t);
                    }
                    EmittersColorsStagingTexture.SetPixel(x, y * EMITTER_GRADIENT_ITEM_STRIDE + 1, data2);
                }

                if (curEmitter.RenderMode == ZibraParticleEmitter.RenderingMode.Sprite
                    && EmittersSpriteTextureCache[y].IsDirty(curEmitter.ParticleSprite))
                {
                    RenderTexture rt =
                        new RenderTexture(EMITTER_SPRITE_TEX_SIZE, EMITTER_SPRITE_TEX_SIZE, 0, textureFormat);
                    Graphics.Blit(curEmitter.ParticleSprite, rt);
                    int slice = y;
                    Graphics.CopyTexture(rt, 0, EmittersSpriteTexture, slice);
                    EmittersSpriteTextureCache[y].Update(curEmitter.ParticleSprite);
                }
            }
            EmittersColorsStagingTexture.Apply();
            Graphics.CopyTexture(EmittersColorsStagingTexture, EmittersColorsTexture);
        }

#if ZIBRA_EFFECTS_DEBUG
        private void UpdateDebugTimestamps()
        {
            if (!IsSimulationEnabled())
            {
                return;
            }
            DebugTimestampsItemsCount =
                SmokeAndFireBridge.ZibraSmokeAndFire_GetDebugTimestamps(CurrentInstanceID, DebugTimestampsItems);
        }
#endif
#endregion

#region Render functions
        private void InitializeNativeCameraParams(Camera cam)
        {
            if (!camNativeParams.ContainsKey(cam))
            {
                // allocate memory for camera parameters
                camNativeParams[cam] = Marshal.AllocHGlobal(Marshal.SizeOf(cameraRenderParams));
            }
        }

        private void SetMaterialParams(Material material, Vector2 resolution)
        {
            material.SetFloat(ShaderParam.SmokeDensity, MaterialParameters.SmokeDensity * MaterialParameters.FadeCoefficient);
            material.SetFloat(ShaderParam.FuelDensity, MaterialParameters.FuelDensity * MaterialParameters.FadeCoefficient);

            material.SetVector(ShaderParam.ShadowColor, MaterialParameters.ShadowAbsorptionColor);
            material.SetVector(ShaderParam.AbsorptionColor, MaterialParameters.AbsorptionColor);
            material.SetVector(ShaderParam.ScatteringColor, MaterialParameters.ScatteringColor);
            material.SetFloat(ShaderParam.ScatteringAttenuation, MaterialParameters.ScatteringAttenuation);
            material.SetFloat(ShaderParam.ScatteringContribution, MaterialParameters.ScatteringContribution);
            material.SetFloat(ShaderParam.FakeShadows, MaterialParameters.ObjectShadowIntensity);
            material.SetFloat(ShaderParam.ShadowDistanceDecay, MaterialParameters.ShadowDistanceDecay);
            material.SetFloat(ShaderParam.ShadowIntensity, MaterialParameters.ShadowIntensity);
            material.SetFloat(ShaderParam.StepSize, MaterialParameters.RayMarchingStepSize);

            material.SetInt(ShaderParam.PrimaryShadows, (MaterialParameters.ObjectPrimaryShadows && MainLight.enabled) ? 1 : 0);
            material.SetInt(ShaderParam.IlluminationShadows, MaterialParameters.ObjectIlluminationShadows ? 1 : 0);

            material.SetVector(ShaderParam.ContainerScale, ContainerSize);
            material.SetVector(ShaderParam.ContainerPosition, SimulationContainerPosition);
            material.SetVector(ShaderParam.GridSize, (Vector3)GridSize);
            material.SetVector(ShaderParam.ShadowGridSize, (Vector3)ShadowGridSize);
            material.SetVector(ShaderParam.LightGridSize, (Vector3)LightGridSize);

            if (MainLight == null)
            {
                Debug.LogError("No main light source set in the Zibra Flames instance.");
            }
            else
            {
                material.SetVector(ShaderParam.LightColor, GetLightColor(MainLight));
                material.SetVector(ShaderParam.LightDirWorld, MainLight.transform.rotation * new Vector3(0, 0, -1));
            }

            material.SetTexture(ShaderParam.ParticlesTex, ParticlesRT);
            material.SetTexture(ShaderParam.BlueNoise, MaterialParameters.BlueNoise);
            material.SetTexture(ShaderParam.Color, RenderColor);
            material.SetTexture(ShaderParam.Illumination, RenderIllumination);
            material.SetTexture(ShaderParam.Density, RenderDensity);
            material.SetInt(ShaderParam.DensityDownscale, 1);

            material.SetTexture(ShaderParam.Shadowmap, Shadowmap);
            material.SetTexture(ShaderParam.Lightmap, Lightmap);

            int mainLightMode = MainLight.enabled ? 1 : 0;
            Vector4[] lightColors = new Vector4[MAX_LIGHT_COUNT];
            Vector4[] lightPositions = new Vector4[MAX_LIGHT_COUNT];
            int lightCount = GetLights(ref lightColors, ref lightPositions);

            material.SetVectorArray(ShaderParam.LightColorArray, lightColors);
            material.SetVectorArray(ShaderParam.LightPositionArray, lightPositions);
            material.SetInt(ShaderParam.LightCount, lightCount);
            material.SetInt(ShaderParam.MainLightMode, mainLightMode);
            material.SetVector(ShaderParam.Resolution, resolution);

            float simulationScale = (1.0f / 3.0f) * (ContainerSize.x + ContainerSize.y + ContainerSize.z);
            float cellSize = ContainerSize.x / GridSize.x;
            float dt = MaterialParameters.RayMarchingStepSize * cellSize;
            float stepScale = dt / simulationScale;

            material.SetFloat(ShaderParam.DeltaT, dt);
            material.SetFloat(ShaderParam.StepScale, stepScale);
            material.SetVector(ShaderParam.ContainerMinPoint, new Vector3(SimulationContainerPosition.x - ContainerSize.x * 0.5f, 
                SimulationContainerPosition.y - ContainerSize.y * 0.5f, SimulationContainerPosition.z - ContainerSize.z * 0.5f));
            material.SetVector(ShaderParam.ContainerMaxPoint, new Vector3(SimulationContainerPosition.x + ContainerSize.x * 0.5f,
                SimulationContainerPosition.y + ContainerSize.y * 0.5f, SimulationContainerPosition.z + ContainerSize.z * 0.5f));
        }

        private bool SetMaterialParams(Camera cam)
        {
            CameraResources camRes = CameraResourcesMap[cam];

            Material usedUpscaleMaterial = EnableDownscale ? MaterialParameters.UpscaleMaterial : null;

            bool isDirty = camRes.UpscaleMaterial.SetMaterial(usedUpscaleMaterial);

            Material CurrentSharedMaterial = MaterialParameters.SmokeMaterial;

            bool isSmokeMaterialDirty = camRes.SmokeAndFireMaterial.SetMaterial(CurrentSharedMaterial);
            Material CurrentMaterial = camRes.SmokeAndFireMaterial.CurrentMaterial;

            if (isSmokeMaterialDirty)
            {
                ShaderParamContainer.SmokeShader_FLIP_NATIVE_TEXTURES = new LocalKeyword(CurrentMaterial.shader, "FLIP_NATIVE_TEXTURES");
                ShaderParamContainer.SmokeShader_FULLSCREEN_QUAD = new LocalKeyword(CurrentMaterial.shader, "FULLSCREEN_QUAD");
                ShaderParamContainer.SmokeShader_INPUT_2D_ARRAY = new LocalKeyword(CurrentMaterial.shader, "INPUT_2D_ARRAY");

#if UNITY_PIPELINE_HDRP
                if (RenderPipelineDetector.GetRenderPipelineType() == RenderPipelineDetector.RenderPipeline.HDRP)
                {
                    CurrentMaterial.EnableKeyword("HDRP");
                }
#endif
            }

            isDirty = isSmokeMaterialDirty || isDirty;


            SetMaterialParams(CurrentMaterial, new Vector2(cam.pixelWidth, cam.pixelHeight));

            Material CurrenSharedShadowProjectionMaterial = MaterialParameters.ShadowProjectionMaterial;
            bool isSmokeShadowMaterialDirty = camRes.SmokeShadowProjectionMaterial.SetMaterial(CurrenSharedShadowProjectionMaterial);
            Material CurrentShadowProjectionMaterial = camRes.SmokeShadowProjectionMaterial.CurrentMaterial;

            if (isSmokeShadowMaterialDirty)
            {
                ShaderParamContainer.SmokeShadowProjectionShader_FLIP_NATIVE_TEXTURES = new LocalKeyword(CurrentShadowProjectionMaterial.shader, "FLIP_NATIVE_TEXTURES");
                ShaderParamContainer.SmokeShadowProjectionShader_INPUT_2D_ARRAY = new LocalKeyword(CurrentShadowProjectionMaterial.shader, "INPUT_2D_ARRAY");
                ShaderParamContainer.SmokeShadowProjectionShader_TRICUBIC = new LocalKeyword(CurrentShadowProjectionMaterial.shader, "TRICUBIC");

#if UNITY_PIPELINE_HDRP
                if (RenderPipelineDetector.GetRenderPipelineType() == RenderPipelineDetector.RenderPipeline.HDRP)
                {
                    CurrentShadowProjectionMaterial.EnableKeyword("HDRP");
                }
#endif
            }

            isDirty = isSmokeShadowMaterialDirty || isDirty;

            SetMaterialParams(CurrentShadowProjectionMaterial, new Vector2(cam.pixelWidth, cam.pixelHeight));
            CurrentShadowProjectionMaterial.SetKeyword(ShaderParamContainer.SmokeShadowProjectionShader_TRICUBIC, 
                MaterialParameters.ShadowProjectionQualityLevel == ZibraSmokeAndFireMaterialParameters.ShadowProjectionQuality.Tricubic);

#if UNITY_IOS && !UNITY_EDITOR
            CurrentMaterial.SetKeyword(ShaderParamContainer.SmokeShader_FLIP_NATIVE_TEXTURES, !EnableDownscale);
            CurrentShadowProjectionMaterial.EnableKeyword(ShaderParamContainer.SmokeShadowProjectionShader_FLIP_NATIVE_TEXTURES);
#endif
#if UNITY_ANDROID && !UNITY_EDITOR
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan)
            {
                CurrentMaterial.SetKeyword(ShaderParamContainer.SmokeShader_FLIP_NATIVE_TEXTURES, !EnableDownscale && !cam.stereoEnabled);
                CurrentShadowProjectionMaterial.EnableKeyword(ShaderParamContainer.SmokeShadowProjectionShader_FLIP_NATIVE_TEXTURES);
            }
#endif
            Material usedSDFRenderMaterial = VisualizeSceneSDF ? MaterialParameters.SDFRenderMaterial : null;
            bool isSDFRenderMaterialDirty = camRes.SDFRenderMaterial.SetMaterial(usedSDFRenderMaterial);
            isDirty = isDirty || isSDFRenderMaterialDirty;

            if (VisualizeSceneSDF)
            {
                Material CurrentSDFRenderMaterial = camRes.SDFRenderMaterial.CurrentMaterial;
                CurrentSDFRenderMaterial.SetTexture(ShaderParam.SDFRenderSmoke, VisualizeSDFTarget);

#if UNITY_PIPELINE_HDRP
                if (RenderPipelineDetector.GetRenderPipelineType() == RenderPipelineDetector.RenderPipeline.HDRP)
                {
                    if (isSDFRenderMaterialDirty)
                    {
                        CurrentSDFRenderMaterial.EnableKeyword("HDRP");
                    }
                    CurrentSDFRenderMaterial.SetVector(ShaderParam.LightColor, MainLight.color *
                                                                         Mathf.Log(MainLight.intensity) / 8.0f);
                    CurrentSDFRenderMaterial.SetVector(ShaderParam.LightDirection,
                                                       MainLight.transform.rotation * new Vector3(0, 0, -1));
                }
#endif
            }

            return isDirty;
        }

        internal Vector2Int ApplyDownscaleFactor(Vector2Int val)
        {
            if (!EnableDownscale)
                return val;
            return new Vector2Int((int)(val.x * DownscaleFactor), (int)(val.y * DownscaleFactor));
        }

        private Vector2Int ApplyRenderPipelineRenderScale(Vector2Int val, float renderPipelineRenderScale)
        {
            return new Vector2Int((int)(val.x * renderPipelineRenderScale), (int)(val.y * renderPipelineRenderScale));
        }

        private RenderTexture CreateTexture(RenderTexture texture, Vector2Int resolution, bool applyDownscaleFactor,
                                            FilterMode filterMode, int depth, RenderTextureFormat format,
                                            bool enableRandomWrite, ref bool hasBeenUpdated)
        {
            if (texture == null || texture.width != resolution.x || texture.height != resolution.y ||
                forceTextureUpdate)
            {
                ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(texture);

                var newTexture = new RenderTexture(resolution.x, resolution.y, depth, format);
                newTexture.enableRandomWrite = enableRandomWrite;
                newTexture.filterMode = filterMode;
                newTexture.Create();
                hasBeenUpdated = true;
                return newTexture;
            }

            return texture;
        }
        
        private RenderTexture CreateStereoTexture(RenderTexture texture, Vector2Int resolution, bool applyDownscaleFactor,
                                                  FilterMode filterMode, int depth, RenderTextureFormat format,
                                                  bool enableRandomWrite, ref bool hasBeenUpdated)
        {
            if (texture == null || texture.width != resolution.x || texture.height != resolution.y ||
                forceTextureUpdate)
            {
                ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(texture);

                var newTexture = new RenderTexture(resolution.x, resolution.y, depth, format);
                newTexture.dimension = TextureDimension.Tex2DArray;
                newTexture.volumeDepth = 2;
                newTexture.enableRandomWrite = enableRandomWrite;
                newTexture.filterMode = filterMode;
                newTexture.Create();
                hasBeenUpdated = true;
                return newTexture;
            }

            return texture;
        }

        private void UpdateCameraResolution(Camera cam, float renderPipelineRenderScale)
        {
            Vector2Int cameraResolution = XRSettings.eyeTextureWidth != 0
                                            ? new Vector2Int(XRSettings.eyeTextureWidth, XRSettings.eyeTextureHeight)
                                            : new Vector2Int(cam.pixelWidth, cam.pixelHeight);
            if (!XRSettings.enabled)
                cameraResolution = ApplyRenderPipelineRenderScale(cameraResolution, renderPipelineRenderScale);
            camNativeResolutions[cam] = cameraResolution;
            Vector2Int cameraResolutionDownscaled = ApplyDownscaleFactor(cameraResolution);
            CamRenderResolutions[cam] = cameraResolutionDownscaled;
        }

        internal void RenderSmokeAndFireMain(CommandBuffer cmdBuffer, Camera cam, Rect? viewport = null)
        {
            RenderSmokeAndFire(cmdBuffer, cam, viewport);
        }

        internal void UpscaleSmokeAndFireDirect(CommandBuffer cmdBuffer, Camera cam,
                                                RenderTargetIdentifier? sourceColorTexture = null,
                                                RenderTargetIdentifier? sourceDepthTexture = null,
                                                Rect? viewport = null)
        {
            Material CurrentUpscaleMaterial = CameraResourcesMap[cam].UpscaleMaterial.CurrentMaterial;
            Vector2Int cameraNativeResolution = camNativeResolutions[cam];

            cmdBuffer.SetViewport(new Rect(0, 0, cameraNativeResolution.x, cameraNativeResolution.y));
            cmdBuffer.SetGlobalTexture(ShaderParam.RenderedVolume, sourceColorTexture ?? UpscaleColor);
            cmdBuffer.DrawProcedural(transform.localToWorldMatrix, CurrentUpscaleMaterial, 0, MeshTopology.Triangles, 6);
        }

        internal void RenderSDFVisualization(CommandBuffer cmdBuffer, Camera cam, Rect? viewport = null)
        {
            Vector2Int cameraRenderResolution = CamRenderResolutions[cam];

            Material CurrentMaterial = CameraResourcesMap[cam].SDFRenderMaterial.CurrentMaterial;

            cmdBuffer.DrawProcedural(transform.localToWorldMatrix, CurrentMaterial, 0, MeshTopology.Triangles, 6);
        }

        private void UpdateCamera(Camera cam)
        {
            Vector2Int resolution = CamRenderResolutions[cam];

            Material CurrentMaterial = CameraResourcesMap[cam].SmokeAndFireMaterial.CurrentMaterial;
            Material CurrentUpscaleMaterial = CameraResourcesMap[cam].UpscaleMaterial.CurrentMaterial;
            Material CurrentShadowProjectionMaterial =
                CameraResourcesMap[cam].SmokeShadowProjectionMaterial.CurrentMaterial;
            Material CurrentSDFRenderMaterial = CameraResourcesMap[cam].SDFRenderMaterial.CurrentMaterial;

            Matrix4x4 Projection = GL.GetGPUProjectionMatrix(cam.projectionMatrix, true);
            Matrix4x4 ProjectionInverse = Projection.inverse;
            Matrix4x4 View = cam.worldToCameraMatrix;
            Matrix4x4 ViewProjection = Projection * View;
            Matrix4x4 ViewProjectionInverse = ViewProjection.inverse;

            cameraRenderParams.View = cam.worldToCameraMatrix;
            cameraRenderParams.Projection = Projection;
            cameraRenderParams.ProjectionInverse = ProjectionInverse;
            cameraRenderParams.ViewProjection = ViewProjection;
            cameraRenderParams.ViewProjectionInverse = ViewProjectionInverse;
            cameraRenderParams.WorldSpaceCameraPos = cam.transform.position;
            cameraRenderParams.CameraResolution = new Vector2(resolution.x, resolution.y);
            cameraRenderParams.CameraDownscaleFactor = EnableDownscale ? DownscaleFactor : 1f;
            { // Same as Unity's built-in _ZBufferParams
                float y = cam.farClipPlane / cam.nearClipPlane;
                float x = 1 - y;
                cameraRenderParams.ZBufferParams = new Vector4(x, y, x / cam.farClipPlane, y / cam.farClipPlane);
            }
            cameraRenderParams.CameraID = cameras.IndexOf(cam);

            Vector2 textureScale = new Vector2(resolution.x, resolution.y) / GetRequiredTextureResolution();

            CurrentMaterial.SetVector(ShaderParam.Resolution, cameraRenderParams.CameraResolution);
            CurrentMaterial.SetFloat(ShaderParam.DownscaleFactor, cameraRenderParams.CameraDownscaleFactor);
            CurrentMaterial.SetVector(ShaderParam.TextureScale, textureScale);

            MaterialParameters.RendererCompute.SetVector(ShaderParam.OriginalCameraResolution, new Vector2(cam.pixelWidth, cam.pixelHeight));
            CurrentShadowProjectionMaterial.SetMatrix(ShaderParam.ViewProjectionInverse,
                                                      cameraRenderParams.ViewProjectionInverse);

            MaterialParameters.RendererCompute.SetVector(ShaderParam.Resolution, cameraRenderParams.CameraResolution);
            MaterialParameters.RendererCompute.SetMatrix(ShaderParam.ViewProjectionInverse, cameraRenderParams.ViewProjectionInverse);

            // update the data at the pointer
            Marshal.StructureToPtr(cameraRenderParams, camNativeParams[cam], true);

            if (EnableDownscale)
            {
                CurrentUpscaleMaterial.SetVector(ShaderParam.TextureScale, textureScale);
                CurrentUpscaleMaterial.SetMatrix(ShaderParam.ViewProjectionInverse, cameraRenderParams.ViewProjectionInverse);
            }

            float nearClip = cam.nearClipPlane * 3.0f;
            bool IsCameraInside = new Bounds(transform.position, ContainerSize + new Vector3(nearClip, nearClip, nearClip)).Contains(cam.transform.position);

            CurrentMaterial.SetKeyword(ShaderParamContainer.SmokeShader_FULLSCREEN_QUAD, IsCameraInside);

            if (VisualizeSceneSDF)
            {
                CurrentSDFRenderMaterial.SetVector(ShaderParam.TextureScale, textureScale);
                CurrentSDFRenderMaterial.SetMatrix(ShaderParam.EyeRayCameraCoeficients,
                                                   CalculateEyeRayCameraCoeficients(cam));
            }
        }

        private void DisableForCamera(Camera cam)
        {
            cam.RemoveCommandBuffer(ActiveInjectionPoint, cameraCBs[cam]);
            cameraCBs[cam].Dispose();
            cameraCBs.Remove(cam);
        }
#endregion

#region Render
        private void UpdateNativeRenderParams(CommandBuffer cmdBuffer, Camera cam)
        {
            SmokeAndFireBridge.SubmitInstanceEvent(
                cmdBuffer, CurrentInstanceID, SmokeAndFireBridge.EventID.SetRenderParameters, camNativeParams[cam]);
        }

        /// <summary>
        ///     Rendering callback which is called by every camera in the scene
        /// </summary>
        internal void RenderCallBack(Camera cam, float renderPipelineRenderScale = 1.0f)
        {
            if (cam.cameraType == CameraType.Preview || cam.cameraType == CameraType.Reflection ||
                cam.cameraType == CameraType.VR)
            {
                ClearCameraCommandBuffers();
                return;
            }
#if ZIBRA_EFFECTS_OTP_VERSION
            if (cam.stereoEnabled)
            {
                Debug.LogError("Stereo Rendering is not supported in the OTP version");
                enabled = false;
                return;
            }
#endif

            UpdateCameraResolution(cam, renderPipelineRenderScale);

            if (!CameraResourcesMap.ContainsKey(cam))
            {
                CameraResourcesMap[cam] = new CameraResources();
            }

            // Re-add command buffers to cameras with new injection points
            if (CurrentInjectionPoint != ActiveInjectionPoint)
            {
                foreach (KeyValuePair<Camera, CommandBuffer> entry in cameraCBs)
                {
                    entry.Key.RemoveCommandBuffer(ActiveInjectionPoint, entry.Value);
                    entry.Key.AddCommandBuffer(CurrentInjectionPoint, entry.Value);
                }
                ActiveInjectionPoint = CurrentInjectionPoint;
            }

            bool visibleInCamera =
                (RenderPipelineDetector.GetRenderPipelineType() != RenderPipelineDetector.RenderPipeline.BuiltInRP) ||
                ((cam.cullingMask & (1 << this.gameObject.layer)) != 0);

            if (!IsRenderingEnabled() || !visibleInCamera || MaterialParameters.SmokeMaterial == null ||
                MaterialParameters.ShadowProjectionMaterial == null ||
                (EnableDownscale && MaterialParameters.UpscaleMaterial == null))
            {
                if (cameraCBs.ContainsKey(cam))
                {
                    cam.RemoveCommandBuffer(ActiveInjectionPoint, cameraCBs[cam]);
                    cameraCBs[cam].Clear();
                    cameraCBs.Remove(cam);
                }

                return;
            }

            bool isDirty = SetMaterialParams(cam);
            isDirty = UpdateNativeTextures(cam, renderPipelineRenderScale) || isDirty;
            isDirty = !cameraCBs.ContainsKey(cam) || isDirty;
#if UNITY_EDITOR
            isDirty = isDirty || ForceRepaint;
#endif

            isDirty = isDirty || isSimulationContainerPositionChanged;
            isDirty = true;
            InitializeNativeCameraParams(cam);
            UpdateCamera(cam);

            if (RenderPipelineDetector.GetRenderPipelineType() == RenderPipelineDetector.RenderPipeline.BuiltInRP)
            {
                if (!cameraCBs.ContainsKey(cam) || isDirty)
                {
                    CommandBuffer renderCommandBuffer;
                    if (isDirty && cameraCBs.ContainsKey(cam))
                    {
                        renderCommandBuffer = cameraCBs[cam];
                        renderCommandBuffer.Clear();
                    }
                    else
                    {
                        // Create render command buffer
                        renderCommandBuffer = new CommandBuffer { name = "ZibraSmokeAndFire.Render" };
                        // add command buffer to camera
                        cam.AddCommandBuffer(ActiveInjectionPoint, renderCommandBuffer);
                        // add camera to the list
                        cameraCBs[cam] = renderCommandBuffer;
                    }

                    // enable depth texture
                    cam.depthTextureMode = DepthTextureMode.Depth;

                    // update native camera parameters
                    RenderParticlesNative(renderCommandBuffer, cam);
                    RenderSDFNative(renderCommandBuffer);
                    RenderFluid(renderCommandBuffer, cam);
                }
            }
        }

        internal void RenderSDFNative(CommandBuffer cmdBuffer)
        {
            if (VisualizeSceneSDF)
            {
                SmokeAndFireBridge.SubmitInstanceEvent(cmdBuffer, CurrentInstanceID, SmokeAndFireBridge.EventID.RenderSDF);
            }
        }

        private void RenderCallBackWrapper(Camera cam)
        {
            RenderCallBack(cam);
        }

        /// <summary>
        /// Render the simulation volume
        /// </summary>
        /// <param name="cmdBuffer">Command Buffer to add the rendering commands to</param>
        /// <param name="cam">Camera</param>
        internal void RenderFluid(CommandBuffer cmdBuffer, Camera cam, RenderTargetIdentifier? renderTargetParam = null,
                                  RenderTargetIdentifier? depthTargetParam = null, Rect? viewport = null)
        {

#if ZIBRA_EFFECTS_OTP_VERSION
            if (cam.stereoEnabled)
            {
                Debug.LogError("Stereo Rendering is not supported in the OTP version");
                enabled = false;
                return;
            }
#endif

            RenderTargetIdentifier renderTarget =
                renderTargetParam ??
                new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget, 0, CubemapFace.Unknown,
                                           RenderTargetIdentifier.AllDepthSlices);

            // Render fluid to temporary RenderTexture if downscale enabled
            // Otherwise render straight to final RenderTexture
            if (EnableDownscale)
            {
                cmdBuffer.SetRenderTarget(UpscaleColor, 0, CubemapFace.Unknown, -1);
                cmdBuffer.ClearRenderTarget(true, true, Color.clear);

                RenderSmokeAndFireMain(cmdBuffer, cam, viewport);

                if (VisualizeSceneSDF)
                {
                    RenderSDFVisualization(cmdBuffer, cam, viewport);
                }
            }

            if (depthTargetParam != null)
            {
                RenderTargetIdentifier depthTarget = depthTargetParam.Value;
                cmdBuffer.SetRenderTarget(renderTarget, depthTarget, 0, CubemapFace.Unknown,
                                          RenderTargetIdentifier.AllDepthSlices);
            }
            else
            {
                cmdBuffer.SetRenderTarget(renderTarget);
            }

            RenderSmokeShadows(cmdBuffer, cam, viewport); // smoke shadows should not be affected by downscale

            if (EnableDownscale)
            {
                UpscaleSmokeAndFireDirect(cmdBuffer, cam, null, null, viewport);
            }
            else
            {
                RenderSmokeAndFireMain(cmdBuffer, cam, viewport);

                if (VisualizeSceneSDF)
                {
                    RenderSDFVisualization(cmdBuffer, cam, viewport);
                }
            }
        }

        internal void RenderParticlesNative(CommandBuffer cmdBuffer, Camera cam, bool isTextureArray = false)
        {
            if(cam.stereoEnabled)
            {
                return;
            }
            ForceCloseCommandEncoder(cmdBuffer);

            MaterialParameters.RendererCompute.SetKeyword(ShaderParamContainer.RenderCompute_INPUT_2D_ARRAY, isTextureArray);

            cmdBuffer.SetComputeTextureParam(MaterialParameters.RendererCompute, CopyDepthID, ShaderParam.DepthDest, DepthTexture);
            cmdBuffer.DispatchCompute(MaterialParameters.RendererCompute, CopyDepthID, IntDivCeil(cam.pixelWidth, DEPTH_COPY_WORKGROUP),
                                      IntDivCeil(cam.pixelHeight, DEPTH_COPY_WORKGROUP), 1);

            UpdateNativeRenderParams(cmdBuffer, cam);
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan)
            {
                cmdBuffer.SetRenderTarget(ParticlesRT, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                cmdBuffer.ClearRenderTarget(true, true, Color.clear);
            }
            SmokeAndFireBridge.SubmitInstanceEvent(cmdBuffer, CurrentInstanceID, SmokeAndFireBridge.EventID.Draw);
        }

        /// <summary>
        /// Render the simulation volume
        /// </summary>
        /// <param name="cmdBuffer">Command Buffer to add the rendering commands to</param>
        /// <param name="cam">Camera</param>
        private void RenderSmokeAndFire(CommandBuffer cmdBuffer, Camera cam, Rect? viewport = null)
        {
            Vector2Int cameraRenderResolution = CamRenderResolutions[cam];

            Material CurrentMaterial = CameraResourcesMap[cam].SmokeAndFireMaterial.CurrentMaterial;

            // Render fluid to temporary RenderTexture if downscale enabled
            // Otherwise render straight to final RenderTexture
            if (EnableDownscale)
            {
                cmdBuffer.SetViewport(new Rect(0, 0, cameraRenderResolution.x, cameraRenderResolution.y));
            }
            else
            {
                if (viewport != null)
                {
                    cmdBuffer.SetViewport(viewport.Value);
                }
            }

            cmdBuffer.DrawMesh(renderSimulationCube, transform.localToWorldMatrix * Matrix4x4.Scale(ContainerSize), CurrentMaterial, 0, 0);
        }

        /// <summary>
        /// Project the smoke shadows
        /// </summary>
        /// <param name="cmdBuffer">Command Buffer to add the rendering commands to</param>
        /// <param name="cam">Camera</param>
        internal void RenderSmokeShadows(CommandBuffer cmdBuffer, Camera cam, Rect? viewport = null)
        {
            if (!MaterialParameters.EnableProjectedShadows || (!MaterialParameters.ObjectPrimaryShadows && !MaterialParameters.ObjectIlluminationShadows))
            {
                return;
            }

            Vector2Int cameraRenderResolution = CamRenderResolutions[cam];

            if (viewport != null)
            {
                cmdBuffer.SetViewport(viewport.Value);
            }

            Material CurrentMaterial = CameraResourcesMap[cam].SmokeShadowProjectionMaterial.CurrentMaterial;

            cmdBuffer.DrawMesh(renderQuad, Matrix4x4.identity, CurrentMaterial, 0, 0);
        }

        private void Illumination()
        {
            solverCommandBuffer.Clear();

            solverCommandBuffer.SetComputeVectorParam(MaterialParameters.RendererCompute, ShaderParam.ContainerScale, ContainerSize);
            solverCommandBuffer.SetComputeVectorParam(MaterialParameters.RendererCompute, ShaderParam.ContainerPosition, SimulationContainerPosition);
            solverCommandBuffer.SetComputeVectorParam(MaterialParameters.RendererCompute, ShaderParam.GridSize, (Vector3)GridSize);
            solverCommandBuffer.SetComputeVectorParam(MaterialParameters.RendererCompute, ShaderParam.ShadowGridSize, (Vector3)ShadowGridSize);
            solverCommandBuffer.SetComputeVectorParam(MaterialParameters.RendererCompute, ShaderParam.LightGridSize, (Vector3)LightGridSize);
            solverCommandBuffer.SetComputeVectorParam(MaterialParameters.RendererCompute, ShaderParam.ShadowColor, MaterialParameters.ShadowAbsorptionColor);
            solverCommandBuffer.SetComputeVectorParam(MaterialParameters.RendererCompute, ShaderParam.ScatteringColor, MaterialParameters.ScatteringColor);
            solverCommandBuffer.SetComputeVectorParam(MaterialParameters.RendererCompute, ShaderParam.LightColor, GetLightColor(MainLight));
            solverCommandBuffer.SetComputeVectorParam(MaterialParameters.RendererCompute, ShaderParam.LightDirWorld,
                                                      MainLight.transform.rotation * new Vector3(0, 0, -1));

            int mainLightMode = MainLight.enabled ? 1 : 0;
            Vector4[] lightColors = new Vector4[MAX_LIGHT_COUNT];
            Vector4[] lightPositions = new Vector4[MAX_LIGHT_COUNT];
            int lightCount = GetLights(ref lightColors, ref lightPositions, MaterialParameters.IlluminationBrightness);

            solverCommandBuffer.SetComputeVectorArrayParam(MaterialParameters.RendererCompute, ShaderParam.LightColorArray, lightColors);
            solverCommandBuffer.SetComputeVectorArrayParam(MaterialParameters.RendererCompute, ShaderParam.LightPositionArray, lightPositions);
            solverCommandBuffer.SetComputeIntParam(MaterialParameters.RendererCompute, ShaderParam.LightCount, lightCount);
            solverCommandBuffer.SetComputeIntParam(MaterialParameters.RendererCompute, ShaderParam.MainLightMode, mainLightMode);
            solverCommandBuffer.SetComputeIntParam(MaterialParameters.RendererCompute, ShaderParam.SimulationMode, (int)ActiveSimulationMode);

            solverCommandBuffer.SetComputeFloatParam(MaterialParameters.RendererCompute, ShaderParam.IlluminationSoftness,
                                                     MaterialParameters.IlluminationSoftness);
            solverCommandBuffer.SetComputeFloatParam(MaterialParameters.RendererCompute, ShaderParam.SmokeDensity, MaterialParameters.SmokeDensity * MaterialParameters.FadeCoefficient);
            solverCommandBuffer.SetComputeFloatParam(MaterialParameters.RendererCompute, ShaderParam.FuelDensity, MaterialParameters.FuelDensity * MaterialParameters.FadeCoefficient);
            solverCommandBuffer.SetComputeFloatParam(MaterialParameters.RendererCompute, ShaderParam.ShadowIntensity, MaterialParameters.ShadowIntensity);
            solverCommandBuffer.SetComputeFloatParam(MaterialParameters.RendererCompute, ShaderParam.FireBrightness, MaterialParameters.FireBrightness);
            solverCommandBuffer.SetComputeFloatParam(MaterialParameters.RendererCompute, ShaderParam.BlackBodyBrightness, MaterialParameters.BlackBodyBrightness);
            solverCommandBuffer.SetComputeFloatParam(MaterialParameters.RendererCompute, ShaderParam.ReactionSpeed, SolverParameters.ReactionSpeed);
            solverCommandBuffer.SetComputeFloatParam(MaterialParameters.RendererCompute, ShaderParam.TempThreshold, SolverParameters.TempThreshold);
            solverCommandBuffer.SetComputeFloatParam(MaterialParameters.RendererCompute, ShaderParam.TemperatureDensityDependence,
                                                     MaterialParameters.TemperatureDensityDependence);
            solverCommandBuffer.SetComputeFloatParam(MaterialParameters.RendererCompute, ShaderParam.ScatteringAttenuation,
                                                     MaterialParameters.ScatteringAttenuation);
            solverCommandBuffer.SetComputeFloatParam(MaterialParameters.RendererCompute, ShaderParam.ScatteringContribution,
                                                     MaterialParameters.ScatteringContribution);
            solverCommandBuffer.SetComputeVectorParam(MaterialParameters.RendererCompute, ShaderParam.FireColor, MaterialParameters.FireColor);

            if (MainLight.enabled)
            {
                solverCommandBuffer.SetComputeFloatParam(MaterialParameters.RendererCompute, ShaderParam.ShadowStepSize, MaterialParameters.ShadowStepSize);
                solverCommandBuffer.SetComputeIntParam(MaterialParameters.RendererCompute, ShaderParam.ShadowMaxSteps, MaterialParameters.ShadowMaxSteps);

                if (GridDownscale > 1)
                {
                    solverCommandBuffer.SetComputeTextureParam(MaterialParameters.RendererCompute, ShadowmapID, ShaderParam.Density, RenderDensityLOD);
                }
                else
                {
                    solverCommandBuffer.SetComputeTextureParam(MaterialParameters.RendererCompute, ShadowmapID, ShaderParam.Density, RenderDensity);
                }

                solverCommandBuffer.SetComputeIntParam(MaterialParameters.RendererCompute, ShaderParam.DensityDownscale, GridDownscale);
                solverCommandBuffer.SetComputeTextureParam(MaterialParameters.RendererCompute, ShadowmapID, ShaderParam.Color, RenderColor);
                solverCommandBuffer.SetComputeTextureParam(MaterialParameters.RendererCompute, ShadowmapID, ShaderParam.BlueNoise,
                                                           MaterialParameters.BlueNoise);
                solverCommandBuffer.SetComputeTextureParam(MaterialParameters.RendererCompute, ShadowmapID, ShaderParam.ShadowmapOUT, Shadowmap);
                solverCommandBuffer.DispatchCompute(MaterialParameters.RendererCompute, ShadowmapID, ShadowWorkGroupsXYZ.x, ShadowWorkGroupsXYZ.y,
                                                    ShadowWorkGroupsXYZ.z);
            }

            if (Lights.Count > 0)
            {
                solverCommandBuffer.SetComputeFloatParam(MaterialParameters.RendererCompute, ShaderParam.ShadowStepSize,
                                                         MaterialParameters.IlluminationStepSize);
                solverCommandBuffer.SetComputeIntParam(MaterialParameters.RendererCompute, ShaderParam.ShadowMaxSteps,
                                                       MaterialParameters.IlluminationMaxSteps);

                if (GridDownscale > 1)
                {
                    solverCommandBuffer.SetComputeTextureParam(MaterialParameters.RendererCompute, LightmapID, ShaderParam.Density, RenderDensityLOD);
                }
                else
                {
                    solverCommandBuffer.SetComputeTextureParam(MaterialParameters.RendererCompute, LightmapID, ShaderParam.Density, RenderDensity);
                }

                solverCommandBuffer.SetComputeIntParam(MaterialParameters.RendererCompute, ShaderParam.DensityDownscale, GridDownscale);
                solverCommandBuffer.SetComputeTextureParam(MaterialParameters.RendererCompute, LightmapID, ShaderParam.Color, RenderColor);
                solverCommandBuffer.SetComputeTextureParam(MaterialParameters.RendererCompute, LightmapID, ShaderParam.BlueNoise,
                                                           MaterialParameters.BlueNoise);
                solverCommandBuffer.SetComputeTextureParam(MaterialParameters.RendererCompute, LightmapID, ShaderParam.LightmapOUT, Lightmap);
                solverCommandBuffer.DispatchCompute(MaterialParameters.RendererCompute, LightmapID, LightWorkGroupsXYZ.x, LightWorkGroupsXYZ.y,
                                                    LightWorkGroupsXYZ.z);
            }

            solverCommandBuffer.SetComputeTextureParam(MaterialParameters.RendererCompute, IlluminationID, ShaderParam.Density, RenderDensity);
            solverCommandBuffer.SetComputeIntParam(MaterialParameters.RendererCompute, ShaderParam.DensityDownscale, 1);
            solverCommandBuffer.SetComputeTextureParam(MaterialParameters.RendererCompute, IlluminationID, ShaderParam.Shadowmap, Shadowmap);
            solverCommandBuffer.SetComputeTextureParam(MaterialParameters.RendererCompute, IlluminationID, ShaderParam.Lightmap, Lightmap);
            solverCommandBuffer.SetComputeTextureParam(MaterialParameters.RendererCompute, IlluminationID, ShaderParam.Color, RenderColor);
            solverCommandBuffer.SetComputeTextureParam(MaterialParameters.RendererCompute, IlluminationID, ShaderParam.IlluminationOUT, RenderIllumination);
            solverCommandBuffer.DispatchCompute(MaterialParameters.RendererCompute, IlluminationID, WorkGroupsXYZ.x, WorkGroupsXYZ.y,
                                                WorkGroupsXYZ.z);

            Graphics.ExecuteCommandBuffer(solverCommandBuffer);

            solverCommandBuffer.Clear();
        }
#endregion

#region Initialisation

        private void ClearTexture(RenderTexture texture, CommandBuffer commandBuffer)
        {
            switch (texture.dimension)
            {
                case TextureDimension.Tex3D:
                    {
                        switch (texture.graphicsFormat)
                        {
                            case GraphicsFormat.R32_SFloat:
                            case GraphicsFormat.R16_SFloat:
                                {
                                    commandBuffer.SetComputeTextureParam(MaterialParameters.ClearResourceCompute, ClearTexture3DFloatID, ShaderParam.Texture3DFloat, texture);
                                    commandBuffer.SetComputeVectorParam(MaterialParameters.ClearResourceCompute, ShaderParam.Texture3DFloatDimensions, new Vector3(texture.width, texture.height, texture.volumeDepth));
                                    commandBuffer.DispatchCompute(MaterialParameters.ClearResourceCompute, ClearTexture3DFloatID, IntDivCeil(texture.width, TEXTURE3D_CLEAR_GROUPSIZE),
                                                              IntDivCeil(texture.height, TEXTURE3D_CLEAR_GROUPSIZE), IntDivCeil(texture.volumeDepth, TEXTURE3D_CLEAR_GROUPSIZE));
                                }
                                break;
                            case GraphicsFormat.R32G32_SFloat:
                            case GraphicsFormat.R16G16_SFloat:
                                {
                                    commandBuffer.SetComputeTextureParam(MaterialParameters.ClearResourceCompute, ClearTexture3DFloat2ID, ShaderParam.Texture3DFloat2, texture);
                                    commandBuffer.SetComputeVectorParam(MaterialParameters.ClearResourceCompute, ShaderParam.Texture3DFloat2Dimensions, new Vector3(texture.width, texture.height, texture.volumeDepth));
                                    commandBuffer.DispatchCompute(MaterialParameters.ClearResourceCompute, ClearTexture3DFloat2ID, IntDivCeil(texture.width, TEXTURE3D_CLEAR_GROUPSIZE),
                                                              IntDivCeil(texture.height, TEXTURE3D_CLEAR_GROUPSIZE), IntDivCeil(texture.volumeDepth, TEXTURE3D_CLEAR_GROUPSIZE));
                                }
                                break;
                            case GraphicsFormat.R32G32B32_SFloat:
                            case GraphicsFormat.R16G16B16_SFloat:
                            case GraphicsFormat.B10G11R11_UFloatPack32:
                                {
                                    commandBuffer.SetComputeTextureParam(MaterialParameters.ClearResourceCompute, ClearTexture3DFloat3ID, ShaderParam.Texture3DFloat3, texture);
                                    commandBuffer.SetComputeVectorParam(MaterialParameters.ClearResourceCompute, ShaderParam.Texture3DFloat3Dimensions, new Vector3(texture.width, texture.height, texture.volumeDepth));
                                    commandBuffer.DispatchCompute(MaterialParameters.ClearResourceCompute, ClearTexture3DFloat3ID, IntDivCeil(texture.width, TEXTURE3D_CLEAR_GROUPSIZE),
                                                              IntDivCeil(texture.height, TEXTURE3D_CLEAR_GROUPSIZE), IntDivCeil(texture.volumeDepth, TEXTURE3D_CLEAR_GROUPSIZE));
                                }
                                break;
                            case GraphicsFormat.R32G32B32A32_SFloat:
                            case GraphicsFormat.R16G16B16A16_SFloat:
                                {
                                    commandBuffer.SetComputeTextureParam(MaterialParameters.ClearResourceCompute, ClearTexture3DFloat4ID, ShaderParam.Texture3DFloat4, texture);
                                    commandBuffer.SetComputeVectorParam(MaterialParameters.ClearResourceCompute, ShaderParam.Texture3DFloat4Dimensions, new Vector3(texture.width, texture.height, texture.volumeDepth));
                                    commandBuffer.DispatchCompute(MaterialParameters.ClearResourceCompute, ClearTexture3DFloat4ID, IntDivCeil(texture.width, TEXTURE3D_CLEAR_GROUPSIZE),
                                                              IntDivCeil(texture.height, TEXTURE3D_CLEAR_GROUPSIZE), IntDivCeil(texture.volumeDepth, TEXTURE3D_CLEAR_GROUPSIZE));
                                }
                                break;
                            default:
                                throw new NotSupportedException($"Clearing texture of format {texture.graphicsFormat} is not supported");
                        }
                    }
                    break;
                default:
                    throw new NotSupportedException($"Clearing texture of type {texture.dimension} is not supported");
            }
        }

        private RenderTexture InitVolumeTexture(Vector3Int resolution, string name,
                                                GraphicsFormat format = GraphicsFormat.R32G32B32A32_SFloat)
        {
            bool isFormatSupported =
#if UNITY_2023_2_OR_NEWER
                SystemInfo.IsFormatSupported(format, GraphicsFormatUsage.LoadStore);
#else
                SystemInfo.IsFormatSupported(format, FormatUsage.LoadStore);
#endif
            format = isFormatSupported ? format : GraphicsFormat.R32G32B32A32_SFloat;

            var volume = new RenderTexture(resolution.x, resolution.y, 0, format);
            volume.volumeDepth = resolution.z;
            volume.dimension = TextureDimension.Tex3D;
            volume.enableRandomWrite = true;
            volume.filterMode = FilterMode.Trilinear;
            volume.name = name;
            volume.Create();
            if (!volume.IsCreated())
            {
                throw new NotSupportedException("Failed to create 3D texture.");
            }

            return volume;
        }

        private void FindComputeKernels()
        {
            ShadowmapID = MaterialParameters.RendererCompute.FindKernel("CS_Shadowmap");
            LightmapID = MaterialParameters.RendererCompute.FindKernel("CS_Lightmap");
            IlluminationID = MaterialParameters.RendererCompute.FindKernel("CS_Illumination");
            CopyDepthID = MaterialParameters.RendererCompute.FindKernel("CS_CopyDepth");
            ShaderParamContainer.RenderCompute_INPUT_2D_ARRAY = new LocalKeyword(MaterialParameters.RendererCompute, "INPUT_2D_ARRAY");
            ClearTexture3DFloatID = MaterialParameters.ClearResourceCompute.FindKernel("CS_ClearTexture3DFloat");
            ClearTexture3DFloat2ID = MaterialParameters.ClearResourceCompute.FindKernel("CS_ClearTexture3DFloat2");
            ClearTexture3DFloat3ID = MaterialParameters.ClearResourceCompute.FindKernel("CS_ClearTexture3DFloat3");
            ClearTexture3DFloat4ID = MaterialParameters.ClearResourceCompute.FindKernel("CS_ClearTexture3DFloat4");
        }

        private void Clear3DTextures()
        {
            solverCommandBuffer.Clear();
            ClearTexture(RenderDensity, solverCommandBuffer);
            if (GridDownscale > 1)
            {
                ClearTexture(RenderDensityLOD, solverCommandBuffer);
            }
            ClearTexture(RenderColor, solverCommandBuffer);
            ClearTexture(RenderIllumination, solverCommandBuffer);
            ClearTexture(ColorTexture0, solverCommandBuffer);
            ClearTexture(VelocityTexture0, solverCommandBuffer);
            ClearTexture(ColorTexture1, solverCommandBuffer);
            ClearTexture(VelocityTexture1, solverCommandBuffer);
            ClearTexture(TmpSDFTexture, solverCommandBuffer);
            ClearTexture(Divergence, solverCommandBuffer);
            ClearTexture(ResidualLOD0, solverCommandBuffer);
            ClearTexture(Pressure0LOD0, solverCommandBuffer);
            ClearTexture(Pressure1LOD0, solverCommandBuffer);

#if !UNITY_ANDROID || UNITY_EDITOR
            ClearTexture(ResidualLOD1, solverCommandBuffer);
            ClearTexture(ResidualLOD2, solverCommandBuffer);
            ClearTexture(Pressure0LOD1, solverCommandBuffer);
            ClearTexture(Pressure0LOD2, solverCommandBuffer);
            ClearTexture(Pressure1LOD1, solverCommandBuffer);
            ClearTexture(Pressure1LOD2, solverCommandBuffer);
#endif

            Graphics.ExecuteCommandBuffer(solverCommandBuffer);
            solverCommandBuffer.Clear();
        }

        private void Initialize2DTextures()
        {
            var textureFormat = GraphicsFormat.R8G8B8A8_UNorm;
            var textureFlags = TextureCreationFlags.None;

            var emitters = Manipulators.FindAll(manip => manip is ZibraParticleEmitter);
            
            if (emitters.Count == 0)
            {
                EmittersColorsTexture = new Texture2D(1, 1, textureFormat, textureFlags);
                EmittersColorsStagingTexture = new Texture2D(1, 1, textureFormat, textureFlags);
            }
            else
            {
                EmittersColorsTexture = new Texture2D(EMITTER_GRADIENT_TEX_WIDTH, Mathf.Max(emitters.Count * EMITTER_GRADIENT_ITEM_STRIDE, 1), textureFormat, textureFlags);
                EmittersColorsStagingTexture = new Texture2D(EMITTER_GRADIENT_TEX_WIDTH, Mathf.Max(emitters.Count * EMITTER_GRADIENT_ITEM_STRIDE, 1), textureFormat, textureFlags);
            }
        }
        
        private void Initialize3DTextures()
        {
            RenderDensity = InitVolumeTexture(GridSize, nameof(RenderDensity), GraphicsFormat.R16_SFloat);

            if (GridDownscale > 1)
            {
                DownscaleXYZ = new Vector3Int(IntDivCeil((int)GridSizeLOD.x, WORKGROUP_SIZE_X),
                                              IntDivCeil((int)GridSizeLOD.y, WORKGROUP_SIZE_Y),
                                              IntDivCeil((int)GridSizeLOD.z, WORKGROUP_SIZE_Z));
                RenderDensityLOD = InitVolumeTexture(GridSizeLOD, nameof(RenderDensityLOD), GraphicsFormat.R16_SFloat);
            }

            RenderColor = InitVolumeTexture(GridSize, nameof(RenderColor), GraphicsFormat.R16G16_SFloat);
            RenderIllumination =
                InitVolumeTexture(GridSize, nameof(RenderIllumination), GraphicsFormat.R16G16B16A16_SFloat);
            ColorTexture0 = InitVolumeTexture(GridSize, nameof(ColorTexture0), GraphicsFormat.R16G16B16A16_SFloat);
            VelocityTexture0 =
                InitVolumeTexture(GridSize, nameof(VelocityTexture0), GraphicsFormat.R16G16B16A16_SFloat);
            ColorTexture1 = InitVolumeTexture(GridSize, nameof(ColorTexture1), GraphicsFormat.R16G16_SFloat);
            VelocityTexture1 =
                InitVolumeTexture(GridSize, nameof(VelocityTexture1), GraphicsFormat.R16G16B16A16_SFloat);
            TmpSDFTexture = InitVolumeTexture(GridSize, nameof(TmpSDFTexture), GraphicsFormat.R16G16_SFloat);

            Divergence =
                InitVolumeTexture(PressureGridSize(GridSize, 1), nameof(Divergence), GraphicsFormat.R16_SFloat);
            ResidualLOD0 =
                InitVolumeTexture(PressureGridSize(GridSize, 1), nameof(ResidualLOD0), GraphicsFormat.R16_SFloat);
            Pressure0LOD0 =
                InitVolumeTexture(PressureGridSize(GridSize, 1), nameof(Pressure0LOD0), GraphicsFormat.R16_SFloat);
            Pressure1LOD0 =
                InitVolumeTexture(PressureGridSize(GridSize, 1), nameof(Pressure1LOD0), GraphicsFormat.R16_SFloat);
#if !UNITY_ANDROID || UNITY_EDITOR
            ResidualLOD1 =
                InitVolumeTexture(PressureGridSize(GridSize, 2), nameof(ResidualLOD1), GraphicsFormat.R16_SFloat);
            ResidualLOD2 =
                InitVolumeTexture(PressureGridSize(GridSize, 4), nameof(ResidualLOD2), GraphicsFormat.R16_SFloat);
            Pressure0LOD1 =
                InitVolumeTexture(PressureGridSize(GridSize, 2), nameof(Pressure0LOD1), GraphicsFormat.R16_SFloat);
            Pressure0LOD2 =
                InitVolumeTexture(PressureGridSize(GridSize, 4), nameof(Pressure0LOD2), GraphicsFormat.R16_SFloat);
            Pressure1LOD1 =
                InitVolumeTexture(PressureGridSize(GridSize, 2), nameof(Pressure1LOD1), GraphicsFormat.R16_SFloat);
            Pressure1LOD2 =
                InitVolumeTexture(PressureGridSize(GridSize, 4), nameof(Pressure1LOD2), GraphicsFormat.R16_SFloat);
#endif
        }

        private void CalculateWorkgroupSizes()
        {
            Vector3 ShadowGridSizeFloat = new Vector3(GridSize.x, GridSize.y, GridSize.z) * MaterialParameters.ShadowResolution;
            ShadowGridSize =
                new Vector3Int((int)ShadowGridSizeFloat.x, (int)ShadowGridSizeFloat.y, (int)ShadowGridSizeFloat.z);
            Shadowmap =
                InitVolumeTexture(ShadowGridSize, nameof(Shadowmap), GraphicsFormat.R16_SFloat);
            ShadowWorkGroupsXYZ = new Vector3Int(IntDivCeil(ShadowGridSize.x, WORKGROUP_SIZE_X),
                                                 IntDivCeil(ShadowGridSize.y, WORKGROUP_SIZE_Y),
                                                 IntDivCeil(ShadowGridSize.z, WORKGROUP_SIZE_Z));

            Vector3 LightGridSizeFloat = new Vector3(GridSize.x, GridSize.y, GridSize.z) * MaterialParameters.IlluminationResolution;
            LightGridSize =
                new Vector3Int((int)LightGridSizeFloat.x, (int)LightGridSizeFloat.y, (int)LightGridSizeFloat.z);
            Lightmap =
                InitVolumeTexture(LightGridSize, nameof(Lightmap), GraphicsFormat.R16G16B16A16_SFloat);
            LightWorkGroupsXYZ = new Vector3Int(IntDivCeil(LightGridSize.x, WORKGROUP_SIZE_X),
                                                IntDivCeil(LightGridSize.y, WORKGROUP_SIZE_Y),
                                                IntDivCeil(LightGridSize.z, WORKGROUP_SIZE_Z));

            WorkGroupsXYZ = new Vector3Int(IntDivCeil(GridSize.x, WORKGROUP_SIZE_X),
                                           IntDivCeil(GridSize.y, WORKGROUP_SIZE_Y),
                                           IntDivCeil(GridSize.z, WORKGROUP_SIZE_Z));
            MaxEffectParticleWorkgroups = IntDivCeil(MaterialParameters.MaxEffectParticles, PARTICLE_WORKGROUP);
        }

        private void RegisterResources()
        {
            var registerBuffersParams = new RegisterBuffersBridgeParams();
            registerBuffersParams.SimulationParams = NativeSimulationData;

            registerBuffersParams.RenderDensity = MakeTextureNativeBridge(RenderDensity);
            registerBuffersParams.RenderDensityLOD = MakeTextureNativeBridge(RenderDensityLOD);
            registerBuffersParams.RenderColor = MakeTextureNativeBridge(RenderColor);
            registerBuffersParams.RenderIllumination = MakeTextureNativeBridge(RenderIllumination);
            registerBuffersParams.ColorTexture0 = MakeTextureNativeBridge(ColorTexture0);
            registerBuffersParams.VelocityTexture0 = MakeTextureNativeBridge(VelocityTexture0);
            registerBuffersParams.ColorTexture1 = MakeTextureNativeBridge(ColorTexture1);
            registerBuffersParams.VelocityTexture1 = MakeTextureNativeBridge(VelocityTexture1);
            registerBuffersParams.TmpSDFTexture = MakeTextureNativeBridge(TmpSDFTexture);
            registerBuffersParams.EmitterTexture = MakeTextureNativeBridge(ManipulatorManager.EmitterTexture);
            registerBuffersParams.ParticleColors = MakeTextureNativeBridge(EmittersColorsTexture);

            registerBuffersParams.Divergence = MakeTextureNativeBridge(Divergence);
            registerBuffersParams.ResidualLOD0 = MakeTextureNativeBridge(ResidualLOD0);
            registerBuffersParams.Pressure0LOD0 = MakeTextureNativeBridge(Pressure0LOD0);
            registerBuffersParams.Pressure1LOD0 = MakeTextureNativeBridge(Pressure1LOD0);

#if !UNITY_ANDROID || UNITY_EDITOR
            registerBuffersParams.ResidualLOD1 = MakeTextureNativeBridge(ResidualLOD1);
            registerBuffersParams.ResidualLOD2 = MakeTextureNativeBridge(ResidualLOD2);
            registerBuffersParams.Pressure0LOD1 = MakeTextureNativeBridge(Pressure0LOD1);
            registerBuffersParams.Pressure0LOD2 = MakeTextureNativeBridge(Pressure0LOD2);
            registerBuffersParams.Pressure1LOD1 = MakeTextureNativeBridge(Pressure1LOD1);
            registerBuffersParams.Pressure1LOD2 = MakeTextureNativeBridge(Pressure1LOD2);
#endif

            registerBuffersParams.AtomicCounters = GetNativePtr(AtomicCounters);
            registerBuffersParams.EffectParticleData0 = GetNativePtr(EffectParticleData0);
            registerBuffersParams.EffectParticleData1 = GetNativePtr(EffectParticleData1);
            registerBuffersParams.EffectParticleEmissionColorData = GetNativePtr(EffectParticleEmissionColorData);

            RandomTexture =
                new Texture3D(RANDOM_TEX_SIZE, RANDOM_TEX_SIZE, RANDOM_TEX_SIZE, TextureFormat.RGBA32, false);
            RandomTexture.filterMode = FilterMode.Trilinear;
            registerBuffersParams.RandomTexture = MakeTextureNativeBridge(RandomTexture);

            GCHandle randomDataHandle = default(GCHandle);
            System.Random rand = new System.Random();
            int RandomTextureSize = RANDOM_TEX_SIZE * RANDOM_TEX_SIZE * RANDOM_TEX_SIZE;
            Color32[] RandomTextureData = new Color32[RandomTextureSize];
            for (int i = 0; i < RandomTextureSize; i++)
            {
                RandomTextureData[i] =
                    new Color32((byte)rand.Next(255), (byte)rand.Next(255), (byte)rand.Next(255), (byte)rand.Next(255));
            }

            randomDataHandle = GCHandle.Alloc(RandomTextureData, GCHandleType.Pinned);
            registerBuffersParams.RandomData.dataSize = Marshal.SizeOf(new Color32()) * RandomTextureData.Length;
            registerBuffersParams.RandomData.data = randomDataHandle.AddrOfPinnedObject();
            registerBuffersParams.RandomData.rowPitch = Marshal.SizeOf(new Color32()) * RANDOM_TEX_SIZE;
            registerBuffersParams.RandomData.dimensionX = RANDOM_TEX_SIZE;
            registerBuffersParams.RandomData.dimensionY = RANDOM_TEX_SIZE;
            registerBuffersParams.RandomData.dimensionZ = RANDOM_TEX_SIZE;

            IntPtr nativeRegisterBuffersParams = Marshal.AllocHGlobal(Marshal.SizeOf(registerBuffersParams));

            solverCommandBuffer.Clear();
            Marshal.StructureToPtr(registerBuffersParams, nativeRegisterBuffersParams, true);
            SmokeAndFireBridge.SubmitInstanceEvent(solverCommandBuffer, CurrentInstanceID,
                                                   SmokeAndFireBridge.EventID.RegisterSolverBuffers,
                                                   nativeRegisterBuffersParams);
            Graphics.ExecuteCommandBuffer(solverCommandBuffer);
            solverCommandBuffer.Clear();
            toFreeOnExit.Add(nativeRegisterBuffersParams);
        }

        private void InitializeManipulators()
        {
            if (ManipulatorManager == null)
            {
                throw new Exception("No manipulator ManipulatorManager has been set");
            }

            ManipulatorManager.UpdateConst(Manipulators);
            ManipulatorManager.UpdateDynamic(this);

            if (ManipulatorManager.TextureCount > 0)
            {
                EmbeddingsTexture = new Texture3D(
                    ManipulatorManager.EmbeddingTextureDimension, ManipulatorManager.EmbeddingTextureDimension,
                    ManipulatorManager.EmbeddingTextureDimension, TextureFormat.RGBA32, false);

                SDFGridTexture =
                    new Texture3D(ManipulatorManager.SDFTextureDimension, ManipulatorManager.SDFTextureDimension,
                                    ManipulatorManager.SDFTextureDimension, TextureFormat.RHalf, false);

                EmbeddingsTexture.filterMode = FilterMode.Trilinear;
                SDFGridTexture.filterMode = FilterMode.Trilinear;
            }
            else
            {
                EmbeddingsTexture = new Texture3D(1, 1, 1, TextureFormat.RGBA32, 0);
                SDFGridTexture = new Texture3D(1, 1, 1, TextureFormat.RHalf, 0);
            }

            int ManipSize = Marshal.SizeOf(typeof(ZibraManipulatorManager.ManipulatorParam));
            int SDFSize = Marshal.SizeOf(typeof(ZibraManipulatorManager.SDFObjectParams));
            // Need to create at least some buffer to bind to shaders
            NativeManipData = Marshal.AllocHGlobal(ManipulatorManager.Elements * ManipSize);
            NativeManipIndices = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(ZibraManipulatorManager.ManipulatorIndices)));
            NativeSDFData = Marshal.AllocHGlobal(ManipulatorManager.SDFObjectList.Count * SDFSize);
            DynamicManipulatorData = new ComputeBuffer(Math.Max(ManipulatorManager.Elements, 1), ManipSize);

            AtomicCounters = new ComputeBuffer(8, sizeof(int));
            EffectParticleData0 = new ComputeBuffer(5 * MaterialParameters.MaxEffectParticles, sizeof(uint));
            EffectParticleData1 = new ComputeBuffer(5 * MaterialParameters.MaxEffectParticles, sizeof(uint));
            EffectParticleEmissionColorData = new ComputeBuffer(4 * (GridSize.x * GridSize.y * GridSize.z), sizeof(uint));

            SDFObjectData = new ComputeBuffer(Math.Max(ManipulatorManager.SDFObjectList.Count, 1),
                                                Marshal.SizeOf(typeof(ZibraManipulatorManager.SDFObjectParams)));
            ManipulatorStatistics = new ComputeBuffer(
                Math.Max(STATISTICS_PER_MANIPULATOR * ManipulatorManager.Elements, 1), sizeof(int));

#if ZIBRA_EFFECTS_DEBUG
            DynamicManipulatorData.name = "DynamicManipulatorData";
            SDFObjectData.name = "SDFObjectData";
            ManipulatorStatistics.name = "ManipulatorStatistics";
#endif
            UpdateInteropBuffers();

            var registerManipulatorsBridgeParams = new RegisterManipulatorsBridgeParams();
            registerManipulatorsBridgeParams.ManipulatorNum = ManipulatorManager.Elements;
            registerManipulatorsBridgeParams.ManipulatorBufferDynamic = GetNativePtr(DynamicManipulatorData);
            registerManipulatorsBridgeParams.SDFObjectBuffer = GetNativePtr(SDFObjectData);
            registerManipulatorsBridgeParams.ManipulatorBufferStatistics =
                ManipulatorStatistics.GetNativeBufferPtr();
            registerManipulatorsBridgeParams.ManipulatorParams = NativeManipData;
            registerManipulatorsBridgeParams.SDFObjectCount = ManipulatorManager.SDFObjectList.Count;
            registerManipulatorsBridgeParams.SDFObjectData = NativeSDFData;
            registerManipulatorsBridgeParams.ManipIndices = NativeManipIndices;
            registerManipulatorsBridgeParams.EmbeddingsTexture = MakeTextureNativeBridge(EmbeddingsTexture);
            registerManipulatorsBridgeParams.SDFGridTexture = MakeTextureNativeBridge(SDFGridTexture);

            GCHandle embeddingDataHandle = default(GCHandle);
            if (ManipulatorManager.Embeddings.Length > 0)
            {
                embeddingDataHandle = GCHandle.Alloc(ManipulatorManager.Embeddings, GCHandleType.Pinned);
                registerManipulatorsBridgeParams.EmbeddigsData.dataSize =
                    Marshal.SizeOf(new Color32()) * ManipulatorManager.Embeddings.Length;
                registerManipulatorsBridgeParams.EmbeddigsData.data = embeddingDataHandle.AddrOfPinnedObject();
                registerManipulatorsBridgeParams.EmbeddigsData.rowPitch =
                    Marshal.SizeOf(new Color32()) * EmbeddingsTexture.width;
                registerManipulatorsBridgeParams.EmbeddigsData.dimensionX = EmbeddingsTexture.width;
                registerManipulatorsBridgeParams.EmbeddigsData.dimensionY = EmbeddingsTexture.height;
                registerManipulatorsBridgeParams.EmbeddigsData.dimensionZ = EmbeddingsTexture.depth;
            }

            GCHandle sdfGridHandle = default(GCHandle);
            if (ManipulatorManager.SDFGrid.Length > 0)
            {
                sdfGridHandle = GCHandle.Alloc(ManipulatorManager.SDFGrid, GCHandleType.Pinned);
                registerManipulatorsBridgeParams.SDFGridData.dataSize =
                    Marshal.SizeOf(new byte()) * ManipulatorManager.SDFGrid.Length;
                registerManipulatorsBridgeParams.SDFGridData.data = sdfGridHandle.AddrOfPinnedObject();
                registerManipulatorsBridgeParams.SDFGridData.rowPitch =
                    Marshal.SizeOf(new byte()) * 2 * SDFGridTexture.width;
                registerManipulatorsBridgeParams.SDFGridData.dimensionX = SDFGridTexture.width;
                registerManipulatorsBridgeParams.SDFGridData.dimensionY = SDFGridTexture.height;
                registerManipulatorsBridgeParams.SDFGridData.dimensionZ = SDFGridTexture.depth;
            }

            IntPtr nativeRegisterManipulatorsBridgeParams =
                Marshal.AllocHGlobal(Marshal.SizeOf(registerManipulatorsBridgeParams));
            Marshal.StructureToPtr(registerManipulatorsBridgeParams, nativeRegisterManipulatorsBridgeParams, true);
            solverCommandBuffer.Clear();
            SmokeAndFireBridge.SubmitInstanceEvent(solverCommandBuffer, CurrentInstanceID,
                                                    SmokeAndFireBridge.EventID.RegisterManipulators,
                                                    nativeRegisterManipulatorsBridgeParams);
            Graphics.ExecuteCommandBuffer(solverCommandBuffer);
        }

        private void InitializeSolver()
        {
            SimulationInternalTime = 0.0f;
            SimulationInternalFrame = 0;
            simulationParams = new SimulationParams();
            cameraRenderParams = new RenderParams();

            UpdateGridSize();
            SetSimulationParameters();

            NativeSimulationData = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(SimulationParams)));

            InitializeManipulators();
            FindComputeKernels();
            // TODO remove. Called twice
            SetSimulationParameters();
            UpdateInteropBuffers();
            Initialize2DTextures();
            Initialize3DTextures();
            Clear3DTextures();
            CalculateWorkgroupSizes();
            RegisterResources();

            // create a quad mesh for fullscreen rendering
            renderQuad = PrimitiveHelper.GetPrimitiveMesh(PrimitiveType.Quad);
            renderSimulationCube = PrimitiveHelper.GetPrimitiveMesh(PrimitiveType.Cube);
        }

        private void SetupScriptableRenderComponents()
        {
#if UNITY_PIPELINE_HDRP
#if UNITY_EDITOR
            if (RenderPipelineDetector.GetRenderPipelineType() == RenderPipelineDetector.RenderPipeline.HDRP)
            {
                HDRPRenderer = gameObject.GetComponent<SmokeAndFireHDRPRenderComponent>();
                if (HDRPRenderer != null && HDRPRenderer.customPasses.Count == 0)
                {
                    DestroyImmediate(HDRPRenderer);
                    HDRPRenderer = null;
                }
                if (HDRPRenderer == null)
                {
                    HDRPRenderer = gameObject.AddComponent<SmokeAndFireHDRPRenderComponent>();
                    HDRPRenderer.injectionPoint = CustomPassInjectionPoint.BeforePostProcess;
                    HDRPRenderer.AddPassOfType(typeof(SmokeAndFireHDRPRenderComponent.FluidHDRPRender));
                    SmokeAndFireHDRPRenderComponent.FluidHDRPRender renderer =
                        HDRPRenderer.customPasses[0] as SmokeAndFireHDRPRenderComponent.FluidHDRPRender;
                    renderer.name = "ZibraSmokeAndFireRenderer";
                    renderer.smokeAndFire = this;
                }
            }
#endif
#endif // UNITY_PIPELINE_HDRP
        }
#endregion

#region Cleanup
        private void ClearRendering()
        {
            Camera.onPreRender -= RenderCallBackWrapper;

            ClearCameraCommandBuffers();

            // free allocated memory
            foreach (var data in camNativeParams)
            {
                Marshal.FreeHGlobal(data.Value);
            }

            CameraResourcesMap.Clear();

            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(SDFGridTexture);
            SDFGridTexture = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(EmbeddingsTexture);
            EmbeddingsTexture = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(EmittersSpriteTexture);
            EmittersSpriteTexture = null;
            camNativeParams.Clear();
        }
        private void ClearSolver()
        {
            if (solverCommandBuffer != null)
            {
                SmokeAndFireBridge.SubmitInstanceEvent(solverCommandBuffer, CurrentInstanceID,
                                                       SmokeAndFireBridge.EventID.ReleaseResources);
                Graphics.ExecuteCommandBuffer(solverCommandBuffer);
            }

            if (solverCommandBuffer != null)
            {
                solverCommandBuffer.Release();
                solverCommandBuffer = null;
            }

            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(SDFObjectData);
            SDFObjectData = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(ManipulatorStatistics);
            ManipulatorStatistics = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(DynamicManipulatorData);
            ManipulatorStatistics = null;
            Marshal.FreeHGlobal(NativeManipData);
            NativeManipData = IntPtr.Zero;
            Marshal.FreeHGlobal(NativeSimulationData);
            NativeSimulationData = IntPtr.Zero;
            Marshal.FreeHGlobal(NativeManipIndices);
            NativeManipIndices = IntPtr.Zero;

            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(VelocityTexture0);
            VelocityTexture0 = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(VelocityTexture1);
            VelocityTexture1 = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(TmpSDFTexture);
            TmpSDFTexture = null;

            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(RenderColor);
            RenderColor = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(RenderDensity);
            RenderDensity = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(RenderDensityLOD);
            RenderDensityLOD = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(RenderIllumination);
            RenderIllumination = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(ColorTexture0);
            ColorTexture0 = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(ColorTexture1);
            ColorTexture1 = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(Divergence);
            Divergence = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(ResidualLOD0);
            ResidualLOD0 = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(Pressure0LOD0);
            Pressure0LOD0 = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(Pressure1LOD0);
            Pressure1LOD0 = null;

#if !UNITY_ANDROID || UNITY_EDITOR
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(ResidualLOD1);
            ResidualLOD1 = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(ResidualLOD2);
            ResidualLOD2 = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(Pressure0LOD1);
            Pressure0LOD1 = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(Pressure0LOD2);
            Pressure0LOD2 = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(Pressure1LOD1);
            Pressure1LOD1 = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(Pressure1LOD2);
            Pressure1LOD2 = null;
#endif
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(AtomicCounters);
            AtomicCounters = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(EffectParticleData0);
            EffectParticleData0 = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(EffectParticleData1);
            EffectParticleData1 = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(EffectParticleEmissionColorData);
            EffectParticleEmissionColorData = null;
            ZibraSmokeAndFireGPUGarbageCollector.SafeRelease(EmittersColorsTexture);
            EmittersColorsTexture = null;

            CurrentTextureResolution = new Vector2Int(0, 0);
            GridSize = new Vector3Int(0, 0, 0);
            NumNodes = 0;
            SimulationInternalFrame = 0;
            SimulationInternalTime = 0.0f;
            LastTimestep = 0.0f;
            CamRenderResolutions.Clear();
            camNativeResolutions.Clear();

            Initialized = false;

            ActiveSimulationMode = 0;

            CopyDepthID = 0;
            ClearTexture3DFloatID = 0;
            ClearTexture3DFloat2ID = 0;
            ClearTexture3DFloat3ID = 0;
            ClearTexture3DFloat4ID = 0;
            DownscaleXYZ = Vector3Int.zero;
            GridDownscale = 0;
            GridSize = Vector3Int.zero;
            GridSizeLOD = Vector3Int.zero;
            IlluminationID = 0;
            LightGridSize = Vector3Int.zero;
            LightWorkGroupsXYZ = Vector3Int.zero;
            LightmapID = 0;
            MaxEffectParticleWorkgroups = 0;
            NumNodes = 0;
            ShadowGridSize = Vector3Int.zero;
            ShadowWorkGroupsXYZ = Vector3Int.zero;
            timeAccumulation = 0;

            ManipulatorManager.Clear();

            // DO NOT USE AllInstances.Remove(this)
            // This will not result in equivalent code
            // ZibraSmokeAndFire::Equals is overriden and don't have correct implementation
            int instanceIndex = AllInstances.FindIndex(fluid => ReferenceEquals(fluid, this));
            if (instanceIndex != -1)
            {
                AllInstances.RemoveAt(instanceIndex);
            }
        }
        private void ClearCameraCommandBuffers()
        {
            // clear all rendering command buffers if not rendering
            foreach (KeyValuePair<Camera, CommandBuffer> entry in cameraCBs)
            {
                if (entry.Key != null)
                {
                    entry.Key.RemoveCommandBuffer(ActiveInjectionPoint, entry.Value);
                }
            }
            cameraCBs.Clear();
            cameras.Clear();
        }
#endregion

#region Structures

        [StructLayout(LayoutKind.Sequential)]
        private class UnityTextureBridge
        {
            public IntPtr texture;
            public SmokeAndFireBridge.TextureFormat format;
        }

        [StructLayout(LayoutKind.Sequential)]
        private class RegisterBuffersBridgeParams
        {
            public IntPtr SimulationParams;
            public UnityTextureBridge RenderDensity;
            public UnityTextureBridge RenderColor;
            public UnityTextureBridge RenderIllumination;
            public UnityTextureBridge ColorTexture0;
            public UnityTextureBridge VelocityTexture0;
            public UnityTextureBridge ColorTexture1;
            public UnityTextureBridge VelocityTexture1;
            public UnityTextureBridge TmpSDFTexture;
            public UnityTextureBridge Divergence;
            public UnityTextureBridge ResidualLOD0;
            public UnityTextureBridge ResidualLOD1;
            public UnityTextureBridge ResidualLOD2;
            public UnityTextureBridge Pressure0LOD0;
            public UnityTextureBridge Pressure0LOD1;
            public UnityTextureBridge Pressure0LOD2;
            public UnityTextureBridge Pressure1LOD0;
            public UnityTextureBridge Pressure1LOD1;
            public UnityTextureBridge Pressure1LOD2;
            public IntPtr AtomicCounters;
            public UnityTextureBridge RandomTexture;
            public TextureUploadData RandomData;
            public IntPtr EffectParticleData0;
            public IntPtr EffectParticleData1;
            public IntPtr EffectParticleEmissionColorData;
            public UnityTextureBridge RenderDensityLOD;
            public UnityTextureBridge EmitterTexture;
            public UnityTextureBridge ParticleColors;
        }

        [StructLayout(LayoutKind.Sequential)]
        private class RegisterRenderResourcesBridgeParams
        {
            public UnityTextureBridge ParticleSprites;
            public UnityTextureBridge Depth;
            public UnityTextureBridge ParticlesRT;
            public UnityTextureBridge VisualizeSDFTarget;
        }

        [StructLayout(LayoutKind.Sequential)]
        private class InitializeGPUReadbackParams
        {
            public UInt32 readbackBufferSize;
            public Int32 maxFramesInFlight;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TextureUploadData
        {
            public IntPtr data;
            public Int32 dataSize;
            public Int32 rowPitch;
            public Int32 dimensionX;
            public Int32 dimensionY;
            public Int32 dimensionZ;
        };

        [StructLayout(LayoutKind.Sequential)]
        private class RegisterManipulatorsBridgeParams
        {
            public Int32 ManipulatorNum;
            public IntPtr ManipulatorBufferDynamic;
            public IntPtr SDFObjectBuffer;
            public IntPtr ManipulatorBufferStatistics;
            public IntPtr ManipulatorParams;
            public Int32 SDFObjectCount;
            public IntPtr SDFObjectData;
            public IntPtr ManipIndices;
            public UnityTextureBridge EmbeddingsTexture;
            public UnityTextureBridge SDFGridTexture;
            public TextureUploadData EmbeddigsData;
            public TextureUploadData SDFGridData;
        }

        [StructLayout(LayoutKind.Sequential)]
        private class SimulationParams
        {
            public Vector3 GridSize;
            public Int32 NodeCount;

            public Vector3 ContainerScale;
            public Single MinimumVelocity;

            public Vector3 ContainerPos;
            public Single MaximumVelocity;

            public Single TimeStep;
            public Single SimulationTime;
            public Int32 SimulationFrame;
            public Int32 JacobiIterations;

            public Single ColorDecay;
            public Single VelocityDecay;
            public Single PressureReuse;
            public Single PressureReuseClamp;

            public Single Sharpen;
            public Single SharpenThreshold;
            public Single PressureProjection;
            public Single PressureClamp;

            public Vector3 Gravity;
            public Single SmokeBuoyancy;

            public Int32 LOD0Iterations;
            public Int32 LOD1Iterations;
            public Int32 LOD2Iterations;
            public Int32 PreIterations;

            public Single MainOverrelax;
            public Single EdgeOverrelax;
            public Single VolumeEdgeFadeoff;
            public Int32 SimulationIterations;

            public Vector3 SimulationContainerPosition;
            public Int32 SimulationMode;

            public Vector3 PreviousContainerPosition;
            public Int32 FixVolumeWorldPosition;

            public Single TempThreshold;
            public Single HeatEmission;
            public Single ReactionSpeed;
            public Single HeatBuoyancy;

            public Single SmokeDensity;
            public Single FuelDensity;
            public Single TemperatureDensityDependence;
            public Single FireBrightness;

            public int MaxEffectParticleCount;
            public int ParticleLifetime;
            public int SimulateEffectParticles;
            public int RenderEffectParticles;

            public Vector3 GridSizeLOD;
            public int GridDownscale;
        }

        [StructLayout(LayoutKind.Sequential)]
        private class RenderParams
        {
            public Matrix4x4 View;
            public Matrix4x4 Projection;
            public Matrix4x4 ProjectionInverse;
            public Matrix4x4 ViewProjection;
            public Matrix4x4 ViewProjectionInverse;
            public Matrix4x4 EyeRayCameraCoeficients;
            public Vector3 WorldSpaceCameraPos;
            public Int32 CameraID;
            public Vector4 ZBufferParams;
            public Vector2 CameraResolution;
            public Single CameraDownscaleFactor;
            Single CameraParamsPadding1;
        }

        internal struct MaterialPair
        {
            public Material CurrentMaterial;
            public Material SharedMaterial;

            // Returns true if dirty
            public bool SetMaterial(Material mat)
            {
                if (SharedMaterial != mat)
                {
                    CurrentMaterial = (mat != null ? Material.Instantiate(mat) : null);
                    SharedMaterial = mat;
                    return true;
                }
                return false;
            }
        }

        internal struct TexState
        {
            public bool IsDirty(Texture2D texture)
            {
                return Texture != texture || UpdateCount != texture.updateCount;
            }

            public void Update(Texture2D texture)
            {
                Texture = texture;
                UpdateCount = texture.updateCount;
            }

            private Texture2D Texture;
            private uint UpdateCount;
        }

        internal class CameraResources
        {
            public MaterialPair SmokeAndFireMaterial;
            public MaterialPair SmokeShadowProjectionMaterial;
            public MaterialPair UpscaleMaterial;
            public MaterialPair SDFRenderMaterial;
            public bool isDirty = true;
        }
#endregion

#region Native utils

        private IntPtr GetNativePtr(ComputeBuffer buffer)
        {
            return buffer == null ? IntPtr.Zero : buffer.GetNativeBufferPtr();
        }

        private IntPtr GetNativePtr(GraphicsBuffer buffer)
        {
            return buffer == null ? IntPtr.Zero : buffer.GetNativeBufferPtr();
        }

        private IntPtr GetNativePtr(RenderTexture texture)
        {
            return texture == null ? IntPtr.Zero : texture.GetNativeTexturePtr();
        }

        private IntPtr GetNativePtr(Texture2D texture)
        {
            return texture == null ? IntPtr.Zero : texture.GetNativeTexturePtr();
        }

        private IntPtr GetNativePtr(Texture3D texture)
        {
            return texture == null ? IntPtr.Zero : texture.GetNativeTexturePtr();
        }

        private UnityTextureBridge MakeTextureNativeBridge(RenderTexture texture)
        {
            var unityTextureBridge = new UnityTextureBridge();
            if (texture != null)
            {
                unityTextureBridge.texture = GetNativePtr(texture);
                unityTextureBridge.format = SmokeAndFireBridge.ToBridgeTextureFormat(texture.graphicsFormat);
            }
            else
            {
                unityTextureBridge.texture = IntPtr.Zero;
                unityTextureBridge.format = SmokeAndFireBridge.TextureFormat.None;
            }

            return unityTextureBridge;
        }

        private UnityTextureBridge MakeTextureNativeBridge(Texture3D texture)
        {
            var unityTextureBridge = new UnityTextureBridge();
            unityTextureBridge.texture = GetNativePtr(texture);
            unityTextureBridge.format = SmokeAndFireBridge.ToBridgeTextureFormat(texture.graphicsFormat);

            return unityTextureBridge;
        }

        private UnityTextureBridge MakeTextureNativeBridge(Texture2D texture)
        {
            var unityTextureBridge = new UnityTextureBridge();
            unityTextureBridge.texture = GetNativePtr(texture);
            unityTextureBridge.format = SmokeAndFireBridge.ToBridgeTextureFormat(texture.graphicsFormat);

            return unityTextureBridge;
        }

        private void SetInteropBuffer<T>(IntPtr NativeBuffer, List<T> list)
        {
            long LongPtr = NativeBuffer.ToInt64(); // Must work both on x86 and x64
            for (int I = 0; I < list.Count; I++)
            {
                IntPtr Ptr = new IntPtr(LongPtr);
                Marshal.StructureToPtr(list[I], Ptr, true);
                LongPtr += Marshal.SizeOf(typeof(T));
            }
        }

        private bool UpdateNativeTextures(Camera cam, float renderPipelineRenderScale)
        {
            RefreshEmitterColorsTexture();
            UpdateCameraList();

            Vector2Int cameraResolution = new Vector2Int(cam.pixelWidth, cam.pixelHeight);
            cameraResolution = ApplyRenderPipelineRenderScale(cameraResolution, renderPipelineRenderScale);

            Vector2Int textureResolution = GetRequiredTextureResolution();
            int pixelCount = textureResolution.x * textureResolution.y;

            if (!cameras.Contains(cam))
            {
                // add camera to list
                cameras.Add(cam);
            }

            int CameraID = cameras.IndexOf(cam);

            bool isGlobalTexturesDirty = false;
            bool isCameraDirty = CameraResourcesMap[cam].isDirty;

            FilterMode defaultFilter = EnableDownscale ? FilterMode.Bilinear : FilterMode.Point;

            bool updateFlag = false;
            if (XRSettings.enabled &&
                XRSettings.stereoRenderingMode == XRSettings.StereoRenderingMode.SinglePassInstanced)
            {
                UpscaleColor = CreateStereoTexture(UpscaleColor, textureResolution, true, FilterMode.Bilinear, 0,
                                                    RenderTextureFormat.ARGBHalf, true, ref updateFlag);
            }
            else
            {
                UpscaleColor = CreateTexture(UpscaleColor, textureResolution, true, FilterMode.Bilinear, 0,
                                            RenderTextureFormat.ARGBHalf, true, ref updateFlag);                
            }
            
            ParticlesRT = CreateTexture(ParticlesRT, textureResolution, true, FilterMode.Point, 0,
                                        RenderTextureFormat.ARGB32, true, ref updateFlag);
            DepthTexture = CreateTexture(DepthTexture, cameraResolution, true, defaultFilter, 32,
                                         RenderTextureFormat.RFloat, true, ref updateFlag);
            VisualizeSDFTarget = CreateTexture(VisualizeSDFTarget, textureResolution, true, FilterMode.Point, 0,
                                      RenderTextureFormat.ARGBFloat, true, ref updateFlag);
            isGlobalTexturesDirty = updateFlag || isGlobalTexturesDirty;

            if (isGlobalTexturesDirty || isCameraDirty || forceTextureUpdate)
            {
                if (isGlobalTexturesDirty || forceTextureUpdate)
                {
                    foreach (var camera in CameraResourcesMap)
                    {
                        camera.Value.isDirty = true;
                    }

                    CurrentTextureResolution = textureResolution;
                }

                CameraResourcesMap[cam].isDirty = false;

                var registerRenderResourcesBridgeParams = new RegisterRenderResourcesBridgeParams();
                registerRenderResourcesBridgeParams.ParticleSprites = MakeTextureNativeBridge(EmittersSpriteTexture);
                registerRenderResourcesBridgeParams.Depth = MakeTextureNativeBridge(DepthTexture);
                registerRenderResourcesBridgeParams.ParticlesRT = MakeTextureNativeBridge(ParticlesRT);
                registerRenderResourcesBridgeParams.VisualizeSDFTarget = MakeTextureNativeBridge(VisualizeSDFTarget);

                IntPtr nativeRegisterRenderResourcesBridgeParams =
                    Marshal.AllocHGlobal(Marshal.SizeOf(registerRenderResourcesBridgeParams));
                Marshal.StructureToPtr(registerRenderResourcesBridgeParams, nativeRegisterRenderResourcesBridgeParams,
                                       true);
                solverCommandBuffer.Clear();
                SmokeAndFireBridge.SubmitInstanceEvent(solverCommandBuffer, CurrentInstanceID,
                                                       SmokeAndFireBridge.EventID.RegisterRenderResources,
                                                       nativeRegisterRenderResourcesBridgeParams);
                Graphics.ExecuteCommandBuffer(solverCommandBuffer);

                toFreeOnExit.Add(nativeRegisterRenderResourcesBridgeParams);
                forceTextureUpdate = false;
            }

            return isGlobalTexturesDirty || isCameraDirty;
        }
#endregion

#region MonoBehaviour interface
        private void OnEnable()
        {
            SetupScriptableRenderComponents();

            AllInstances?.Add(this);

#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                return;
            }
#endif

            AddToStatReporter();
            InitializeSimulation();
        }

        private void Update()
        {
            if (!Initialized)
            {
                return;
            }

            ZibraSmokeAndFireGPUGarbageCollector.GCUpdateWrapper();

#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                return;
            }
#endif

            if (LimitFramerate)
            {
                if (MaximumFramerate > 0.0f)
                {
                    timeAccumulation += Time.deltaTime;

                    if (timeAccumulation > 1.0f / MaximumFramerate)
                    {
                        UpdateSimulation();
                        timeAccumulation = 0;
                    }
                }
            }
            else
            {
                UpdateSimulation();
            }

            UpdateReadback();
            RefreshEmitterColorsTexture();
#if ZIBRA_EFFECTS_DEBUG
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan)
            {
                UpdateDebugTimestamps();
            }
#endif
        }

        private void OnApplicationQuit()
        {
            OnDisable();
        }

        private void OnDisable()
        {
            RemoveFromStatReporter();
            ReleaseSimulation();
        }

#if UNITY_EDITOR
        internal void OnValidate()
        {
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            ContainerSize[0] = Math.Max(ContainerSize[0], 1e-3f);
            ContainerSize[1] = Math.Max(ContainerSize[1], 1e-3f);
            ContainerSize[2] = Math.Max(ContainerSize[2], 1e-3f);

            CellSize = Math.Max(ContainerSize.x, Math.Max(ContainerSize.y, ContainerSize.z)) / GridResolution;

            if (GetComponent<ZibraSmokeAndFireMaterialParameters>() == null)
            {
                gameObject.AddComponent<ZibraSmokeAndFireMaterialParameters>();
                UnityEditor.EditorUtility.SetDirty(this);
            }

            if (GetComponent<ZibraSmokeAndFireSolverParameters>() == null)
            {
                gameObject.AddComponent<ZibraSmokeAndFireSolverParameters>();
                UnityEditor.EditorUtility.SetDirty(this);
            }

            if (GetComponent<ZibraManipulatorManager>() == null)
            {
                gameObject.AddComponent<ZibraManipulatorManager>();
                UnityEditor.EditorUtility.SetDirty(this);
            }

            ValidateManipulators();
        }

        void OnDrawGizmosInternal(bool isSelected)
        {
            Gizmos.color = Color.yellow;
            if (!isSelected)
            {
                Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, Gizmos.color.a * 0.5f);
            }
            Gizmos.DrawWireCube(transform.position, ContainerSize);

            Gizmos.color = Color.cyan;
            if (!isSelected)
            {
                Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, Gizmos.color.a * 0.5f);
            }

            Vector3 voxelSize =
                new Vector3(ContainerSize.x / GridSize.x, ContainerSize.y / GridSize.y, ContainerSize.z / GridSize.z);
            const int GizmosVoxelCubeSize = 2;
            for (int i = -GizmosVoxelCubeSize; i <= GizmosVoxelCubeSize; i++)
                for (int j = -GizmosVoxelCubeSize; j <= GizmosVoxelCubeSize; j++)
                    for (int k = -GizmosVoxelCubeSize; k <= GizmosVoxelCubeSize; k++)
                        Gizmos.DrawWireCube(transform.position +
                                                new Vector3(i * voxelSize.x, j * voxelSize.y, k * voxelSize.z),
                                            voxelSize);
        }

        private void OnDrawGizmosSelected()
        {
            OnDrawGizmosInternal(true);
        }

        private void OnDrawGizmos()
        {
            OnDrawGizmosInternal(false);
        }
#endif
#endregion

#region Misc
        private Vector2Int GetRequiredTextureResolution()
        {
            if (CamRenderResolutions.Count == 0)
                Debug.Log("camRenderResolutions dictionary was empty when GetRequiredTextureResolution was called.");

            Vector2Int result = new Vector2Int(0, 0);
            foreach (var item in CamRenderResolutions)
            {
                result = Vector2Int.Max(result, item.Value);
            }

            return result;
        }

        private void UpdateCameraList()
        {
            List<Camera> toRemove = new List<Camera>();
            foreach (var camResource in CameraResourcesMap)
            {
                if (camResource.Key == null ||
                    (!camResource.Key.isActiveAndEnabled && camResource.Key.cameraType != CameraType.SceneView))
                {
                    toRemove.Add(camResource.Key);
                    continue;
                }
            }
        }

        private Vector3Int LODGridSize(Vector3Int size, int downscale)
        {
            return new Vector3Int(size.x / downscale, size.y / downscale, size.z / downscale);
        }

        private Vector3Int PressureGridSize(Vector3Int size, int downscale)
        {
            return new Vector3Int(size.x / downscale + 1, size.y / downscale + 1, size.z / downscale + 1);
        }

        private int IntDivCeil(int a, int b)
        {
            return (a + b - 1) / b;
        }

        private Vector3 GetLightColor(Light light)
        {
            Vector3 lightColor = new Vector3(light.color.r, light.color.g, light.color.b);
#if UNITY_PIPELINE_HDRP
            var lightData = light.GetComponent<HDAdditionalLightData>();
            if (lightData != null)
            {
                float intensityHDRP = lightData.intensity;
                return 0.03f * lightColor * intensityHDRP;
            }
#endif
            float intensity = light.intensity;
            return lightColor * intensity;
        }

        private int GetLights(ref Vector4[] lightColors, ref Vector4[] lightPositions, float brightness = 1.0f)
        {
            int lightCount = 0;
            for (int i = 0; i < Lights.Count; i++)
            {
                if (Lights[i] == null || !Lights[i].enabled)
                    continue;
                Vector3 color = GetLightColor(Lights[i]);
                Vector3 pos = Lights[i].transform.position;
                lightColors[lightCount] = brightness * new Vector4(color.x, color.y, color.z, 0.0f);
                lightPositions[lightCount] =
                    new Vector4(pos.x, pos.y, pos.z, 1.0f / Mathf.Max(Lights[i].range * Lights[i].range, 0.00001f));
                lightCount++;
                if (lightCount == MAX_LIGHT_COUNT)
                {
                    Debug.Log("Zibra Flames instance: Max light count reached.");
                    break;
                }
            }
            return lightCount;
        }

        private Matrix4x4 CalculateEyeRayCameraCoeficients(Camera cam)
        {
            float fovTan = Mathf.Tan(cam.GetGateFittedFieldOfView() * 0.5f * Mathf.Deg2Rad);
            if (cam.orthographic)
            {
                fovTan = 0.0f;
            }
            Vector3 r = cam.transform.right * cam.aspect * fovTan;
            Vector3 u = -cam.transform.up * fovTan;
            Vector3 v = cam.transform.forward;

            return new Matrix4x4(new Vector4(r.x, r.y, r.z, 0.0f), new Vector4(u.x, u.y, u.z, 0.0f),
                                 new Vector4(v.x, v.y, v.z, 0.0f), new Vector4(0.0f, 0.0f, 0.0f, 0.0f))
                .transpose;
        }

        void ValidateManipulators()
        {
            if (Manipulators != null)
            {
                HashSet<Manipulator> manipulatorsSet = new HashSet<Manipulator>(Manipulators);
                manipulatorsSet.Remove(null);
                Manipulators = new List<Manipulator>(manipulatorsSet);
                Manipulators.Sort(new ManipulatorCompare());
            }
        }
        private void ForceCloseCommandEncoder(CommandBuffer cmdList)
        {
#if UNITY_EDITOR_OSX || (!UNITY_EDITOR && UNITY_STANDALONE_OSX) || (!UNITY_EDITOR && UNITY_IOS)
            // Unity bug workaround
            // For whatever reason, Unity sometimes doesn't close command encoder when we request it from native plugin
            // So when we try to start our command encoder with active encoder already present it leads to crash
            // This happens when scene have Terrain (I still have no idea why)
            // So we force change command encoder like that, and this one closes gracefully
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal)
            {
                cmdList.DispatchCompute(MaterialParameters.NoOpCompute, 0, 1, 1, 1);
            }
#endif
        }

        void AddToStatReporter()
        {
            StatReporterCollection.Add(this);
        }

        void RemoveFromStatReporter()
        {
            StatReporterCollection.Remove(this);
        }

        public override void SetTime(float time)
        {
            // NOOP
            // Can't rewind or fast forward real time simulation
        }

        public override void SetFadeCoefficient(float fadeCoefficient)
        {
            MaterialParameters.FadeCoefficient = fadeCoefficient;
        }

        public override void StartPlayback(ControlBehaviour controller)
        {
            RunRendering = true;
            RunSimulation = true;
            MaximumFramerate = controller.FrameRate;
        }

        public override void StopPlayback()
        {
            RunRendering = false;
            RunSimulation = false;
        }
#endregion
#endregion
    }
}
