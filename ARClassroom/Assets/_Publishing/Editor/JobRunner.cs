#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;

namespace RealisticClassroom.Publishing
{
    /// <summary>Runs a long editor job on the next editor tick and writes the outcome to Temp/jobs/&lt;name&gt;.txt (so callers never block on the pipeline timeout).</summary>
    public static class JobRunner
    {
        public static string Start(string name, Func<string> job)
        {
            Directory.CreateDirectory("Temp/jobs");
            var path = "Temp/jobs/" + name + ".txt";
            File.WriteAllText(path, "RUNNING");
            EditorApplication.delayCall += () =>
            {
                try { File.WriteAllText(path, "DONE\n" + job()); }
                catch (Exception e) { File.WriteAllText(path, "FAILED\n" + e); }
            };
            return "started " + name;
        }
    }
}
#endif
