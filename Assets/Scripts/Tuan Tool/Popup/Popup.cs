


using UnityEngine;

using System;

using DG.Tweening;

using Sirenix.OdinInspector;
using UnityEngine.UI;
using UnityEngine.Events;

namespace TuanTool.Popup
{
    public class Popup : MonoBehaviour
    {

        [ReadOnly]
        public bool IsShowing
        {
            get
            {
                if (content == null)
                    return false;
                return content.gameObject.activeSelf && gameObject.activeSelf;
            }
        }

        [Tooltip("Đưa popup vào PopupManager để hiển thị lần lượt, không chồng lên nhau.")]
        public bool IsUseManager = true;
        [Tooltip("Co content theo vùng an toàn (notch) của màn hình.")]
        public bool IsUseSafeArea = true;
        [Tooltip("Ẩn content ngay khi Awake.")]
        public bool HideOnAwake = true;
        [Tooltip("Tween không bị ảnh hưởng bởi Time.timeScale.")]
        public bool IgnoreScaleTime = true;
        [Tooltip("Nền đen phía sau popup (có Image và CanvasGroup). Để trống nếu không cần.")]
        [OnValueChanged(nameof(onBGCHange))] public RectTransform blackScreen;

        private bool onBGCHange()
        {
            canvasGroupBG = blackScreen?.GetComponent<CanvasGroup>();
            return true;
        }

        [Tooltip("Phần nội dung chính của popup, sẽ được animate khi Show/Hide.")]
        [Required][OnValueChanged(nameof(oncontentCHange))] public RectTransform content;
        [Tooltip("CanvasGroup của content và nền, tự lấy khi gán trong Inspector.")]
        [SerializeField] CanvasGroup canvasGroupContent, canvasGroupBG;

        private bool oncontentCHange()
        {
            canvasGroupContent = content?.GetComponent<CanvasGroup>();
            return true;
        }

        [Tooltip("X = alpha khi ẩn, Y = alpha khi hiện của content.")]
        [MinMaxSlider(0, 1f), SerializeField] Vector2 minMaxCanvasGroupContent = new Vector2(0, 1);

        [Tooltip("If true, BS will unactive when Show complete, else do nothing.")]
        [BoxGroup("Black Screen")][ShowIf("@blackScreen!=null")] public bool AutoDisableBlackScreenInCompleteShow = false;
        [BoxGroup("Black Screen")][LabelText("Color Start Show")][ShowIf("@blackScreen!=null")] public Color ColorBlackScreenStartShow = new Color(0, 0, 0, 0);
        [BoxGroup("Black Screen")][LabelText("Color End Show")][ShowIf("@blackScreen!=null")] public Color ColorBlackScreenEndShow = new Color(0, 0, 0, 1f);
        [BoxGroup("Black Screen")][LabelText("Color Start Hide")][ShowIf("@blackScreen!=null")] public Color ColorBlackScreenStartHide = new Color(0, 0, 0, 1f);
        [BoxGroup("Black Screen")][LabelText("Color End Hide")][ShowIf("@blackScreen!=null")] public Color ColorBlackScreenEndHide = new Color(0, 0, 0, 0);
        [MinMaxSlider(0, 1f), SerializeField][BoxGroup("Black Screen")] Vector2 minMaxCanvasGroupBG = new Vector2(0, 0.7f);

        [Tooltip("Thời gian animate khi mở popup (giây).")]
        public float durationOpen = 0.4f;
        [Tooltip("Thời gian animate khi đóng popup (giây).")]
        public float durationClose = 0.4f;


        [BoxGroup("Use Scale")][Tooltip("Bật hiệu ứng phóng to/thu nhỏ khi Show/Hide.")] public bool useScale = true;

        [BoxGroup("Use Scale")][ShowIf("useScale")][Tooltip("Scale lúc bắt đầu mở.")] public Vector3 startScale = Vector3.zero;
        [BoxGroup("Use Scale")][ShowIf("useScale")][Tooltip("Scale khi popup đang hiển thị.")] public Vector3 defaultScale = Vector3.one;
        [BoxGroup("Use Scale")][ShowIf("useScale")][Tooltip("Scale lúc kết thúc khi đóng.")] public Vector3 endScale = Vector3.one;

