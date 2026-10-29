// BRAXHORN CITY - original estate runtime GLB shader preservation.
// The existing source meshes, textures and PBR parameters are NEVER replaced.
// Shader stripping in Unity WebGL omits GLTFast's runtime-only shaders unless pinned.
#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

namespace Braxhorn.EstateMigration.Editor
{
    public static class EstateShaderGuard
    {
        // Built-In Render Pipeline uses glTFast's own three material shaders.
        // The original Brax surface uses Standard; without it Shader.Find returns null.
        static readonly string[] RequiredShaderNames = {
            "glTF/PbrMetallicRoughness",
            "glTF/PbrSpecularGlossiness",
            "glTF/Unlit",
            "Standard"
        };

        public static void PreserveOriginalGltfShaders()
        {
            if (GraphicsSettings.currentRenderPipeline != null)
                throw new BuildFailedException(
                    "Braxhorn source lock requires Built-In Render Pipeline; found different pipeline");

            var included = new List<Shader>(GraphicsSettings.alwaysIncludedShaders ?? Array.Empty<Shader>());
            foreach (var name in RequiredShaderNames)
            {
                var shader = Shader.Find(name);
                if (shader == null)
                    throw new BuildFailedException(
                        "Original glTFast shader unavailable: '" + name +
                        "'. Refusing to ship magenta Unity WebGL. Check GLTFast package and pipeline.");
                if (!included.Contains(shader))
                    included.Add(shader);
                Debug.Log("[BRAXHORN ESTATE SHADER LOCK] " + name + " -> " + shader.name);
            }

            // Persist in GraphicsSettings prior to BuildPipeline.BuildPlayer.
            // Keeping these glTF shader variants in the player is essential because
            // models and their materials are instantiated AFTER the WebGL build.
            GraphicsSettings.alwaysIncludedShaders = included.ToArray();
            AssetDatabase.SaveAssets();
            foreach (var name in RequiredShaderNames)
            {
                if (!GraphicsSettings.alwaysIncludedShaders.Any(s => s != null && s.name == name))
                    throw new BuildFailedException("Missing shader in WebGL inclusion list: " + name);
            }
            Debug.Log("[BRAXHORN ESTATE SHADER LOCK] PASS: " +
                      string.Join(", ", RequiredShaderNames) +
                      ". Visual screenshot QA still required.");
        }
    }
}
#endif
