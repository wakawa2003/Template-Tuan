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
        public bool IsShowing { get; private set; }
        [field: SerializeField] public bool IsUseQueue { get; private set; } = true;


        [SerializeField] List<InterfaceReference<IUITask>> TasksParallelsBeforeOpen;
        [SerializeField] List<InterfaceReference<IUITask>> TasksParallelsBeforeHide;

        public void Reset()
        {
            TasksParallelsBeforeOpen = new List<InterfaceReference<IUITask>>();
            TasksParallelsBeforeHide = new List<InterfaceReference<IUITask>>();

            foreach (var item in GetComponentsInChildren<IUITaskOpen>(true))
            {
                var a = new InterfaceReference<IUITask>();
                a.Value = item;
                TasksParallelsBeforeOpen.Add(a);
            }

            foreach (var item in GetComponentsInChildren<IUITaskClose>(true))
            {
                var a = new InterfaceReference<IUITask>();
                a.Value = item;
                TasksParallelsBeforeHide.Add(a);
            }
        }

        public async UniTask Hide()
        {
            try
            {

                IPopup.OnPopupStartClose?.Invoke(this);

                List<UniTask> tasks = new List<UniTask>();
                foreach (var task in TasksParallelsBeforeHide)
                {
                    tasks.Add(task.Value.Execute(destroyCancellationToken));
                    Debug.Log($"run {task.Value}");
                }
                await UniTask.WhenAll(tasks);
                await OnCompleteHide();

                IPopup.OnPopupClosed?.Invoke(this);

            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
            }
            IsShowing = false;
        }


        public async UniTask Show(object payloadData)
        {
            try
            {
                IsShowing = true;
                IPopup.OnPopupStartOpen?.Invoke(this);
                List<UniTask> tasks = new List<UniTask>();
                foreach (var task in TasksParallelsBeforeOpen)
                {
                    tasks.Add(task.Value.Execute(destroyCancellationToken));
                    Debug.Log($"run {task.Value}");
                }
                await UniTask.WhenAll(tasks);
                await OnCompleteShow(payloadData);

                IPopup.OnPopupOpened?.Invoke(this);

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

        public string CallRequestShow(object payloadData)
        {
            return (this as IPopup).RequestShow(payloadData);
        }
    }
}