        [BoxGroup("Use Scale")][ShowIf("useScale")][Tooltip("Easing khi mở popup.")] public Ease easeScaleOpen = Ease.OutBack;
        [BoxGroup("Use Scale")][ShowIf("@useScale && this.easeScaleOpen == Ease.INTERNAL_Custom")][Tooltip("Đường cong easing tùy chỉnh khi mở.")] public AnimationCurve animCurverScaleOpen;

        [BoxGroup("Use Scale")][ShowIf("useScale")][Tooltip("Easing khi đóng popup.")] public Ease easeScaleClose = Ease.InBack;
        [BoxGroup("Use Scale")][ShowIf("@useScale && this.easeScaleClose == Ease.INTERNAL_Custom")][Tooltip("Đường cong easing tùy chỉnh khi đóng.")] public AnimationCurve animCurverScaleClose;

        [BoxGroup("Use Translation")][Tooltip("Bật hiệu ứng trượt vị trí khi Show/Hide.")] public bool useTranslation = false;

        [BoxGroup("Use Translation")][ShowIf("useTranslation")][Tooltip("Vị trí anchoredPosition lúc bắt đầu mở.")] public Vector3 startPos = new Vector3(-500, 0, 0);
        [BoxGroup("Use Translation")][ShowIf("useTranslation")][Tooltip("Vị trí anchoredPosition khi đang hiển thị.")] public Vector3 defaultPos = new Vector3(0, 0, 0);
        [BoxGroup("Use Translation")][ShowIf("useTranslation")][Tooltip("Vị trí anchoredPosition lúc kết thúc khi đóng.")] public Vector3 endPos = new Vector3(500, 0, 0);

        [BoxGroup("Use Translation")][ShowIf("useTranslation")][Tooltip("Easing khi mở popup.")] public Ease easeTranslationOpen = Ease.OutBack;
        [BoxGroup("Use Translation")][ShowIf("@useTranslation && easeTranslationOpen==Ease.INTERNAL_Custom")][Tooltip("Đường cong easing tùy chỉnh khi mở.")] public AnimationCurve curverTranslationOpen;

        [BoxGroup("Use Translation")][ShowIf("useTranslation")][Tooltip("Easing khi đóng popup.")] public Ease easeTranslationClose = Ease.InBack;
        [BoxGroup("Use Translation")][ShowIf("@useTranslation && easeTranslationClose==Ease.INTERNAL_Custom")][Tooltip("Đường cong easing tùy chỉnh khi đóng.")] public AnimationCurve curverTranslationClose;

        [FoldoutGroup("Events")][Tooltip("Gọi khi bắt đầu Show.")] public UnityEvent OnStartShow;
        [FoldoutGroup("Events")][Tooltip("Gọi khi Show xong.")] public UnityEvent OnCompleteShow;
        [FoldoutGroup("Events")][Tooltip("Gọi khi bắt đầu Hide.")] public UnityEvent OnStartHide;
        [FoldoutGroup("Events")][Tooltip("Gọi khi Hide xong.")] public UnityEvent OnCompleteHide;

        float initAlphaBlackScreen;
        Image imgBlackScreen;
        string initName;
        private Rect LastSafeArea = new Rect(0, 0, 0, 0);

        protected virtual void Awake()
        {
            if (canvasGroupContent == null)
                canvasGroupContent = content?.GetComponent<CanvasGroup>();
            initName = gameObject.name;
            gameObject.SetActive(true);
            if (blackScreen != null)
                blackScreen.gameObject.SetActive(false);
            if (content != null)
                content.gameObject.SetActive(!HideOnAwake);
            //gameObject.SetActive(false);
            if (blackScreen != null)
            {
                imgBlackScreen = blackScreen.GetComponent<Image>();
                if (useScale || useTranslation)
                    imgBlackScreen.color = ColorBlackScreenStartShow;
                initAlphaBlackScreen = imgBlackScreen.color.a;
                canvasGroupBG = imgBlackScreen.GetComponent<CanvasGroup>();
            }
        }



