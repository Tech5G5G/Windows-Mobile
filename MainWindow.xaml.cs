using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using System.Numerics;
using Windows.Storage.Pickers.Provider;
using System.IO.Enumeration;
using Windows.Storage;
using Windows.Devices.AllJoyn;
using Windows.ApplicationModel.Contacts;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using Windows.Networking;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Management.Deployment;
using System.Net.Http.Headers;
using System.Security.Principal;
using System.Xml.Linq;
using Microsoft.Windows.ApplicationModel.DynamicDependency;
using Microsoft.WindowsAPICodePack.Shell;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Drawing.Imaging;
using System.Drawing;
using Windows_Mobile;
using System.Linq.Expressions;
using System.Windows.Forms;
using Windows.ApplicationModel;
using System.Runtime.CompilerServices;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Windows_Mobile
{
    public enum ApplicationKind
    {
        Normal = 1,

        Packaged = 2
    }

    public class StartMenuItem
    {
        public string ItemName { get; set;  }
        public ApplicationKind ItemKind { get; set; }
        public string ItemStartURI { get; set; }
        public BitmapImage Icon { get; set; }
    }

    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();

            Title = "Windows Mobile";
            AppWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen);

            wallpaperImage.ImageSource = new BitmapImage() { UriSource = new Uri("C:\\Users\\" + Environment.UserName + "\\AppData\\Roaming\\Microsoft\\Windows\\Themes\\TranscodedWallpaper") };

            IndexStartMenuFolder("C:\\Users\\" + Environment.UserName + "\\AppData\\Roaming\\Microsoft\\Windows\\Start Menu\\Programs");
            IndexStartMenuFolder("C:\\ProgramData\\Microsoft\\Windows\\Start Menu\\Programs");
            IndexPackagedApps();
            AddAppsToStartMenu();
        }

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

        private void IndexPackagedApps()
        {
            PackageManager packageManager = new();
            IEnumerable<Package> packages = packageManager.FindPackagesForUser(string.Empty);

            foreach (Package package in packages)
            {
                if (!package.IsResourcePackage && !package.IsFramework && !package.IsStub && !package.IsBundle && package.GetAppListEntries().FirstOrDefault() != null)
                {
                    IReadOnlyList<AppListEntry> appListEntries = package.GetAppListEntries();

                    foreach (AppListEntry appListEntry in appListEntries)
                    {
                        var MenuItem = new StartMenuItem()
                        {
                            ItemName = appListEntry.DisplayInfo.DisplayName,
                            ItemStartURI = package.Id.FullName,
                            ItemKind = ApplicationKind.Packaged,
                            Icon = new BitmapImage() { UriSource = package.Logo }
                        };

                        allApps.Add(MenuItem);
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
                allApps.Add(item);
                appsList.Add(item);
            }
        }

        private void IndexStartMenuFolder(string userItemsDirectory)
        {
            IEnumerable<string> userStartMenuItems = Directory.EnumerateFiles(userItemsDirectory);
            string[] userStartMenuFolders = Directory.GetDirectories(userItemsDirectory);

            foreach (string folder in userStartMenuFolders)
            {
                string[] folderItems = IndexFolder(folder);

                if (folderItems.Length == 1)
                {
                    foreach (string item in folderItems)
                    {
                        userStartMenuItems = userStartMenuItems.Append(item);
                    }
                }
                else
                {
                    userStartMenuItems = userStartMenuItems.Append(folder);
                }
            }

            foreach (string item in userStartMenuItems)
            {
                if (!item.EndsWith(".ini"))
                {
                    if (File.Exists(item))
                    {
                        FileInfo file = new(item);
                        string name = file.Name.Replace(file.Extension, string.Empty);

                        BitmapImage bitmapImage = new();
                        var shellFile = ShellFile.FromFilePath(item);

                        try
                        {
                            string path = shellFile.Properties.System.Link.TargetParsingPath.Value switch
                            {
                                "File Explorer" => @"C:\Windows\explorer.exe",
                                "Run" => "run icon location",
                                _ => shellFile.Properties.System.Link.TargetParsingPath.Value
                            };

                            Bitmap bitmap;
                            var icon = Extract(path, 0, true);
                            if (icon is not null)
                                bitmap = icon.ToBitmap();
                            else
                                bitmap = Icon.ExtractAssociatedIcon(item).ToBitmap();
                            using MemoryStream stream = new();
                            bitmap.Save(stream, ImageFormat.Png);
                            stream.Position = 0;
                            bitmapImage.SetSource(stream.AsRandomAccessStream());
                        }
                        catch (Exception)
                        {
                            using MemoryStream stream = new();
                            shellFile.Thumbnail.ExtraLargeBitmap.Save(stream, ImageFormat.Png);
                            stream.Position = 0;
                            bitmapImage.SetSource(stream.AsRandomAccessStream());
                        }

                        var MenuItem = new StartMenuItem()
                        {
                            ItemName = name,
                            ItemStartURI = item,
                            ItemKind = ApplicationKind.Normal,
                            Icon = bitmapImage
                        };

                        allApps.Add(MenuItem);
                    }
                    else
                    {
                        DirectoryInfo directory = new(item);
                        string name = directory.Name;
                        BitmapImage bitmapImage = new() { UriSource = new Uri("ms-appx:///Assets/FolderIcon.png") };

                        var MenuItem = new StartMenuItem()
                        {
                            ItemName = name,
                            ItemStartURI = item,
                            ItemKind = ApplicationKind.Normal,
                            Icon = bitmapImage
                        };

                        allApps.Add(MenuItem);
                    }
                }
            }
        }

        private void StartMenu_Click(object sender, RoutedEventArgs e)
        {
            startMenu.Translation = startMenu.Translation == new Vector3(0, 900, 40) ? new Vector3(0, 0, 40) : new Vector3(0, 900, 40);
        }

        private void TaskView_Click(object sender, RoutedEventArgs e) => taskViewBackground.Visibility = taskViewBackground.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;

        private void Apps_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (apps.SelectedItem is not null)
            {
                var selectedItemInfo = apps.SelectedItem as StartMenuItem;

                if (selectedItemInfo.ItemKind == ApplicationKind.Normal)
                    Process.Start(new ProcessStartInfo(selectedItemInfo.ItemStartURI) { UseShellExecute = true });
                else
                {
                    PackageManager packageManager = new();
                    Package package = packageManager.FindPackageForUser(string.Empty, selectedItemInfo.ItemStartURI);

                    IReadOnlyList<AppListEntry> appListEntries = package.GetAppListEntries();
                    appListEntries[0].LaunchAsync();
                }
            }
        }

        ObservableCollection<StartMenuItem> allApps = [];
        ObservableCollection<StartMenuItem> appsList = [];

        private void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            var filteredUnordered = allApps.Where(entry => Filter(entry, sender.Text));

            var filtered = from item in filteredUnordered orderby item.ItemName[..1] select item;

            for (int i = appsList.Count - 1; i >= 0; i--)
            {
                var item = appsList[i];

                if (!filtered.Contains(item))
                {
                    appsList.Remove(item);
                }
            }

            foreach (StartMenuItem item in filtered)
            {
                if (!appsList.Contains(item))
                {
                    appsList.Add(item);
                }
            }
        }

        private bool Filter(StartMenuItem entry, string filter)
        {
            return entry.ItemName.Contains(filter, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
