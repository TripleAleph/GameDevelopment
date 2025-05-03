using System.Linq;
using com.zibra.common.Analytics;
using com.zibra.common.SDFObjects;
using com.zibra.smoke_and_fire.Editor.Solver;
using UnityEditor;
using UnityEngine;
using static com.zibra.smoke_and_fire.Manipulators.ZibraParticleEmitter;

namespace com.zibra.smoke_and_fire.Manipulators.Editors
{
    [CustomEditor(typeof(ZibraParticleEmitter))]
    [CanEditMultipleObjects]
    internal class ZibraParticlesEmitterEditor : ZibraSmokeAndFireManipulatorEditor
    {
        private ZibraParticleEmitter[] EmitterInstances;

        private SerializedProperty EmitedParticlesPerFrame;
        private SerializedProperty RenderMode;
        private SerializedProperty ParticleSprite;
        private SerializedProperty ParticleSize;
        private SerializedProperty ParticleColor;
        private SerializedProperty ParticleMotionBlur;
        private SerializedProperty ParticleBrightness;
        private SerializedProperty ParticleColorOscillationAmount;
        private SerializedProperty ParticleColorOscillationFrequency;
        private SerializedProperty ParticleSizeOscillationAmount;
        private SerializedProperty ParticleSizeOscillationFrequency;
        private SerializedProperty AddImpulse;
        private SerializedProperty ImpulseDirection;
        private SerializedProperty ImpulseSpreadAngle;
        private SerializedProperty ParticleMass;
        private SerializedProperty MassRandomize;
        private SerializedProperty ImpulseInitialVelocity;
        private SerializedProperty EmissionEnabled;
        private SerializedProperty SmokeColor;
        private SerializedProperty SmokeDensity;
        private SerializedProperty Temperature;
        private SerializedProperty Fuel;
        private SerializedProperty Renderable;
        
        private static bool ShowColorOscillationOptions = false;
        private static bool ShowSizeOscillationOptions = false;
        private static bool ShowRenderingOptions = true;
        private static bool ShowPhysicsOptions = false;
        private static bool ShowEmissionOptions = false;

        public override void OnInspectorGUI()
        {
            bool hasTerrainSDF = false;

            foreach (var instance in EmitterInstances)
            {
                if (instance.GetComponent<TerrainSDF>() != null)
                {
                    hasTerrainSDF = true;
                    break;
                }
            }

            if (hasTerrainSDF)
            {
                EditorGUILayout.HelpBox("TerrainSDF can't be used with Smoke & Fire", MessageType.Error);

                if (GUILayout.Button(EmitterInstances.Length > 1 ? "Remove TerrainSDFs" : "Remove TerrainSDF"))
                {
                    foreach (var instance in EmitterInstances)
                    {
                        TerrainSDF terrainSDF = instance.GetComponent<TerrainSDF>();
                        if (terrainSDF != null)
                        {
                            DestroyImmediate(terrainSDF);
                        }
                    }
                }
            }

            bool missingSDF = false;

            foreach (var instance in EmitterInstances)
            {
                SDFObject sdf = instance.GetComponent<SDFObject>();
                if (sdf == null)
                {
                    missingSDF = true;
                    continue;
                }
            }

            if (missingSDF)
            {
                if (EmitterInstances.Length > 1)
                    EditorGUILayout.HelpBox("At least 1 particle emitter missing shape. Please add SDF Component.",
                                            MessageType.Error);
                else
                    EditorGUILayout.HelpBox("Missing particle emitter shape. Please add SDF Component.",
                                            MessageType.Error);
                if (GUILayout.Button(EmitterInstances.Length > 1 ? "Add Analytic SDFs" : "Add Analytic SDF"))
                {
                    foreach (var instance in EmitterInstances)
                    {
                        if (instance.GetComponent<SDFObject>() == null)
                        {
                            Undo.AddComponent<AnalyticSDF>(instance.gameObject);
                        }
                    }
                }
                if (GUILayout.Button(EmitterInstances.Length > 1 ? "Add Neural SDFs" : "Add Neural SDF"))
                {
                    foreach (var instance in EmitterInstances)
                    {
                        if (instance.GetComponent<SDFObject>() == null)
                        {
                            Undo.AddComponent<NeuralSDF>(instance.gameObject);
                        }
                    }
                }
                if (GUILayout.Button(EmitterInstances.Length > 1 ? "Add Skinned Mesh SDFs" : "Add Skinned Mesh SDF"))
                {
                    foreach (var instance in EmitterInstances)
                    {
                        if (instance.GetComponent<SDFObject>() == null)
                        {
                            Undo.AddComponent<SkinnedMeshSDF>(instance.gameObject);
                        }
                    }
                }
                GUILayout.Space(5);
            }

            serializedObject.Update();

            EmitedParticlesPerFrame.floatValue = Mathf.Round(EmitedParticlesPerFrame.floatValue);
            EditorGUILayout.PropertyField(EmitedParticlesPerFrame, new GUIContent("Emitted particles per frame"));

            ShowRenderingOptions = EditorGUILayout.Foldout(ShowRenderingOptions, "Render");
            if (ShowRenderingOptions)
            {
                EditorGUILayout.PropertyField(Renderable);
                EditorGUI.BeginDisabledGroup(EmitterInstances.All(e => !e.Renderable));
                EditorGUILayout.PropertyField(RenderMode);
                if (RenderMode.hasMultipleDifferentValues ||
                    RenderMode.enumValueIndex == (int)ZibraParticleEmitter.RenderingMode.Default)
                {
                    EditorGUILayout.PropertyField(ParticleColor);
                    EditorGUILayout.PropertyField(ParticleMotionBlur);
                    ShowColorOscillationOptions = EditorGUILayout.Foldout(ShowColorOscillationOptions, "Color oscillation");
                    if (ShowColorOscillationOptions)
                    {
                        EditorGUILayout.PropertyField(ParticleColorOscillationAmount, new GUIContent("Amount"));
                        EditorGUILayout.PropertyField(ParticleColorOscillationFrequency, new GUIContent("Frequency"));
                    }
                }
                if (RenderMode.hasMultipleDifferentValues ||
                    RenderMode.enumValueIndex == (int)ZibraParticleEmitter.RenderingMode.Sprite)
                {
                    EditorGUILayout.PropertyField(ParticleSprite);
                }

                EditorGUILayout.PropertyField(ParticleBrightness);
                EditorGUILayout.PropertyField(ParticleSize);

                ShowSizeOscillationOptions = EditorGUILayout.Foldout(ShowSizeOscillationOptions, "Size oscillation");
                if (ShowSizeOscillationOptions)
                {
                    EditorGUILayout.PropertyField(ParticleSizeOscillationAmount);
                    EditorGUILayout.PropertyField(ParticleSizeOscillationFrequency);
                }
                EditorGUI.EndDisabledGroup();
            }
            ShowPhysicsOptions = EditorGUILayout.Foldout(ShowPhysicsOptions, "Physics");
            if (ShowPhysicsOptions)
            {
                EditorGUILayout.PropertyField(AddImpulse);
                EditorGUI.BeginDisabledGroup(EmitterInstances.All(e => !e.AddImpulse));
                EditorGUILayout.PropertyField(ImpulseDirection);
                EditorGUILayout.PropertyField(ImpulseSpreadAngle);
                EditorGUILayout.PropertyField(ParticleMass);
                EditorGUILayout.PropertyField(MassRandomize);
                EditorGUILayout.PropertyField(ImpulseInitialVelocity);
                EditorGUI.EndDisabledGroup();
            }

            ShowEmissionOptions = EditorGUILayout.Foldout(ShowEmissionOptions, "Emission");
            if (ShowEmissionOptions)
            {
                EditorGUILayout.PropertyField(EmissionEnabled);
                EditorGUI.BeginDisabledGroup(EmitterInstances.All(e => !e.EmissionEnabled));
                EditorGUILayout.PropertyField(SmokeColor);
                EditorGUILayout.PropertyField(SmokeDensity);
                EditorGUILayout.PropertyField(Temperature);
                EditorGUILayout.PropertyField(Fuel);
                EditorGUI.EndDisabledGroup();
            }

            serializedObject.ApplyModifiedProperties();
        }

