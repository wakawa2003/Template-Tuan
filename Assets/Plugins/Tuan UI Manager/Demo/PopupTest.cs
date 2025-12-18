using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;

namespace TuanTool.Popup.Test
{
    public class PopupTest : PopupBase
    {

        [Button]
        public void Test()
        {
            PopupManager.Ins.AddPopup(this);
        }

        public override UniTask OnCompleteShow(object payloadData)
        {
            return UniTask.CompletedTask;
        }


    }
}
