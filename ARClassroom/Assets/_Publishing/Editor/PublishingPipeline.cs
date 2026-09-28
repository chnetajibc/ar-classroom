#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RealisticClassroom.Publishing
{
    public static class PublishingPipeline
    {
        const string Root = ModelConverter.Root;

        /// <summary>Deletes every generated asset (keeps scripts, scene, docs, sky) so a run cannot leave stale files behind.</summary>
        public static void Clean()
        {
            foreach (var p in new[]
            {
                Root + "/Models", Root + "/Prefabs", Root + "/Materials/Architecture", Root + "/Materials/Furniture", Root + "/Materials/Props",
                Root + "/Materials/Lighting", Root + "/Materials/Characters", Root + "/Textures/Architecture", Root + "/Textures/Furniture",
                Root + "/Textures/Props", Root + "/Textures/Lighting", Root + "/Textures/Characters",
            })
                if (AssetDatabase.IsValidFolder(p)) AssetDatabase.DeleteAsset(p);
            AssetDatabase.Refresh();
        }

        /// <summary>Re-creates every converted asset from the source downloads (models, characters, architecture, animations).</summary>
        public static string ConvertAll()
        {
            UrpMaterialFactory.ResetCache();
            Clean();
            var sb = new System.Text.StringBuilder();
            sb.AppendLine(ModelConverter.ConvertAllStatics());
            sb.AppendLine(ModelConverter.ConvertAllCharacters());
            sb.AppendLine(ArchitectureConverter.ConvertAll());
            sb.AppendLine(AnimationSetup.Run());
            return sb.ToString();
        }

        public static string Validate()
        {
            return PackageValidator.Run(Path.GetFullPath("../Publishing/reports/validation.md"));
        }

        /// <summary>Exports Assets/RealisticClassroom as a .unitypackage (for direct-sale channels and as the Fab upload).</summary>
        public static string ExportPackage(string outPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outPath));
            AssetDatabase.ExportPackage(Root, outPath, ExportPackageOptions.Recurse);
            return "exported " + outPath + " (" + (new FileInfo(outPath).Length / 1048576) + " MB)";
        }

        /// <summary>
        /// Command line entry point used by Publishing/tools/build_release.sh:
        /// Unity -batchmode -projectPath ARClassroom -executeMethod RealisticClassroom.Publishing.PublishingPipeline.ReleaseCli -outPath file.unitypackage -quit
        /// Runs the pre-flight checks (fails the build on any FAIL row) and exports the package.
        /// </summary>
        public static void ReleaseCli()
        {
            var args = System.Environment.GetCommandLineArgs();
            string outPath = null;
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-outPath") outPath = args[i + 1];
            if (string.IsNullOrEmpty(outPath)) { Debug.LogError("ReleaseCli: missing -outPath <file.unitypackage>"); EditorApplication.Exit(2); return; }
            try
            {
                string reportPath = Path.GetFullPath("../Publishing/reports/validation.md");
                Debug.Log(PackageValidator.Run(reportPath));
                if (File.ReadAllText(reportPath).Contains("| FAIL |")) { Debug.LogError("ReleaseCli: validation has FAIL rows, see " + reportPath); EditorApplication.Exit(3); return; }
                Debug.Log(ExportPackage(Path.GetFullPath(outPath)));
            }
            catch (System.Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        /// <summary>Builds the demo scene and the complete-room prefab, then runs the local pre-flight checks.</summary>
        public static string BuildDemoAndValidate()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine(RealisticClassroom.EditorTools.ClassroomSceneBuilder.Build());
            sb.AppendLine("prefab: " + RealisticClassroom.EditorTools.ClassroomSceneBuilder.SaveCompletePrefab());
            sb.AppendLine(PackageValidator.SceneStats());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            string report = Path.GetFullPath("../Publishing/reports/validation.md");
            sb.AppendLine(PackageValidator.Run(report));
            return sb.ToString();
        }
    }
}
#endif
