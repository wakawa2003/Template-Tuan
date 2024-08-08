//#if UNITY_EDITOR
using Newtonsoft.Json;
using Sirenix.Utilities.Editor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;


namespace Tuan.UtilitiesEditor
{
    public class GameAssetProcessor : AssetPostprocessor
    {
        bool isDirty = false;
        private void OnPostprocessModel(GameObject gameObject)
        {
            isDirty = false;
            if (gameObject.name.StartsWith("~") || assetPath.Contains("common-lib"))//bo qua textures bat dau = "~"
                //if (texture.name.StartsWith("~"))//bo qua textures bat dau = "~"
                return;
            var a = assetImporter as ModelImporter;
            if (a.meshCompression != ModelImporterMeshCompression.High)
            {
                a.meshCompression = ModelImporterMeshCompression.High;
                isDirty = true;
            }
            if (isDirty)
                EditorCoroutineUtility.StartCoroutine(IESave(), a);
            IEnumerator IESave()
            {
                yield return null;
                Debug.Log($"import model: {a.assetPath}");
                assetImporter.SaveAndReimport();
                AssetDatabase.SaveAssetIfDirty(AssetDatabase.GUIDFromAssetPath(assetPath));
            }

        }
        private void OnPostprocessTexture(Texture2D texture)
        {
            if (texture.name.StartsWith("~") || assetPath.Contains("common-lib"))//bo qua textures bat dau = "~"
                //if (texture.name.StartsWith("~"))//bo qua textures bat dau = "~"
                return;

            if (!powerOfTwo(texture.width) || !powerOfTwo(texture.height))
            {
                var path = assetPath.Split('/').ToList();
                //if (path[0] != "Packages")
                //    if (!path.Any(_ => _.Contains("Editor")) && !path.Contains("GUI"))
                Debug.LogError($"Texture {assetPath} is not Power Of Two!");
            }
            isDirty = false;


            var mipmapEnabled = false;
            var textureCompression = TextureImporterCompression.CompressedLQ;
            var alphaIsTransparency = true;
            var compressionQuality = 100;//khong nen de la 50% => bi banding textures
            var crunchedCompression = true;//can phai xep o cuoi

            var a = assetImporter as TextureImporter;

            if (a.mipmapEnabled != mipmapEnabled)
            {

                a.mipmapEnabled = mipmapEnabled;
                isDirty = true;
            }


            if (a.textureCompression != textureCompression)
            {
                a.textureCompression = textureCompression;
                isDirty = true;
            }


            if (a.alphaIsTransparency != alphaIsTransparency)
            {
                a.alphaIsTransparency = alphaIsTransparency;
                isDirty = true;
            }

            if (a.compressionQuality != compressionQuality)
            {
                a.compressionQuality = compressionQuality;
                isDirty = true;
            }


            if (a.crunchedCompression != crunchedCompression)
            {
                a.crunchedCompression = crunchedCompression;
                isDirty = true;
            }


            if (isDirty)
                EditorCoroutineUtility.StartCoroutine(IESave(), a);
            IEnumerator IESave()
            {
                yield return null;
                Debug.Log($"import texture: {a.assetPath}");
                assetImporter.SaveAndReimport();
                AssetDatabase.SaveAssetIfDirty(AssetDatabase.GUIDFromAssetPath(assetPath));
                //isDirty = false;
                //AssetDatabase.SaveAssets();
            }

        }

        bool powerOfTwo(int n)
        {
            if (n == 0) { return false; }
            while (n != 1)
            {
                n = n / 2;
                if (n % 2 != 0 && n != 1) { return false; }
            }
            return true;
        }

    }
}
//#endif