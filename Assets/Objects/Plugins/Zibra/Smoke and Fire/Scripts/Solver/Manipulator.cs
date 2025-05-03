using com.zibra.smoke_and_fire.Solver;
using System;
using System.Collections.Generic;
using com.zibra.common.SDFObjects;
using UnityEngine;

namespace com.zibra.smoke_and_fire.Manipulators
{
    /// <summary>
    ///     Comparer used for sorting manipulators.
    /// </summary>
    public class ManipulatorCompare : Comparer<Manipulator>
    {
        /// <summary>
        ///     Compares 2 manipulator, first by their type, then by their hash code.
        /// </summary>
        /// <remarks>
        ///     Disabled manipulators are being placed to the end of the list.
        /// </remarks>
        public override int Compare(Manipulator x, Manipulator y)
        {
            int result = x.GetManipulatorType().CompareTo(y.GetManipulatorType());
            if (result != 0)
            {
                return result;
            }

            // Can't change order of particle emitters since emitted particles save index of used emitter
            if (x.isActiveAndEnabled != y.isActiveAndEnabled && x is not ZibraParticleEmitter)
            {
                return y.isActiveAndEnabled.CompareTo(x.isActiveAndEnabled);
            }
            else if (x is ZibraSmokeAndFireForceField || x is ZibraSmokeAndFireEmitter) // We assume x and y have same type here
            {
                var xSDF = x.GetComponent<SDFObject>();
                var ySDF = y.GetComponent<SDFObject>();
                int res = ySDF.GetSDFType().CompareTo(xSDF.GetSDFType());
                if(res != 0)
                {
                    return res;
                }
            }

            return x.GetHashCode().CompareTo(y.GetHashCode());
        }
    }

    /// <summary>
    ///     Base class for fluid manipulator.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         It doesn't execute anything by itself, it is used by <see cref="ZibraSmokeAndFire"/> instead.
    ///     </para>
    ///     <para>
    ///         Each manipulator needs to have shape.
    ///         Shape is definited by adding SDF component to same GameObject.
    ///     </para>
    ///     <para>
    ///         Do not try to make custom manipulator.
    ///         Each and every type of manipulator is heavily tied to logic inside the native plugin.
    ///         And since you can't change native plugin, you can't add new manipulators.
    ///     </para>
    ///     <para>
    ///         Each manipulator needs to be added to list of manipulators in liquid.
    ///         Otherwise it won't do anything.
    ///         This is needed since you may have multiple simulations with separate manipulators.
    ///     </para>
    /// </remarks>
    [ExecuteInEditMode]
    abstract public class Manipulator : com.zibra.common.Manipulators.Manipulator
    {
#region Public Interface
        /// <summary>
        ///     List of all enabled manipulators.
        /// </summary>
        public static readonly List<Manipulator> AllManipulators = new List<Manipulator>();

        /// <summary>
        ///     Manipulator types.
        /// </summary>
        public enum ManipulatorType
        {
            None,
            Emitter,
            Void,
            ForceField,
            AnalyticCollider,
            NeuralCollider,
            GroupCollider,
            Detector,
            SpeciesModifier,
            EffectParticleEmitter,
            TextureEmitter,
            TypeNum
        }

        /// <summary>
        ///     Returns the manipulator type.
        /// </summary>
        virtual public ManipulatorType GetManipulatorType()
        {
            return ManipulatorType.None;
        }

#if UNITY_EDITOR
        /// <summary>
        ///     (Editor only) Event that is triggered when state of manipulator changes to trigger update of custom
        ///     editor.
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
#region Implementation details
        internal class SimulationData
        {
            private const int MAX_ADDITIONAL_DATA = 3; 
            private List<Vector4> AdditionalData;

            public SimulationData()
            {
                AdditionalData = new List<Vector4>(MAX_ADDITIONAL_DATA);
                for (int i = 0; i < MAX_ADDITIONAL_DATA; i++)
                {
                    AdditionalData.Add(Vector4.zero);
                }
            }

            public SimulationData(Vector4[] additionalData)
            {
                if (additionalData.Length > MAX_ADDITIONAL_DATA)
                {
                    throw new Exception($"Too many additional data! Max additional data is {MAX_ADDITIONAL_DATA}.");
                }
                AdditionalData = new List<Vector4>(MAX_ADDITIONAL_DATA);
                for (int i = 0; i < MAX_ADDITIONAL_DATA; i++)
                {
                    AdditionalData.Add(i<additionalData.Length ? additionalData[i] : Vector4.zero);
                }
            }
            
            public Vector4 GetAdditionalData(int index)
            {
                if (index > MAX_ADDITIONAL_DATA)
                {
                    throw new Exception($"Max additional data is {MAX_ADDITIONAL_DATA}.");
                }
                return AdditionalData[index];
            }
        }

        internal abstract SimulationData GetSimulationData();

        private void OnEnable()
        {
            if (!AllManipulators?.Contains(this) ?? false)
            {
                AllManipulators.Add(this);
            }
        }

        private void OnDisable()
        {
            if (AllManipulators?.Contains(this) ?? false)
            {
                AllManipulators.Remove(this);
            }
        }

#if UNITY_EDITOR
        private void OnDestroy()
        {
            foreach (var instance in ZibraSmokeAndFire.AllInstances)
            {
                if (instance != null)
                {
                    instance.RemoveManipulator(this);
                }
            }
        }
#endif
#endregion
    }
}
