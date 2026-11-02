using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;

namespace UITool.Popup.test
{
    public class PopupTest : PopupBase
    {

        [Button]
        public void Test()
        {
            // if (Application.isPlaying)
            //     PopupManager.Ins.AddPopup(this);
            // else
            CallRequestShow(null);
        }

        [Button]
        void _hide()
        {
            Hide();
        }

        public override UniTask OnCompleteShow(object payloadData)
        {
            return UniTask.CompletedTask;
        }


    }
}
