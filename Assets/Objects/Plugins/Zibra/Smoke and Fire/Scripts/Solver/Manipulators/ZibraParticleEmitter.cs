using System;
using UnityEngine;
using com.zibra.common;

namespace com.zibra.smoke_and_fire.Manipulators
{
    /// <summary>
    ///     Manipulator that emits Effect Particles.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Effects Particles are purely visual, they don't affect main simulation.
    ///     </para>
    ///     <para>
    ///         Changing its parameters affects previously emitted particles.
    ///     </para>
    /// </remarks>
    [AddComponentMenu(Effects.SmokeAndFireComponentMenuPath + "Zibra Smoke & Fire Particle Emitter")]
    [DisallowMultipleComponent]
    public class ZibraParticleEmitter : Manipulator
    {
#region Public Interface
        /// <summary>
        ///     See <see cref="RenderMode"/>.
        /// </summary>
        public enum RenderingMode
        {
            Default,
            Sprite
        }

        /// <summary>
        ///     Whether the particle itself is going to be rendered.
        /// </summary>
        [Tooltip("Whether the particle itself is going to be rendered.")]
        public bool Renderable = true;
        
        /// <summary>
        ///     Number of Effect Particles emitted per simulation iteration.
        /// </summary>
        [Min(0)]
        [Tooltip("Number of Effect Particles emitted per simulation iteration.")]
        public float EmitedParticlesPerFrame = 1.0f;

        /// <summary>
        ///     Define how particles are going to be rendered.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Depending on render mode different parameters will be used for rendering.
        ///     </para>
        ///     <para>
        ///         Only used when <see cref="Renderable"/> is enabled.
        ///     </para>
        /// </remarks>
        [Tooltip("Define how particles are going to be rendered.")]
        public RenderingMode RenderMode = RenderingMode.Default;

        /// <summary>
        ///     Sprite that will be used to render particles.
        /// </summary>
        /// <remarks>
        ///     Only used in Sprite render mode.
        /// </remarks>
        [Tooltip("Sprite that will be used to render particles.")]
        public Texture2D ParticleSprite;

        /// <summary>
        ///     Curve that defines size depending on the particle's lifetime
        /// </summary>
        [Tooltip("Curve that defines size depending on the particle's lifetime")]
        public AnimationCurve ParticleSize = AnimationCurve.Linear(0, 0.1f, 1, 0.1f);

        /// <summary>
        ///     Curve that defines color depending on the particle's lifetime.
        /// </summary>
        /// <remarks>
        ///     Only used in Default render mode.
        /// </remarks>
        [Tooltip("Curve that defines color depending on the particle's lifetime.")]
        [GradientUsageAttribute(true)]
        public Gradient ParticleColor;

        /// <summary>
        ///     Scale for motion blur of a particle.
        /// </summary>
        /// <remarks>
        ///     Only used in Default render mode.
        /// </remarks>
        [Tooltip("Scale for motion blur of a particle.")]
        [Range(0, MAX_MOTION_BLUR)]
        public float ParticleMotionBlur = 1.0f;

        /// <summary>
        ///     Particle’s relative brightness.
        /// </summary>
        [Tooltip("Particle’s relative brightness.")]
        [Range(0, MAX_BRIGHTNESS)]
        public float ParticleBrightness = 1.0f;

        /// <summary>
        ///     Define oscillation magnitude of particle color with time.
        /// </summary>
        /// <remarks>
        ///     Only used in Default render mode.
        /// </remarks>
        [Tooltip("Define oscillation magnitude of particle color with time.")]
        [Range(0, 1f)]
        public float ParticleColorOscillationAmount = 0;

        /// <summary>
        ///     Define oscillation frequency of particle color with time.
        /// </summary>
        /// <remarks>
        ///     Only used in Default render mode.
        /// </remarks>
        [Tooltip("Define oscillation frequency of particle color with time.")]
        [Range(0, MAX_COLOR_OSCILLATION_FREQUENCY)]
        public float ParticleColorOscillationFrequency = 0;

