using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace TuanTool
{
    public class TutorialComponent : MonoBehaviour, ITurtorialComponent
    {

        [SerializeField] private bool autoHandleInteraction = true;
        [SerializeField] private int _sortLayerLayer = 1010;
        Canvas canvas;
        GraphicRaycaster raycaster;

        public Action OnInteract { get; set; }
        public Button[] BtnClickConfirmList { get => btnClickConfirmList; }
        public Button BtnClickConfirm { get => btnClickConfirm; }

        [SerializeField] GameObject objectTut;
        [SerializeField] Button btnClickConfirm;
        [SerializeField] Button[] btnClickConfirmList;

        private void Awake()
        {
            if (objectTut)
                objectTut?.gameObject.SetActive(false);

            btnClickConfirm?.onClick.AsObservable().Subscribe(delegate
                            {
                                Interact();

                            }).AddTo(this);

            foreach (var item in btnClickConfirmList)
            {
                item.onClick.AsObservable().Subscribe(delegate
                {
                    Interact();

                }).AddTo(this);
            }


        }

        public void Interact()
        {
            OnInteract?.Invoke();
        }

        public void Hide(Action OnComplete)
        {
            try
            {
                Destroy(raycaster);
                Destroy(canvas);
            }
            catch (System.Exception)
            {

                throw;
            }
            finally
            {

                canvas = null;
                raycaster = null;
                if (objectTut)
                    objectTut?.gameObject.SetActive(false);
                OnComplete?.Invoke();
            }
        }

        public void Show(Action OnComplete)
        {
            try
            {
                if (objectTut)
                    objectTut?.gameObject.SetActive(true);
                var a = gameObject.GetComponent<Canvas>();
                if (a == null)
                    a = gameObject.AddComponent<Canvas>();
                canvas = a;
                canvas.overrideSorting = true;
                canvas.sortingLayerID = SortingLayer.layers.Last().id;
                canvas.sortingOrder = _sortLayerLayer;
                raycaster = GetComponent<GraphicRaycaster>();
                if (raycaster == null)
                {
                    raycaster = gameObject.AddComponent<GraphicRaycaster>();
                }
            }
            catch (System.Exception)
            {

                throw;
            }
            finally
            {
                OnComplete?.Invoke();
            }
        }

        public void ShowOnlyObjectTut(Action OnComplete)
        {
            if (objectTut)
                objectTut?.gameObject.SetActive(true);
            OnComplete?.Invoke();
        }

        public void HideOnlyObjectTut(Action OnComplete)
        {
            if (objectTut)
                objectTut?.gameObject.SetActive(false);
            OnComplete?.Invoke();
        }
    }

    public interface ITurtorialComponent
    {
        Action OnInteract { get; set; }
        void Show(Action OnComplete);
        void Hide(Action OnComplete);
        void ShowOnlyObjectTut(Action OnComplete);
        void HideOnlyObjectTut(Action OnComplete);

    }
}
