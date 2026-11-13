using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace TuanTool
{
    /// <summary>
    /// Cửa sổ Editor (Tools/Thay thế Asset Reference toàn bộ) dùng để thay mọi reference
    /// từ Asset A (cũ) sang Asset B (mới) trong toàn bộ Assets và Scenes của project.
    /// Chỉ thực hiện khi cả A và B đều được gán và khác nhau.
    /// </summary>
    public class AssetReferenceReplacer : EditorWindow
    {
        /// <summary>Asset cũ cần bị thay thế.</summary>
        Object assetA;
        /// <summary>Asset mới sẽ được gán vào các reference của Asset A.</summary>
        Object assetB;

        /// <summary>Thống kê số lần thay thế theo từng file/scene.</summary>
        Dictionary<string, int> fileStats = new Dictionary<string, int>();
        /// <summary>Tổng số reference đã thay thế trong lần chạy gần nhất.</summary>
        int totalReplaced = 0;

        [MenuItem("Tools/Thay thế Asset Reference toàn bộ")]
        static void Init()
        {
            GetWindow<AssetReferenceReplacer>("Thay thế Asset toàn bộ");
        }

        /// <summary>
        /// Vẽ 2 ô chọn Asset và nút thực hiện. Nút bị khoá nếu A/B chưa gán hoặc A == B.
        /// </summary>
        void OnGUI()
        {
            GUILayout.Label("Thay thế tất cả reference từ Asset A sang Asset B", EditorStyles.boldLabel);

            assetA = EditorGUILayout.ObjectField("Asset A (cũ)", assetA, typeof(Object), false);
            assetB = EditorGUILayout.ObjectField("Asset B (mới)", assetB, typeof(Object), false);

            bool canReplace = CanReplace();
            if (!canReplace)
                EditorGUILayout.HelpBox("Cần gán Asset A và Asset B khác nhau trước khi thay thế.", MessageType.Info);

            EditorGUI.BeginDisabledGroup(!canReplace);
            if (GUILayout.Button("Thực hiện thay thế"))
            {
                fileStats.Clear();
                totalReplaced = 0;

                try
                {
                    ReplaceInAssets();
                    ReplaceInScenes();
                    Debug.Log($"✅ Tổng số thay thế: {totalReplaced}");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"❌ Lỗi trong quá trình thay thế: {ex.Message}");
                }
            }
            EditorGUI.EndDisabledGroup();

            if (fileStats.Count > 0)
            {
                GUILayout.Space(10);
                GUILayout.Label($"📊 Thống kê:", EditorStyles.boldLabel);
                foreach (var kvp in fileStats)
                {
                    GUILayout.Label($"{kvp.Key}: {kvp.Value} lần");
                }
            }
        }

        /// <summary>
        /// Chỉ cho phép thay thế khi A và B đều có và khác nhau.
        /// Nếu thiếu A, mọi reference rỗng trong project sẽ bị gán thành B.
        /// </summary>
        bool CanReplace() => assetA != null && assetB != null && assetA != assetB;

        /// <summary>
        /// Duyệt toàn bộ asset trong project (trừ scene) và thay reference A bằng B bằng SerializedObject.
        /// </summary>
        void ReplaceInAssets()
        {
            if (!CanReplace()) return;

            string[] guids = AssetDatabase.FindAssets("t:Object");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".unity")) continue;

                Object[] objs;
                try
                {
                    objs = AssetDatabase.LoadAllAssetsAtPath(path);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"⚠️ Không thể load asset tại {path}: {ex.Message}");
                    continue;
                }

                int replacedInFile = 0;

                foreach (Object obj in objs)
                {
                    if (obj == null) continue;

                    try
                    {
                        SerializedObject so = new SerializedObject(obj);
                        SerializedProperty prop = so.GetIterator();

                        while (prop.NextVisible(true))
                        {
                            if (prop.propertyType == SerializedPropertyType.ObjectReference &&
                                prop.objectReferenceValue == assetA)
                            {
                                prop.objectReferenceValue = assetB;
                                so.ApplyModifiedProperties();
                                EditorUtility.SetDirty(obj);
                                replacedInFile++;
                                totalReplaced++;
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"⚠️ Lỗi khi xử lý object trong {path}: {ex.Message}");
                        continue;
                    }
                }

                if (replacedInFile > 0)
                    fileStats[path] = replacedInFile;
            }
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Mở lần lượt từng scene trong project, thay reference A bằng B trên mọi component rồi lưu scene.
        /// </summary>
        void ReplaceInScenes()
        {
            if (!CanReplace()) return;

            string[] scenePaths = Directory.GetFiles("Assets", "*.unity", SearchOption.AllDirectories);
            foreach (string scenePath in scenePaths)
            {
                SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
                if (sceneAsset == null) continue;

                Scene scene;
                try
                {
                    scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"⚠️ Không thể mở scene {scenePath}: {ex.Message}");
                    continue;
                }

                int replacedInScene = 0;

                GameObject[] rootObjects = scene.GetRootGameObjects();
                foreach (GameObject go in rootObjects)
                {
                    Component[] components = go.GetComponentsInChildren<Component>(true);
                    foreach (Component comp in components)
                    {
                        if (comp == null) continue;

                        try
                        {
                            SerializedObject so = new SerializedObject(comp);
                            SerializedProperty prop = so.GetIterator();

                            while (prop.NextVisible(true))
                            {
                                if (prop.propertyType == SerializedPropertyType.ObjectReference &&
                                    prop.objectReferenceValue == assetA)
                                {
                                    prop.objectReferenceValue = assetB;
                                    so.ApplyModifiedProperties();
                                    replacedInScene++;
                                    totalReplaced++;
                                }
                            }
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogWarning($"⚠️ Lỗi khi xử lý component trong scene {scenePath}: {ex.Message}");
                            continue;
                        }
                    }
                }

                if (replacedInScene > 0)
                {
                    fileStats[scenePath] = replacedInScene;
                    try
                    {
                        EditorSceneManager.MarkSceneDirty(scene);
                        EditorSceneManager.SaveScene(scene);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"⚠️ Không thể lưu scene {scenePath}: {ex.Message}");
                    }
                }
            }
        }
    }
}
