using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

namespace TuanTool
{
    /// <summary>
    /// Chứa menu Tools/Reimport Textures: áp cấu hình texture chuẩn cho các texture đang chọn trong Project
    /// rồi reimport. Cấu hình ép buộc: tắt mipmap, CompressedLQ, alpha is transparency,
    /// compression quality 100 (tránh banding), crunched compression.
    /// </summary>
    public class GameAssetProcessor : AssetPostprocessor
    {
        /// <summary>
        /// Reimport các texture đang được chọn trong Project, chạy song song và áp cấu hình chuẩn.
        /// </summary>
        [MenuItem("Tools/Reimport Textures")]
        static async void ReimportTextures()
        {
            var startTime = System.DateTime.Now;
            AssetDatabase.StartAssetEditing();
            try
            {
                // Lấy đối tượng đang được chọn trong Project
                List<Task> listTask = new List<Task>();
                Object[] selectedObjects = Selection.objects;
                if (selectedObjects.Length > 0)
                {
                    foreach (Object obj in selectedObjects)
                    {
                        // Lấy đường dẫn tới asset
                        string path = AssetDatabase.GetAssetPath(obj);

                        // Kiểm tra xem asset có phải là Texture không
                        TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;
                        if (textureImporter != null)
                        {
                            Debug.Log($"Texture Importer settings for {obj.name}:");
                            Debug.Log($"Path: {path}");
                            Debug.Log($"Texture Type: {textureImporter.textureType}");
                            Debug.Log($"Wrap Mode: {textureImporter.wrapMode}");
                            Debug.Log($"Filter Mode: {textureImporter.filterMode}");
                            listTask.Add(Execute(textureImporter));
                        }
                        else
                        {
                            Debug.LogWarning($"The asset '{obj.name}' at path '{path}' is not a texture!");
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("No assets are selected!");
                }
                await Task.WhenAll(listTask);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                Debug.Log($"Hoàn thành import: {(System.DateTime.Now - startTime).TotalSeconds}");
            }
        }

        /// <summary>
        /// Ghi cấu hình chuẩn vào TextureImporter rồi SaveAndReimport sau vài frame editor.
        /// </summary>
        static async Task Execute(AssetImporter assetImporter)
        {
            var a = assetImporter as TextureImporter;

            var mipmapEnabled = false;
            var textureCompression = TextureImporterCompression.CompressedLQ;
            var alphaIsTransparency = true;
            var compressionQuality = 100;//khong nen de la 50% => bi banding textures
            var crunchedCompression = true;//can phai xep o cuoi

            if (a.mipmapEnabled != mipmapEnabled)
            {

                a.mipmapEnabled = mipmapEnabled;
            }


            if (a.textureCompression != textureCompression)
            {
                a.textureCompression = textureCompression;
            }


            if (a.alphaIsTransparency != alphaIsTransparency)
            {
                a.alphaIsTransparency = alphaIsTransparency;
            }

            if (a.compressionQuality != compressionQuality)
            {
                a.compressionQuality = compressionQuality;
            }

            if (a.crunchedCompression != crunchedCompression)
            {
                a.crunchedCompression = crunchedCompression;
            }

            Debug.Log($"import texture: {a.assetPath}");

            bool isCOmplte = false;
            EditorCoroutineUtility.StartCoroutineOwnerless(IEImport());
            while (!isCOmplte)
            {
                await Task.Delay(100);
            }

            // Chờ vài frame rồi mới reimport để tránh xung đột với batch StartAssetEditing
            System.Collections.IEnumerator IEImport()
            {
                for (int i = 0; i < 10; i++)
                    yield return null;
                assetImporter.SaveAndReimport();
                for (int i = 0; i < 10; i++)
                    yield return null;
                isCOmplte = true;
            }
        }
    }
}
