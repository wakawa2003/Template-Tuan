using System;
using DG.Tweening;
using TMPro;
using UITool.Popup;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UITool.Popup.Editor
{
    [CustomEditor(typeof(DotweenAnimations))]
    public class DotweenAnimationsEditor : UnityEditor.Editor
    {
        static readonly GUIContent[] AnimationLabels =
        {
            new GUIContent("None"),
            new GUIContent("Move"),
            new GUIContent("Local Move"),
            new GUIContent("Rotate"),
            new GUIContent("Local Rotate"),
            new GUIContent("Scale"),
            new GUIContent("Color"),
            new GUIContent("Fade"),
            new GUIContent("Text"),
            new GUIContent("Punch Position"),
            new GUIContent("Punch Rotation"),
            new GUIContent("Punch Scale"),
            new GUIContent("Shake Position"),
            new GUIContent("Shake Rotation"),
            new GUIContent("Shake Scale"),
            new GUIContent("Camera Aspect"),
            new GUIContent("Camera Background Color"),
            new GUIContent("Camera Field Of View"),
            new GUIContent("Camera Ortho Size"),
            new GUIContent("Camera Pixel Rect"),
            new GUIContent("Camera Rect"),
            new GUIContent("UI Width / Height")
        };

        static readonly GUIContent[] EaseLabels = Array.ConvertAll(
            (Ease[])Enum.GetValues(typeof(Ease)),
            ease => new GUIContent(ease.ToString()));

        SerializedProperty animationType;
        SerializedProperty targetIsSelf;
        SerializedProperty targetGO;
        SerializedProperty animationTarget;
        SerializedProperty targetType;
        SerializedProperty forcedTargetType;
        SerializedProperty isValid;
        SerializedProperty useTargetAsV3;
        SerializedProperty easeType;
        SerializedProperty easeCurve;

        void OnEnable()
        {
            animationType = serializedObject.FindProperty("animationType");
            targetIsSelf = serializedObject.FindProperty("targetIsSelf");
            targetGO = serializedObject.FindProperty("targetGO");
            animationTarget = serializedObject.FindProperty("target");
            targetType = serializedObject.FindProperty("targetType");
            forcedTargetType = serializedObject.FindProperty("forcedTargetType");
            isValid = serializedObject.FindProperty("isValid");
            useTargetAsV3 = serializedObject.FindProperty("useTargetAsV3");
            easeType = serializedObject.FindProperty("easeType");
            easeCurve = serializedObject.FindProperty("easeCurve");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawDetail();
            DrawActivation();
            DrawTarget();
            DrawTweenSettings();
            DrawEndValue();
            DrawCallbacks();
            DrawRuntimeControls();

            serializedObject.ApplyModifiedProperties();
        }

        void DrawDetail()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Detail"),
                new GUIContent("Detail"));
            EditorGUILayout.Space();
        }

        void DrawActivation()
        {
            EditorGUILayout.LabelField("Animation", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("isActive"), new GUIContent("Active"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("autoGenerate"), new GUIContent("Auto Generate"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("autoPlay"), new GUIContent("Auto Play"));
            DrawEnumPopup(animationType, AnimationLabels, "Animation Type");
        }

        void DrawTarget()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Target", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(targetIsSelf, new GUIContent("Animate This GameObject"));

            if (!targetIsSelf.boolValue)
                EditorGUILayout.PropertyField(targetGO, new GUIContent("Target GameObject"));

            EditorGUILayout.PropertyField(animationTarget, new GUIContent("Target Component"));
            DrawFadeTargetSelector();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("tweenTargetIsTargetGO"),
                new GUIContent("Use Target GameObject for Group Operations"));

            var component = animationTarget.objectReferenceValue as Component;
            bool validTarget = animationType.enumValueIndex == (int)DotweenAnimations.AnimationType.None
                ? false
                : IsCompatibleTarget(animationType.enumValueIndex, component);
            if (isValid.boolValue != validTarget)
                isValid.boolValue = validTarget;
            if (validTarget)
                targetType.enumValueIndex = (int)GetTargetType(component);

            if (validTarget)
            {
                EditorGUILayout.HelpBox($"Ready: {component.GetType().Name}", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("Choose a target component supported by this animation type.", MessageType.Warning);
                if (GUILayout.Button("Find Compatible Component"))
                    FindCompatibleTarget();
            }
        }

        void DrawFadeTargetSelector()
        {
            if ((DotweenAnimations.AnimationType)animationType.enumValueIndex
                    != DotweenAnimations.AnimationType.Fade)
                return;

            var owner = serializedObject.targetObject as DotweenAnimations;
            var gameObject = targetIsSelf.boolValue
                ? (owner != null ? owner.gameObject : null)
                : targetGO.objectReferenceValue as GameObject;
            if (gameObject == null)
                return;

            var canvasGroup = gameObject.GetComponent<CanvasGroup>();
            var graphic = gameObject.GetComponent<Graphic>();
            if (canvasGroup == null || graphic == null)
                return;

            bool canvasGroupSelected = animationTarget.objectReferenceValue == canvasGroup;
            int selectedTarget = EditorGUILayout.Popup(
                "Fade Target",
                canvasGroupSelected ? 0 : 1,
                new[] { "Canvas Group", graphic.GetType().Name });
            var selectedComponent = selectedTarget == 0 ? (Component)canvasGroup : graphic;
            if (animationTarget.objectReferenceValue != selectedComponent)
            {
                animationTarget.objectReferenceValue = selectedComponent;
                targetType.enumValueIndex = (int)GetTargetType(selectedComponent);
                forcedTargetType.enumValueIndex = (int)DotweenAnimations.TargetType.Unset;
            }
        }

        void DrawTweenSettings()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Tween Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("duration"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("delay"));
            DrawEnumPopup(easeType, EaseLabels, "Ease");
            if (easeType.enumValueIndex == (int)Ease.INTERNAL_Custom)
                EditorGUILayout.PropertyField(easeCurve, new GUIContent("Ease Curve"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("loopType"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("loops"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("id"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("isRelative"));
            var hasStartValue = serializedObject.FindProperty("hasStartValue");
            using (new EditorGUI.DisabledScope(hasStartValue.boolValue))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("isFrom"));
            if (hasStartValue.boolValue)
                EditorGUILayout.HelpBox("Custom start and end values are absolute; Relative and From are ignored.", MessageType.Info);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("isSpeedBased"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("isIndependentUpdate"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("autoKill"));
        }

        void DrawEndValue()
        {
            var type = (DotweenAnimations.AnimationType)animationType.enumValueIndex;
            if (type == DotweenAnimations.AnimationType.None)
                return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Tween Values", EditorStyles.boldLabel);
            DrawStartValue(type);
            EditorGUILayout.LabelField("End Value (B)", EditorStyles.miniBoldLabel);

            switch (type)
            {
                case DotweenAnimations.AnimationType.Move:
                    EditorGUILayout.PropertyField(useTargetAsV3, new GUIContent("Use Transform as Target"));
                    if (useTargetAsV3.boolValue)
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("endValueTransform"),
                            new GUIContent("End Transform (B)"));
                    else
                        DrawVector3();
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("snapping"));
                    break;
                case DotweenAnimations.AnimationType.LocalMove:
                    DrawVector3();
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("snapping"));
                    break;
                case DotweenAnimations.AnimationType.Rotate:
                case DotweenAnimations.AnimationType.LocalRotate:
                    DrawVector3();
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("rotationMode"),
                        new GUIContent("Rotation Mode"));
                    break;
                case DotweenAnimations.AnimationType.Scale:
                    DrawUniformOrVector3();
                    break;
                case DotweenAnimations.AnimationType.Color:
                case DotweenAnimations.AnimationType.CameraBackgroundColor:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("endValueColor"),
                        new GUIContent("End Color (B)"));
                    break;
                case DotweenAnimations.AnimationType.Fade:
                case DotweenAnimations.AnimationType.CameraAspect:
                case DotweenAnimations.AnimationType.CameraFieldOfView:
                case DotweenAnimations.AnimationType.CameraOrthoSize:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("endValueFloat"),
                        new GUIContent(type == DotweenAnimations.AnimationType.Fade
                            ? "End Alpha / Intensity (B)"
                            : "End Value (B)"));
                    break;
                case DotweenAnimations.AnimationType.Text:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("endValueString"),
                        new GUIContent("End Text (B)"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("richTextEnabled"),
                        new GUIContent("Rich Text Enabled"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("scrambleMode"),
                        new GUIContent("Scramble Mode"));
                    if ((ScrambleMode)serializedObject.FindProperty("scrambleMode").enumValueIndex != ScrambleMode.None)
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("customScrambleCharacters"),
                            new GUIContent("Custom Scramble Characters"));
                    break;
                case DotweenAnimations.AnimationType.PunchPosition:
                    DrawVector3();
                    DrawPunchOptions(true);
                    break;
                case DotweenAnimations.AnimationType.PunchRotation:
                case DotweenAnimations.AnimationType.PunchScale:
                    DrawVector3();
                    DrawPunchOptions(false);
                    break;
                case DotweenAnimations.AnimationType.ShakePosition:
                    DrawVector3();
                    DrawShakeOptions(true);
                    break;
                case DotweenAnimations.AnimationType.ShakeRotation:
                case DotweenAnimations.AnimationType.ShakeScale:
                    DrawVector3();
                    DrawShakeOptions(false);
                    break;
                case DotweenAnimations.AnimationType.CameraPixelRect:
                case DotweenAnimations.AnimationType.CameraRect:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("endValueRect"),
                        new GUIContent("End Rect (B)"));
                    break;
                case DotweenAnimations.AnimationType.UIWidthHeight:
                    DrawUniformOrVector2();
                    break;
            }
        }

        void DrawStartValue(DotweenAnimations.AnimationType type)
        {
            if (!SupportsCustomStartValue(type))
                return;

            var hasStartValue = serializedObject.FindProperty("hasStartValue");
            EditorGUILayout.PropertyField(hasStartValue, new GUIContent("Set Start Value (A)"));
            if (!hasStartValue.boolValue)
                return;

            switch (type)
            {
                case DotweenAnimations.AnimationType.Move:
                case DotweenAnimations.AnimationType.LocalMove:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("startValueV3"),
                        new GUIContent("Start Position (A)"));
                    break;
                case DotweenAnimations.AnimationType.Rotate:
                    if ((DotweenAnimations.TargetType)targetType.enumValueIndex
                        == DotweenAnimations.TargetType.Rigidbody2D)
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("startValueFloat"),
                            new GUIContent("Start Angle (A)"));
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("startValueV3"),
                            new GUIContent("Start Rotation (A)"));
                    }
                    break;
                case DotweenAnimations.AnimationType.LocalRotate:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("startValueV3"),
                        new GUIContent("Start Rotation (A)"));
                    break;
                case DotweenAnimations.AnimationType.Scale:
                    if (serializedObject.FindProperty("useUniformScale").boolValue)
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("startValueFloat"),
                            new GUIContent("Start Scale (A)"));
                    else
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("startValueV3"),
                            new GUIContent("Start Scale (A)"));
                    break;
                case DotweenAnimations.AnimationType.UIWidthHeight:
                    if (serializedObject.FindProperty("useUniformSize").boolValue)
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("startValueFloat"),
                            new GUIContent("Start Size (A)"));
                    else
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("startValueV2"),
                            new GUIContent("Start Size (A)"));
                    break;
                case DotweenAnimations.AnimationType.Color:
                case DotweenAnimations.AnimationType.CameraBackgroundColor:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("startValueColor"),
                        new GUIContent("Start Color (A)"));
                    break;
                case DotweenAnimations.AnimationType.Fade:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("startValueFloat"),
                        new GUIContent("Start Alpha / Intensity (A)"));
                    break;
                case DotweenAnimations.AnimationType.Text:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("startValueString"),
                        new GUIContent("Start Text (A)"));
                    break;
                case DotweenAnimations.AnimationType.CameraAspect:
                case DotweenAnimations.AnimationType.CameraFieldOfView:
                case DotweenAnimations.AnimationType.CameraOrthoSize:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("startValueFloat"),
                        new GUIContent("Start Value (A)"));
                    break;
                case DotweenAnimations.AnimationType.CameraPixelRect:
                case DotweenAnimations.AnimationType.CameraRect:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("startValueRect"),
                        new GUIContent("Start Rect (A)"));
                    break;
            }
        }

        static bool SupportsCustomStartValue(DotweenAnimations.AnimationType type)
        {
            switch (type)
            {
                case DotweenAnimations.AnimationType.Move:
                case DotweenAnimations.AnimationType.LocalMove:
                case DotweenAnimations.AnimationType.Rotate:
                case DotweenAnimations.AnimationType.LocalRotate:
                case DotweenAnimations.AnimationType.Scale:
                case DotweenAnimations.AnimationType.Color:
                case DotweenAnimations.AnimationType.Fade:
                case DotweenAnimations.AnimationType.Text:
                case DotweenAnimations.AnimationType.CameraAspect:
                case DotweenAnimations.AnimationType.CameraBackgroundColor:
                case DotweenAnimations.AnimationType.CameraFieldOfView:
                case DotweenAnimations.AnimationType.CameraOrthoSize:
                case DotweenAnimations.AnimationType.CameraPixelRect:
                case DotweenAnimations.AnimationType.CameraRect:
                case DotweenAnimations.AnimationType.UIWidthHeight:
                    return true;
                default:
                    return false;
            }
        }

        void DrawCallbacks()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Callbacks", EditorStyles.boldLabel);
            DrawEvent("hasOnStart", "onStart", "On Start");
            DrawEvent("hasOnPlay", "onPlay", "On Play");
            DrawEvent("hasOnUpdate", "onUpdate", "On Update");
            DrawEvent("hasOnStepComplete", "onStepComplete", "On Step Complete");
            DrawEvent("hasOnComplete", "onComplete", "On Complete");
            DrawEvent("hasOnRewind", "onRewind", "On Rewind");
            DrawEvent("hasOnTweenCreated", "onTweenCreated", "On Tween Created");
        }

        void DrawRuntimeControls()
        {
            var component = serializedObject.targetObject as DotweenAnimations;
            if (!Application.isPlaying || component == null)
                return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Create / Play"))
                    component.CreateTween(true, true);
                if (GUILayout.Button("Rewind"))
                    component.DORewind();
                if (GUILayout.Button("Restart"))
                    component.DORestart();
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Pause"))
                    component.DOPause();
                if (GUILayout.Button("Play Backwards"))
                    component.DOPlayBackwards();
                if (GUILayout.Button("Complete"))
                    component.DOComplete();
                if (GUILayout.Button("Kill"))
                    component.DOKill();
            }
        }

        void FindCompatibleTarget()
        {
            var animation = (DotweenAnimations.AnimationType)animationType.enumValueIndex;
            if (animation == DotweenAnimations.AnimationType.None)
                return;

            var owner = serializedObject.targetObject as DotweenAnimations;
            if (owner == null)
                return;

            var gameObject = targetIsSelf.boolValue
                ? owner.gameObject
                : targetGO.objectReferenceValue as GameObject;
            if (gameObject == null)
                return;

            foreach (var component in gameObject.GetComponents<Component>())
            {
                if (!IsCompatibleTarget(animationType.enumValueIndex, component))
                    continue;

                animationTarget.objectReferenceValue = component;
                targetType.enumValueIndex = (int)GetTargetType(component);
                return;
            }
        }

        static DotweenAnimations.TargetType GetTargetType(Component component)
        {
            if (component is TextMeshProUGUI)
                return DotweenAnimations.TargetType.TextMeshProUGUI;
            if (component is TextMeshPro)
                return DotweenAnimations.TargetType.TextMeshPro;
            if (component is Text)
                return DotweenAnimations.TargetType.Text;
            if (component is Graphic)
                return DotweenAnimations.TargetType.Image;
            if (component is SpriteRenderer)
                return DotweenAnimations.TargetType.SpriteRenderer;
            if (component is Renderer)
                return DotweenAnimations.TargetType.Renderer;
            return DotweenAnimations.TypeToDOTargetType(component.GetType());
        }

        void DrawVector3()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("endValueV3"), new GUIContent("End Value (B)"));
        }

        void DrawUniformOrVector3()
        {
            var useUniform = serializedObject.FindProperty("useUniformScale");
            EditorGUILayout.PropertyField(useUniform,
                new GUIContent("Use Uniform Scale"));
            if (useUniform.boolValue)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("endValueFloat"), new GUIContent("End Scale (B)"));
            else
                DrawVector3();
        }

        void DrawUniformOrVector2()
        {
            var useUniform = serializedObject.FindProperty("useUniformSize");
            EditorGUILayout.PropertyField(useUniform,
                new GUIContent("Use Uniform Size"));
            if (useUniform.boolValue)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("endValueFloat"), new GUIContent("End Size (B)"));
            else
                EditorGUILayout.PropertyField(serializedObject.FindProperty("endValueV2"), new GUIContent("End Size (B)"));
        }

        void DrawPunchOptions(bool includeSnapping)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("vibrato"), new GUIContent("Vibrato"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("punchElasticity"), new GUIContent("Elasticity"));
            if (includeSnapping)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("punchSnapping"),
                    new GUIContent("Snapping"));
        }

        void DrawShakeOptions(bool includePositionOptions)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("vibrato"), new GUIContent("Vibrato"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("shakeRandomness"), new GUIContent("Randomness"));
            if (includePositionOptions)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("shakeSnapping"),
                    new GUIContent("Snapping"));
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("shakeFadeOut"), new GUIContent("Fade Out"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("shakeRandomnessMode"),
                new GUIContent("Randomness Mode"));
        }

        void DrawEvent(string enabledField, string eventField, string label)
        {
            var enabled = serializedObject.FindProperty(enabledField);
            EditorGUILayout.PropertyField(enabled, new GUIContent(label));
            if (enabled.boolValue)
                EditorGUILayout.PropertyField(serializedObject.FindProperty(eventField), GUIContent.none, true);
        }

        void DrawEnumPopup(SerializedProperty property, GUIContent[] labels, string label)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(label);
                property.enumValueIndex = EditorGUILayout.Popup(property.enumValueIndex, labels);
            }
        }

        static bool IsCompatibleTarget(int animationIndex, Component component)
        {
            if (component == null)
                return false;

            var type = (DotweenAnimations.AnimationType)animationIndex;
            if (type == DotweenAnimations.AnimationType.None)
                return false;

            Type componentType = component.GetType();
            switch (type)
            {
                case DotweenAnimations.AnimationType.Move:
                case DotweenAnimations.AnimationType.Rotate:
                    return component is Transform || component is RectTransform
                        || component is Rigidbody || component is Rigidbody2D;
                case DotweenAnimations.AnimationType.LocalMove:
                case DotweenAnimations.AnimationType.LocalRotate:
                case DotweenAnimations.AnimationType.Scale:
                case DotweenAnimations.AnimationType.PunchScale:
                case DotweenAnimations.AnimationType.PunchRotation:
                case DotweenAnimations.AnimationType.ShakeScale:
                case DotweenAnimations.AnimationType.ShakeRotation:
                    return component is Transform;
                case DotweenAnimations.AnimationType.Color:
                    return component is Renderer || component is Light || component is SpriteRenderer
                        || component is Graphic || component is TextMeshPro || component is TextMeshProUGUI;
                case DotweenAnimations.AnimationType.Fade:
                    return component is Renderer || component is Light || component is SpriteRenderer
                        || component is Graphic || component is CanvasGroup
                        || component is TextMeshPro || component is TextMeshProUGUI;
                case DotweenAnimations.AnimationType.Text:
                    return component is Text || component is TextMeshPro || component is TextMeshProUGUI;
                case DotweenAnimations.AnimationType.PunchPosition:
                case DotweenAnimations.AnimationType.ShakePosition:
                    return component is Transform || component is RectTransform;
                case DotweenAnimations.AnimationType.CameraAspect:
                case DotweenAnimations.AnimationType.CameraBackgroundColor:
                case DotweenAnimations.AnimationType.CameraFieldOfView:
                case DotweenAnimations.AnimationType.CameraOrthoSize:
                case DotweenAnimations.AnimationType.CameraPixelRect:
                case DotweenAnimations.AnimationType.CameraRect:
                    return component is Camera;
                case DotweenAnimations.AnimationType.UIWidthHeight:
                    return component is RectTransform;
                default:
                    return false;
            }
        }
    }
}
