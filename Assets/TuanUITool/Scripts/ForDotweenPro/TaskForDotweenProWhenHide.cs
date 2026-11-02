using UnityEngine;

namespace UITool.Popup
{
    public class TaskForDotweenProWhenHide : TaskForDotweenPro, IUITaskClose
    {
        [field: SerializeField] public override string Description { get; set; } = "Hide Tween Animation";
        protected override bool isActiveOnExecute { get; set; } = false;
        protected override bool isRunBackwards { get; set; } = true;
    }
}