        protected virtual void Update()
        {
            if (content != null)
                if (IsUseSafeArea)
                {
                    Rect safeArea = Screen.safeArea;

                    if (safeArea != LastSafeArea)
                        ApplySafeArea(safeArea);
                }
        }

        private void ApplySafeArea(Rect r)
        {
            LastSafeArea = r;

            // Convert safe area rectangle from absolute pixels to normalised anchor coordinates
            Vector2 anchorMin = r.position;
            Vector2 anchorMax = r.position + r.size;
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;
            content.anchorMin = anchorMin;
            content.anchorMax = anchorMax;

            //Debug.LogFormat("New safe area applied to {0}: x={1}, y={2}, w={3}, h={4} on full extents w={5}, h={6}",
            //    name, r.x, r.y, r.width, r.height, Screen.width, Screen.height);
        }
        Tween tweenBGColor, tweenBGCanvasGroup, tweenCanvasGroupContent;
        /// <summary>
        /// Mở popup với hiệu ứng (fade, scale, translation theo cấu hình). Gọi onComplete khi hiệu ứng xong.
        /// </summary>
        public virtual void Show(System.Action onComplete = null)
        {

            gameObject.SetActive(true);
            content.gameObject.SetActive(true);
            if (blackScreen != null)
                blackScreen?.gameObject.SetActive(true);


            if (blackScreen != null)
            {
                tweenBGColor?.Kill();
                imgBlackScreen.DOColor(ColorBlackScreenStartShow, 0).SetUpdate(IgnoreScaleTime);
                tweenBGColor = imgBlackScreen.DOColor(ColorBlackScreenEndShow, durationOpen).SetUpdate(IgnoreScaleTime);
            }

            _showFadeIncanvasGroup(canvasGroupBG, ref tweenBGCanvasGroup, minMaxCanvasGroupBG, durationOpen, IgnoreScaleTime);
            _showFadeIncanvasGroup(canvasGroupContent, ref tweenCanvasGroupContent, minMaxCanvasGroupContent, durationOpen, IgnoreScaleTime);

            if (!useTranslation && !useScale)
            {
                CompleteShow(delegate
                {
                    onComplete?.Invoke();
                });
            }



            if (useScale && !useTranslation)
            {
                content.transform.localScale = startScale;
                content.anchoredPosition = defaultPos;

                Tween tweenScale = content.transform.DOScale(defaultScale, durationOpen).SetUpdate(IgnoreScaleTime).OnComplete(delegate ()
                   {
                       CompleteShow(delegate
                       {
                           onComplete?.Invoke();
                       });
                   });

                setEaseTween(tweenScale, easeScaleOpen, animCurverScaleOpen);

            }

            if (useTranslation && !useScale)
            {
                content.transform.localScale = defaultScale;
                content.anchoredPosition = startPos;

                Tween tweeen1 = content.DOAnchorPos(defaultPos, durationOpen).SetUpdate(IgnoreScaleTime);
                tweeen1.OnComplete(delegate ()
                  {
                      CompleteShow(delegate
                      {
                          onComplete?.Invoke();
                      });
                  });
                setEaseTween(tweeen1, easeTranslationOpen, curverTranslationOpen);
            }



            if (useScale && useTranslation)
            {

                content.anchoredPosition = startPos;

                var tweenTransOpen = content.DOAnchorPos(defaultPos, durationOpen).SetUpdate(IgnoreScaleTime);
                setEaseTween(tweenTransOpen, easeTranslationOpen, curverTranslationOpen);
                content.transform.localScale = startScale;

                var tweenScale = content.transform.DOScale(defaultScale, durationOpen).SetUpdate(IgnoreScaleTime);
                setEaseTween(tweenScale, easeScaleOpen, animCurverScaleOpen).OnComplete(delegate ()
                    {
                        CompleteShow(delegate
                        {
                            onComplete?.Invoke();
                        });
                    });

            }
            OnStartShow?.Invoke();

            static void _showFadeIncanvasGroup(CanvasGroup canvasGroup, ref Tween tween, Vector2 minMax, float duration, bool ignoreTImeScale)
            {
                if (canvasGroup != null)
                {
                    tween?.Kill();
                    canvasGroup.alpha = minMax.x;
                    tween = canvasGroup.DOFade(minMax.y, duration).SetUpdate(ignoreTImeScale);
                }
            }
        }

