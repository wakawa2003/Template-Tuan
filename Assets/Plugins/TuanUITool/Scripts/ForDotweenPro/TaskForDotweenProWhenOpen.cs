using UnityEngine;

namespace UITool.Popup
{
    public class TaskForDotweenProWhenOpen : TaskForDotweenPro, IUITaskOpen
    {
        [field: SerializeField] public override string Description { get; set; } = "Open Tween Animation";
        protected override bool isActiveOnExecute { get; set; } = true;
        protected override bool isRunBackwards { get; set; } = false;
    }
}
