using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace UITool.Popup
{
    [AddComponentMenu("UI manager/Task For Dotween Pro")]
    public class TaskForDotweenPro : MonoBehaviour, IUITask
    {


        protected virtual string DefaultDescription => "Tween Animation";

        [Header("Gameobject to active")]
        [SerializeField] private GameObject activeObject;
        public virtual bool IsActiveOnExecute => true;

        [Header("Tween to run")]
        [SerializeField] private DotweenAnimations[] dotweenAnimationComponent;
        public virtual bool IsRunBackwards => true;

        bool isInit = false;

        protected virtual void Reset()
        {
            activeObject = gameObject;
            // dotweenAnimationComponent = new DotweenAnimations[] { };
            foreach (var item in GetComponentsInChildren<DotweenAnimations>(true))
            {
                if (!dotweenAnimationComponent.Contains(item))
                {
                    dotweenAnimationComponent = dotweenAnimationComponent.Append(item).ToArray();
                }
            }
        }

        void Start()
        {
            Init();
        }

        private void Init()
        {
            if (isInit) return;
            isInit = true;
            if (activeObject == null)
                activeObject = gameObject;

            // if (dotweenAnimationComponent.Count() == 0)
            //     dotweenAnimationComponent = GetComponents<DotweenAnimations>();

            if (IsActiveOnExecute)
                activeObject.SetActive(false);
        }

        public async UniTask Execute(CancellationToken cancellationToken)
        {
            try
            {

                Init();
                // await UniTask.DelayFrame(2, cancellationToken: cancellationToken);
                // Debug.Log($"Execute: {isActiveOnExecute} {isRunBackwards}");

                List<UniTask> tasks = new();
                List<DotweenAnimations> animationsToPlay = new();

                foreach (var item in dotweenAnimationComponent)
                {
                    if (item == null)
                    {
                        Debug.LogError($"{nameof(TaskForDotweenPro)} on '{name}' has an empty animation entry.", this);
                        continue;
                    }

                    item.CreateTween(true, false);
                    Tween tween = item.tween;
                    if (tween == null || !tween.active)
                    {
                        string targetType = item.target != null
                            ? item.target.GetType().Name
                            : "None";
                        Debug.LogError(
                            $"Dotween animation '{item.name}' could not create a tween " +
                            $"(isValid: {item.isValid}, animation: {item.animationType}, " +
                            $"target: {targetType}, configured target type: {item.targetType}). " +
                            "Check that the Target Component matches the configured target type.",
                            item);
                        continue;
                    }

                    tween.Pause();
                    bool isCompleted = false;
                    tween.onComplete += OnComplete;
                    tween.onRewind += OnComplete;

                    animationsToPlay.Add(item);
                    tasks.Add(WaitForCompletion());

                    void OnComplete()
                    {
                        isCompleted = true;
                        tween.onComplete -= OnComplete;
                        tween.onRewind -= OnComplete;
                    }

                    async UniTask WaitForCompletion()
                    {
                        try
                        {
                            await UniTask.WaitUntil(() => isCompleted, cancellationToken: cancellationToken);
                        }
                        finally
                        {
                            tween.onComplete -= OnComplete;
                            tween.onRewind -= OnComplete;
                        }
                    }
                }

                if (IsActiveOnExecute)
                    activeObject.SetActive(true);

                foreach (var item in animationsToPlay)
                {
                    Tween tween = item.tween;
                    if (IsRunBackwards)
                    {
                        tween.PlayBackwards();
                        item.easeCurve = item.easeCurve.InvertX();
                    }
                    else
                    {
                        tasks.Add(RestartTweenFromCurrentPositionNextFrame(item, cancellationToken));
                    }
                }

                await UniTask.WhenAll(tasks);
                if (!IsActiveOnExecute)
                    activeObject.SetActive(false);
                // Debug.Log($"Complete open task");
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
            await UniTask.CompletedTask;
        }

        static async UniTask RestartTweenFromCurrentPositionNextFrame(
            DotweenAnimations animation,
            CancellationToken cancellationToken)
        {
            await UniTask.NextFrame(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            animation.DORestartThisTween(fromHere: true);
        }

        [Button]
        public void ForceRunEnd()
        {
            Init();
            if (IsActiveOnExecute)
                activeObject.SetActive(true);
            foreach (var item in dotweenAnimationComponent)
            {
                item.CreateTween(false, false);
                item.DOComplete(false);
            }
            if (!IsActiveOnExecute)
                activeObject.SetActive(false);
        }
    }
}
