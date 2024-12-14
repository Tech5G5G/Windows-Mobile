using Windows.UI.Input.Preview.Injection;

namespace Windows_Mobile.Input
{
    public readonly struct GamepadCheckButton (bool newButtonEnabled, InjectedInputKeyboardInfo inputInfo)
    {
        public bool ButtonEnabled { get; } = newButtonEnabled;
        public InjectedInputKeyboardInfo InputInfo { get; } = inputInfo;
    }

    public readonly struct GamepadCheckTimedButton(bool newButtonEnabled, DateTime? newButtonEnabledChanged, double newTimerInterval, InjectedInputKeyboardInfo inputInfo)
    {
        public bool ButtonEnabled { get; } = newButtonEnabled;
        public DateTime? ButtonEnabledChanged { get; } = newButtonEnabledChanged;
        public double TimerInterval { get; } = newTimerInterval;
        public InjectedInputKeyboardInfo InputInfo { get; } = inputInfo;
    }
}
