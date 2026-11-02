using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace UITool.Popup
{
    public class PlayAnimation : MonoBehaviour, IUITask
    {
        [field: SerializeField] public string Description { get; set; }
        [field: SerializeField] public Animator animator;
        [field: SerializeField] public string animationTriggerName;

        public UniTask Execute(CancellationToken cancellationToken)
        {
            animator.SetTrigger(animationTriggerName);
            return UniTask.CompletedTask;
        }

        void Reset()
        {
            animator = GetComponent<Animator>();
        }
    }
}
