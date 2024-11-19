using craftersmine.SteamGridDBNet.Exceptions;
using craftersmine.SteamGridDBNet;
using GameFinder.RegistryUtils;
using GameFinder.StoreHandlers.Steam;
using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Windows_Mobile.Types;
using NexusMods.Paths;
using Microsoft.UI.Xaml.Media.Imaging;
using GameFinder.StoreHandlers.GOG;
using Microsoft.Win32;
using Windows.ApplicationModel.Core;
using Windows.Management.Deployment;
using Windows.ApplicationModel;
using Microsoft.WindowsAPICodePack.Shell;
using Windows_Mobile.MC;

namespace Windows_Mobile.Indexing
{
    public static class Indexers
    {
        public static void IndexMCMods(ObservableCollection<MCModInfo> mods)
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

        public static async Task IndexSteamGames(ObservableCollection<StartMenuItem> allApps)
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

        public static async Task IndexEGSGames(ObservableCollection<StartMenuItem> allApps)
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

        //private async Task IndexEAGames(ObservableCollection<StartMenuItem> allApps)
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

        public static async Task IndexGOGGames(ObservableCollection<StartMenuItem> allApps)
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

        public static async Task IndexPackagedApps(ObservableCollection<StartMenuItem> allApps)
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

        public static void IndexStartMenuFolder(string userItemsDirectory, ObservableCollection<StartMenuItem> allApps)
        {
            IEnumerable<string> userStartMenuItems = Directory.EnumerateFiles(userItemsDirectory);
            string[] userStartMenuFolders = Directory.GetDirectories(userItemsDirectory);

            foreach (string folder in userStartMenuFolders)
            {
                string[] folderItems = Utils.PathUtils.IndexFolder(folder);

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

                    if (targetPath is not null && !targetPath.StartsWith("steam://rungameid/") && !targetPath.StartsWith("com.epicgames.launcher://") && !targetPath.Contains("unins000.exe", StringComparison.InvariantCultureIgnoreCase) && !name.Contains("Uninstall", StringComparison.InvariantCultureIgnoreCase) && !(arguments.Contains("/command=runGame", StringComparison.InvariantCultureIgnoreCase) && arguments.Contains("/gameId=", StringComparison.InvariantCultureIgnoreCase)))
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
    }
}
