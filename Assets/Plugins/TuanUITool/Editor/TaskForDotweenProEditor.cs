using UnityEditor;
using UnityEngine;
using UITool.Popup;

namespace UITool.Popup.Editor
{
    [CustomEditor(typeof(TaskForDotweenPro), true)]
    public class TaskForDotweenProEditor : UnityEditor.Editor
    {
        SerializedProperty dotweenAnimationComponent;

        void OnEnable()
        {
            dotweenAnimationComponent = serializedObject.FindProperty("dotweenAnimationComponent");
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var task = (TaskForDotweenPro)target;
            if (dotweenAnimationComponent != null)
            {

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("----------------------------------", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Animation Details", EditorStyles.boldLabel);

                for (int i = 0; i < dotweenAnimationComponent.arraySize; i++)
                {
                    var element = dotweenAnimationComponent.GetArrayElementAtIndex(i);
                    var animation = element.objectReferenceValue as DotweenAnimations;
                    if (animation != null && !string.IsNullOrEmpty(animation.Detail))
                        EditorGUILayout.LabelField((i + 1) + ": " + animation.Detail);
                }
            }
            EditorGUILayout.LabelField("----------------------------------", EditorStyles.boldLabel);

            if (Application.isPlaying)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Execute"))
                        _ = task.Execute(default);

                    if (GUILayout.Button("Force Run End"))
                        task.ForceRunEnd();
                }
            }
        }
    }
}
