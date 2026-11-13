using System;
using UnityEngine;

namespace TuanTool
{
    /// <summary>
    /// Dùng (Use()) đến số lần tối đa,
    ///  đến 00:00 UTC hằng ngày sẽ reset về MaximumUse.
    /// <br>Yêu cầu: project phải tự cung cấp <see cref="ISaveLoad"/> truyền vào constructor.</br>
    /// </summary>
    [System.Serializable]
    public class UsageRegeneratorDaily
    {
        /// <summary>Key lưu số lượt còn lại.</summary>
        private string saveKeyRemaining;
        /// <summary>Key lưu mốc reset gần nhất (UTC, FileTimeUtc).</summary>
        private string saveKeyLastDepleted;

        /// <summary>Số lượt tối đa mỗi ngày.</summary>
        public int MaximumUse { get; private set; }
        private Func<DateTime> getCurrentTimeUTC;
        private ISaveLoad saveLoad;

        /// <summary>
        /// Tạo bộ đếm lượt theo ngày. Key chưa có trong save thì khởi tạo đầy lượt.
        /// </summary>
        /// <param name="saveLoadSystem">Hệ thống lưu dữ liệu của project (ISaveLoad).</param>
        /// <param name="saveKeyRemaining">Key lưu số lượt còn lại. Phải khác saveKeyLastDepleted.</param>
        /// <param name="saveKeyLastDepleted">Key lưu mốc reset gần nhất.</param>
        /// <param name="maximumUse">Số lượt tối đa mỗi ngày.</param>
        /// <param name="getCurrentTimeUTC">Hàm lấy giờ hiện tại UTC.</param>
        public UsageRegeneratorDaily(ISaveLoad saveLoadSystem, string saveKeyRemaining, string saveKeyLastDepleted, int maximumUse, Func<DateTime> getCurrentTimeUTC)
        {
            if (saveKeyLastDepleted == saveKeyRemaining)
                Debug.LogError("2 key không được giống nhau!!!");

            this.MaximumUse = maximumUse;
            this.getCurrentTimeUTC = getCurrentTimeUTC;
            this.saveLoad = saveLoadSystem;
            this.saveKeyRemaining = saveKeyRemaining;
            this.saveKeyLastDepleted = saveKeyLastDepleted;

            // init nếu chưa có
            if (!saveLoadSystem.HasKey(this.saveKeyRemaining))
                RemainingUse = maximumUse;
            if (!saveLoadSystem.HasKey(this.saveKeyLastDepleted))
                LastDepletedTime = GetPreviousResetTime();
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
        /// Mốc reset gần nhất (UTC). Dùng để biết đã qua ngày mới hay chưa.
        /// </summary>
        public DateTime LastDepletedTime
        {
            get
            {
                var s = saveLoad.GetString(saveKeyLastDepleted, GetPreviousResetTime().ToFileTimeUtc().ToString());
                if (long.TryParse(s, out var fileTime))
                    return DateTime.FromFileTimeUtc(fileTime);
                Debug.LogError("Lỗi khi parse!!!");
                return GetPreviousResetTime();
            }
            private set
            {
                saveLoad.SetString(saveKeyLastDepleted, value.ToFileTimeUtc().ToString());
                saveLoad.Save();
            }
        }

        /// <summary>
        /// Nếu đã sang ngày UTC mới so với mốc reset gần nhất thì hồi đầy lượt và cập nhật mốc reset.
        /// </summary>
        public void Refresh()
        {
            DateTime now = getCurrentTimeUTC.Invoke();
            DateTime nextReset = GetNextResetTimeFrom(LastDepletedTime);

            if (now >= nextReset)
            {
                // Tính số ngày đã qua kể từ lần reset cuối
                int daysPassed = (now.Date - LastDepletedTime.Date).Days;
                if (daysPassed > 0)
                {
                    ResetUses();
                    // cập nhật LastDepletedTime về mốc reset gần nhất
                    LastDepletedTime = LastDepletedTime.Date.AddDays(daysPassed);
                }
            }
        }


        /// <summary>
        /// Gọi Refresh() rồi trả về true nếu còn lượt.
        /// </summary>
        public bool CanUse()
        {
            Refresh();
            return RemainingUse > 0;
        }

        /// <summary>
        /// Trừ 1 lượt nếu CanUse(). Hết lượt thì chờ đến 00:00 UTC hôm sau.
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
                Debug.Log("Đã dùng hết, chờ hồi vào 00:00 UTC.");
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
        /// Thời gian còn lại đến 00:00 UTC tiếp theo, định dạng "HH:mm:ss". Trả về "00:00:00" nếu đã hồi.
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
        /// Khoảng thời gian còn lại đến lần reset tiếp theo (00:00 UTC).
        /// </summary>
        public TimeSpan GetCooldownTimeSpan()
        {
            DateTime nextReset = GetNextResetTimeFrom(LastDepletedTime);
            return nextReset - getCurrentTimeUTC.Invoke();
        }

        /// <summary>
        /// Tính thời điểm reset tiếp theo (00:00 UTC) kể từ một mốc bất kỳ
        /// </summary>
        private DateTime GetNextResetTimeFrom(DateTime fromTime)
        {
            return fromTime.Date.AddDays(1); // 00:00 UTC của ngày mai
        }

        /// <summary>
        /// Lấy thời điểm reset gần nhất (00:00 UTC hôm qua hoặc hôm nay)
        /// </summary>
        private DateTime GetPreviousResetTime()
        {
            DateTime now = getCurrentTimeUTC.Invoke();
            DateTime todayReset = now.Date; // 00:00 UTC hôm nay
            if (now >= todayReset)
                return todayReset;
            return todayReset.AddDays(-1);
        }
    }
}
