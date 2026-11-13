#if UNITY_EDITOR
using TuanTool;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor.UI
{
    [CustomEditor(typeof(ButtonSyncWithText), true)]
    [CanEditMultipleObjects]
    /// <summary>
    /// Inspector cho ButtonSyncWithText: vẽ thêm OnClick và CanvasGroup
    /// bên dưới các field của Selectable.
    /// </summary>
    public class ButtonWithTextEditor : SelectableEditor
    {
        SerializedProperty m_OnClickProperty;
        SerializedProperty m_CanvasGroup;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_OnClickProperty = serializedObject.FindProperty("m_OnClick");
            m_CanvasGroup = serializedObject.FindProperty("m_CanvasGroup");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();

            serializedObject.Update();
            EditorGUILayout.PropertyField(m_OnClickProperty);
            EditorGUILayout.PropertyField(m_CanvasGroup);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif