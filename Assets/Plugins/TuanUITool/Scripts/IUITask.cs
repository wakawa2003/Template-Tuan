using System.Threading;
using Cysharp.Threading.Tasks;

namespace UITool.Popup
{
    public interface IUITask
    {

        public string Description { get; }
        public UniTask Execute(CancellationToken cancellationToken);
    }
}