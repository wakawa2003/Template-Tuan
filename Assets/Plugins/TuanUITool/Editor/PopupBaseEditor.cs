using System;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UITool.Popup;
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

                if (GUILayout.Button("Spawn Default Popup that use Animator"))
                {
                    spawnDefaultPopupUseAnimator();
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

            void spawnDefaultPopupUseAnimator()
            {
                Undo.RegisterFullObjectHierarchyUndo(targetObject.gameObject, nameof(spawnDefaultPopupUseDotween));
                GameObject popupPrefabs = Resources.LoadAll<GameObject>("UI Tool Templete/Popup Templete use Animator")[0].gameObject;
                var newObject = Instantiate(popupPrefabs, targetObject.transform);
                // for (int i = 0; i < popupPrefabs.transform.childCount; i++)
                // {
                //     var child = popupPrefabs.transform.GetChild(i).gameObject;
                //     var newObject = Instantiate(child, targetObject.transform);
                //     newObject.name = child.name;
                // }



                // //add animator
                // var animator = popupPrefabs.GetComponent<Animator>();
                // if (targetObject.gameObject.GetComponent<Animator>() != null)
                //     DestroyImmediate(targetObject.gameObject.GetComponent<Animator>());
                // var newAnimator = targetObject.gameObject.AddComponent<Animator>();
                // newAnimator.runtimeAnimatorController = animator.runtimeAnimatorController;
                // newAnimator.avatar = animator.avatar;
                // newAnimator.applyRootMotion = animator.applyRootMotion;
                // newAnimator.animatePhysics = animator.animatePhysics;
                // newAnimator.updateMode = animator.updateMode;
                // newAnimator.cullingMode = animator.cullingMode;

                // var open = popupPrefabs.GetComponent<PlayAnimationOnOpen>();
                // var newAnimatorOpen = targetObject.GetComponent<PlayAnimationOnOpen>() ?? targetObject.gameObject.AddComponent<PlayAnimationOnOpen>();
                // newAnimatorOpen.Description = open.Description;
                // newAnimatorOpen.animator = open.animator;
                // newAnimatorOpen.animationTriggerName = open.animationTriggerName;
                // newAnimatorOpen.DisableObjects = open.DisableObjects;

                // var close = popupPrefabs.GetComponent<PlayAnimationOnClose>();
                // var newAnimatorClose = targetObject.GetComponent<PlayAnimationOnClose>() ?? targetObject.gameObject.AddComponent<PlayAnimationOnClose>();
                // newAnimatorClose.Description = close.Description;
                // newAnimatorClose.animator = close.animator;
                // newAnimatorClose.animationTriggerName = close.animationTriggerName;
                // newAnimatorClose.DisableObjects = close.DisableObjects;

                targetObject.Reset();
            }
        }


    }
}
