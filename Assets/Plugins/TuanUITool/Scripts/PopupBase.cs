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

        public virtual async UniTask Hide()
        {
            try
            {

                IPopup.OnPopupStartClose?.Invoke(this);

                List<UniTask> tasks = new List<UniTask>();
                foreach (var task in TasksParallelsBeforeHide)
                {
                    tasks.Add(task.Value.Execute(destroyCancellationToken));
                    // Debug.Log($"run {task.Value}");
                }
                await OnBeforeHide();
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

        public void HideFromButton()
        {
            Hide().Forget();
        }


        public virtual async UniTask Show(object payloadData)
        {
            try
            {
                IsShowing = true;
                IPopup.OnPopupStartOpen?.Invoke(this);
                List<UniTask> tasks = new List<UniTask>();
                foreach (var task in TasksParallelsBeforeOpen)
                {
                    tasks.Add(task.Value.Execute(destroyCancellationToken));
                    // Debug.Log($"run {task.Value}");
                }
                await OnBeforeShow(payloadData);
                await UniTask.WhenAll(tasks);
                await OnCompleteShow(payloadData);

                IPopup.OnPopupOpened?.Invoke(this);

            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
            }
        }

        /// <summary>
        /// Before Running Anims Show
        /// </summary>
        /// <param name="payloadData"></param>
        /// <returns></returns>
        public virtual UniTask OnBeforeShow(object payloadData)
        {
            return UniTask.CompletedTask;
        }


        /// <summary>
        /// Before Running Anims Hide
        /// </summary>
        /// <returns></returns>
        public virtual UniTask OnBeforeHide()
        {
            return UniTask.CompletedTask;

        }


        public virtual UniTask OnCompleteShow(object payloadData)
        {
            return UniTask.CompletedTask;
        }

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
