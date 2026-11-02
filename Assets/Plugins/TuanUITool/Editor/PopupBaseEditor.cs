using UnityEditor;
using UnityEngine;
using UITool.Popup;
#if UNITY_EDITOR
#if ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
#endif
#endif
namespace UITool.Popup.Editor
{
#if UNITY_EDITOR
    [CustomEditor(typeof(PopupBase), true)]
#if ODIN_INSPECTOR
    public class PopupBaseEditor : OdinEditor
#else
    public class PopupBaseEditor : UnityEditor.Editor
#endif
    {
        private bool showButtons = true;

        public override void OnInspectorGUI()
        {
            PopupBase targetObject = (PopupBase)target;

            showButtons = EditorGUILayout.Foldout(showButtons, "Popup Settings");

            if (showButtons)
            {
                EditorGUILayout.BeginVertical("box");

                if (GUILayout.Button("Spawn Default Popup that use Dotween Pro"))
                {
                    SpawnDefaultPopupUseDotween(targetObject);
                }

                if (GUILayout.Button("Spawn Default Popup that use Animator"))
                {
                    SpawnDefaultPopupUseAnimator(targetObject);
                }

                EditorGUILayout.EndVertical();
            }

            if (GUI.changed)
            {
                EditorUtility.SetDirty(targetObject);
            }

            base.OnInspectorGUI();
        }

        private void SpawnDefaultPopupUseDotween(PopupBase targetObject)
        {
            Undo.RegisterFullObjectHierarchyUndo(
                targetObject.gameObject,
                nameof(SpawnDefaultPopupUseDotween));

            var prefabs = Resources.Load<GameObject>(
                "UI Tool Templete/Popup Templete use Dotween Pro");

            if (prefabs == null)
            {
                Debug.LogError("Popup template not found.");
                return;
            }

            for (int i = 0; i < prefabs.transform.childCount; i++)
            {
                var child = prefabs.transform.GetChild(i);

                var newObject = Object.Instantiate(
                    child.gameObject,
                    targetObject.transform);

                newObject.name = child.name;
            }

            targetObject.Reset();
        }

        private void SpawnDefaultPopupUseAnimator(PopupBase targetObject)
        {
            Undo.RegisterFullObjectHierarchyUndo(
                targetObject.gameObject,
                nameof(SpawnDefaultPopupUseAnimator));

            var prefab = Resources.Load<GameObject>(
                "UI Tool Templete/Popup Templete use Animator");

            if (prefab == null)
            {
                Debug.LogError("Popup template not found.");
                return;
            }

            Object.Instantiate(prefab, targetObject.transform);

            targetObject.Reset();
        }
    }
#endif
}