using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TuanTool
{
    /// <summary>
    /// Gọi UnityEvent theo tên từ Animation/Animator Event.
    /// Lưu ý: tên class trùng với UnityEngine.AnimationEvent. Trong namespace TuanTool, "AnimationEvent" trỏ về class này;
    /// khi cần kiểu của Unity hãy viết đầy đủ UnityEngine.AnimationEvent.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public class AnimationEvent : MonoBehaviour
    {
        [Tooltip("Danh sách sự kiện, mỗi sự kiện có tên duy nhất và UnityEvent sẽ được gọi khi CallEvent với tên đó.")]
        public List<StackEvent> Events = new List<StackEvent>();
        Dictionary<string, StackEvent> dictEvents = new Dictionary<string, StackEvent>();

        bool isInit = false;
        private void Awake()
        {
            if (isInit)
                return;

            isInit = true;
            foreach (var item in Events)
            {
                if (dictEvents.ContainsKey(item.EventName))
                    Debug.LogError($"Contain 2 AnimationEvent with name \"{item.EventName}\" in \"{gameObject.name}\"!!!");
                else
                    dictEvents.Add(item.EventName, item);
            }
        }



        /// <summary>
        /// Gọi sự kiện có tên EventName. Đặt hàm này làm tên Animation Event trong clip.
        /// </summary>
        public void CallEvent(string EventName)
        {
            if (!isInit)
                Awake();
            if (dictEvents.ContainsKey(EventName))
                dictEvents[EventName].OnCall?.Invoke();
            else
                Debug.LogError($"Don't have AnimationEvent with name <color=green>{EventName}</color> in <color=green>{gameObject.name}</color> !");
        }

        /// <summary>
        /// Lấy UnityEvent theo tên để gán thêm listener bằng code. Trả về null nếu không có tên đó.
        /// </summary>
        public UnityEvent GetEvent(string EventName)
        {
            if (!isInit)
                Awake();
            if (dictEvents.ContainsKey(EventName))
                return dictEvents[EventName].OnCall;
            else
            {
                Debug.LogError($"Don't have AnimationEvent with name <color=green>{EventName}</color> in <color=green>{gameObject.name}</color> !");
                return null;
            }
        }

        /// <summary>
        /// Một sự kiện: tên để tra cứu và UnityEvent được gọi.
        /// </summary>
        [Serializable]
        public class StackEvent
        {
            [Tooltip("Tên sự kiện, phải khớp với tham số truyền vào CallEvent.")]
            public string EventName;
            [Tooltip("Các hàm sẽ được gọi khi sự kiện được kích hoạt.")]
            public UnityEvent OnCall = new UnityEvent();
        }
    }
}
