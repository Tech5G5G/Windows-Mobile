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
using System.IO.Packaging;
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
    }

    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();

            Title = "Windows Mobile";
            this.AppWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen);

            wallpaperImage.ImageSource = new BitmapImage() { UriSource = new Uri("C:\\Users\\" + Environment.UserName + "\\AppData\\Roaming\\Microsoft\\Windows\\Themes\\TranscodedWallpaper") };

            IndexStartMenuItems();
        }

        private void IndexStartMenuItems()
        {
            string userItemsDirectory = "C:\\Users\\" + Environment.UserName + "\\AppData\\Roaming\\Microsoft\\Windows\\Start Menu\\Programs";

            IEnumerable<string> userStartMenuItems = Directory.EnumerateFiles(userItemsDirectory);
            string[] userStartMenuFolders = Directory.GetDirectories(userItemsDirectory);

            foreach (string folder in userStartMenuFolders)
            {
                //Replace GetFiles with something that gets all files (including in subdirectories)
                string[] folderItems = Directory.GetFiles(folder);

                if (folderItems.Length == 1)
                {
                    foreach (string item in folderItems)
                    {
                        userStartMenuItems = userStartMenuItems.Append(item);
                    }
                }
            }

            foreach (string item in userStartMenuItems)
            {
                if (!item.EndsWith(".ini"))
                {
                    FileInfo file = new FileInfo(item);

                    string name = file.Name.Replace(file.Extension, string.Empty);

                    allApps.Add(new TextBlock() { Text = name, Name = item, Tag = "Normal" });
                }
            }


            string systemItemsDirectory = "C:\\ProgramData\\Microsoft\\Windows\\Start Menu\\Programs";

            IEnumerable<string> systemStartMenuItems = Directory.EnumerateFiles(systemItemsDirectory);
            string[] systemStartMenuFolders = Directory.GetDirectories(systemItemsDirectory);

            foreach (string folder in systemStartMenuFolders)
            {
                //Replace GetFiles with something that gets all files (including in subdirectories)
                string[] folderItems = Directory.GetFiles(folder);

                if (folderItems.Length == 1)
                {
                    foreach (string item in folderItems)
                    {
                        systemStartMenuItems = systemStartMenuItems.Append(item);
                    }
                }
            }

            foreach (string item in systemStartMenuItems)
            {
                if (!item.EndsWith(".ini"))
                {
                    FileInfo file = new FileInfo(item);

                    string name = file.Name.Replace(file.Extension, string.Empty);

                    allApps.Add(new TextBlock() { Text = name, Name = item, Tag = "Normal" });
                }
            }


            PackageManager packageManager = new PackageManager();
            IEnumerable<Windows.ApplicationModel.Package> packages = packageManager.FindPackagesForUser(string.Empty);

            foreach (Windows.ApplicationModel.Package package in packages)
            {
                if (!package.IsResourcePackage && !package.IsFramework && !package.IsStub && !package.IsBundle)
                {
                    IReadOnlyList<AppListEntry> appListEntries = package.GetAppListEntries();

                    foreach (AppListEntry appListEntry in appListEntries)
                    {
                        allApps.Add(new TextBlock() { Text = appListEntry.DisplayInfo.DisplayName, Name = package.Id.Name, Tag = "Packaged" });
                    }
                }
            }


            var ordered = from item in allApps
                           orderby item.Text.Substring(0, 1)
                           select item;

            foreach (TextBlock item in ordered)
            {
                apps.Items.Add(item);
            }
        }

        private void StartMenu_Click(object sender, RoutedEventArgs e)
        {
            startMenu.Translation = startMenu.Translation == new Vector3(0, 900, 40) ? new Vector3(0, 0, 40) : new Vector3(0, 900, 40);
        }

        private void TaskView_Click(object sender, RoutedEventArgs e)
        {
            if (taskViewBackground.Opacity == 1)
            {
                taskViewBackground.Visibility = Visibility.Collapsed;
                taskViewBackground.Opacity = 0;
            }
            else
            {
                taskViewBackground.Visibility = Visibility.Visible;
                taskViewBackground.Opacity = 1;
            }
        }

        private void apps_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (apps.SelectedItem is not null)
            {
                TextBlock selectedItem = apps.SelectedItem as TextBlock;

                if (selectedItem.Tag as string == "Normal")
                {
                    Process.Start(new ProcessStartInfo(selectedItem.Name) { UseShellExecute = true });
                }
                else
                {
                    PackageManager packageManager = new PackageManager();
                    IEnumerable<Windows.ApplicationModel.Package> packages = packageManager.FindPackagesForUser(string.Empty);

                    foreach (Windows.ApplicationModel.Package package in packages)
                    {
                        if (package.Id.Name == selectedItem.Name)
                        {
                            IReadOnlyList<AppListEntry> appListEntries = package.GetAppListEntries();

                            foreach (AppListEntry appListEntry in appListEntries)
                            {
                                appListEntry.LaunchAsync();
                            }
                        }
                    }
                }
            }
        }

        Collection<TextBlock> allApps = new();

        private void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            var filteredUnordered = allApps.Where(textBlock => Filter(textBlock, sender.Text));

            var filtered = from item in filteredUnordered
                        orderby item.Text.Substring(0, 1)
                        select item;

            for (int i = apps.Items.Count - 1; i >= 0; i--)
            {
                var item = apps.Items[i];

                if (!filtered.Contains(item))
                {
                    apps.Items.Remove(item);
                }
            }

            foreach (TextBlock item in filtered)
            {
                if (!apps.Items.Contains(item))
                {
                    apps.Items.Add(item);
                }
            }
        }

        private bool Filter(TextBlock textBlock, string filter)
        {
            return textBlock.Text.Contains(filter, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
