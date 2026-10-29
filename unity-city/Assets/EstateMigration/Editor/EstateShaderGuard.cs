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

            // Unity 6000.0 uses the *serialized* GraphicsSettings object.
            // The direct alwaysIncludedShaders API exists only in newer Unity releases.
            var graphics = GraphicsSettings.GetGraphicsSettings();
            if (graphics == null)
                throw new BuildFailedException("GraphicsSettings asset is unavailable in Unity 6000.0");

            var serialized = new SerializedObject(graphics);
            var list = serialized.FindProperty("m_AlwaysIncludedShaders");
            if (list == null || !list.isArray)
                throw new BuildFailedException("Unity 6000.0 m_AlwaysIncludedShaders property unavailable");

            foreach (var name in RequiredShaderNames)
            {
                var shader = Shader.Find(name);
                if (shader == null)
                    throw new BuildFailedException(
                        "Original glTFast shader unavailable: '" + name +
                        "'. Refusing to ship magenta Unity WebGL. Check GLTFast package and pipeline.");

                bool found = false;
                for (int i = 0; i < list.arraySize; i++)
                    if (list.GetArrayElementAtIndex(i).objectReferenceValue == shader)
                    {
                        found = true;
                        break;
                    }
                if (!found)
                {
                    int idx = list.arraySize;
                    list.InsertArrayElementAtIndex(idx);
                    list.GetArrayElementAtIndex(idx).objectReferenceValue = shader;
                }
                Debug.Log("[BRAXHORN ESTATE SHADER LOCK] " + name + " -> " + shader.name);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(graphics);
            AssetDatabase.SaveAssets();

            var verify = new SerializedObject(GraphicsSettings.GetGraphicsSettings());
            var saved = verify.FindProperty("m_AlwaysIncludedShaders");
            foreach (var name in RequiredShaderNames)
            {
                bool found = false;
                for (int i = 0; i < saved.arraySize; i++)
                {
                    var shader = saved.GetArrayElementAtIndex(i).objectReferenceValue as Shader;
                    if (shader != null && shader.name == name) { found = true; break; }
                }
                if (!found)
                    throw new BuildFailedException("Missing WebGL shader after save: " + name);
            }
            Debug.Log("[BRAXHORN ESTATE SHADER LOCK] PASS: " +
                      string.Join(", ", RequiredShaderNames) +
                      ". Visual screenshot QA still required.");
        }
    }
}
#endif
