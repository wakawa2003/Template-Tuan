using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AYellowpaper;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace UITool.Popup
{
    public class PopupManager : MonoBehaviour, IPopupManager
    {
        public UnityEvent<string> OnClosePopupEvent;
        [SerializeField] private List<RequestData> requestQueue = new List<RequestData>();
        [SerializeField] private List<InterfaceReference<IPopup>> popupsOpening = new List<InterfaceReference<IPopup>>();

        public ReadOnlyCollection<InterfaceReference<IPopup>> PopupsOpening { get => popupsOpening.AsReadOnly(); }
        public ReadOnlyCollection<RequestData> RequestQueue { get => requestQueue.AsReadOnly(); }

        #region Singleton
        private static PopupManager ins;
        public static PopupManager Ins
        {
            get
            {
                if (ins == null)
                {
                    var a = new GameObject("Popup Manager").AddComponent<PopupManager>();
                    a?.Awake();
                }
                return ins;
            }
            set => ins = value;
        }

        #endregion

        private void Awake()
        {
            #region Singleton
            if (ins == null)
                ins = this;
            else
            {
                if (ins != this)
                    Destroy(gameObject);
                return;
            }
            #endregion
            DontDestroyOnLoad(gameObject);

            requestQueue.Clear();
            popupsOpening.Clear();

            UniTask.Create(async (cancelTokken) =>
            {
                while (true)
                {
                    if (requestQueue.Count > 0)
                    {
                        var request = requestQueue[0];
                        if (!request.Popup.Value.IsShowing)
                        {
                            try
                            {
                                await request.Show();
                                await UniTask.WaitUntil(() => !request.Popup.Value.IsShowing);
                            }
                            catch (System.Exception e)
                            {
                                Debug.LogException(e);
                            }
                            OnClosePopupEvent?.Invoke(request.ID);
                            requestQueue.RemoveAt(0);
                        }
                    }
                    await UniTask.NextFrame();
                }
            }, cancellationToken: destroyCancellationToken).Forget();


            IPopup.OnPopupOpened.AddListener(OnPopupOpened);
            IPopup.OnPopupClosed.AddListener(OnPopupClosed);
        }

        void OnDestroy()
        {

            IPopup.OnPopupOpened.RemoveListener(OnPopupOpened);
            IPopup.OnPopupClosed.RemoveListener(OnPopupClosed);
        }

        private void OnPopupClosed(IPopup popup)
        {
            if (popup != null)
                popupsOpening.RemoveAll(_ => _.Value == popup);
        }

        private void OnPopupOpened(IPopup arg0)
        {
            var a = new InterfaceReference<IPopup>();
            a.Value = arg0;
            popupsOpening.Add(a);
        }

        public string AddShowPopupRequest(IPopup popup, object payloadData = null)
        {
            RequestData requestData = new RequestData(GetNewID(), payloadData, popup);
            requestQueue.Add(requestData);
            return requestData.ID;
        }

        string GetNewID()
        {
            return "Popup_" + System.Guid.NewGuid().ToString();
        }

        [System.Serializable]
        public class RequestData
        {
            public RequestData(string id, object payloadData, IPopup popup)
            {
                Debug.Log($"tao request: {id}");
                this.id = id;
                this.payloadData = payloadData;
                var p = new InterfaceReference<IPopup>();
                p.Value = popup;
                this.popup = p;
            }

            [SerializeField] private string id;
            [SerializeField] private InterfaceReference<IPopup> popup;
            private object payloadData;

            public string ID { get => id; private set => id = value; }
            public object PayloadData { get => payloadData; private set => payloadData = value; }
            public InterfaceReference<IPopup> Popup { get => popup; private set => popup = value; }
            public async UniTask Show()
            {
                try
                {
                    Debug.Log($"request show");
                    // Show popup logic
                    await popup.Value.Show(payloadData);
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
    }
}
