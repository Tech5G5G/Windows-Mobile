using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Management.Deployment;
using Microsoft.WindowsAPICodePack.Shell;
using System.Drawing.Imaging;
using System.Drawing;
using Windows.ApplicationModel;
using craftersmine.SteamGridDBNet;
using NexusMods.Paths;
using Windows_Mobile.Types;
using System.Text.Json;
using GameFinder.RegistryUtils;
using GameFinder.StoreHandlers.GOG;
using GameFinder.StoreHandlers.Steam;
using craftersmine.SteamGridDBNet.Exceptions;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Windows_Mobile
{
    public enum ApplicationKind
    {
        Normal,
        SteamGame,
        EpicGamesGame,
        XboxGame,
        Launcher,
        LauncherPackaged,
        Packaged
    }

    public class StartMenuItem
    {
        public string ItemName { get; set;  }
        public ApplicationKind ItemKind { get; set; }
        public string ItemStartURI { get; set; }
        public BitmapImage Icon { get; set; }
        public SteamGridDbGame GameInfo { get; set; }
    }

    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();

            Title = "Windows Mobile";
            AppWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen);

            startMenu.Height = (AppWindow.Size.Height * 7) / 8;
            startNV.SelectedItem = games_NavItem;

            wallpaperImage.ImageSource = new BitmapImage() { UriSource = new Uri("C:\\Users\\" + Environment.UserName + "\\AppData\\Roaming\\Microsoft\\Windows\\Themes\\TranscodedWallpaper") };
            db = new SteamGridDb("a267ca54f99e5f8521e6f04f052aeeeb");
            PopulateStartMenu();
        }

        private async Task PopulateStartMenu()
        {
            var egsHandler = new GameFinder.StoreHandlers.EGS.EGSHandler(OperatingSystem.IsWindows() ? GameFinder.RegistryUtils.WindowsRegistry.Shared : null, FileSystem.Shared);
            var eGames = egsHandler.FindAllGames();

            var xhandler = new GameFinder.StoreHandlers.Xbox.XboxHandler(FileSystem.Shared);
            var xgames = xhandler.FindAllGames();

            var handler = new GameFinder.StoreHandlers.Steam.SteamHandler(FileSystem.Shared, OperatingSystem.IsWindows() ? GameFinder.RegistryUtils.WindowsRegistry.Shared : null);
            var games = handler.FindAllGames();
            
            await IndexEGSGames();
            await IndexSteamGames();
            await IndexPackagedApps();
            IndexStartMenuFolder("C:\\Users\\" + Environment.UserName + "\\AppData\\Roaming\\Microsoft\\Windows\\Start Menu\\Programs");
            IndexStartMenuFolder("C:\\ProgramData\\Microsoft\\Windows\\Start Menu\\Programs");
            AddAppsToStartMenu();
        }

        private async Task IndexSteamGames()
        {
            var fs = new InMemoryFileSystem();
            var steamPath = SteamLocationFinder.GetDefaultSteamInstallationPaths(fs).First();
            if (Directory.Exists(steamPath.GetFullPath() + "\\steamapps\\"))
            {
                var apps = Directory.GetFiles(steamPath.GetFullPath() + "\\steamapps\\");
                foreach (string app in apps)
                {
                    if (app.EndsWith(".acf"))
                    {
                        var appInfo = VdfConvert.Deserialize(File.ReadAllText(app)).Value.ToJson().ToObject<SteamGameInfo>();
                        if (appInfo.appid != "228980")
                        {
                            SteamGridDbGame game = null;
                            BitmapImage bitmapImage = new();
                            try
                            {
                                game = await db.GetGameBySteamIdAsync(int.Parse(appInfo.appid));
                            }
                            catch (craftersmine.SteamGridDBNet.Exceptions.SteamGridDbNotFoundException)
                            {
                                game = (await db.SearchForGamesAsync(appInfo.name)).First();
                            }
                            var image = await db.GetIconsForGameAsync(game);
                            if (image.Length != 0)
                                bitmapImage.UriSource = new Uri(image[0].FullImageUrl);
                            else
                            {
                                using var stream = new MemoryStream();
                                Icon.ExtractAssociatedIcon(Directory.GetFiles(steamPath.GetFullPath() + "\\steamapps\\common\\" + appInfo.installdir).First(i => i.EndsWith(".exe"))).ToBitmap().Save(stream, ImageFormat.Png);
                                stream.Position = 0;
                                bitmapImage.SetSource(stream.AsRandomAccessStream());
                            }

                            var MenuItem = new StartMenuItem()
                            {
                                ItemName = appInfo.name,
                                ItemStartURI = "steam://rungameid/" + appInfo.appid,
                                ItemKind = ApplicationKind.SteamGame,
                                Icon = bitmapImage,
                                GameInfo = game
                            };
                            allApps.Add(MenuItem);
                        }
                    }
                }
            }
            else
                return;
        }

        private async Task IndexEGSGames()
        {
            if (Directory.Exists("C:\\ProgramData\\Epic\\EpicGamesLauncher\\Data\\Manifests"))
            {
                var apps = Directory.GetFiles("C:\\ProgramData\\Epic\\EpicGamesLauncher\\Data\\Manifests");
                foreach (var app in apps)
                {
                    var appInfo = JsonSerializer.Deserialize<EGSGameInfo>(File.ReadAllText(app));
                    BitmapImage bitmapImage = new();

                    SteamGridDbGame game = (await db.SearchForGamesAsync(appInfo.DisplayName)).First();
                    var image = await db.GetIconsForGameAsync(game);
                    if (image.Length != 0)
                        bitmapImage.UriSource = new Uri(image[0].FullImageUrl);
                    else
                    {
                        using var stream = new MemoryStream();
                        Icon.ExtractAssociatedIcon(appInfo.InstallLocation + "/" + appInfo.LaunchExecutable).ToBitmap().Save(stream, ImageFormat.Png);
                        stream.Position = 0;
                        bitmapImage.SetSource(stream.AsRandomAccessStream());
                    }

                    var MenuItem = new StartMenuItem()
                    {
                        ItemName = appInfo.DisplayName,
                        ItemStartURI = "com.epicgames.launcher://apps/" + appInfo.CatalogNamespace + "%3A" + appInfo.CatalogItemId + "%3A" + appInfo.AppName + "?action=launch&silent=true",
                        ItemKind = ApplicationKind.EpicGamesGame,
                        Icon = bitmapImage,
                        GameInfo = game
                    };
                    allApps.Add(MenuItem);
                }
            }
            else
                return;
        }

        private SteamGridDb db;

        private static Icon Extract(string file, int number, bool largeIcon)
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
        public static extern int ExtractIconEx(string sFile, int iIndex, out IntPtr piLargeVersion, out IntPtr piSmallVersion, int amountIcons);

        private static string[] IndexFolder(string folderPath)
        {
            string[] files = Directory.GetFiles(folderPath);

            string[] subDirectories = Directory.GetDirectories(folderPath);
            if (subDirectories.Length != 0)
            {
                foreach (string directory in subDirectories)
                {
                    string[] subfiles = IndexFolder(directory);
                    foreach (string subfile in subfiles)
                        files = [.. files, subfile];
                }
            }

            return files;
        }

        private async Task IndexPackagedApps()
        {
            PackageManager packageManager = new();
            IEnumerable<Package> packages = packageManager.FindPackagesForUser(string.Empty);

            foreach (Package package in packages)
            {
                if (File.Exists(package.InstalledPath + "\\MicrosoftGame.Config"))
                {
                    var serilizer = new System.Xml.Serialization.XmlSerializer(typeof(Game));
                    var reader = new StreamReader(package.InstalledPath + "\\MicrosoftGame.Config");
                    var productId = (Game)serilizer.Deserialize(reader);

                    IReadOnlyList<AppListEntry> appListEntries = package.GetAppListEntries();

                    foreach (AppListEntry appListEntry in appListEntries)
                    {
                        var MenuItem = new StartMenuItem()
                        {
                            ItemName = appListEntry.DisplayInfo.DisplayName,
                            ItemStartURI = package.Id.FullName + " " + productId.StoreId,
                            ItemKind = ApplicationKind.XboxGame,
                            Icon = new BitmapImage() { UriSource = package.Logo },
                            GameInfo = (await db.SearchForGamesAsync(appListEntry.DisplayInfo.DisplayName)).First()
                        };
                        allApps.Add(MenuItem);
                    }
                }
                else if (File.Exists(package.InstalledPath + "\\xboxservices.config"))
                {
                    IReadOnlyList<AppListEntry> appListEntries = package.GetAppListEntries();

                    foreach (AppListEntry appListEntry in appListEntries)
                    {
                        var MenuItem = new StartMenuItem()
                        {
                            ItemName = appListEntry.DisplayInfo.DisplayName,
                            ItemStartURI = package.Id.FullName,
                            ItemKind = ApplicationKind.XboxGame,
                            Icon = new BitmapImage() { UriSource = package.Logo },
                            GameInfo = (await db.SearchForGamesAsync(appListEntry.DisplayInfo.DisplayName)).First()
                        };
                        allApps.Add(MenuItem);
                    }
                }
                else if (!package.IsResourcePackage && !package.IsFramework && !package.IsStub && !package.IsBundle)
                {
                    IReadOnlyList<AppListEntry> appListEntries = package.GetAppListEntries();

                    foreach (AppListEntry appListEntry in appListEntries)
                    {
                        switch (appListEntry.DisplayInfo.DisplayName)
                        {
                            case "Windows Security":
                                var SecurityMenuItem = new StartMenuItem()
                                {
                                    ItemName = appListEntry.DisplayInfo.DisplayName,
                                    ItemStartURI = package.Id.FullName,
                                    ItemKind = ApplicationKind.Packaged,
                                    Icon = new BitmapImage() { UriSource = new Uri(package.Logo.AbsoluteUri.Replace("WindowsSecuritySplashScreen.scale-200.png", "WindowsSecurityAppList.targetsize-256.png").Replace("WindowsSecuritySplashScreen.scale-100.png", "WindowsSecurityAppList.targetsize-256.png")) }
                                };
                                allApps.Add(SecurityMenuItem);
                                break;
                            case "Windows Backup":
                                var BackupMenuItem = new StartMenuItem()
                                {
                                    ItemName = appListEntry.DisplayInfo.DisplayName,
                                    ItemStartURI = package.Id.FullName,
                                    ItemKind = ApplicationKind.Packaged,
                                    Icon = new BitmapImage() { UriSource = new Uri(@"C:\Windows\SystemApps\MicrosoftWindows.Client.CBS_cw5n1h2txyewy\WindowsBackup\Assets\AppList.targetsize-256.png") }
                                };
                                allApps.Add(BackupMenuItem);
                                break;
                            case "Get Started":
                                var GetStartedMenuItem = new StartMenuItem()
                                {
                                    ItemName = appListEntry.DisplayInfo.DisplayName,
                                    ItemStartURI = package.Id.FullName,
                                    ItemKind = ApplicationKind.Packaged,
                                    Icon = new BitmapImage() { UriSource = new Uri(@"C:\Windows\SystemApps\MicrosoftWindows.Client.CBS_cw5n1h2txyewy\Assets\GetStartedAppList.targetsize-256.png") }
                                };
                                allApps.Add(GetStartedMenuItem);
                                break;
                            case "Xbox":
                                var XboxMenuItem = new StartMenuItem()
                                {
                                    ItemName = appListEntry.DisplayInfo.DisplayName,
                                    ItemStartURI = package.Id.FullName,
                                    ItemKind = ApplicationKind.LauncherPackaged,
                                    Icon = new BitmapImage() { UriSource = package.Logo }
                                };
                                allApps.Add(XboxMenuItem);
                                break;
                            default:
                                var MenuItem = new StartMenuItem()
                                {
                                    ItemName = appListEntry.DisplayInfo.DisplayName,
                                    ItemStartURI = package.Id.FullName,
                                    ItemKind = ApplicationKind.Packaged,
                                    Icon = new BitmapImage() { UriSource = package.Logo }
                                };
                                allApps.Add(MenuItem);
                                break;

                        }
                    }
                }
            }
        }

        private void AddAppsToStartMenu()
        {
            var ordered = from item in allApps orderby item.ItemName[..1] select item;
            var orderedList = ordered.ToList();
            allApps.Clear();

            foreach (StartMenuItem item in orderedList)
            {
                switch (item.ItemKind)
                {
                    case ApplicationKind.Packaged:
                    case ApplicationKind.Normal:
                        appsList.Add(item);
                        break;
                    case ApplicationKind.SteamGame:
                    case ApplicationKind.EpicGamesGame:
                    case ApplicationKind.XboxGame:
                        gamesList.Add(item);
                        break;
                    case ApplicationKind.LauncherPackaged:
                    case ApplicationKind.Launcher:
                        launcherList.Add(item);
                        break;
                }
                allApps.Add(item);
            }
        }

        private void IndexStartMenuFolder(string userItemsDirectory)
        {
            IEnumerable<string> userStartMenuItems = Directory.EnumerateFiles(userItemsDirectory);
            string[] userStartMenuFolders = Directory.GetDirectories(userItemsDirectory);

            foreach (string folder in userStartMenuFolders)
            {
                string[] folderItems = IndexFolder(folder);

                foreach (string item in folderItems)
                    userStartMenuItems = userStartMenuItems.Append(item);
            }

            foreach (string item in userStartMenuItems)
            {
                if (item.EndsWith(".lnk") || item.EndsWith(".url"))
                {
                    var shellFile = ShellFile.FromFilePath(item);
                    string name = shellFile.Name == "Administrative Tools" ? "Windows Tools" : shellFile.Name;
                    string targetPath = shellFile.Properties.System.Link.TargetParsingPath.Value;

                    BitmapImage bitmapImage = new();
                    ApplicationKind appKind = (targetPath.EndsWith("steam.exe") && name == "Steam") || (targetPath.EndsWith("EpicGamesLauncher.exe") && name == "Epic Games Launcher") ? ApplicationKind.Launcher : ApplicationKind.Normal;
                    SteamGridDbGame game = null;

                    if (targetPath.StartsWith("steam://rungameid/") || targetPath.StartsWith("com.epicgames.launcher://"))
                        continue;
                    else
                    {
                        int number = targetPath switch
                        {
                            "Control Panel" => 21,
                            "Run..." => 24,
                            @"C:\Windows\system32\control.exe" => name == "Windows Tools" ? 109 : 0,
                            _ => 0
                        };
                        string path = targetPath switch
                        {
                            "File Explorer" => @"C:\Windows\explorer.exe",
                            "Control Panel" => @"C:\Windows\System32\shell32.dll",
                            "Run..." => @"C:\Windows\System32\shell32.dll",
                            @"C:\Windows\system32\control.exe" => name == "Windows Tools" ? @"C:\Windows\System32\imageres.dll" : targetPath,
                            "Administrative Tools" => @"C:\Windows\System32\imageres.dll",
                            _ => targetPath
                        };

                        var icon = Extract(path, number, true);
                        Bitmap bitmap = icon is not null ? icon.ToBitmap() : Icon.ExtractAssociatedIcon(item).ToBitmap();
                        using MemoryStream stream = new();
                        bitmap.Save(stream, ImageFormat.Png);
                        stream.Position = 0;
                        bitmapImage.SetSource(stream.AsRandomAccessStream());
                    }

                    var MenuItem = new StartMenuItem()
                    {
                        ItemName = name,
                        ItemStartURI = item,
                        ItemKind = appKind,
                        Icon = bitmapImage,
                        GameInfo = game
                    };

                    allApps.Add(MenuItem);
                }
            }
        }

        private void StartMenu_Click(object sender, RoutedEventArgs e) => startMenu.Translation = startMenu.Translation == new Vector3(0, 900, 40) ? new Vector3(0, 0, 40) : new Vector3(0, 900, 40);
        private void TaskView_Click(object sender, RoutedEventArgs e) => taskViewBackground.Visibility = taskViewBackground.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;

        private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            apps.SetBinding(ItemsControl.ItemsSourceProperty, new Binding() { Source = (NavigationViewItem)args.SelectedItem == games_NavItem ? gamesList : (NavigationViewItem)args.SelectedItem == launchers_NavItem ? launcherList : appsList });
            autoSuggestBox.PlaceholderText = (NavigationViewItem)args.SelectedItem == games_NavItem ? "Search games" : (NavigationViewItem)args.SelectedItem == launchers_NavItem ? "Search launchers" : "Search apps";
            autoSuggestBox.Text = null;
        }

        ObservableCollection<StartMenuItem> allApps = [];
        ObservableCollection<StartMenuItem> gamesList = [];
        ObservableCollection<StartMenuItem> launcherList = [];
        ObservableCollection<StartMenuItem> appsList = [];

        private void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            List<ApplicationKind> applicationTypes = (NavigationViewItem)startNV.SelectedItem == games_NavItem ? [ApplicationKind.SteamGame, ApplicationKind.EpicGamesGame, ApplicationKind.XboxGame] : (NavigationViewItem)startNV.SelectedItem == launchers_NavItem ? [ApplicationKind.Launcher, ApplicationKind.LauncherPackaged] : [ApplicationKind.Normal, ApplicationKind.Packaged];
            ObservableCollection<StartMenuItem> list = (NavigationViewItem)startNV.SelectedItem == games_NavItem ? gamesList : (NavigationViewItem)startNV.SelectedItem == launchers_NavItem ? launcherList : appsList;
            var filteredUnordered = allApps.Where(entry => Filter(entry, sender.Text));
            var filtered = from item in filteredUnordered orderby item.ItemName[..1] select item;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                var item = list[i];

                if (!filtered.Contains(item))
                    list.Remove(item);
            }

            foreach (StartMenuItem item in filtered)
            {
                if (!list.Contains(item) && applicationTypes.Contains(item.ItemKind))
                    list.Add(item);
            }
        }

        private bool Filter(StartMenuItem entry, string filter)
        {
            return entry.ItemName.Contains(filter, StringComparison.InvariantCultureIgnoreCase);
        }

        private async void Apps_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is not null)
            {
                var selectedItemInfo = e.ClickedItem as StartMenuItem;

                if (selectedItemInfo.ItemKind == ApplicationKind.Normal || selectedItemInfo.ItemKind == ApplicationKind.Launcher)
                    Process.Start(new ProcessStartInfo(selectedItemInfo.ItemStartURI) { UseShellExecute = true });
                else if (selectedItemInfo.ItemKind == ApplicationKind.SteamGame)
                {
                    var dialog = new ContentDialog();

                    var content = new Grid() { Margin = new Thickness(-24) };
                    var heros = await db.GetHeroesByGameIdAsync(selectedItemInfo.GameInfo.Id);
                    var logos = await db.GetLogosForGameAsync(selectedItemInfo.GameInfo);
                    content.Children.Add(new Microsoft.UI.Xaml.Controls.Image() { Source = new BitmapImage() { UriSource = new Uri(heros.Length != 0 ? heros[0].FullImageUrl : "ms-appx:///Assets/Placeholder.png") } });
                    content.Children.Add(new Microsoft.UI.Xaml.Controls.Image() { MaxHeight = 90, Margin = new Thickness(40), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Stretch, Source = new BitmapImage() { UriSource = new Uri(logos.Length != 0 ? logos[0].FullImageUrl : "ms-appx:///Assets/Placeholder.png") } });
                    dialog.Content = content;

                    dialog.XamlRoot = Content.XamlRoot;
                    dialog.CloseButtonText = "Cancel";
                    dialog.SecondaryButtonText = "View in Steam";
                    dialog.PrimaryButtonText = "Play";
                    dialog.DefaultButton = ContentDialogButton.Primary;
                    var selection = await dialog.ShowAsync();

                    switch (selection)
                    {
                        case ContentDialogResult.Primary:
                            Process.Start(new ProcessStartInfo(selectedItemInfo.ItemStartURI) { UseShellExecute = true });
                            break;
                        case ContentDialogResult.Secondary:
                            Process.Start(new ProcessStartInfo("steam://openurl/https://store.steampowered.com/app/" + selectedItemInfo.ItemStartURI.Replace("steam://rungameid/", null)) { UseShellExecute = true });
                            break;
                    }
                }
                else if (selectedItemInfo.ItemKind == ApplicationKind.EpicGamesGame)
                {
                    var dialog = new ContentDialog();

                    var content = new Grid() { Margin = new Thickness(-24) };
                    var heros = await db.GetHeroesByGameIdAsync(selectedItemInfo.GameInfo.Id);
                    var logos = await db.GetLogosForGameAsync(selectedItemInfo.GameInfo);
                    content.Children.Add(new Microsoft.UI.Xaml.Controls.Image() { Source = new BitmapImage() { UriSource = new Uri(heros.Length != 0 ? heros[0].FullImageUrl : "ms-appx:///Assets/Placeholder.png") } });
                    content.Children.Add(new Microsoft.UI.Xaml.Controls.Image() { MaxHeight = 90, Margin = new Thickness(40), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Stretch, Source = new BitmapImage() { UriSource = new Uri(logos.Length != 0 ? logos[0].FullImageUrl : "ms-appx:///Assets/Placeholder.png") } });
                    dialog.Content = content;

                    dialog.XamlRoot = Content.XamlRoot;
                    dialog.CloseButtonText = "Cancel";
                    dialog.PrimaryButtonText = "Play";
                    dialog.DefaultButton = ContentDialogButton.Primary;
                    var selection = await dialog.ShowAsync();

                    if (selection == ContentDialogResult.Primary)
                        Process.Start(new ProcessStartInfo(selectedItemInfo.ItemStartURI) { UseShellExecute = true });
                }
                else if (selectedItemInfo.ItemKind == ApplicationKind.XboxGame)
                {
                    var dialog = new ContentDialog();

                    var content = new Grid() { Margin = new Thickness(-24) };
                    var heros = await db.GetHeroesByGameIdAsync(selectedItemInfo.GameInfo.Id);
                    var logos = await db.GetLogosForGameAsync(selectedItemInfo.GameInfo);
                    content.Children.Add(new Microsoft.UI.Xaml.Controls.Image() { Source = new BitmapImage() { UriSource = new Uri(heros.Length != 0 ? heros[0].FullImageUrl : "ms-appx:///Assets/Placeholder.png") } });
                    content.Children.Add(new Microsoft.UI.Xaml.Controls.Image() { MaxHeight = 90, Margin = new Thickness(40), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Stretch, Source = new BitmapImage() { UriSource = new Uri(logos.Length != 0 ? logos[0].FullImageUrl : "ms-appx:///Assets/Placeholder.png") } });
                    dialog.Content = content;

                    dialog.XamlRoot = Content.XamlRoot;
                    dialog.CloseButtonText = "Cancel";
                    dialog.SecondaryButtonText = selectedItemInfo.ItemStartURI.Contains(' ') ? "View in Xbox app" : null;
                    dialog.PrimaryButtonText = "Play";
                    dialog.DefaultButton = ContentDialogButton.Primary;
                    var selection = await dialog.ShowAsync();

                    switch (selection)
                    {
                        case ContentDialogResult.Primary:
                            var packageName = selectedItemInfo.ItemStartURI.Split(' ').First();

                            PackageManager packageManager = new();
                            Package package = packageManager.FindPackageForUser(string.Empty, packageName);

                            IReadOnlyList<AppListEntry> appListEntries = package.GetAppListEntries();
                            await appListEntries.First(i => i.DisplayInfo.DisplayName == selectedItemInfo.ItemName).LaunchAsync();
                            break;
                        case ContentDialogResult.Secondary:
                            var productID = selectedItemInfo.ItemStartURI.Split(' ').Last();
                            Process.Start(new ProcessStartInfo($"msxbox://game/?productId={productID}") { UseShellExecute = true });
                            break;
                    }
                }
                else if (selectedItemInfo.ItemKind == ApplicationKind.Packaged || selectedItemInfo.ItemKind == ApplicationKind.LauncherPackaged)
                {
                    PackageManager packageManager = new();
                    Package package = packageManager.FindPackageForUser(string.Empty, selectedItemInfo.ItemStartURI);

                    IReadOnlyList<AppListEntry> appListEntries = package.GetAppListEntries();
                    await appListEntries.First(i => i.DisplayInfo.DisplayName == selectedItemInfo.ItemName).LaunchAsync();
                }
            }
        }
    }
}
