﻿using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering;

namespace WaterSystem
{
    public class WaterSystemFeature : ScriptableRendererFeature
    {
        #region Water Effects Pass

        class WaterFxPass : ScriptableRenderPass
        {
            private const string k_RenderWaterFXTag = "Render Water FX";
            private ProfilingSampler m_WaterFX_Profile = new ProfilingSampler(k_RenderWaterFXTag);
            private readonly ShaderTagId m_WaterFXShaderTag = new ShaderTagId("WaterFX");
            private readonly Color m_ClearColor = new Color(0.0f, 0.5f, 0.5f, 0.5f); // r = foam mask, g = normal.x, b = normal.z, a = displacement
            private FilteringSettings m_FilteringSettings;
            private RTHandle m_WaterFX;

            public WaterFxPass()
            {
                // יצירת RTHandle
                m_WaterFX = RTHandles.Alloc(
                    Vector2.one, // קנה מידה יחסי למסך
                    TextureXR.slices, // מספר פרוסות (לרוב 1 עבור 2D)
                    DepthBits.None, // אין שימוש בעומק
                    GraphicsFormat.R8G8B8A8_UNorm, // פורמט צבע רגיל
                    FilterMode.Bilinear, // שיטת סינון
                    TextureWrapMode.Clamp, // שיטת עיטוף
                    dimension: TextureDimension.Tex2D, // ממד (2D)
                    useDynamicScale: true, // שימוש בקנה מידה דינמי
                    name: "_WaterFXMap" // שם לזיהוי
                );

                // רק אובייקטים שקופים ייכללו
                m_FilteringSettings = new FilteringSettings(RenderQueueRange.transparent);
            }

            // הגדרת המטרה לרינדור ושימוש ב-RTHandle
            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                // אין צורך ב-buffer לעומק
                cameraTextureDescriptor.depthBufferBits = 0;
                // הפחתת הרזולוציה לחצי
                cameraTextureDescriptor.width /= 2;
                cameraTextureDescriptor.height /= 2;
                // פורמט צבע רגיל
                cameraTextureDescriptor.colorFormat = RenderTextureFormat.Default;

                // הגדרת המטרה לרינדור
                ConfigureTarget(m_WaterFX); // שימוש ב-RTHandle ישירות
                ConfigureClear(ClearFlag.Color, m_ClearColor); // ניקוי עם צבע ברירת מחדל
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                CommandBuffer cmd = CommandBufferPool.Get();
                using (new ProfilingScope(cmd, m_WaterFX_Profile)) // מאפשר פרופילינג
                {
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();

                    // יצירת הגדרות לציור
                    var drawSettings = CreateDrawingSettings(m_WaterFXShaderTag, ref renderingData,
                        SortingCriteria.CommonTransparent);

                    // ציור כל הרנדרים העונים לתנאים
                    context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref m_FilteringSettings);
                }
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            public override void OnCameraCleanup(CommandBuffer cmd)
            {
                // שחרור RTHandle
                RTHandles.Release(m_WaterFX);
            }
        }

        #endregion

        #region Caustics Pass

        class WaterCausticsPass : ScriptableRenderPass
        {
            private const string k_RenderWaterCausticsTag = "Render Water Caustics";
            private ProfilingSampler m_WaterCaustics_Profile = new ProfilingSampler(k_RenderWaterCausticsTag);
            public Material WaterCausticMaterial;
            private static Mesh m_mesh;

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var cam = renderingData.cameraData.camera;
                // Stop the pass rendering in the preview or material missing
                if (cam.cameraType == CameraType.Preview || !WaterCausticMaterial)
                    return;

                CommandBuffer cmd = CommandBufferPool.Get();
                using (new ProfilingScope(cmd, m_WaterCaustics_Profile))
                {
                    var sunMatrix = RenderSettings.sun != null
                        ? RenderSettings.sun.transform.localToWorldMatrix
                        : Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(-45f, 45f, 0f), Vector3.one);
                    WaterCausticMaterial.SetMatrix("_MainLightDir", sunMatrix);

