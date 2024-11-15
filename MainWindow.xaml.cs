using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
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
using Microsoft.Win32;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Controls.Primitives;
using ICSharpCode.SharpZipLib.Zip;
using System.Diagnostics;
using CommunityToolkit.WinUI.UI.Animations;

namespace Windows_Mobile
{
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
            PopulateStartMenu();

            allSearch.CollectionChanged += (sender, e) => MenuBar_HeightUpdate();
        }

        private async void PopulateStartMenu()
        {
            await IndexSteamGames();
            await IndexEGSGames();
            //await IndexEAGames();
            await IndexGOGGames();
            await IndexPackagedApps();
            IndexMCMods();
            IndexStartMenuFolder("C:\\Users\\" + Environment.UserName + "\\AppData\\Roaming\\Microsoft\\Windows\\Start Menu\\Programs");
            IndexStartMenuFolder("C:\\ProgramData\\Microsoft\\Windows\\Start Menu\\Programs");
            AddAppsToStartMenu();
        }

        ObservableCollection<MCModInfo> mods = [];
        private void IndexMCMods()
        {
            if (Directory.Exists(@$"C:\Users\{Environment.UserName}\AppData\Roaming\.minecraft\mods"))
            {
                var jars = Directory.GetFiles(@$"C:\Users\{Environment.UserName}\AppData\Roaming\.minecraft\mods");
                foreach (string jar in jars)
                {
                    if (!jar.EndsWith(".jar", StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    using var zf = new ZipFile(new FileStream(jar, FileMode.Open, FileAccess.Read));

                    var ze = zf.FindEntry("fabric.mod.json", true);
                    if (ze != -1)
                    {
                        using Stream s = zf.GetInputStream(ze);
                        StreamReader reader = new(s);
                        string json = reader.ReadToEnd();
                        var modInfo = JsonSerializer.Deserialize<MCModInfo>(json);
                        modInfo.image = zf.GetInputStream(zf.GetEntry(modInfo.icon)).ToBitmapImage();

                        modInfo.kind = ModKind.FabricQuilt;
                        mods.Add(modInfo);
                    }
                    else
                    {
                        ze = zf.FindEntry("META-INF/mods.toml", true);
                        if (ze != -1)
                        {
                            using Stream s = zf.GetInputStream(ze);
                            StreamReader reader = new(s);
                            var table = Tommy.TOML.Parse(reader);
                            var modInfoTOML = table["mods"].Children.First();

                            var modInfo = new MCModInfo() { name = (string)modInfoTOML["displayName"], description = (string)modInfoTOML["description"], version = (string)modInfoTOML["version"], license = (string)table["license"], contact = new MCModContact() { homepage = (string)modInfoTOML["displayURL"], issues = (string)table["issueTrackerURL"] }, icon = (string)modInfoTOML["logoFile"], kind = ModKind.Forge };
                            modInfo.image = zf.GetInputStream(zf.GetEntry(modInfo.icon)).ToBitmapImage();
                            mods.Add(modInfo);
                        }
                        else
                        {
                            ze = zf.FindEntry("META-INF/neoforge.mods.toml", true);
                            if (ze != -1)
                            {
                                using Stream s = zf.GetInputStream(ze);
                                StreamReader reader = new(s);
                                var table = Tommy.TOML.Parse(reader);
                                var modInfoTOML = table["mods"].Children.First();

                                var modInfo = new MCModInfo() { name = (string)modInfoTOML["displayName"], description = (string)modInfoTOML["description"], version = (string)modInfoTOML["version"], license = (string)table["license"], contact = new MCModContact() { homepage = (string)modInfoTOML["displayURL"], issues = (string)table["issueTrackerURL"] }, icon = (string)modInfoTOML["logoFile"], kind = ModKind.NeoForge };
                                modInfo.image = zf.GetInputStream(zf.GetEntry(modInfo.icon)).ToBitmapImage();
                                mods.Add(modInfo);
                            }
                        }
                    }
                }
            }
        }

        private async Task IndexSteamGames()
        {
            var handler = new SteamHandler(FileSystem.Shared, WindowsRegistry.Shared);
            var games = handler.FindAllGames();
            foreach (var game in games)
            {
                var steamGame = game.Value as SteamGame;
                if (steamGame is not null && steamGame.AppId.Value != 228980) 
                {
                    SteamGridDbGame gameInfo = null;
                    BitmapImage bitmapImage = new();
                    try
                    {
                        gameInfo = await App.db.GetGameBySteamIdAsync((int)steamGame.AppId.Value);
                    }
                    catch (SteamGridDbNotFoundException)
                    {
                        gameInfo = (await App.db.SearchForGamesAsync(steamGame.Name)).First();
                    }

                    var image = await App.db.GetIconsForGameAsync(gameInfo);
                    if (image.Length != 0)
                        bitmapImage.UriSource = new Uri(image[0].FullImageUrl);
                    else
                    {
                        using var stream = new MemoryStream();
                        Icon.ExtractAssociatedIcon(Directory.GetFiles(steamGame.Path.ToString()).First(i => i.EndsWith(".exe"))).ToBitmap().Save(stream, ImageFormat.Png);
                        stream.Position = 0;
                        bitmapImage.SetSource(stream.AsRandomAccessStream());
                    }

                    var MenuItem = new StartMenuItem()
                    {
                        ItemName = steamGame.Name,
                        ItemStartURI = "steam://rungameid/" + steamGame.AppId.Value,
                        ItemKind = ApplicationKind.SteamGame,
                        Icon = bitmapImage,
                        GameInfo = gameInfo,
                        Id = steamGame.AppId.Value.ToString()
                    };
                    allApps.Add(MenuItem);
                }
            }
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

                    SteamGridDbGame game = (await App.db.SearchForGamesAsync(appInfo.DisplayName)).First();
                    var image = await App.db.GetIconsForGameAsync(game);
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

        //private async Task IndexEAGames()
        //{
        //    var eHandler = new EADesktopHandler(FileSystem.Shared, new HardwareInfoProvider());
        //    var eGames = eHandler.FindAllGames();
        //    foreach (var game in eGames)
        //    {
        //        var eGame = game.Value as EADesktopGame;
        //        SteamGridDbGame gameInfo = null;
        //        BitmapImage bitmapImage = new();

        //        gameInfo = (await db.SearchForGamesAsync(eGame.BaseSlug)).First();

        //        var image = await db.GetIconsForGameAsync(gameInfo);
        //        if (image.Length != 0)
        //            bitmapImage.UriSource = new Uri(image[0].FullImageUrl);
        //        else
        //        {
        //            using var stream = new MemoryStream();
        //            Icon.ExtractAssociatedIcon(Directory.GetFiles(eGame.BaseInstallPath.ToString()).First(i => i.EndsWith(".exe"))).ToBitmap().Save(stream, ImageFormat.Png);
        //            stream.Position = 0;
        //            bitmapImage.SetSource(stream.AsRandomAccessStream());
        //        }

        //        var MenuItem = new StartMenuItem()
        //        {
        //            ItemName = eGame.BaseSlug,
        //            ItemStartURI = "steam://rungameid/" + steamGame.AppId.Value,
        //            ItemKind = ApplicationKind.,
        //            Icon = bitmapImage,
        //            GameInfo = gameInfo
        //        };
        //        allApps.Add(MenuItem);
        //    }
        //}

        private async Task IndexGOGGames()
        {
            var gogInstallationPath = Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\WOW6432Node\\GOG.com\\GalaxyClient\\paths", "client", string.Empty);
            string gogInstallation = gogInstallationPath is not null ? gogInstallationPath.ToString() : string.Empty;

            var handler = new GOGHandler(WindowsRegistry.Shared, FileSystem.Shared);
            var games = handler.FindAllGames();
            foreach (var game in games)
            {
                var gogGame = game.Value as GOGGame;
                if (gogGame is not null)
                {
                    SteamGridDbGame gameInfo = (await App.db.SearchForGamesAsync(gogGame.Name)).First();
                    BitmapImage bitmapImage = new();

                    var image = await App.db.GetIconsForGameAsync(gameInfo);
                    if (image.Length != 0)
                        bitmapImage.UriSource = new Uri(image[0].FullImageUrl);
                    else
                    {
                        using var stream = new MemoryStream();
                        Icon.ExtractAssociatedIcon(Directory.GetFiles(gogGame.Path.ToString()).First(i => i.EndsWith(".exe"))).ToBitmap().Save(stream, ImageFormat.Png);
                        stream.Position = 0;
                        bitmapImage.SetSource(stream.AsRandomAccessStream());
                    }

                    var MenuItem = new StartMenuItem()
                    {
                        ItemName = gogGame.Name,
                        ItemStartURI = $"\"{gogInstallation}\\GalaxyClient.exe\" /command=runGame /gameId={gogGame.Id.Value} /path=\"{gogGame.Path}\"",
                        ItemKind = ApplicationKind.GOGGame,
                        Icon = bitmapImage,
                        GameInfo = gameInfo,
                        Id = gogGame.Id.Value.ToString()
                    };
                    allApps.Add(MenuItem);
                }
            }
        }

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
                            ItemStartURI = package.Id.FullName,
                            ItemKind = ApplicationKind.XboxGame,
                            Icon = new BitmapImage() { UriSource = package.Logo },
                            GameInfo = (await App.db.SearchForGamesAsync(appListEntry.DisplayInfo.DisplayName)).First(),
                            Id = productId.StoreId
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
                            GameInfo = (await App.db.SearchForGamesAsync(appListEntry.DisplayInfo.DisplayName)).First()
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
                            case "Roblox":
                                var RobloxMenuItem = new StartMenuItem()
                                {
                                    ItemName = appListEntry.DisplayInfo.DisplayName,
                                    ItemStartURI = package.Id.FullName,
                                    ItemKind = ApplicationKind.XboxGame,
                                    Icon = new BitmapImage() { UriSource = package.Logo },
                                    GameInfo = (await App.db.SearchForGamesAsync(appListEntry.DisplayInfo.DisplayName)).First()
                                };
                                allApps.Add(RobloxMenuItem);
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
                if (!item.IsDuplicate(allApps))
                {
                    switch (item.ItemKind)
                    {
                        case ApplicationKind.Packaged:
                        case ApplicationKind.Normal:
                            appsList.Add(item);
                            break;
                        case ApplicationKind.SteamGame:
                        case ApplicationKind.EpicGamesGame:
                        case ApplicationKind.GOGGame:
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
                    string arguments = shellFile.Properties.System.Link.Arguments.Value is not null ? shellFile.Properties.System.Link.Arguments.Value : string.Empty;

                    if (!targetPath.StartsWith("steam://rungameid/") && !targetPath.StartsWith("com.epicgames.launcher://") && !targetPath.Contains("unins000.exe", StringComparison.InvariantCultureIgnoreCase) && !name.Contains("Uninstall", StringComparison.InvariantCultureIgnoreCase) && !(arguments.Contains("/command=runGame", StringComparison.InvariantCultureIgnoreCase) && arguments.Contains("/gameId=", StringComparison.InvariantCultureIgnoreCase)))
                    {
                        BitmapImage bitmapImage = new();
                        ApplicationKind appKind = targetPath.Contains("Steam.exe", StringComparison.InvariantCultureIgnoreCase) || targetPath.Contains("EpicGamesLauncher.exe", StringComparison.InvariantCultureIgnoreCase) || targetPath.EndsWith("GalaxyClient.exe", StringComparison.InvariantCultureIgnoreCase) ? ApplicationKind.Launcher : ApplicationKind.Normal;
                        SteamGridDbGame game = null;

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

                        var icon = App.ExtractIcon(path, number, true);
                        Bitmap bitmap = icon is not null ? icon.ToBitmap() : Icon.ExtractAssociatedIcon(item).ToBitmap();
                        using MemoryStream stream = new();
                        bitmap.Save(stream, ImageFormat.Png);
                        stream.Position = 0;
                        bitmapImage.SetSource(stream.AsRandomAccessStream());

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
        }

        private void StartMenu_Click(object sender, RoutedEventArgs e) => startMenu.Translation = startMenu.Translation == new Vector3(0, 900, 40) ? new Vector3(0, 0, 40) : new Vector3(0, 900, 40);
        private void GameView_Click(object sender, RoutedEventArgs e) => taskViewBackground.Visibility = taskViewBackground.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
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
        ObservableCollection<StartMenuItem> allSearch = [];

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

                if (selectedItemInfo.ItemKind == ApplicationKind.Normal || selectedItemInfo.ItemKind == ApplicationKind.Launcher || selectedItemInfo.ItemKind == ApplicationKind.Packaged || selectedItemInfo.ItemKind == ApplicationKind.LauncherPackaged)
                    App.StartApplication(selectedItemInfo);
                else
                {
                    var content = new Grid() { Margin = new Thickness(-24) };
                    var heros = await App.db.GetHeroesByGameIdAsync(selectedItemInfo.GameInfo.Id);
                    var logos = await App.db.GetLogosForGameAsync(selectedItemInfo.GameInfo);
                    content.Children.Add(new Microsoft.UI.Xaml.Controls.Image() { Source = new BitmapImage() { UriSource = new Uri(heros.Length != 0 ? heros[0].FullImageUrl : "ms-appx:///Assets/Placeholder.png") } });
                    content.Children.Add(new Microsoft.UI.Xaml.Controls.Image() { MaxHeight = 90, Margin = new Thickness(40), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Stretch, Source = new BitmapImage() { UriSource = new Uri(logos.Length != 0 ? logos[0].FullImageUrl : "ms-appx:///Assets/Placeholder.png") } });
                    var dialog = new ContentDialog() { XamlRoot = this.Content.XamlRoot, Content = content, PrimaryButtonText = "Play", CloseButtonText = "Cancel", DefaultButton = ContentDialogButton.Primary };

                    if (selectedItemInfo.ItemKind == ApplicationKind.SteamGame)
                    {
                        dialog.SecondaryButtonText = "View in Steam";
                        var selection = await dialog.ShowAsync();

                        if (selection == ContentDialogResult.Primary)
                            App.StartApplication(selectedItemInfo);
                        else if (selection == ContentDialogResult.Secondary)
                            Process.Start(new ProcessStartInfo($"steam://openurl/https://store.steampowered.com/app/{selectedItemInfo.Id}") { UseShellExecute = true });
                    }
                    else if (selectedItemInfo.ItemKind == ApplicationKind.EpicGamesGame)
                    {
                        var selection = await dialog.ShowAsync();

                        if (selection == ContentDialogResult.Primary)
                            App.StartApplication(selectedItemInfo);
                    }
                    else if (selectedItemInfo.ItemKind == ApplicationKind.XboxGame)
                    {
                        dialog.SecondaryButtonText = selectedItemInfo.Id is not null ? "View in the Xbox app" : null;
                        var selection = await dialog.ShowAsync();

                        if (selection == ContentDialogResult.Primary)
                            App.StartApplication(selectedItemInfo);
                        else if (selection == ContentDialogResult.Secondary)
                            Process.Start(new ProcessStartInfo($"msxbox://game/?productId={selectedItemInfo.Id}") { UseShellExecute = true });
                    }
                    else if (selectedItemInfo.ItemKind == ApplicationKind.GOGGame)
                    {
                        dialog.SecondaryButtonText = "View in GOG Galaxy";
                        var selection = await dialog.ShowAsync();

                        if (selection == ContentDialogResult.Primary)
                            App.StartApplication(selectedItemInfo);
                        else if (selection == ContentDialogResult.Secondary)
                            Process.Start(new ProcessStartInfo($"goggalaxy://openGameView/{selectedItemInfo.Id}") { UseShellExecute = true });
                    }
                }
            }
        }

        private void StartMenuItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            var senderPanel = sender as StackPanel;
            var appType = (senderPanel.Tag as StartMenuItem).ItemKind;
            var flyout = new MenuFlyout();

            var openButton = new MenuFlyoutItem();
            openButton.Click += (sender, args) => App.StartApplication(allApps.First(i => i.Icon == (senderPanel.Tag as StartMenuItem).Icon));

            var adminButton = new MenuFlyoutItem() { Text = "Open as admin", Icon = new FontIcon() { Glyph = "\uE7EF" }, Visibility = Visibility.Collapsed };
            adminButton.Click += (sender, args) => App.StartApplication(allApps.First(i => i.Icon == (senderPanel.Tag as StartMenuItem).Icon), true);

            var locationButton = new MenuFlyoutItem() { Text = "Open file location", Icon = new FontIcon() { Glyph = "\uED43" }, Visibility = Visibility.Collapsed };
            locationButton.Click += (sender, args) => Process.Start(new ProcessStartInfo("explorer.exe", $"/select, {(senderPanel.Tag as StartMenuItem).ItemStartURI}") { UseShellExecute = true });

            var uninstallButton = new MenuFlyoutItem() { Text = "Uninstall", Icon = new FontIcon() { Glyph = "\uE74D" } };

            switch (appType)
            {
                default:
                case ApplicationKind.Normal:
                case ApplicationKind.Launcher:
                    openButton.Text = "Open";
                    openButton.Icon = new FontIcon() { Glyph = "\uE737" };
                    adminButton.Visibility = Visibility.Visible;
                    locationButton.Visibility = Visibility.Visible;
                    uninstallButton.Click += (sender, args) => Process.Start(new ProcessStartInfo("ms-settings:appsfeatures") { UseShellExecute = true });
                    break;
                case ApplicationKind.Packaged:
                case ApplicationKind.LauncherPackaged:
                    openButton.Text = "Open";
                    openButton.Icon = new FontIcon() { Glyph = "\uE737" };
                    uninstallButton.Click += (sender, args) => Process.Start(new ProcessStartInfo("ms-settings:appsfeatures") { UseShellExecute = true });
                    break;
                case ApplicationKind.SteamGame:
                    openButton.Text = "Play";
                    openButton.Icon = new FontIcon() { Glyph = "\uE768" };
                    uninstallButton.Click += (sender, args) => App.StartApplication(launcherList.First(i => i.ItemName.Contains("Steam", StringComparison.InvariantCultureIgnoreCase)));
                    break;
                case ApplicationKind.EpicGamesGame:
                    openButton.Text = "Play";
                    openButton.Icon = new FontIcon() { Glyph = "\uE768" };
                    uninstallButton.Click += (sender, args) => App.StartApplication(launcherList.First(i => i.ItemName.Contains("Epic", StringComparison.InvariantCultureIgnoreCase)));
                    break;
                case ApplicationKind.GOGGame:
                    openButton.Text = "Play";
                    openButton.Icon = new FontIcon() { Glyph = "\uE768" };
                    uninstallButton.Click += (sender, args) => App.StartApplication(launcherList.First(i => i.ItemName.Equals("GOG GALAXY", StringComparison.InvariantCultureIgnoreCase)));
                    break;
                case ApplicationKind.XboxGame:
                    openButton.Text = "Play";
                    openButton.Icon = new FontIcon() { Glyph = "\uE768" };
                    uninstallButton.Click += (sender, args) => Process.Start(new ProcessStartInfo("ms-settings:appsfeatures") { UseShellExecute = true });
                    break;
            }

            flyout.Items.Add(openButton);
            if (adminButton.Visibility == Visibility.Visible)
                flyout.Items.Add(adminButton);

            flyout.Items.Add(new MenuFlyoutSeparator());

            if (locationButton.Visibility == Visibility.Visible)
                flyout.Items.Add(locationButton);

            flyout.Items.Add(uninstallButton);

            FlyoutShowOptions options = new() { Position = e.GetPosition(senderPanel), Placement = FlyoutPlacementMode.RightEdgeAlignedTop };

            flyout.ShowAt(senderPanel, options);
        }

        private void MenuBar_HeightUpdate()
        {
            if (Animated == true) 
            {
                var newHeight = ((allSearch.Count * 36) + ((allSearch.Count - 1) * 4) + 70).Clamp(((int)startMenu.Height).Clamp(600, 400));
                var oldHeight = menuBar.ActualHeight;

                if (newHeight != oldHeight && newHeight != 64)
                    AnimationBuilder.Create().Size(axis: Axis.Y, to: newHeight, from: oldHeight, duration: TimeSpan.FromMilliseconds(500), easingType: EasingType.Default, easingMode: Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseOut, layer: FrameworkLayer.Xaml).Start(menuBar);
            }
        }

        //Reduce starting width of top search box and animate to be bigger when text starts being inputed
        private bool? Animated { get; set; } = null;
        private Windows.Foundation.Size OriginalSize { get; set; }
        private void TopAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (Animated == true && string.IsNullOrWhiteSpace(sender.Text))
            {
                launcherGrid.Visibility = controlCenter.Visibility = time.Visibility = Visibility.Visible;
                allSearchList.Visibility = Visibility.Collapsed;
                sender.CornerRadius = new CornerRadius(20);
                sender.Translation = Vector3.Zero;
                var animationBuilder = AnimationBuilder.Create();
                animationBuilder.Size(axis: Axis.X, to: OriginalSize.Width, from: 698, duration: TimeSpan.FromMilliseconds(500), easingType: EasingType.Default, easingMode: Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseOut, layer: FrameworkLayer.Xaml).Start(menuBar);
                animationBuilder.Size(axis: Axis.Y, to: OriginalSize.Height, from: menuBar.ActualHeight, duration: TimeSpan.FromMilliseconds(500), easingType: EasingType.Default, easingMode: Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseOut, layer: FrameworkLayer.Xaml).Start(menuBar);
                Animated = false;
            }
            else if (Animated == false && !string.IsNullOrWhiteSpace(sender.Text))
            {
                launcherGrid.Visibility = controlCenter.Visibility = time.Visibility = Visibility.Collapsed;
                allSearchList.Visibility = Visibility.Visible;
                sender.CornerRadius = new CornerRadius(4);
                sender.Translation = new Vector3(0, 5, 0);
                var animationBuilder = AnimationBuilder.Create();
                animationBuilder.Size(axis: Axis.X, to: 698, from: OriginalSize.Width, duration: TimeSpan.FromMilliseconds(500), easingType: EasingType.Default, easingMode: Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseOut, layer: FrameworkLayer.Xaml).Start(menuBar);
                animationBuilder.Size(axis: Axis.Y, to: ((allSearch.Count * 36) + ((allSearch.Count - 1) * 4) + 70).Clamp(((int)startMenu.Height).Clamp(600, 400)), from: OriginalSize.Height, duration: TimeSpan.FromMilliseconds(500), easingType: EasingType.Default, easingMode: Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseOut, layer: FrameworkLayer.Xaml).Start(menuBar);
                Animated = true;
            }
            else if (Animated is null)
            {
                OriginalSize = new(menuBar.ActualWidth, menuBar.ActualHeight);
                
                launcherGrid.Visibility = controlCenter.Visibility = time.Visibility = Visibility.Collapsed;
                allSearchList.Visibility = Visibility.Visible;
                sender.CornerRadius = new CornerRadius(4);
                sender.Translation = new Vector3(0, 5, 0);
                var animationBuilder = AnimationBuilder.Create();
                animationBuilder.Size(axis: Axis.X, to: 698, from: OriginalSize.Width, duration: TimeSpan.FromMilliseconds(500), easingType: EasingType.Default, easingMode: Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseOut, layer: FrameworkLayer.Xaml).Start(menuBar);
                animationBuilder.Size(axis: Axis.Y, to: ((allSearch.Count * 36) + ((allSearch.Count - 1) * 4) + 70).Clamp(((int)startMenu.Height).Clamp(600, 400)), from: OriginalSize.Height, duration: TimeSpan.FromMilliseconds(500), easingType: EasingType.Default, easingMode: Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseOut, layer: FrameworkLayer.Xaml).Start(menuBar);
                Animated = true;
            }

            var filteredUnordered = allApps.Where(entry => Filter(entry, sender.Text));
            var filtered = from item in filteredUnordered orderby item.ItemName[..1] select item;

            for (int i = allSearch.Count - 1; i >= 0; i--)
            {
                var item = allSearch[i];

                if (!filtered.Contains(item))
                    allSearch.Remove(item);
            }

            foreach (StartMenuItem item in filtered)
            {
                if (!allSearch.Contains(item))
                    allSearch.Add(item);
            }
        }

        private void Open_Time(object sender, RoutedEventArgs args)
        {
            Process.Start(new ProcessStartInfo("ms-actioncenter://") { UseShellExecute = true });
        }

        private void Open_ControlCenter(object sender, RoutedEventArgs args)
        {
            Process.Start(new ProcessStartInfo("ms-actioncenter:controlcenter/&showFooter=true") { UseShellExecute = true });
        }
    }
}
