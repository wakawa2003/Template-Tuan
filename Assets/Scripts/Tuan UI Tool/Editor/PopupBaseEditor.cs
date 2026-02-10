using System;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace UITool.Popup.Editor
{
    [CustomEditor(typeof(PopupBase), true)]
#if ODIN_INSPECTOR
    public class PopupBaseEditor : OdinEditor
#else
    public class PopupBaseEditor : Editor
#endif
    {
        private bool showButtons = true;
        public override void OnInspectorGUI()
        {
            // Lấy đối tượng đang được chọn
            PopupBase targetObject = (PopupBase)target;
            showButtons = EditorGUILayout.Foldout(showButtons, "Popup Settings");

            if (showButtons)
            {

                EditorGUILayout.BeginVertical("box");
                // Nút reset
                if (GUILayout.Button("Spawn Default Popup that use Dotween Pro"))
                {
                    spawnDefaultPopupUseDotween();
                }

                EditorGUILayout.EndVertical();
            }

            // Đảm bảo Unity biết có thay đổi
            if (GUI.changed)
            {

                EditorUtility.SetDirty(targetObject);
            }
            base.OnInspectorGUI();
            void spawnDefaultPopupUseDotween()
            {
                Undo.RegisterFullObjectHierarchyUndo(targetObject.gameObject, nameof(spawnDefaultPopupUseDotween));
                GameObject popupPrefabs = Resources.LoadAll<GameObject>("UI Tool Templete/Popup Templete use Dotween Pro")[0].gameObject;
                for (int i = 0; i < popupPrefabs.transform.childCount; i++)
                {
                    var child = popupPrefabs.transform.GetChild(i).gameObject;
                    var newObject = Instantiate(child, targetObject.transform);
                    newObject.name = child.name;
                }
                targetObject.Reset();
            }
        }


    }
}
