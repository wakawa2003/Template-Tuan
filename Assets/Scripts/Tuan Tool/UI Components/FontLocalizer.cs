using UnityEngine;
using UnityEngine.Localization;
using TMPro;


namespace TuanTool
{
    /// <summary>
    /// Localize cho Font, cần tạo font asset database trong localize asset database.
    /// Gắn lên object có TMP_Text, kéo LocalizedAsset vào field localizedFont.
    /// Khi đổi ngôn ngữ, font của TMP_Text sẽ tự đổi theo.
    /// </summary>
    [DisallowMultipleComponent]
    public class FontLocalizer : MonoBehaviour
    {
        private TMP_Text textComponent;
        [Tooltip("Font asset theo ngôn ngữ, lấy từ Localization Font Asset Table")]
        public LocalizedAsset<TMP_FontAsset> localizedFont;

        void Awake()
        {
            textComponent = GetComponent<TMP_Text>();
            if (textComponent == null)
                Debug.LogError($"khong tim thay tmp_text");
            localizedFont.AssetChanged += UpdateFont;
            localizedFont.LoadAssetAsync();

        }

        private void OnDestroy()
        {
            localizedFont.AssetChanged -= UpdateFont;
        }

        /// <summary>
        /// Gán font mới cho TMP_Text (được gọi khi LocalizedAsset đổi).
        /// </summary>
        void UpdateFont(TMP_FontAsset font)
        {
            if (textComponent == null) return;
            textComponent.font = font;
        }
    }
}
