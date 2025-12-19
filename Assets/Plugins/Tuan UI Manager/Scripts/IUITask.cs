using System.Threading;
using Cysharp.Threading.Tasks;

namespace TuanTool.Popup
{
    public interface IUITask
    {
        public UniTask Execute(CancellationToken cancellationToken);
    }
}