using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Compilation;
using UnityEngine;

namespace Genies.Sdk.Editor
{
    internal static class AvatarSdkLinkXmlGenerator
    {
        private const string OutputPath = "Assets/Genies/link.xml";
        private const string AssemblyPrefix = "Genies.";
        
        public static void GenerateLinkXml()
        {
            var assemblies = CompilationPipeline.GetAssemblies()
                .Where(a =>
                    a.name.StartsWith(AssemblyPrefix) &&
                    (a.flags & AssemblyFlags.EditorAssembly) == 0 &&
                    !a.name.EndsWith(".Editor") &&
                    !a.name.Contains(".Tests")
                )
                .Select(a => a.name)
                .OrderBy(name => name)
                .Distinct()
                .ToList();

            if (assemblies.Count == 0)
            {
                Debug.LogWarning($"No player assemblies found starting with \"{AssemblyPrefix}\". Nothing written.");
                return;
            }

            var xml = new StringBuilder();
            xml.AppendLine("<linker>");
            foreach (var asm in assemblies)
            {
                xml.AppendLine($"  <assembly fullname=\"{asm}\" preserve=\"all\" />");
            }
            xml.AppendLine("</linker>");

            var outputDir = Path.GetDirectoryName(OutputPath);
            
            try
            {
                Directory.CreateDirectory(outputDir);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create directory: {outputDir} with exception {e}");
                throw;
            }

            // Safe write with meaningful error reporting
            try
            {
                File.WriteAllText(OutputPath, xml.ToString());
                AssetDatabase.Refresh();
            }
            catch (IOException io)
            {
                Debug.LogError($"Failed to write link.xml due to an IO exception: {io.Message}\nPath: {OutputPath}");
            }
            catch (UnauthorizedAccessException ua)
            {
                Debug.LogError($"Failed to write link.xml (permission denied): {ua.Message}\nPath: {OutputPath}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Unexpected error while writing link.xml: {ex.Message}\nPath: {OutputPath}");
            }
        }
    }

    public class LinkXmlBuildProcessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            Debug.Log("Running LinkXmlGenerator before build...");
            AvatarSdkLinkXmlGenerator.GenerateLinkXml();
        }
    }
}