        /// <summary>
        ///     Define oscillation magnitude of particle size with time.
        /// </summary>
        /// <remarks>
        ///     Only used in Default render mode.
        /// </remarks>
        [Tooltip("Define oscillation magnitude of particle size with time.")]
        [Range(0, 1f)]
        public float ParticleSizeOscillationAmount = 0;

        /// <summary>
        ///     Define oscillation frequency of particle size with time.
        /// </summary>
        /// <remarks>
        ///     Only used in Default render mode.
        /// </remarks>
        [Tooltip("Define oscillation frequency of particle size with time.")]
        [Range(0, MAX_SIZE_OSCILLATION_FREQUENCY)]
        public float ParticleSizeOscillationFrequency = 0;

        /// <summary>
        ///     When enabled, particles are not affected by simulation, and instead emitted with initial impulse and affected by gravity.
        /// </summary>
        [Tooltip("When enabled, particles are not affected by simulation, and instead emitted with initial impulse and affected by gravity.")]
        public bool AddImpulse = false;

        /// <summary>
        ///     Impulse applied to particle on emission.
        /// </summary>
        /// <remarks>
        ///     Only used in when <see cref="AddImpulse"/> is enabled.
        /// </remarks>
        [Tooltip("Impulse applied to particle on emission.")]
        public Vector3 ImpulseDirection = Vector3.up;

        /// <summary>
        ///     Maximum random deviation applied to Impulse Direction.
        /// </summary>
        /// <remarks>
        ///     Only used in when <see cref="AddImpulse"/> is enabled.
        /// </remarks>
        [Tooltip("Maximum random deviation applied to Impulse Direction.")]
        [Range(0, MAX_SPREAD_ANGLE)]
        public float ImpulseSpreadAngle = 45;

        /// <summary>
        ///     Mass of the particle for gravity acceleration calculation.
        /// </summary>
        /// <remarks>
        ///     Only used in when <see cref="AddImpulse"/> is enabled.
        /// </remarks>
        [Tooltip("Mass of the particle for gravity acceleration calculation.")]
        [Range(0, MAX_PARTICLE_MASS)]
        public float ParticleMass = 1f;

        /// <summary>
        ///     Maximum random deviation applied to Particle Mass.
        /// </summary>
        /// <remarks>
        ///     Only used in when <see cref="AddImpulse"/> is enabled.
        /// </remarks>
        [Tooltip("Maximum random deviation applied to Particle Mass.")]
        [Range(0, 1)]
        public float MassRandomize = 0.25f;

        /// <summary>
        ///     Initial velocity of the particle.
        /// </summary>
        /// <remarks>
        ///     Only used in when <see cref="AddImpulse"/> is enabled.
        /// </remarks>
        [Tooltip("Initial velocity of the particle.")]
        [Range(0, MAX_IMPULSE_INITIAL_VELOCITY)]
        public float ImpulseInitialVelocity = 20f;


        /// <summary>
        ///     When enabled, particles will be able to emit smoke/fuel.
        /// </summary>
        [Tooltip("When enabled, particles will be able to emit smoke/fuel.")]
        public bool EmissionEnabled = false;


        /// <summary>
        ///     Color of emitted smoke.
        /// </summary>
        /// <remarks>
        ///     Only used in when <see cref="EmissionEnabled"/> is enabled.
        /// </remarks>
        [Tooltip("Color of emitted smoke.")]
        public Color SmokeColor = Color.white;

        /// <summary>
        ///     Gradient that defines smoke density depending on the particle's lifetime.
        /// </summary>
        /// <remarks>
        ///     Only used in when <see cref="EmissionEnabled"/> is enabled.
        /// </remarks>
        [Tooltip("Gradient that defines smoke density depending on the particle's lifetime.")]
        public AnimationCurve SmokeDensity = AnimationCurve.Linear(0, 0, 1, 0);

        /// <summary>
        ///     Gradient that defines temperature depending on the particle's lifetime.
        /// </summary>
        /// <remarks>
        ///     Only used in when <see cref="EmissionEnabled"/> is enabled.
        /// </remarks>
        [Tooltip("Gradient that defines temperature depending on the particle's lifetime.")]
        public AnimationCurve Temperature = AnimationCurve.Linear(0, 0.4f, 1, 0.4f);

