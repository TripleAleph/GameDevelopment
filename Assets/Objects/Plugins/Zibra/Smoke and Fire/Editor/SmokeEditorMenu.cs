#if UNITY_2019_4_OR_NEWER
using com.zibra.common;
using com.zibra.common.Utilities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace com.zibra.smoke_and_fire
{
    /// <summary>
    /// Class that contains code for useful actions for the liquid plugin
    /// Those actions exposed to user via MenuItem
    /// You can call them from C# via ExecuteMenuItem
    /// </summary>
    internal static class SmokeEditorMenu
    {
        [MenuItem(Effects.BaseMenuBarPath + "Open Sample Scene/Smoke and Fire", false, 32)]
        static void OpenLiquidSampleScene()
        {
            string GUID = "";
            switch (RenderPipelineDetector.GetRenderPipelineType())
            {
                case RenderPipelineDetector.RenderPipeline.BuiltInRP:
                    GUID = "260e6173e5e4dbd49a47aeffaf0247a2";
                    break;
                case RenderPipelineDetector.RenderPipeline.URP:
                    GUID = "1aec6be9b39611247b96bd1a801161e5";
                    break;
                case RenderPipelineDetector.RenderPipeline.HDRP:
                    GUID = "0173b6eb9ef2fc84f88835d7915fba43";
                    break;
            }
            EditorSceneManager.OpenScene(AssetDatabase.GUIDToAssetPath(GUID));
        }

        [MenuItem(Effects.BaseMenuBarPath + "Open Sample Scene/Smoke and Fire (Mobile)", false, 33)]
        static void OpenLiquidMobileSampleScene()
        {
            string GUID = "";
            switch (RenderPipelineDetector.GetRenderPipelineType())
            {
                case RenderPipelineDetector.RenderPipeline.BuiltInRP:
                    GUID = "3c76e9efb3ee66942922b1849eff91cc";
                    break;
                case RenderPipelineDetector.RenderPipeline.URP:
                    GUID = "e908fa213d3fbef478bf28910b75e9d9";
                    break;
                case RenderPipelineDetector.RenderPipeline.HDRP:
                    GUID = "3c76e9efb3ee66942922b1849eff91cc";
                    string errorMessage = "Mobile platforms don't support HDRP. Opening BiRP sample scene instead.";
                    EditorUtility.DisplayDialog("Zibra Effects Error.", errorMessage, "OK");
                    Debug.LogError(errorMessage);
                    break;
            }
            EditorSceneManager.OpenScene(AssetDatabase.GUIDToAssetPath(GUID));
        }
    }
}
#endif
