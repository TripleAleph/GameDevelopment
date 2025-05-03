using System;
using UnityEngine;
using com.zibra.common;

namespace com.zibra.smoke_and_fire.Manipulators
{
    /// <summary>
    ///     Manipulator used for applying force to the simulation.
    /// </summary>
    [AddComponentMenu(Effects.SmokeAndFireComponentMenuPath + "Zibra Smoke & Fire Force Field")]
    public class ZibraSmokeAndFireForceField : Manipulator
    {
#region Public Interface
        /// <summary>
        ///     See <see cref="Type"/>.
        /// </summary>
        public enum ForceFieldType
        {
            Directional,
            Swirl,
            Random
        }

        /// <summary>
        ///     Type of force field, which defines how force will be applied.
        /// </summary>
        [Tooltip("Type of force field, which defines how force will be applied")]
        public ForceFieldType Type = ForceFieldType.Directional;

        /// <summary>
        ///     The strength of the force acting on the volume.
        /// </summary>
        [Tooltip("The strength of the force acting on the volume")]
        [Range(0.0f, 15.0f)]
        public float Strength = 1.0f;

        /// <summary>
        ///     Speed of changing randomness.
        /// </summary>
        /// <remarks>
        ///     Only has effect when Type set to Random.
        /// </remarks>
        [Tooltip("Speed of changing randomness. Only has effect when Type set to Random.")]
        [Range(0.0f, 5.0f)]
        public float Speed = 1.0f;

        /// <summary>
        ///     Size of the random swirls.
        /// </summary>
        /// <remarks>
        ///     Only has effect when Type set to Random.
        /// </remarks>
        [Tooltip("Size of the random swirls. Only has effect when Type set to Random.")]
        [Range(0.0f, 64.0f)]
        public float RandomScale = 16.0f;

        /// <summary>
        ///     Direction for the force. Behaviour depends on <see cref="Type"/>.
        /// </summary>
        /// <remarks>
        ///     Direction is used as follows, depending on <see cref="Type"/>:
        ///     * Directional - Liquid is pushed into specified direction
        ///     * Swirl - Liquid is rotated along specified axis. To reverse rotation, invert direction
        ///     * Random - Unused
        /// </remarks>
        [Tooltip("Direction for the force. Behaviour depends on Type parameter.")]
        public Vector3 ForceDirection = Vector3.up;

        override public ManipulatorType GetManipulatorType()
        {
            return ManipulatorType.ForceField;
        }
#endregion
#region Implementation details
        internal override SimulationData GetSimulationData()
        {
            Vector4 additionalData1 = new Vector4();
            if (Type == ForceFieldType.Random)
            {
                additionalData1.x = RandomScale;
            }
            else
            {
                additionalData1.x = ForceDirection.x;
                additionalData1.y = ForceDirection.y;
                additionalData1.z = ForceDirection.z;
            }
            
            Vector4[] additionalData = { new((int)Type, Strength, Speed, 0.0f), additionalData1 };
            return new SimulationData(additionalData);
        }

#if UNITY_EDITOR
        public override Color GetGizmosColor()
        {
            return new Color(1.0f, 0.55f, 0.0f);
        }
#endif
#endregion
    }
}