        /// <summary>
        /// Gọi khi popup đã hiện xong: fire OnCompleteShow rồi onComplete.
        /// </summary>
        public virtual void CompleteShow()
        {
            CompleteShow(null);
        }

        /// <summary>
        /// Gọi khi popup đã hiện xong: tắt nền nếu AutoDisableBlackScreenInCompleteShow, fire event rồi onComplete.
        /// </summary>
        public virtual void CompleteShow(System.Action onComplete = null)
        {
            if (AutoDisableBlackScreenInCompleteShow)
                blackScreen?.gameObject.SetActive(false);
            this.OnCompleteShow?.Invoke();
            onComplete?.Invoke();
            onCompleteShow();
        }

        protected virtual void onCompleteShow()
        {

        }

        /// <summary>
        /// Đóng popup với hiệu ứng ngược lại Show. Gọi onComplete khi hiệu ứng xong.
        /// </summary>
        public virtual void Hide(System.Action onComplete = null)
        {

            if (!Application.isPlaying)
            {
                if (blackScreen != null)
                    blackScreen?.gameObject.SetActive(false);

                content.gameObject.SetActive(false);
                if (useScale)
                    content.localScale = startScale;
                if (useTranslation)
                    content.anchoredPosition = startPos;

                if (canvasGroupBG)
                    canvasGroupBG.alpha = minMaxCanvasGroupBG.x;
                if (canvasGroupContent)
                    canvasGroupContent.alpha = minMaxCanvasGroupContent.x;
                return;

            }
            if (blackScreen != null)
            {
                blackScreen?.gameObject.SetActive(true);

                tweenBGColor?.Kill();
                tweenBGColor = imgBlackScreen.DOColor(ColorBlackScreenStartHide, 0).SetUpdate(IgnoreScaleTime);
                tweenBGColor = imgBlackScreen.DOColor(ColorBlackScreenEndHide, durationClose).SetUpdate(IgnoreScaleTime);

            }

            _hideFadeIncanvasGroup(canvasGroupBG, ref tweenBGCanvasGroup, minMaxCanvasGroupBG, durationClose, IgnoreScaleTime);
            _hideFadeIncanvasGroup(canvasGroupContent, ref tweenCanvasGroupContent, minMaxCanvasGroupContent, durationClose, IgnoreScaleTime);


            if (!useScale && !useTranslation)
            {
                content.gameObject.SetActive(false);
                CompleteHide(delegate
                {
                    onComplete?.Invoke();
                });
            }
            if (useTranslation)
            {
                content.anchoredPosition = defaultPos;
                var a = content.DOAnchorPos(endPos, durationClose).SetUpdate(IgnoreScaleTime);
                setEaseTween(a, easeTranslationClose, curverTranslationClose).OnComplete(delegate ()
                {
                    CompleteHide(delegate
                    {
                        onComplete?.Invoke();
                    });
                });
            }
            if (useScale)
            {
                content.localScale = defaultScale;
                var a = content.transform.DOScale(endScale, durationClose).SetUpdate(IgnoreScaleTime);
                setEaseTween(a, easeScaleClose, animCurverScaleClose);
                a.OnComplete(delegate ()
                            {
                                CompleteHide(delegate
                                {
                                    onComplete?.Invoke();
                                });
                            });

            }
            OnStartHide?.Invoke();

            static void _hideFadeIncanvasGroup(CanvasGroup canvasGroup, ref Tween tween, Vector2 minMax, float duration, bool ignoreTImeScale)
            {
                if (canvasGroup != null)
                {
                    tween?.Kill();
                    canvasGroup.alpha = minMax.y;
                    tween = canvasGroup.DOFade(minMax.x, duration).SetUpdate(ignoreTImeScale);
                }
            }
        }

