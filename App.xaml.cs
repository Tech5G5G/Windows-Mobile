using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using Windows.ApplicationModel;
using System.Linq;
using System.Runtime.InteropServices;
using Windows.ApplicationModel.Core;
using Windows.Management.Deployment;
using Windows_Mobile.Types;
using craftersmine.SteamGridDBNet;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

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
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();
            m_window.Activate();
        }

        public static Icon ExtractIcon(string file, int number, bool largeIcon)
        {
            var outInt = ExtractIconEx(file, number, out IntPtr large, out IntPtr small, 1);
            try
            {
                return Icon.FromHandle(largeIcon ? large : small);
            }
            catch
            {
                return null;
            }
        }
        [DllImport("Shell32.dll", EntryPoint = "ExtractIconExW", CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int ExtractIconEx(string sFile, int iIndex, out IntPtr piLargeVersion, out IntPtr piSmallVersion, int amountIcons);

        private Window m_window;
        public async static void StartApplication(StartMenuItem selectedItemInfo, bool runAsAdmin = false)
        {
            if (selectedItemInfo.ItemKind == ApplicationKind.Normal || selectedItemInfo.ItemKind == ApplicationKind.Launcher || selectedItemInfo.ItemKind == ApplicationKind.SteamGame || selectedItemInfo.ItemKind == ApplicationKind.EpicGamesGame || selectedItemInfo.ItemKind == ApplicationKind.GOGGame)
            {
                try { Process.Start(new ProcessStartInfo(selectedItemInfo.ItemStartURI) { UseShellExecute = true, Verb = runAsAdmin ? "runas" : null }); }
                catch { }
            }
            else if (selectedItemInfo.ItemKind == ApplicationKind.Packaged || selectedItemInfo.ItemKind == ApplicationKind.LauncherPackaged || selectedItemInfo.ItemKind == ApplicationKind.XboxGame)
            {
                PackageManager packageManager = new();
                Package package = packageManager.FindPackageForUser(string.Empty, selectedItemInfo.ItemStartURI);

                IReadOnlyList<AppListEntry> appListEntries = package.GetAppListEntries();
                await appListEntries.First(i => i.DisplayInfo.DisplayName == selectedItemInfo.ItemName).LaunchAsync();
            }
        }
    }
}
