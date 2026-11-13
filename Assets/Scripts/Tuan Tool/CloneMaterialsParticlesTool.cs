
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using Unity.EditorCoroutines.Editor;
using UnityEditor;
#endif

namespace TuanTool
{
    /// <summary>
    /// Tạo bản sao Material cho tất cả ParticleSystem con của particlesRoot, lưu thành file .mat trong folderMaterial
    /// và gán lại vào Renderer. Chỉ chạy trong Editor (nút Button trong Inspector).
    /// </summary>
    public class CloneMaterialsParticlesTool : MonoBehaviour
    {
        [Tooltip("Root chứa các ParticleSystem cần clone material (tìm trong tất cả con cháu).")]
        [SerializeField] Transform particlesRoot;
#if UNITY_EDITOR
        [Tooltip("Folder trong Project để lưu các Material mới (đường dẫn bắt đầu bằng Assets/).")]
        [SerializeField][FolderPath] string folderMaterial;

        /// <summary>
        /// Clone material của từng ParticleSystem và gán lại renderer.
        /// </summary>
        [Button]
        void CloneParticleMaterial()
        {
            if (string.IsNullOrEmpty(folderMaterial))
            {
                Debug.LogWarning("Chưa chọn folderMaterial để lưu Material.");
                return;
            }

            ParticleSystem[] particles = particlesRoot.GetComponentsInChildren<ParticleSystem>();

            for (int i = 0; i < particles.Count(); i++)
            {
                var item = particles[i];
                var r = item.GetComponent<ParticleSystemRenderer>();
                if (r == null || r.sharedMaterial == null)
                    continue;

                Material newmat = new Material(r.sharedMaterial);
                var path = $"{folderMaterial}/{newmat.name}.mat";
                AssetDatabase.CreateAsset(newmat, path);
                AssetDatabase.SaveAssets();

                EditorCoroutineUtility.StartCoroutine(IEFix(), this);

                // Gán reference vào renderer chỉ được làm ổn định sau vài frame (asset chưa load xong ngay),
                // nên phải chờ trong coroutine. Đổi Selection để Inspector refresh và hiện reference mới.
                IEnumerator IEFix()
                {
                    while (r.sharedMaterial != AssetDatabase.LoadMainAssetAtPath(path) as Material)
                    {
                        var asset = AssetDatabase.LoadMainAssetAtPath(path) as Material;
                        r.sharedMaterial = asset;

                        EditorUtility.SetDirty(r);

                        var objs = Selection.objects.ToList();
                        objs.Add(asset);
                        Selection.objects = objs.ToArray();

                        yield return null;
                    }
                }
            }

        }
#endif

    }
}