        // clang-format doesn't parse code with new keyword properly
        // clang-format off

        protected new void OnEnable()
        {
            base.OnEnable();

            EmitterInstances = new ZibraParticleEmitter[targets.Length];

            for (int i = 0; i < targets.Length; i++)
            {
                EmitterInstances[i] = targets[i] as ZibraParticleEmitter;
            }

            EmitedParticlesPerFrame = serializedObject.FindProperty("EmitedParticlesPerFrame");
            RenderMode = serializedObject.FindProperty("RenderMode");
            ParticleSprite = serializedObject.FindProperty("ParticleSprite");
            ParticleSize = serializedObject.FindProperty("ParticleSize");
            ParticleColor = serializedObject.FindProperty("ParticleColor");
            ParticleMotionBlur = serializedObject.FindProperty("ParticleMotionBlur");
            ParticleBrightness = serializedObject.FindProperty("ParticleBrightness");
            ParticleColorOscillationAmount = serializedObject.FindProperty("ParticleColorOscillationAmount");
            ParticleColorOscillationFrequency = serializedObject.FindProperty("ParticleColorOscillationFrequency");
            ParticleSizeOscillationAmount = serializedObject.FindProperty("ParticleSizeOscillationAmount");
            ParticleSizeOscillationFrequency = serializedObject.FindProperty("ParticleSizeOscillationFrequency");

            Renderable = serializedObject.FindProperty("Renderable");
            AddImpulse = serializedObject.FindProperty("AddImpulse");
            ImpulseDirection = serializedObject.FindProperty("ImpulseDirection");
            ImpulseSpreadAngle = serializedObject.FindProperty("ImpulseSpreadAngle");
            ParticleMass = serializedObject.FindProperty("ParticleMass");
            MassRandomize = serializedObject.FindProperty("MassRandomize");
            ImpulseInitialVelocity = serializedObject.FindProperty("ImpulseInitialVelocity");
            
            EmissionEnabled = serializedObject.FindProperty("EmissionEnabled");
            SmokeColor = serializedObject.FindProperty("SmokeColor");
            SmokeDensity = serializedObject.FindProperty("SmokeDensity");
            Temperature = serializedObject.FindProperty("Temperature");
            Fuel = serializedObject.FindProperty("Fuel");
        }
    }
}