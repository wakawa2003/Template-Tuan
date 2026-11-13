using Sirenix.OdinInspector;
using UnityEngine;
using DG.Tweening; // Yêu cầu cài đặt DOTween
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
namespace TuanTool
{

    /// <summary>
    /// Component này dùng để quản lý việc chuyển đổi giữa các trang UI (Switch Pages) với hiệu ứng trượt mượt mà.
    /// </summary>
    [Tooltip("Component này dùng để quản lý việc chuyển đổi giữa các trang UI (Switch Pages) với hiệu ứng trượt mượt mà.")]
    [AddComponentMenu("UI/Switch Pages")]
    public class SwitchPages : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Title("Settings")]
        [SerializeField] private float animDuration = 0.4f;
        [SerializeField] private Ease animEase = Ease.OutQuint;

        [Title("Swipe")]
        [SerializeField] private bool enableSwipe = true;
        [Tooltip("Khoảng cách kéo tối thiểu (pixel) theo phương ngang để chuyển trang")]
        [SerializeField] private float swipeThreshold = 100f;

        [Title("Pages Setup")]
        [SerializeField] private RectTransform[] pages;
        [SerializeField] private Selectable[] togglesButton;

        [SerializeField] private int currentPageIndex = 0;

        public UnityEvent<int> OnStartShowPage;
        public UnityEvent<int> OnCOmpleteShowPage;

        Sequence sequenceMove;

        // Trạng thái khi đang kéo (swipe)
        bool isSwiping;
        float dragOffset;
        int swipeTargetIndex = -1;

        public int CurrentPageIndex { get => currentPageIndex; private set => currentPageIndex = value; }

        private void Start()
        {
            // Khởi tạo: Chỉ bật trang đầu tiên, các trang khác tắt đi
            for (int i = 0; i < pages.Length; i++)
            {
                pages[i].gameObject.SetActive(i == CurrentPageIndex);
                if (i == CurrentPageIndex) pages[i].anchoredPosition = Vector2.zero;
            }
        }

        [Button(ButtonSizes.Large)]
        public void ShowPage(int indexPage)
        {
            // Kiểm tra lỗi hoặc nếu nhấn vào đúng trang đang hiển thị
            if (indexPage < 0 || indexPage >= pages.Length || indexPage == CurrentPageIndex) return;
            if (IsAnimating()) return;

            // 1. Chuẩn bị trang mới: Bật lên và đặt ở vị trí "chờ" bên ngoài màn hình
            RectTransform newPage = pages[indexPage];
            newPage.gameObject.SetActive(true);
            newPage.anchoredPosition = new Vector2(GetHiddenX(indexPage, pages[CurrentPageIndex].rect.width), 0);

            // 2. Chạy animation chuyển trang
            PlayCommit(indexPage);
        }

        // Trang cũ trượt ra ngoài, trang mới trượt vào tâm (chạy từ vị trí hiện tại, kể cả khi đang kéo dở)
        void PlayCommit(int indexPage)
        {
            RectTransform oldPage = pages[CurrentPageIndex];
            RectTransform newPage = pages[indexPage];

            // Hướng: Nếu index mới lớn hơn -> Trang cũ trượt ra TRÁI
            bool isNext = indexPage > CurrentPageIndex;
            float exitX = isNext ? -oldPage.rect.width : oldPage.rect.width;

            sequenceMove = DOTween.Sequence();

            var anim1 = oldPage.DOAnchorPos(new Vector2(exitX, 0), animDuration)
                  .SetEase(animEase)
                  .OnComplete(() =>
                  {
                      oldPage.gameObject.SetActive(false);
                      OnCOmpleteShowPage?.Invoke(indexPage);
                  });

            var anim2 = newPage.DOAnchorPos(Vector2.zero, animDuration)
                 .SetEase(animEase);

            sequenceMove.Append(anim1);
            sequenceMove.Join(anim2);

            // Cập nhật index hiện tại
            CurrentPageIndex = indexPage;
            OnStartShowPage?.Invoke(CurrentPageIndex);
        }

        // Kéo chưa đủ xa: trang hiện tại về giữa, trang đích trượt ra lại bên ngoài rồi tắt
        void PlaySnapBack()
        {
            RectTransform currentPage = pages[CurrentPageIndex];

            sequenceMove = DOTween.Sequence();
            sequenceMove.Join(currentPage.DOAnchorPos(Vector2.zero, animDuration).SetEase(animEase));

            if (swipeTargetIndex >= 0)
            {
                RectTransform targetPage = pages[swipeTargetIndex];
                float hiddenX = GetHiddenX(swipeTargetIndex, currentPage.rect.width);

                sequenceMove.Join(targetPage.DOAnchorPos(new Vector2(hiddenX, 0), animDuration)
                    .SetEase(animEase)
                    .OnComplete(() => targetPage.gameObject.SetActive(false)));
            }
        }

        bool IsAnimating() => sequenceMove != null && sequenceMove.IsActive() && sequenceMove.IsPlaying();

        // Vị trí "chờ" của trang đích: bên phải nếu đi tới, bên trái nếu quay lại
        float GetHiddenX(int indexPage, float width) => indexPage > CurrentPageIndex ? width : -width;

        void Update()
        {
            for (int i = 0; i < togglesButton.Length; i++)
            {
                var item = togglesButton[i];
                item.interactable = i != CurrentPageIndex; // Cập nhật trạng thái Toggle tương ứng
            }
        }

        #region Swipe (Chuột hoặc tay đều dùng chung sự kiện Pointer của EventSystem)
        // Cần có Graphic (vd: Image, raycastTarget = true) trên object này để nhận sự kiện kéo
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!enableSwipe || IsAnimating()) return;

            isSwiping = true;
            dragOffset = 0f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isSwiping) return;

            Vector2 delta = eventData.position - eventData.pressPosition;

            // Chỉ kéo trang theo khi kéo chủ yếu theo phương ngang (tránh xung đột với ScrollRect)
            dragOffset = Mathf.Abs(delta.x) > Mathf.Abs(delta.y) ? delta.x : 0f;

            // Vuốt sang trái -> trang kế tiếp, vuốt sang phải -> trang trước
            int dir = dragOffset < 0 ? 1 : -1;
            int targetIndex = CurrentPageIndex + dir;
            if (dragOffset == 0f || targetIndex < 0 || targetIndex >= pages.Length) targetIndex = -1;

            // Đổi hướng kéo: ẩn trang đích cũ, bật trang đích mới
            if (targetIndex != swipeTargetIndex)
            {
                if (swipeTargetIndex >= 0) pages[swipeTargetIndex].gameObject.SetActive(false);
                swipeTargetIndex = targetIndex;
                if (swipeTargetIndex >= 0) pages[swipeTargetIndex].gameObject.SetActive(true);
            }

            // Di chuyển trang hiện tại và trang đích theo ngón tay / chuột
            RectTransform currentPage = pages[CurrentPageIndex];
            currentPage.anchoredPosition = new Vector2(dragOffset, 0);

            if (swipeTargetIndex >= 0)
            {
                float width = currentPage.rect.width;
                pages[swipeTargetIndex].anchoredPosition = new Vector2(dragOffset + dir * width, 0);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isSwiping) return;
            isSwiping = false;

            // Kéo đủ xa thì chuyển trang, không thì quay về trang cũ
            if (swipeTargetIndex >= 0 && Mathf.Abs(dragOffset) >= swipeThreshold)
            {
                int indexPage = swipeTargetIndex;
                swipeTargetIndex = -1;
                PlayCommit(indexPage);
            }
            else
            {
                PlaySnapBack();
                swipeTargetIndex = -1;
            }
        }
        #endregion

        #region Helper Methods (Dùng cho các nút bấm UI đơn lẻ)
        [HorizontalGroup("Controls")]
        [Button]
        public void PreviousPage() => ShowPage(CurrentPageIndex - 1);

        [HorizontalGroup("Controls")]
        [Button]
        public void NextPage() => ShowPage(CurrentPageIndex + 1);


        #endregion
    }
}
