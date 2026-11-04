namespace UITool.Popup
{
    public class TaskForDotweenProWhenHide : TaskForDotweenPro, IUITaskClose
    {
        protected override string DefaultDescription => "Hide Tween Animation";
        protected override bool isActiveOnExecute => false;
        protected override bool isRunBackwards => false;
    }
}
