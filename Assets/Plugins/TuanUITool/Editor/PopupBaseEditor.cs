using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;

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
#if !ODIN_INSPECTOR
        private static readonly FieldInfo[] PopupBaseFields = typeof(PopupBase).GetFields(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
#endif

        private bool showButtonsAutoCreate = true;
        private bool showPopupSettings = true;

        public override void OnInspectorGUI()
        {
            PopupBase targetObject = (PopupBase)target;

            showButtonsAutoCreate = EditorGUILayout.Foldout(showButtonsAutoCreate, "Auto Create Settings");

            if (showButtonsAutoCreate)
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

#if !ODIN_INSPECTOR
            DrawPopupSettings(targetObject);
#endif

#if ODIN_INSPECTOR
            base.OnInspectorGUI();
#endif

            if (GUI.changed)
            {
                EditorUtility.SetDirty(targetObject);
            }
        }

#if ODIN_INSPECTOR
        protected override void DrawTree()
        {
            var tree = Tree;
            if (tree == null)
            {
                return;
            }

            var popupBaseProperties = new List<InspectorProperty>();
            var derivedProperties = new List<InspectorProperty>();

            foreach (var property in tree.RootProperty.Children)
            {
                if (IsDeclaredByPopupBase(property))
                {
                    popupBaseProperties.Add(property);
                }
                else
                {
                    derivedProperties.Add(property);
                }
            }

            tree.BeginDraw(true);
            try
            {
                showPopupSettings = EditorGUILayout.Foldout(showPopupSettings, "Popup Settings");

                if (showPopupSettings)
                {
                    EditorGUILayout.BeginVertical("box");
                    DrawPopupActionButtons((PopupBase)target);

                    foreach (var property in popupBaseProperties)
                    {
                        property.Draw();
                    }

                    EditorGUILayout.EndVertical();
                }

                foreach (var property in derivedProperties)
                {
                    property.Draw();
                }
            }
            finally
            {
                tree.EndDraw();
            }
        }

        private static bool IsDeclaredByPopupBase(InspectorProperty property)
        {
            foreach (var member in property.Info.GetMemberInfos())
            {
                if (member.DeclaringType == typeof(PopupBase))
                {
                    return true;
                }
            }

            return false;
        }
#else
        private void DrawPopupSettings(PopupBase targetObject)
        {
            serializedObject.Update();

            showPopupSettings = EditorGUILayout.Foldout(showPopupSettings, "Popup Settings");

            if (showPopupSettings)
            {
                EditorGUILayout.BeginVertical("box");
                DrawPopupActionButtons(targetObject);

                foreach (var field in PopupBaseFields)
                {
                    var property = serializedObject.FindProperty(field.Name);
                    if (property != null)
                    {
                        EditorGUILayout.PropertyField(property, true);
                    }
                }

                EditorGUILayout.EndVertical();
            }

            var excludedProperties = new List<string> { "m_Script" };
            foreach (var field in PopupBaseFields)
            {
                if (serializedObject.FindProperty(field.Name) != null)
                {
                    excludedProperties.Add(field.Name);
                }
            }

            DrawPropertiesExcluding(serializedObject, excludedProperties.ToArray());
            serializedObject.ApplyModifiedProperties();
        }
#endif

        private void DrawPopupActionButtons(PopupBase targetObject)
        {
            if (GUILayout.Button("Show"))
            {
                targetObject.Show(null).Forget();
            }

            if (GUILayout.Button("Hide"))
            {
                targetObject.Hide().Forget();
            }
        }

        private void SpawnDefaultPopupUseDotween(PopupBase targetObject)
        {
            Undo.RegisterFullObjectHierarchyUndo(
                targetObject.gameObject,
                nameof(SpawnDefaultPopupUseDotween));

            var prefabs = Resources.Load<GameObject>(
                "UI Tool Templete/Popup Templete use Dotween");

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

                //gán nút targetObject.Hide() khi ấn vào bg
                var buttonClose = newObject.GetComponent<Button>();
                if (buttonClose != null)
                {
                    UnityEditor.Events.UnityEventTools.AddPersistentListener(
                        buttonClose.onClick,
                        targetObject.HideFromButton);
                }
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