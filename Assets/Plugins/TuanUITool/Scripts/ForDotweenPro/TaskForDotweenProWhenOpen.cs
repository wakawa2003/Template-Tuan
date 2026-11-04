namespace UITool.Popup
{
    public class TaskForDotweenProWhenOpen : TaskForDotweenPro, IUITaskOpen
    {
        protected override string DefaultDescription => "Open Tween Animation";
        protected override bool isActiveOnExecute => true;
        protected override bool isRunBackwards => false;
    }
}
