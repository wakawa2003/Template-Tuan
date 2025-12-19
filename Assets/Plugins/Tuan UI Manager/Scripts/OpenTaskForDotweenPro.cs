using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AmplifyShaderEditor;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using DG.Tweening.Core;
using UniRx;
using UnityEngine;

namespace TuanTool.Popup
{
    [AddComponentMenu("UI manager/Open Task For Dotween Pro")]
    public class OpenTaskForDotweenPro : MonoBehaviour, IUITask
    {
        [SerializeField] private GameObject activeObject;
        [SerializeField] private bool isActiveOnExecute = true;
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
            activeObject.SetActive(false);

        }

        public async UniTask Execute(CancellationToken cancellationToken)
        {
            try
            {

                Init();
                await UniTask.DelayFrame(2, cancellationToken: cancellationToken);
                Debug.Log($"Execute: {isActiveOnExecute} {isRunBackwards}");

                if (isActiveOnExecute)
                    activeObject.SetActive(true);

                List<UniTask> tasks = new();

                foreach (var item in dotweenAnimationComponent)
                {
                    item.CreateTween(false, false);
                    if (isRunBackwards)
                        item.DOPlayBackwards();
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
                    item.tween.onComplete += (() =>
                     {
                         Debug.Log($"Complete: {item.name}");
                         isCOmpleted = true;
                     });

                    item.tween.onRewind += (() =>
                    {
                        Debug.Log($"Complete onRewind:  {item.name}");
                        isCOmpleted = true;
                    });

                    tasks.Add(UniTask.WaitUntil(() => isCOmpleted, cancellationToken: cancellationToken));
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

        // private void Update()
        // {
        //     foreach (var item in dotweenAnimationComponent)
        //     {
        //         item.DORestart(fromHere: true);
        //     }
        // }
    }
}
