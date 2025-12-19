using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TuanTool.Popup.Test
{
    public class PopupTest : PopupBase
    {


        [Button]
        public void Test()
        {
            if (Application.isPlaying)
                PopupManager.Ins.AddPopup(this);
            else
                Show(null);

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
