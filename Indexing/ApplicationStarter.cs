using Windows.Management.Deployment;

namespace Windows_Mobile.Indexing
{
    public class ApplicationStarter
    {
        public async static void FromStartMenuItem(StartMenuItem selectedItemInfo, bool runAsAdmin = false)
        {
            if (selectedItemInfo.ItemKind == ApplicationKind.Normal || selectedItemInfo.ItemKind == ApplicationKind.Launcher || selectedItemInfo.ItemKind == ApplicationKind.SteamGame || selectedItemInfo.ItemKind == ApplicationKind.EpicGamesGame || selectedItemInfo.ItemKind == ApplicationKind.GOGGame || selectedItemInfo.ItemKind == ApplicationKind.RobloxPlayer)
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
