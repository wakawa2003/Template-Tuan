using UnityEngine;

namespace UITool.Popup
{
    [DisallowMultipleComponent]
    public class TaskForDotweenProWhenOpen : TaskForDotweenPro, IUITaskOpen
    {
        protected override string DefaultDescription => "Open Tween Animation";
        public override bool IsActiveOnExecute => true;
        public override bool IsRunBackwards => false;
    }
}