        /// <summary>
        ///     Gradient that defines fuel depending on the particle's lifetime.
        /// </summary>
        /// <remarks>
        ///     Only used in when <see cref="EmissionEnabled"/> is enabled.
        /// </remarks>
        [Tooltip("Gradient that defines fuel depending on the particle's lifetime.")]
        public AnimationCurve Fuel = AnimationCurve.Linear(0, 0.2f, 1, 0.2f);
        
        override public ManipulatorType GetManipulatorType()
        {
            return ManipulatorType.EffectParticleEmitter;
        }

#if UNITY_EDITOR
        public override Color GetGizmosColor()
        {
            return Color.magenta;
        }
#endif
#endregion
#region Implementation details
        private const float MAX_MOTION_BLUR = 2f;
        private const float MAX_BRIGHTNESS = 10f;
        private const float MAX_COLOR_OSCILLATION_FREQUENCY = 100f;
        private const float MAX_SIZE_OSCILLATION_FREQUENCY = 500f;
        private const float MAX_SPREAD_ANGLE = 180f;
        private const float MAX_PARTICLE_MASS = 50f;
        private const float MAX_IMPULSE_INITIAL_VELOCITY = 100f;
        
        internal override SimulationData GetSimulationData()
        {
            float emittedParticlesPerFrame = isActiveAndEnabled ? EmitedParticlesPerFrame : 0.0f;
            Vector2 packedEmitterMode = new Vector2(emittedParticlesPerFrame,
                                                    PackFloats(AddImpulse ? 1 : 0, EmissionEnabled ? 1f : 0f));
            Vector2 packedRenderingProps = new Vector2(PackFloats(Renderable ? 1 : 0, (int) RenderMode),
                                                       PackFloats(Remap(ParticleMotionBlur, 0, MAX_MOTION_BLUR), Remap(ParticleBrightness, 0, MAX_BRIGHTNESS)));
            Vector2 packedOscillations = new Vector2(PackFloats(ParticleColorOscillationAmount, Remap(ParticleColorOscillationFrequency, 0, MAX_COLOR_OSCILLATION_FREQUENCY)),
                                                     PackFloats(ParticleSizeOscillationAmount, Remap(ParticleSizeOscillationFrequency, 0, MAX_SIZE_OSCILLATION_FREQUENCY)));
            Vector3 normalizedImpulseDirection = ImpulseDirection.normalized;
            Vector2 packedImpulse = new Vector2(PackFloats(Remap(normalizedImpulseDirection.x, -1, 1), Remap(normalizedImpulseDirection.y, -1, 1)),
                                                PackFloats(Remap(normalizedImpulseDirection.z, -1, 1), Remap(ImpulseSpreadAngle, 0, MAX_SPREAD_ANGLE)));
            Vector2 packedMass = new Vector2(PackFloats(Remap(ParticleMass, 0, MAX_PARTICLE_MASS), MassRandomize),
                                             PackFloats(Remap(ImpulseInitialVelocity, 0, MAX_IMPULSE_INITIAL_VELOCITY), 0));
            Vector2 packedSmokeColor = new Vector2(PackFloats(SmokeColor.r, SmokeColor.g), PackFloats(SmokeColor.b, 0));

            Vector4[] additionalData =
            {
                new (packedEmitterMode.x, packedEmitterMode.y, packedRenderingProps.x, packedRenderingProps.y),
                new (packedOscillations.x, packedOscillations.y, packedImpulse.x, packedImpulse.y),
                new (packedMass.x, packedMass.y, packedSmokeColor.x, packedSmokeColor.y)
            };
            return new SimulationData(additionalData);
        }
        
        private float Remap(float val, float min, float max)
        {
            return (val - min) / (max - min);
        }
        
        private static float PackFloats(float a, float b)
        {
            uint aScaled = (uint)(a * 65535.0f);
            uint bScaled = (uint)(b * 65535.0f);
            uint packed = (aScaled << 16) | bScaled;
            byte[] bytes = BitConverter.GetBytes(packed);
            return BitConverter.ToSingle(bytes, 0);
        }
#endregion
    }
}