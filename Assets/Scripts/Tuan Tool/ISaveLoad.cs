using System;
using UnityEngine;

namespace TuanTool
{
    /// <summary>
    /// Interface lưu/đọc dữ liệu local (thường bọc PlayerPrefs hoặc file JSON).
    /// Project phải tự implement interface này; UsageRegenerator dùng nó để lưu số lần dùng.
    /// </summary>
    public interface ISaveLoad
    {
        /// <summary>Tiền tố thêm vào mọi key (để tránh trùng key giữa các hệ thống).</summary>
        string KeyPrefix { get; set; }

        /// <summary>Ghi số nguyên.</summary>
        void SetInt(string key, int value);
        /// <summary>Đọc số nguyên, trả về defaultValue nếu chưa có key.</summary>
        int GetInt(string key, int defaultValue = 0);

        /// <summary>Ghi số thực.</summary>
        void SetFloat(string key, float value);
        /// <summary>Đọc số thực, trả về defaultValue nếu chưa có key.</summary>
        float GetFloat(string key, float defaultValue = 0f);

        /// <summary>Ghi bool.</summary>
        void SetBool(string key, bool value);
        /// <summary>Đọc bool, trả về defaultValue nếu chưa có key.</summary>
        bool GetBool(string key, bool defaultValue = false);

        /// <summary>Ghi chuỗi.</summary>
        void SetString(string key, string value);
        /// <summary>Đọc chuỗi, trả về defaultValue nếu chưa có key.</summary>
        string GetString(string key, string defaultValue = "");

        /// <summary>Ghi object (implementation tự serialize, ví dụ JSON).</summary>
        void SetObject<T>(string key, T obj);
        /// <summary>Đọc object, trả về defaultValue nếu chưa có key.</summary>
        T GetObject<T>(string key, T defaultValue = default);

        /// <summary>Kiểm tra key đã tồn tại chưa.</summary>
        bool HasKey(string key);
        /// <summary>Xoá 1 key.</summary>
        void DeleteKey(string key);
        /// <summary>Xoá toàn bộ dữ liệu đã lưu.</summary>
        void DeleteAll();
        /// <summary>Ghi dữ liệu xuống bộ nhớ thật (PlayerPrefs.Save, ghi file...).</summary>
        void Save();
    }


    /// <summary>
    /// Danh sách key dùng chung khi lưu dữ liệu. Thêm key mới ở đây để tránh trùng tên.
    /// </summary>
    public class SaveLoadKeys
    {
        /// <summary>Ngôn ngữ đã chọn.</summary>
        public const string LANGUAGE_KEY = "LANGUAGE_SELECTED";
        /// <summary>Âm lượng nhạc nền.</summary>
        public const string VOLUME_MUSIC = "VolumeMusic";
        /// <summary>Âm lượng hiệu ứng.</summary>
        public const string VOLUME_FX = "VolumeFX";
        /// <summary>Bật/tắt rung.</summary>
        public const string VIBRATION_KEY = "Vibration";

        /// <summary>Thông tin tài khoản (đăng nhập).</summary>
        public const string USER_ID = "UserId";
        public const string ACCESS_TOKEN = "AccessToken";
        public const string REFRESH_TOKEN = "RefreshToken";
        public const string USERNAME = "Username";
        public const string PASSWORD = "Password";
        public const string IS_NEW_USER = "IsNewUser";
        public const string SAVED_LOGIN = "SavedLogin";

        /// <summary>Đã mua gỡ quảng cáo (IAP).</summary>
        public const string IS_ADS_REMOVED = "IsAdsRemoved";
        /// <summary>Tiền tố: cộng thêm id sản phẩm để lưu số lần còn lại (UsageRegenerator).</summary>
        public const string REMAINING_USE_BUY_IAP_ = "RemainingUseBuyIAP_";
        /// <summary>Tiền tố: cộng thêm id sản phẩm để lưu thời điểm hết lượt (UsageRegenerator).</summary>
        public const string LAST_TIME_OVER_BUY_IAP_ = "LastTimeOverBuyIAP_";
    }
}
