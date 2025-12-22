using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AYellowpaper;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace UITool.Popup
{
    public abstract class PopupBase : MonoBehaviour, IPopup
    {
        [SerializeField] List<InterfaceReference<IUITask>> TasksParallelsBeforeOpen;
        [SerializeField] List<InterfaceReference<IUITask>> TasksParallelsBeforeHide;

        public bool IsShowing { get; private set; }

        public async UniTask Hide()
        {
            try
            {
                List<UniTask> tasks = new List<UniTask>();
                foreach (var task in TasksParallelsBeforeHide)
                {
                    tasks.Add(task.Value.Execute(destroyCancellationToken));
                    Debug.Log($"run {task.Value}");
                }
                await UniTask.WhenAll(tasks);
                await OnCompleteHide();
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
            }
            IsShowing = false;
        }

        public async UniTask Show(object payloadData)
        {
            IsShowing = true;

            try
            {
                List<UniTask> tasks = new List<UniTask>();
                foreach (var task in TasksParallelsBeforeOpen)
                {
                    tasks.Add(task.Value.Execute(destroyCancellationToken));
                    Debug.Log($"run {task.Value}");
                }
                await UniTask.WhenAll(tasks);
                await OnCompleteShow(payloadData);
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
            }
        }

        public abstract UniTask OnCompleteShow(object payloadData);
        public virtual UniTask OnCompleteHide()
        {
            return UniTask.CompletedTask;

        }
    }
}
