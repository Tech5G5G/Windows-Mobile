using Windows.UI.Input.Preview.Injection;

namespace Windows_Mobile.Input
{
    public class GamepadCheckButton (bool newButtonEnabled, InjectedInputKeyboardInfo inputInfo)
    {
        public bool ButtonEnabled { get; set; } = newButtonEnabled;
        public InjectedInputKeyboardInfo InputInfo { get; set; } = inputInfo;
    }

    public class GamepadCheckTimedButton(bool newButtonEnabled, DateTime? newButtonEnabledChanged, double newTimerInterval, InjectedInputKeyboardInfo inputInfo)
    {
        public bool ButtonEnabled { get; set; } = newButtonEnabled;
        public DateTime? ButtonEnabledChanged { get; set; } = newButtonEnabledChanged;
        public double TimerInterval { get; set; } = newTimerInterval;
        public InjectedInputKeyboardInfo InputInfo { get; set; } = inputInfo;
    }
}
