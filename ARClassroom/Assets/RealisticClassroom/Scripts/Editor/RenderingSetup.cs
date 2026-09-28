using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RealisticClassroom.EditorTools
{
    /// <summary>
    /// Optional helper that switches the project to the URP settings the demo scene was lit for:
    /// Linear colour space, a Forward+ renderer with SSAO, soft shadows and HDR. It only changes project settings when you choose the menu item.
    /// </summary>
    public static class RenderingSetup
    {
        public const string SettingsDir = "Assets/RealisticClassroom/Settings";

        [MenuItem("Tools/Realistic Classroom/Apply Recommended URP Settings")]
        public static void ApplyWithConfirmation()
        {
            if (!EditorUtility.DisplayDialog("Realistic Classroom",
                    "This sets the project to Linear colour space and assigns the demo URP asset (Forward+, SSAO, soft shadows, HDR) in Graphics and Quality settings.\n\nContinue?",
                    "Apply", "Cancel")) return;
            Apply();
        }

        public static void Apply()
        {
            PlayerSettings.colorSpace = ColorSpace.Linear;
            Directory.CreateDirectory(SettingsDir);
            var rendererPath = SettingsDir + "/RealisticClassroom_Renderer.asset";
            var assetPath = SettingsDir + "/RealisticClassroom_URP.asset";

            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, rendererPath);
            }
            renderer.renderingMode = RenderingMode.ForwardPlus;
            AddFeature(renderer, typeof(ScreenSpaceAmbientOcclusion), "Screen Space Ambient Occlusion");
            EditorUtility.SetDirty(renderer);

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(assetPath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, assetPath);
            }
            var so = new SerializedObject(pipeline);
            Set(so, "m_MSAA", 4);
            Set(so, "m_SupportsHDR", true);
            Set(so, "m_MainLightShadowsSupported", true);
            Set(so, "m_SoftShadowsSupported", true);
            Set(so, "m_AnyShadowsSupported", true);
            Set(so, "m_ShadowDistance", 18f);
            Set(so, "m_ShadowCascadeCount", 2);
            Set(so, "m_ShadowDepthBias", 1.4f);
            Set(so, "m_ShadowNormalBias", 2.0f);
            Set(so, "m_MainLightShadowmapResolution", 2048);
            Set(so, "m_AdditionalLightShadowsSupported", true);
            Set(so, "m_AdditionalLightsShadowmapResolution", 2048);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pipeline);

            GraphicsSettings.defaultRenderPipeline = pipeline;
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(QualitySettings.names.Length - 1, false);
            AssetDatabase.SaveAssets();
            Debug.Log("[Realistic Classroom] URP settings applied (Linear colour space, Forward+, SSAO).");
        }

        static void Set(SerializedObject so, string name, object v)
        {
            var p = so.FindProperty(name);
            if (p == null) return;
            if (v is int i) p.intValue = i;
            else if (v is float f) p.floatValue = f;
            else if (v is bool b) p.boolValue = b;
        }

        static void AddFeature(ScriptableRendererData data, System.Type type, string niceName)
        {
            foreach (var f in data.rendererFeatures) if (f != null && f.GetType() == type) return;
            var feature = (ScriptableRendererFeature)ScriptableObject.CreateInstance(type);
            feature.name = niceName;
            AssetDatabase.AddObjectToAsset(feature, data);
            var so = new SerializedObject(data);
            var list = so.FindProperty("m_RendererFeatures");
            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = feature;
            var map = so.FindProperty("m_RendererFeatureMap");
            map.arraySize++;
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long localId);
            map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
