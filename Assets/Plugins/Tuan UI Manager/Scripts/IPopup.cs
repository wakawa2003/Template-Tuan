using Cysharp.Threading.Tasks;
using UnityEngine;

namespace TuanTool.Popup
{
    public interface IPopup
    {
        public bool IsShowing { get; }
        public UniTask Show(object payloadData);
        public UniTask Hide();
    }
}
