#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TuanTool
{
    /// <summary>
    /// Chơi game luôn từ scene Root: Play sẽ lưu scene đang mở, chuyển sang Root rồi chạy;
    /// khi Stop (quay về Edit Mode) sẽ mở lại scene cũ để tiếp tục chỉnh sửa.
    /// </summary>
    public class PlaySceneEditor
    {
        private const string PREVIOUS_SCENE_KEY = "PreviousScenePath";

        // Scene khởi động của game, bắt buộc phải tồn tại đúng đường dẫn này trong project
        private const string ROOT_SCENE_PATH = "Assets/Scenes/Root.unity";

        /// <summary>
        /// Menu "Play From Start" (phím tắt Shift+P). Nếu đang chạy thì dừng Play.
        /// Nếu không: hỏi lưu scene đang sửa, ghi nhớ đường dẫn scene hiện tại, mở Root rồi bật Play.
        /// </summary>
        [MenuItem("Developer Tools/Play From Start #p")]
        private static void PlayFromStart()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                return;
            }

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                // 1. Lưu lại đường dẫn Scene hiện tại vào EditorPrefs để khôi phục sau khi Stop
                string currentScene = EditorSceneManager.GetActiveScene().path;
                EditorPrefs.SetString(PREVIOUS_SCENE_KEY, currentScene);

                // 2. Mở Scene Root và chạy
                EditorSceneManager.OpenScene(ROOT_SCENE_PATH);
                EditorApplication.isPlaying = true;
            }
        }

        /// <summary>
        /// Đăng ký lắng nghe thay đổi Play/Stop ngay khi Editor load (InitializeOnLoad).
        /// </summary>
        [InitializeOnLoadMethod]
        private static void MonitorPlayModeState()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        /// <summary>
        /// Khi Stop (vào EnteredEditMode) thì mở lại scene đã lưu trước khi Play, rồi xóa key đã lưu.
        /// </summary>
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                if (EditorPrefs.HasKey(PREVIOUS_SCENE_KEY))
                {
                    string previousScene = EditorPrefs.GetString(PREVIOUS_SCENE_KEY);

                    // Chỉ mở lại nếu scene cũ khác scene hiện tại (Root)
                    if (!string.IsNullOrEmpty(previousScene) && previousScene != EditorSceneManager.GetActiveScene().path)
                    {
                        EditorSceneManager.OpenScene(previousScene);
                    }

                    // Xóa key sau khi đã quay lại để tránh nhảy Scene ở lần Stop sau
                    EditorPrefs.DeleteKey(PREVIOUS_SCENE_KEY);
                }
            }
        }
    }
}
#endif
