using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using TuanTool;
using UniRx;
using UnityEngine;

namespace UITool.Popup
{
    [AddComponentMenu("UI manager/Task For Dotween Pro")]
    public class TaskForDotweenPro : MonoBehaviour, IUITask
    {
        [field: SerializeField] public string Description { get; set; }

        [Header("Gameobject to active")]
        [SerializeField] private GameObject activeObject;
        [SerializeField] private bool isActiveOnExecute = true;

        [Header("Tween to run")]
        [SerializeField] private bool isRunBackwards = false;
        [SerializeField] private DOTweenAnimation[] dotweenAnimationComponent;

        bool isInit = false;

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

            if (dotweenAnimationComponent.Count() == 0)
                dotweenAnimationComponent = GetComponents<DOTweenAnimation>();

            if (isActiveOnExecute)
                activeObject.SetActive(false);
        }

        public async UniTask Execute(CancellationToken cancellationToken)
        {
            try
            {

                Init();
                // await UniTask.DelayFrame(2, cancellationToken: cancellationToken);
                Debug.Log($"Execute: {isActiveOnExecute} {isRunBackwards}");

                if (isActiveOnExecute)
                    activeObject.SetActive(true);

                List<UniTask> tasks = new();

                foreach (var item in dotweenAnimationComponent)
                {
                    item.CreateTween(false, false);
                    if (isRunBackwards)
                    {
                        item.DOPlayBackwards();
                        item.easeCurve = item.easeCurve.InvertX();
                    }
                    else
                    {
                        item.DORestart();
                        Observable.TimerFrame(1).Subscribe(_ =>
                        {
                            item.DORestart(fromHere: true);
                        });
                    }

                    // var tweenCompleted = Observable.FromEvent<TweenCallback, Unit>(
                    //     h => () => h(Unit.Default),   // convert Action -> Unit
                    //     h => item.tween.onComplete += h,
                    //     h => item.tween.onComplete -= h
                    // );
                    // await tweenCompleted.ToUniTask(cancellationToken: cancellationToken);
                    // Observable.FromEvent(() => item.tween.onComplete, h => item.tween.onComplete += h, h => item.tween.onComplete -= h)

                    bool isCOmpleted = false;
                    item.tween.onComplete -= _onComplete;
                    item.tween.onRewind -= _onComplete;
                    item.tween.onComplete += _onComplete;
                    item.tween.onRewind += _onComplete;

                    tasks.Add(UniTask.WaitUntil(() => isCOmpleted, cancellationToken: cancellationToken));

                    void _onComplete()
                    {
                        // Debug.Log($"Complete: {item.name}");
                        isCOmpleted = true;
                        item.tween.onComplete -= _onComplete;
                        item.tween.onRewind -= _onComplete;
                    }
                }

                await UniTask.WhenAll(tasks);
                if (!isActiveOnExecute)
                    activeObject.SetActive(false);
                Debug.Log($"Complete open task");
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
            await UniTask.CompletedTask;
        }

        [Button]
        public void ForceRunEnd()
        {
            Init();
            if (isActiveOnExecute)
                activeObject.SetActive(true);
            foreach (var item in dotweenAnimationComponent)
            {
                item.CreateTween(false, false);
                item.DOComplete(false);
            }
            if (!isActiveOnExecute)
                activeObject.SetActive(false);
        }
    }
}
