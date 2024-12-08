using craftersmine.SteamGridDBNet;
using Windows.Storage;
using System.Runtime.InteropServices;

namespace Windows_Mobile
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            db = new SteamGridDb("a267ca54f99e5f8521e6f04f052aeeeb");

            MainWindow = new MainWindow();
            MainWindow.Activate();
        }

        public static Window MainWindow { get; set; }

        public static SteamGridDb db { get; set; }

        [DllImport("user32")]
        public static extern bool ExitWindowsEx(uint uFlags, uint dwReason);

        [DllImport("user32.dll")]
        public static extern bool LockWorkStation();

        public static class Settings
        {
            private static ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            public delegate void SettingChangedEventHandler(SettingChangedEventArgs args);
            public class SettingChangedEventArgs(object newValue) { public object NewValue { get; } = newValue; };

            public static bool IsGlobalNotifCenterEnabled
            {
                get
                {
                    if (localSettings.Values.TryGetValue("IsGlobalNotifCenterEnabled", out object value))
                        return (bool)value;
                    else
                    {
                        localSettings.Values["IsGlobalNotifCenterEnabled"] = false;
                        return false;
                    }
                }
                set
                {
                    localSettings.Values["IsGlobalNotifCenterEnabled"] = value;
                    IsGlobalNotifCenterEnabledChanged?.Invoke(new SettingChangedEventArgs(value));
                }
            }
            public static event SettingChangedEventHandler IsGlobalNotifCenterEnabledChanged;
        }
    }
}
