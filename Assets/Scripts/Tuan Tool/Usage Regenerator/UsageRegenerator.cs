using System;
using UnityEngine;

namespace TuanTool
{

    /// <summary>
    /// sau khi dùng (Use()) đến số lần tối đa sẽ chạy thời gian để reset theo resetDuration
    /// chỉ chạy resetduration khi RemainingUse <= 0
    /// <br>Yêu cầu: project phải tự cung cấp <see cref="ISaveLoad"/> (vd: wrap PlayerPrefs) truyền vào constructor.</br>
    /// </summary>
    [System.Serializable]
    public class UsageRegenerator
    {

        /// <summary>
        /// key con bao nhieu lần dùng
        /// </summary>
        private string saveKeyRemaining;

        /// <summary>
        /// key khi het thoi gian reset
        /// </summary>
        private string saveKeyLastDepleted;

        /// <summary>
        /// khoảng thời gian reset (ví dụ: 24h)
        /// </summary>
        private TimeSpan resetDuration;

        /// <summary>Số lần dùng tối đa mỗi chu kỳ.</summary>
        public int MaximumUse { get; private set; }
        Func<DateTime> getCurrentTimeUTC = () => DateTime.UtcNow;
        ISaveLoad saveLoad;

        /// <summary>
        /// Tạo bộ đếm lượt dùng. Nếu key chưa có trong save thì khởi tạo đầy lượt và đặt mốc hết lượt lùi lại một chu kỳ.
        /// </summary>
        /// <param name="saveLoadSystem">Hệ thống lưu dữ liệu của project (ISaveLoad).</param>
        /// <param name="saveKeyRemaining">Key lưu số lượt còn lại. Phải khác saveKeyLastDepleted.</param>
        /// <param name="saveKeyLastDepleted">Key lưu thời điểm hết lượt (UTC).</param>
        /// <param name="maximumUse">Số lượt tối đa.</param>
        /// <param name="getCurrentTimeUTC">Hàm lấy giờ hiện tại UTC (nên dùng TimeSyncManager để chống chỉnh giờ).</param>
        /// <param name="resetDuration">Thời gian hồi lượt, mặc định 24h.</param>
        public UsageRegenerator(ISaveLoad saveLoadSystem, string saveKeyRemaining, string saveKeyLastDepleted, int maximumUse, Func<DateTime> getCurrentTimeUTC, TimeSpan? resetDuration = null)
        {
            if (saveKeyLastDepleted == saveKeyRemaining)
                Debug.LogError($"2 key Khong duoc giong nhau!!!");

            this.MaximumUse = maximumUse;
            this.resetDuration = resetDuration ?? TimeSpan.FromHours(24); // mặc định 24h
            this.getCurrentTimeUTC = getCurrentTimeUTC;
            this.saveLoad = saveLoadSystem;
            this.saveKeyRemaining = saveKeyRemaining;
            this.saveKeyLastDepleted = saveKeyLastDepleted;

            // init nếu chưa có
            if (!saveLoadSystem.HasKey(this.saveKeyRemaining))
                RemainingUse = maximumUse;
            if (!saveLoadSystem.HasKey(this.saveKeyLastDepleted))
                LastDepletedTime = getCurrentTimeUTC.Invoke() - this.resetDuration;
        }

        /// <summary>
        /// Số lượt còn lại, đọc/ghi trực tiếp vào save. Setter lưu ngay.
        /// </summary>
        public int RemainingUse
        {
            get => saveLoad.GetInt(saveKeyRemaining, MaximumUse);
            private set
            {
                saveLoad.SetInt(saveKeyRemaining, value);
                saveLoad.Save();
            }
        }

        /// <summary>
        /// Thời điểm (UTC) hết lượt lần cuối, lưu dưới dạng FileTimeUtc. Dùng để tính thời gian hồi.
        /// </summary>
        public DateTime LastDepletedTime
        {
            get
            {
                var s = saveLoad.GetString(saveKeyLastDepleted, (getCurrentTimeUTC.Invoke() - resetDuration).ToFileTimeUtc().ToString());
                if (long.TryParse(s, out var fileTime))
                    return DateTime.FromFileTimeUtc(fileTime);
                Debug.LogError($"Loi khi parse!!!");
                return getCurrentTimeUTC.Invoke() - resetDuration;
            }
            private set
            {
                saveLoad.SetString(saveKeyLastDepleted, value.ToFileTimeUtc().ToString());
                saveLoad.Save();
            }
        }

        /// <summary>
        /// Tính toán lại số lần dùng.
        /// </summary>
        public void Refresh()
        {
            if (RemainingUse <= 0)
            {
                if (getCurrentTimeUTC.Invoke() >= LastDepletedTime + resetDuration)
                {
                    ResetUses();
                }
            }
        }

        /// <summary>
        /// Kiểm tra có dùng được không. Nếu đã hết chu kỳ hồi thì tự hồi đầy và trả về true.
        /// </summary>
        public bool CanUse()
        {
            if (RemainingUse <= 0)
            {
                if (getCurrentTimeUTC.Invoke() >= LastDepletedTime + resetDuration)
                {
                    ResetUses();
                    return true;
                }
                return false;
            }
            return true;
        }

        /// <summary>
        /// Trừ 1 lượt nếu CanUse(). Khi hết lượt sẽ ghi lại thời điểm hết lượt để bắt đầu đếm hồi.
        /// </summary>
        public void Use()
        {
            if (!CanUse())
            {
                Debug.Log("Item chưa hồi, không thể dùng.");
                return;
            }

            RemainingUse--;
            if (RemainingUse <= 0)
            {
                LastDepletedTime = getCurrentTimeUTC.Invoke();
                Debug.Log("Đã dùng hết, chờ hồi.");
            }
        }

        /// <summary>
        /// Hồi đầy số lượt về <see cref="MaximumUse"/>.
        /// </summary>
        public void ResetUses()
        {
            RemainingUse = MaximumUse;
            Debug.Log("Item đã hồi đầy.");
        }

        /// <summary>
        /// Thời gian còn lại đến khi hồi, định dạng "HH:mm:ss". Trả về "00:00:00" nếu đã hồi xong.
        /// </summary>
        public string GetCooldownTimeString()
        {
            TimeSpan remain = GetCooldownTimeSpan();
            if (remain.TotalSeconds <= 0)
                return "00:00:00"; // đã hồi xong

            return string.Format("{0:D2}:{1:D2}:{2:D2}",
                (int)remain.TotalHours,
                remain.Minutes,
                remain.Seconds);
        }

        /// <summary>
        /// Khoảng thời gian còn lại đến khi hồi (có thể âm nếu đã qua).
        /// </summary>
        public TimeSpan GetCooldownTimeSpan()
        {
            DateTime readyTime = LastDepletedTime + resetDuration;
            TimeSpan remain = readyTime - getCurrentTimeUTC.Invoke();
            return remain;
        }
    }
}
