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
            if (!HasValidTargets())
                return;

            CacheProperties();
        }

        void CacheProperties()
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
            if (!HasValidTargets())
            {
                EditorGUILayout.HelpBox("The animation target was destroyed or is no longer available.", MessageType.Warning);
                return;
            }

            if (animationType == null)
                CacheProperties();

            if (animationType == null || targetIsSelf == null || targetGO == null
                || animationTarget == null || targetType == null || forcedTargetType == null
                || isValid == null || useTargetAsV3 == null || easeType == null || easeCurve == null)
            {
                EditorGUILayout.HelpBox("The animation component is missing required serialized fields.", MessageType.Error);
                return;
            }

            serializedObject.Update();

            var anim = target as DotweenAnimations;
            string detail = anim != null ? anim.Detail : "";
            string displayName = string.IsNullOrEmpty(detail)
                ? "Dotween Animations"
                : $"Dotween Animations - {detail}";

            EditorGUILayout.LabelField(displayName, EditorStyles.boldLabel);
            EditorGUILayout.Space();

            DrawDetail();
            DrawActivation();
            DrawTarget();
            DrawTweenSettings();
            DrawEndValue();
            DrawCallbacks();
            DrawRuntimeControls();

            serializedObject.ApplyModifiedProperties();
        }

        bool HasValidTargets()
        {
            var editorTargets = targets;
            if (editorTargets == null || editorTargets.Length == 0)
                return false;

            foreach (var editorTarget in editorTargets)
            {
                if (editorTarget == null)
                    return false;
            }

            return true;
        }

        void DrawProperty(SerializedProperty property, GUIContent label = null, bool includeChildren = false)
        {
            if (property == null)
                return;

            if (label == null)
                EditorGUILayout.PropertyField(property, includeChildren);
            else
                EditorGUILayout.PropertyField(property, label, includeChildren);
        }

        void DrawProperty(string propertyName, GUIContent label = null, bool includeChildren = false)
        {
            DrawProperty(serializedObject.FindProperty(propertyName), label, includeChildren);
        }

        void DrawDetail()
        {
            DrawProperty(serializedObject.FindProperty("Detail"),
                new GUIContent("Detail"));
            EditorGUILayout.Space();
        }

        void DrawActivation()
        {
            EditorGUILayout.LabelField("Animation", EditorStyles.boldLabel);
            DrawProperty(serializedObject.FindProperty("isActive"), new GUIContent("Active"));
            DrawProperty(serializedObject.FindProperty("autoGenerate"), new GUIContent("Auto Generate"));
            DrawProperty(serializedObject.FindProperty("autoPlay"), new GUIContent("Auto Play"));
            DrawEnumPopup(animationType, AnimationLabels, "Animation Type");
        }

        void DrawTarget()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Target", EditorStyles.boldLabel);
            DrawProperty(targetIsSelf, new GUIContent("Animate This GameObject"));

            if (!targetIsSelf.boolValue)
                DrawProperty(targetGO, new GUIContent("Target GameObject"));

            DrawProperty(animationTarget, new GUIContent("Target Component"));
            DrawFadeTargetSelector();
            DrawProperty(serializedObject.FindProperty("tweenTargetIsTargetGO"),
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

            var owner = target as DotweenAnimations;
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
            DrawProperty(serializedObject.FindProperty("duration"));
            DrawProperty(serializedObject.FindProperty("delay"));
            DrawEnumPopup(easeType, EaseLabels, "Ease");
            if (easeType.enumValueIndex == (int)Ease.INTERNAL_Custom)
                DrawProperty(easeCurve, new GUIContent("Ease Curve"));

            DrawProperty(serializedObject.FindProperty("loopType"));
            DrawProperty(serializedObject.FindProperty("loops"));
            DrawProperty(serializedObject.FindProperty("id"));
            DrawProperty(serializedObject.FindProperty("isRelative"));
            var hasStartValue = serializedObject.FindProperty("hasStartValue");
            if (hasStartValue != null)
            {
                using (new EditorGUI.DisabledScope(hasStartValue.boolValue))
                    DrawProperty(serializedObject.FindProperty("isFrom"));
                if (hasStartValue.boolValue)
                    EditorGUILayout.HelpBox("Custom start and end values are absolute; Relative and From are ignored.", MessageType.Info);
            }
            DrawProperty(serializedObject.FindProperty("isSpeedBased"));
            DrawProperty(serializedObject.FindProperty("isIndependentUpdate"));
            DrawProperty(serializedObject.FindProperty("autoKill"));
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
                    DrawProperty(useTargetAsV3, new GUIContent("Use Transform as Target"));
                    if (useTargetAsV3 != null && useTargetAsV3.boolValue)
                        DrawProperty(serializedObject.FindProperty("endValueTransform"),
                            new GUIContent("End Transform (B)"));
                    else
                        DrawVector3();
                    DrawProperty(serializedObject.FindProperty("snapping"));
                    break;
                case DotweenAnimations.AnimationType.LocalMove:
                    DrawVector3();
                    DrawProperty(serializedObject.FindProperty("snapping"));
                    break;
                case DotweenAnimations.AnimationType.Rotate:
                case DotweenAnimations.AnimationType.LocalRotate:
                    DrawVector3();
                    DrawProperty(serializedObject.FindProperty("rotationMode"),
                        new GUIContent("Rotation Mode"));
                    break;
                case DotweenAnimations.AnimationType.Scale:
                    DrawUniformOrVector3();
                    break;
                case DotweenAnimations.AnimationType.Color:
                case DotweenAnimations.AnimationType.CameraBackgroundColor:
                    DrawProperty(serializedObject.FindProperty("endValueColor"),
                        new GUIContent("End Color (B)"));
                    break;
                case DotweenAnimations.AnimationType.Fade:
                case DotweenAnimations.AnimationType.CameraAspect:
                case DotweenAnimations.AnimationType.CameraFieldOfView:
                case DotweenAnimations.AnimationType.CameraOrthoSize:
                    DrawProperty(serializedObject.FindProperty("endValueFloat"),
                        new GUIContent(type == DotweenAnimations.AnimationType.Fade
                            ? "End Alpha / Intensity (B)"
                            : "End Value (B)"));
                    break;
                case DotweenAnimations.AnimationType.Text:
                    DrawProperty(serializedObject.FindProperty("endValueString"),
                        new GUIContent("End Text (B)"));
                    DrawProperty(serializedObject.FindProperty("richTextEnabled"),
                        new GUIContent("Rich Text Enabled"));
                    DrawProperty(serializedObject.FindProperty("scrambleMode"),
                        new GUIContent("Scramble Mode"));
                    var scrambleMode = serializedObject.FindProperty("scrambleMode");
                    if (scrambleMode != null
                        && (ScrambleMode)scrambleMode.enumValueIndex != ScrambleMode.None)
                        DrawProperty(serializedObject.FindProperty("customScrambleCharacters"),
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
                    DrawProperty(serializedObject.FindProperty("endValueRect"),
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
            DrawProperty(hasStartValue, new GUIContent("Set Start Value (A)"));
            if (hasStartValue == null || !hasStartValue.boolValue)
                return;

            switch (type)
            {
                case DotweenAnimations.AnimationType.Move:
                case DotweenAnimations.AnimationType.LocalMove:
                    DrawProperty(serializedObject.FindProperty("startValueV3"),
                        new GUIContent("Start Position (A)"));
                    break;
                case DotweenAnimations.AnimationType.Rotate:
                    if ((DotweenAnimations.TargetType)targetType.enumValueIndex
                        == DotweenAnimations.TargetType.Rigidbody2D)
                    {
                        DrawProperty(serializedObject.FindProperty("startValueFloat"),
                            new GUIContent("Start Angle (A)"));
                    }
                    else
                    {
                        DrawProperty(serializedObject.FindProperty("startValueV3"),
                            new GUIContent("Start Rotation (A)"));
                    }
                    break;
                case DotweenAnimations.AnimationType.LocalRotate:
                    DrawProperty(serializedObject.FindProperty("startValueV3"),
                        new GUIContent("Start Rotation (A)"));
                    break;
                case DotweenAnimations.AnimationType.Scale:
                    var useUniformScale = serializedObject.FindProperty("useUniformScale");
                    if (useUniformScale != null && useUniformScale.boolValue)
                        DrawProperty(serializedObject.FindProperty("startValueFloat"),
                            new GUIContent("Start Scale (A)"));
                    else
                        DrawProperty(serializedObject.FindProperty("startValueV3"),
                            new GUIContent("Start Scale (A)"));
                    break;
                case DotweenAnimations.AnimationType.UIWidthHeight:
                    var useUniformSize = serializedObject.FindProperty("useUniformSize");
                    if (useUniformSize != null && useUniformSize.boolValue)
                        DrawProperty(serializedObject.FindProperty("startValueFloat"),
                            new GUIContent("Start Size (A)"));
                    else
                        DrawProperty(serializedObject.FindProperty("startValueV2"),
                            new GUIContent("Start Size (A)"));
                    break;
                case DotweenAnimations.AnimationType.Color:
                case DotweenAnimations.AnimationType.CameraBackgroundColor:
                    DrawProperty(serializedObject.FindProperty("startValueColor"),
                        new GUIContent("Start Color (A)"));
                    break;
                case DotweenAnimations.AnimationType.Fade:
                    DrawProperty(serializedObject.FindProperty("startValueFloat"),
                        new GUIContent("Start Alpha / Intensity (A)"));
                    break;
                case DotweenAnimations.AnimationType.Text:
                    DrawProperty(serializedObject.FindProperty("startValueString"),
                        new GUIContent("Start Text (A)"));
                    break;
                case DotweenAnimations.AnimationType.CameraAspect:
                case DotweenAnimations.AnimationType.CameraFieldOfView:
                case DotweenAnimations.AnimationType.CameraOrthoSize:
                    DrawProperty(serializedObject.FindProperty("startValueFloat"),
                        new GUIContent("Start Value (A)"));
                    break;
                case DotweenAnimations.AnimationType.CameraPixelRect:
                case DotweenAnimations.AnimationType.CameraRect:
                    DrawProperty(serializedObject.FindProperty("startValueRect"),
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
            var component = target as DotweenAnimations;
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

            var owner = target as DotweenAnimations;
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
            if (component == null)
                return DotweenAnimations.TargetType.Unset;

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
            DrawProperty(serializedObject.FindProperty("endValueV3"), new GUIContent("End Value (B)"));
        }

        void DrawUniformOrVector3()
        {
            var useUniform = serializedObject.FindProperty("useUniformScale");
            DrawProperty(useUniform,
                new GUIContent("Use Uniform Scale"));
            if (useUniform != null && useUniform.boolValue)
                DrawProperty(serializedObject.FindProperty("endValueFloat"), new GUIContent("End Scale (B)"));
            else
                DrawVector3();
        }

        void DrawUniformOrVector2()
        {
            var useUniform = serializedObject.FindProperty("useUniformSize");
            DrawProperty(useUniform,
                new GUIContent("Use Uniform Size"));
            if (useUniform != null && useUniform.boolValue)
                DrawProperty(serializedObject.FindProperty("endValueFloat"), new GUIContent("End Size (B)"));
            else
                DrawProperty(serializedObject.FindProperty("endValueV2"), new GUIContent("End Size (B)"));
        }

        void DrawPunchOptions(bool includeSnapping)
        {
            DrawProperty(serializedObject.FindProperty("vibrato"), new GUIContent("Vibrato"));
            DrawProperty(serializedObject.FindProperty("punchElasticity"), new GUIContent("Elasticity"));
            if (includeSnapping)
                DrawProperty(serializedObject.FindProperty("punchSnapping"),
                    new GUIContent("Snapping"));
        }

        void DrawShakeOptions(bool includePositionOptions)
        {
            DrawProperty(serializedObject.FindProperty("vibrato"), new GUIContent("Vibrato"));
            DrawProperty(serializedObject.FindProperty("shakeRandomness"), new GUIContent("Randomness"));
            if (includePositionOptions)
            {
                DrawProperty(serializedObject.FindProperty("shakeSnapping"),
                    new GUIContent("Snapping"));
            }
            DrawProperty(serializedObject.FindProperty("shakeFadeOut"), new GUIContent("Fade Out"));
            DrawProperty(serializedObject.FindProperty("shakeRandomnessMode"),
                new GUIContent("Randomness Mode"));
        }

        void DrawEvent(string enabledField, string eventField, string label)
        {
            var enabled = serializedObject.FindProperty(enabledField);
            DrawProperty(enabled, new GUIContent(label));
            if (enabled != null && enabled.boolValue)
                DrawProperty(serializedObject.FindProperty(eventField), GUIContent.none, true);
        }

        void DrawEnumPopup(SerializedProperty property, GUIContent[] labels, string label)
        {
            if (property == null || labels == null || labels.Length == 0)
                return;

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
