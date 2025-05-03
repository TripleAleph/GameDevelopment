using com.zibra.common.Analytics;
using com.zibra.smoke_and_fire.DataStructures;
using com.zibra.smoke_and_fire.Solver;
using UnityEditor;
using UnityEngine;

namespace com.zibra.liquid.Editor.Solver
{
    [CustomEditor(typeof(ZibraSmokeAndFireSolverParameters))]
    [CanEditMultipleObjects]
    internal class ZibraLiquidSolverParametersEditor : UnityEditor.Editor
    {
        private SerializedProperty Gravity;
        private SerializedProperty SmokeBuoyancy;
        private SerializedProperty HeatBuoyancy;
        private SerializedProperty TempThreshold;
        private SerializedProperty HeatEmission;
        private SerializedProperty ReactionSpeed;
        private SerializedProperty MaximumVelocity;
        private SerializedProperty MinimumVelocity;
        private SerializedProperty Sharpen;
        private SerializedProperty SharpenThreshold;
        private SerializedProperty ColorDecay;
        private SerializedProperty VelocityDecay;
        private SerializedProperty PressureReuse;
        private SerializedProperty PressureProjection;
        private SerializedProperty PressureSolveIterations;
        private SerializedProperty PressureReuseClamp;
        private SerializedProperty PressureClamp;
        private SerializedProperty LOD0Iterations;
        private SerializedProperty LOD1Iterations;
        private SerializedProperty LOD2Iterations;
        private SerializedProperty PreIterations;
        private SerializedProperty MainOverrelax;
        private SerializedProperty EdgeOverrelax;

        private void OnEnable()
        {
            Gravity = serializedObject.FindProperty("Gravity");
            SmokeBuoyancy = serializedObject.FindProperty("SmokeBuoyancy");
            HeatBuoyancy = serializedObject.FindProperty("HeatBuoyancy");
            TempThreshold = serializedObject.FindProperty("TempThreshold");
            HeatEmission = serializedObject.FindProperty("HeatEmission");
            ReactionSpeed = serializedObject.FindProperty("ReactionSpeed");
            MaximumVelocity = serializedObject.FindProperty("MaximumVelocity");
            MinimumVelocity = serializedObject.FindProperty("MinimumVelocity");
            Sharpen = serializedObject.FindProperty("Sharpen");
            SharpenThreshold = serializedObject.FindProperty("SharpenThreshold");
            ColorDecay = serializedObject.FindProperty("ColorDecay");
            VelocityDecay = serializedObject.FindProperty("VelocityDecay");
            PressureReuse = serializedObject.FindProperty("PressureReuse");
            PressureProjection = serializedObject.FindProperty("PressureProjection");
            PressureSolveIterations = serializedObject.FindProperty("PressureSolveIterations");
            PressureReuseClamp = serializedObject.FindProperty("PressureReuseClamp");
            PressureClamp = serializedObject.FindProperty("PressureClamp");
            LOD0Iterations = serializedObject.FindProperty("LOD0Iterations");
            LOD1Iterations = serializedObject.FindProperty("LOD1Iterations");
            LOD2Iterations = serializedObject.FindProperty("LOD2Iterations");
            PreIterations = serializedObject.FindProperty("PreIterations");
            MainOverrelax = serializedObject.FindProperty("MainOverrelax");
            EdgeOverrelax = serializedObject.FindProperty("EdgeOverrelax");
        }

        public bool NeedShowFireParams()
        {
            foreach (var target in targets)
            {
                MonoBehaviour targetComponent = (target as MonoBehaviour);

                ZibraSmokeAndFire smoke = targetComponent.GetComponent<ZibraSmokeAndFire>();

                // It can be null when viewing preset
                if (smoke == null)
                {
                    return true;
                }

                if (smoke.CurrentSimulationMode == ZibraSmokeAndFire.SimulationMode.Fire)
                {
                    return true;
                }
            }

            return false;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(Gravity);
            EditorGUILayout.PropertyField(SmokeBuoyancy);
            if (NeedShowFireParams())
            {
                EditorGUILayout.PropertyField(HeatBuoyancy);
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(TempThreshold);
                EditorGUILayout.PropertyField(HeatEmission);
                EditorGUILayout.PropertyField(ReactionSpeed);
            }
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(MaximumVelocity);
            EditorGUILayout.PropertyField(MinimumVelocity);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(Sharpen);
            EditorGUILayout.PropertyField(SharpenThreshold);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(ColorDecay);
            EditorGUILayout.PropertyField(VelocityDecay);
#if ZIBRA_EFFECTS_DEBUG
            EditorGUILayout.PropertyField(PressureReuse);
            EditorGUILayout.PropertyField(PressureProjection);
            EditorGUILayout.PropertyField(PressureSolveIterations);
            EditorGUILayout.PropertyField(PressureReuseClamp);
            EditorGUILayout.PropertyField(PressureClamp);
            EditorGUILayout.PropertyField(LOD0Iterations);
            EditorGUILayout.PropertyField(LOD1Iterations);
            EditorGUILayout.PropertyField(LOD2Iterations);
            EditorGUILayout.PropertyField(PreIterations);
            EditorGUILayout.PropertyField(MainOverrelax);
            EditorGUILayout.PropertyField(EdgeOverrelax);
#endif

            serializedObject.ApplyModifiedProperties();
        }
    }
}