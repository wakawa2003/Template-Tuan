using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace TuanTool.Popup
{
    public abstract class PopupBase : MonoBehaviour, IPopup
    {
        [SerializeField] private RectTransform blackBG;
        [SerializeField] private RectTransform content;

        public bool IsShowing { get; private set; }

        protected virtual void Awake()
        {
            if (blackBG != null)
                blackBG.gameObject.SetActive(false);
            if (content != null)
                content.gameObject.SetActive(false);
        }

        public async UniTask Hide()
        {
            try
            {
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
