#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ARClassroom.EditorTools
{
    /// <summary>One-time project configuration: linear colour space + URP with SSAO and soft shadows.</summary>
    public static class ProjectSetup
    {
        const string SettingsDir = "Assets/Classroom/Settings";

        [MenuItem("AR Classroom/Configure Rendering (URP, Linear)")]
        public static void ConfigureRendering()
        {
            PlayerSettings.colorSpace = ColorSpace.Linear;
            Directory.CreateDirectory(SettingsDir);

            var rendererPath = SettingsDir + "/ClassroomURP_Renderer.asset";
            var assetPath = SettingsDir + "/ClassroomURP.asset";

            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, rendererPath);
            }
            AddFeature(renderer, "UnityEngine.Rendering.Universal.ScreenSpaceAmbientOcclusion, Unity.RenderPipelines.Universal.Runtime", "SSAO");
            AddFeature(renderer, "UnityEngine.XR.ARFoundation.ARBackgroundRendererFeature, Unity.XR.ARFoundation", "AR Background");
            EditorUtility.SetDirty(renderer);

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(assetPath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, assetPath);
            }

            renderer.renderingMode = RenderingMode.ForwardPlus;
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
            EditorUtility.SetDirty(renderer);
            EditorUtility.SetDirty(pipeline);

            GraphicsSettings.defaultRenderPipeline = pipeline;
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(QualitySettings.names.Length - 1, false);

            AssetDatabase.SaveAssets();
            Debug.Log("[ClassroomSetup] URP + Linear configured.");
        }

        static void Set(SerializedObject so, string name, object v)
        {
            var p = so.FindProperty(name);
            if (p == null) { Debug.LogWarning("[ClassroomSetup] missing property " + name); return; }
            if (v is int i) p.intValue = i;
            else if (v is float f) p.floatValue = f;
            else if (v is bool b) p.boolValue = b;
        }

        static void AddFeature(ScriptableRendererData data, string typeName, string niceName)
        {
            var type = Type.GetType(typeName);
            if (type == null) { Debug.LogWarning("[ClassroomSetup] feature type missing: " + typeName); return; }
            foreach (var f in data.rendererFeatures)
                if (f != null && f.GetType() == type) return;

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
#endif
