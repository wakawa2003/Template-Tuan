using UnityEngine;

namespace UITool.Popup
{
    [DisallowMultipleComponent]
    public class TaskForDotweenProWhenHide : TaskForDotweenPro, IUITaskClose
    {
        protected override string DefaultDescription => "Hide Tween Animation";
        public override bool IsActiveOnExecute => false;
        public override bool IsRunBackwards => false;
    }
}
