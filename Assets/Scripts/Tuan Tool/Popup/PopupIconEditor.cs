#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace TuanTool.Popup
{
    /// <summary>
    /// Vẽ nút On/Off ở bên phải mỗi GameObject có component Popup trong Hierarchy.
    /// "On" (xanh) = popup đang hiện, bấm để Hide. "Off" (đỏ) = popup đang ẩn, bấm để RequestShow.
    /// </summary>
    [InitializeOnLoad]
    class PopupIconEditor
    {
        static PopupIconEditor()
        {
            EditorApplication.hierarchyWindowItemOnGUI += HierarchyItemCB;
        }

        static void HierarchyItemCB(int instanceID, Rect selectionRect)
        {
            // Đặt nút ở bên phải của dòng trong Hierarchy
            Rect r = new Rect(selectionRect);
            r.x = r.width + 30;
            r.width = 30;

            GameObject go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            var popup = go?.GetComponent<Popup>();
            if (popup)
            {
                popup.gameObject.SetActive(true);
                Color prevColor = GUI.color;
                if (popup.IsShowing)
                {
                    GUI.color = Color.green;
                    if (GUI.Button(r, new GUIContent("On")))
                        popup.Hide();
                }
                else
                {
                    GUI.color = Color.red;
                    if (GUI.Button(r, new GUIContent("Off")))
                        popup.RequestShow();
                }
                GUI.color = prevColor;
            }
        }
    }
}
#endif