        Tween setEaseTween(Tween tween, Ease ease, AnimationCurve animationCurve)
        {
            if (ease == Ease.INTERNAL_Custom)
                tween.SetEase(animationCurve);
            else
            {
                tween.SetEase(ease);
            }
            return tween;
        }

        /// <summary>
        /// Kết thúc việc ẩn: tắt content, gỡ khỏi PopupManager (nếu IsUseManager), fire event rồi onComplete.
        /// </summary>
        public virtual void CompleteHide()
        {
            CompleteHide(null);
        }

        /// <summary>
        /// Kết thúc việc ẩn: tắt content, gỡ khỏi PopupManager (nếu IsUseManager), fire event rồi onComplete.
        /// </summary>
        public virtual void CompleteHide(Action onComplete = null)
        {
            if (blackScreen != null)
                blackScreen?.gameObject.SetActive(false);
            content.gameObject.SetActive(false);
            //gameObject.SetActive(false);
            if (IsUseManager)
                PopupManager.Ins.RemovePopup(this);
            this.OnCompleteHide?.Invoke();
            onComplete?.Invoke();
            onCompleteHide();
        }

        protected virtual void onCompleteHide()
        {

        }

        /// <summary>
        /// Yêu cầu hiện popup. Nếu IsUseManager thì xếp hàng qua PopupManager, không thì Show ngay.
        /// Nút "Show" trong Inspector.
        /// </summary>
        [PropertyOrder(-2)]
        [GUIColor(0.5f, 1f, 0.8f)]

        [PropertySpace(SpaceBefore = 10)]
        [ButtonGroup("top")]
        public virtual void RequestShow()
        {
            gameObject.SetActive(true);
            if (Application.isPlaying)
            {
                if (IsUseManager)
                    PopupManager.Ins.AddPopup(this);
                else
                    Show(null);
            }
            else
            {
                gameObject.SetActive(true);
                content.gameObject.SetActive(true);
                if (blackScreen != null)
                {
                    blackScreen?.gameObject.SetActive(true);
                    imgBlackScreen = blackScreen.GetComponent<Image>();
                    if (imgBlackScreen)
                        imgBlackScreen.color = ColorBlackScreenEndShow;
                }
                content.anchoredPosition = defaultPos;
                content.localScale = defaultScale;

                if (canvasGroupBG)
                    canvasGroupBG.alpha = minMaxCanvasGroupBG.y;
                if (canvasGroupContent)
                    canvasGroupContent.alpha = minMaxCanvasGroupContent.y;
            }

        }
        /// <summary>
        /// Đóng popup ngay (dùng Hide với hiệu ứng). Nút "Hide" trong Inspector.
        /// </summary>
        [PropertyOrder(-1)]
        [GUIColor(1f, 0.5f, 0.6f)]
        [ButtonGroup("top")]
        public void Hide()
        {
            Hide(null);
        }

        [PropertySpace(10)]
        [ShowIf("useTranslation")]
        [BoxGroup("Use Translation")]
        [Button("Set Start Position")]
        private void SetStartPos()
        {
            startPos = content.anchoredPosition;
        }
        [ShowIf("useTranslation")]
        [BoxGroup("Use Translation")]
        [Button("Set End Position")]

        private void SetEndPos()
        {
            endPos = content.anchoredPosition;
        }

        [PropertySpace(10)]
        [ShowIf("useScale")]
        [BoxGroup("Use Scale")]
        [Button("Set Start Scale")]
        private void SetStartScale()
        {
            startScale = content.localScale;
        }

        [ShowIf("useScale")]
        [BoxGroup("Use Scale")]
        [Button("Set End Scale")]

        private void SetEndScale()
        {
            endScale = content.localScale;
        }

    }
}