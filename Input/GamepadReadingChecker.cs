using Windows.Gaming.Input;
using Windows.UI.Input.Preview.Injection;

namespace Windows_Mobile.Input
{
    public static class GamepadReadingChecker
    {
        public static GamepadCheckButton CheckButton(GamepadButtons gamepadButton, VirtualKey key, bool buttonEnabled, GamepadReading reading)
        {
            if (reading.Buttons.HasFlag(gamepadButton) && buttonEnabled)
                return new GamepadCheckButton(false, new InjectedInputKeyboardInfo() { VirtualKey = (ushort)key });
            else if (!reading.Buttons.HasFlag(gamepadButton) && !buttonEnabled)
                return new GamepadCheckButton(true, new InjectedInputKeyboardInfo() { VirtualKey = (ushort)key, KeyOptions = InjectedInputKeyOptions.KeyUp });
            else
                return new GamepadCheckButton(buttonEnabled, null);
        }

        public static GamepadCheckTimedButton CheckPadButton(GamepadButtons gamepadButton, VirtualKey key, DateTime? buttonEnabledChanged, bool buttonEnabled, GamepadReading reading, double timerInterval)
        {
            if (reading.Buttons.HasFlag(gamepadButton) && buttonEnabledChanged is not null && DateTime.Now - buttonEnabledChanged > TimeSpan.FromMilliseconds(500))
                return new GamepadCheckTimedButton(buttonEnabled, buttonEnabledChanged, 100, new InjectedInputKeyboardInfo() { VirtualKey = (ushort)key });
            else if (reading.Buttons.HasFlag(gamepadButton) && buttonEnabled)
                return new GamepadCheckTimedButton(false, DateTime.Now, timerInterval, new InjectedInputKeyboardInfo() { VirtualKey = (ushort)key });
            else if (!reading.Buttons.HasFlag(gamepadButton) && !buttonEnabled)
                return new GamepadCheckTimedButton(true, null, 1, null);
            return new GamepadCheckTimedButton(buttonEnabled, buttonEnabledChanged, timerInterval, null);
        }

        public static GamepadCheckTimedButton CheckTrigger(bool leftTrigger, VirtualKey key, DateTime? buttonEnabledChanged, bool buttonEnabled, GamepadReading reading, double timerInterval)
        {
            double triggerPosition = leftTrigger ? reading.LeftTrigger : reading.RightTrigger;
            if (triggerPosition > 0.5 && buttonEnabledChanged is not null && DateTime.Now - buttonEnabledChanged > TimeSpan.FromMilliseconds(500))
                return new GamepadCheckTimedButton(buttonEnabled, buttonEnabledChanged, 100, new InjectedInputKeyboardInfo() { VirtualKey = (ushort)key });
            else if (triggerPosition > 0.5 && buttonEnabled)
                return new GamepadCheckTimedButton(false, DateTime.Now, timerInterval, new InjectedInputKeyboardInfo() { VirtualKey = (ushort)key });
            else if (triggerPosition < 0.5 && !buttonEnabled)
                return new GamepadCheckTimedButton(true, null, 1, null);
            return new GamepadCheckTimedButton(buttonEnabled, buttonEnabledChanged, timerInterval, null);
        }

        public static GamepadCheckTimedButton CheckLeftStick(bool x, DateTime? buttonEnabledChanged, bool buttonEnabled, GamepadReading reading, double timerInterval)
        {
            double stickPosition = x ? reading.LeftThumbstickX : reading.LeftThumbstickY;
            if (stickPosition < 0.5 && stickPosition > -0.5 && !buttonEnabled)
                return new GamepadCheckTimedButton(true, null, 1, null);
            else if (buttonEnabledChanged is not null && DateTime.Now - buttonEnabledChanged.Value > TimeSpan.FromMilliseconds(500))
                return new GamepadCheckTimedButton(buttonEnabled, buttonEnabledChanged, 100, DetermineThumbstickInput(x, stickPosition));
            else if (buttonEnabled && (stickPosition > 0.5 || stickPosition < -0.5))
                return new GamepadCheckTimedButton(false, DateTime.Now, timerInterval, DetermineThumbstickInput(x, stickPosition));
            return new GamepadCheckTimedButton(buttonEnabled, buttonEnabledChanged, timerInterval, null);
        }

        private static InjectedInputKeyboardInfo DetermineThumbstickInput(bool x, double stickPosition)
        {
            if (x)
            {
                if (stickPosition > 0.5)
                    return new InjectedInputKeyboardInfo() { VirtualKey = (ushort)VirtualKey.GamepadLeftThumbstickRight };
                else if (stickPosition < -0.5)
                    return new InjectedInputKeyboardInfo() { VirtualKey = (ushort)VirtualKey.GamepadLeftThumbstickLeft };
            }
            else
            {
                if (stickPosition > 0.5)
                    return new InjectedInputKeyboardInfo() { VirtualKey = (ushort)VirtualKey.GamepadLeftThumbstickUp };
                else if (stickPosition < -0.5)
                    return new InjectedInputKeyboardInfo() { VirtualKey = (ushort)VirtualKey.GamepadLeftThumbstickDown };
            }
            return null;
        }
    }
}