                    // יצירת mesh אם אין כזה
                    if (!m_mesh)
                        m_mesh = GenerateCausticsMesh(1000f);

                    // יצירת המטריצה למיקום ה-caustics
                    var position = cam.transform.position;
                    position.y = 0; // TODO: צריך לקרוא ערך גלובלי לגובה המים
                    var matrix = Matrix4x4.TRS(position, Quaternion.identity, Vector3.one);

                    // ציור ה-mesh
                    cmd.DrawMesh(m_mesh, matrix, WaterCausticMaterial, 0, 0);
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
        }

        #endregion

        WaterFxPass m_WaterFxPass;
        WaterCausticsPass m_CausticsPass;

        public WaterSystemSettings settings = new WaterSystemSettings();
        [HideInInspector] [SerializeField] private Shader causticShader;
        [HideInInspector] [SerializeField] private Texture2D causticTexture;

        private Material _causticMaterial;

        private static readonly int SrcBlend = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlend = Shader.PropertyToID("_DstBlend");
        private static readonly int Size = Shader.PropertyToID("_Size");
        private static readonly int CausticTexture = Shader.PropertyToID("_CausticMap");

        public override void Create()
        {
            // WaterFX Pass
            m_WaterFxPass = new WaterFxPass { renderPassEvent = RenderPassEvent.BeforeRenderingOpaques };

            // Caustic Pass
            m_CausticsPass = new WaterCausticsPass();

            causticShader = causticShader ? causticShader : Shader.Find("Hidden/BoatAttack/Caustics");
            if (causticShader == null) return;
            if (_causticMaterial)
            {
                DestroyImmediate(_causticMaterial);
            }
            _causticMaterial = CoreUtils.CreateEngineMaterial(causticShader);
            _causticMaterial.SetFloat("_BlendDistance", settings.causticBlendDistance);

            if (causticTexture == null)
            {
                Debug.Log("Caustics Texture missing, attempting to load.");
#if UNITY_EDITOR
                causticTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.verasl.water-system/Textures/WaterSurface_single.tif");
#endif
            }
            _causticMaterial.SetTexture(CausticTexture, causticTexture);

            switch (settings.debug)
            {
                case WaterSystemSettings.DebugMode.Caustics:
                    _causticMaterial.SetFloat(SrcBlend, 1f);
                    _causticMaterial.SetFloat(DstBlend, 0f);
                    _causticMaterial.EnableKeyword("_DEBUG");
                    m_CausticsPass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
                    break;
                case WaterSystemSettings.DebugMode.WaterEffects:
                    break;
                case WaterSystemSettings.DebugMode.Disabled:
                    _causticMaterial.SetFloat(SrcBlend, 2f);
                    _causticMaterial.SetFloat(DstBlend, 0f);
                    _causticMaterial.DisableKeyword("_DEBUG");
                    m_CausticsPass.renderPassEvent = RenderPassEvent.AfterRenderingSkybox + 1;
                    break;
            }

            _causticMaterial.SetFloat(Size, settings.causticScale);
            m_CausticsPass.WaterCausticMaterial = _causticMaterial;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(m_WaterFxPass);
            renderer.EnqueuePass(m_CausticsPass);
        }

        private static Mesh GenerateCausticsMesh(float size)
        {
            var m = new Mesh();
            size *= 0.5f;

            var verts = new[]
            {
                new Vector3(-size, 0f, -size),
                new Vector3(size, 0f, -size),
                new Vector3(-size, 0f, size),
                new Vector3(size, 0f, size)
            };
            m.vertices = verts;

            var tris = new[]
            {
                0, 2, 1,
                2, 3, 1
            };
            m.triangles = tris;

            return m;
        }

        [System.Serializable]
        public class WaterSystemSettings
        {
            [Header("Caustics Settings")]
            [Range(0.1f, 1f)]
            public float causticScale = 0.25f;

            public float causticBlendDistance = 3f;

            [Header("Advanced Settings")] public DebugMode debug = DebugMode.Disabled;

            public enum DebugMode
            {
                Disabled,
                WaterEffects,
                Caustics
            }
        }
    }
}
