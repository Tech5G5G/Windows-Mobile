using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Controls.Primitives;
using System.Diagnostics;
using CommunityToolkit.WinUI.UI.Animations;
using Windows.Networking.Connectivity;
using Windows.Devices.Power;
using CoreAudio;
using Windows_Mobile.Indexing;
using Windows_Mobile.Networking;
using Windows_Mobile.MC;

namespace Windows_Mobile
{
    public sealed partial class MainWindow : Window
    {
        ObservableCollection<StartMenuItem> allApps = [];
        ObservableCollection<StartMenuItem> gamesList = [];
        ObservableCollection<StartMenuItem> launcherList = [];
        ObservableCollection<StartMenuItem> appsList = [];
        ObservableCollection<StartMenuItem> allSearch = [];
        ObservableCollection<MCModInfo> mods = [];

        public MainWindow()
        {
            this.InitializeComponent();

            Title = "Windows Mobile";
            AppWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen);

            startMenu.Height = (AppWindow.Size.Height * 7) / 8;
            startNV.SelectedItem = games_NavItem;

            allSearch.CollectionChanged += (sender, e) => MenuBar_HeightUpdate();
            wallpaperImage.ImageSource = new BitmapImage() { UriSource = new Uri("C:\\Users\\" + Environment.UserName + "\\AppData\\Roaming\\Microsoft\\Windows\\Themes\\TranscodedWallpaper") };
            
            PopulateStartMenu();
            SetControlCenterIcons();
        }

        private void SetControlCenterIcons()
        {
            var profile = NetworkInformation.GetInternetConnectionProfile();
            Set_NetworkInfo(profile.GetNetworkType(), profile?.ProfileName);
            NetworkInformation.NetworkStatusChanged += (sender) =>
            {
                var profile = NetworkInformation.GetInternetConnectionProfile();
                Set_NetworkInfo(profile.GetNetworkType(), profile?.ProfileName);
            };

            device = new MMDeviceEnumerator(Guid.NewGuid()).GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            device.AudioEndpointVolume.OnVolumeNotification += (data) => Set_VolumeLevel((int)Math.Ceiling(data.MasterVolume * 100), data.Muted);
            Set_VolumeLevel((int)Math.Ceiling(device.AudioEndpointVolume.MasterVolumeLevelScalar * 100), device.AudioEndpointVolume.Mute);

            var aggregate = Battery.AggregateBattery;
            var report = aggregate.GetReport();
            var powerStatus = System.Windows.Forms.SystemInformation.PowerStatus.PowerLineStatus;
            Set_BatteryLevel(report.RemainingCapacityInMilliwattHours, report.FullChargeCapacityInMilliwattHours, powerStatus == System.Windows.Forms.PowerLineStatus.Online);
            aggregate.ReportUpdated += (sender, e) =>
            {
                var report = sender.GetReport();
                var powerStatus = System.Windows.Forms.SystemInformation.PowerStatus.PowerLineStatus;
                Set_BatteryLevel(report.RemainingCapacityInMilliwattHours, report.FullChargeCapacityInMilliwattHours, powerStatus == System.Windows.Forms.PowerLineStatus.Online);
            };
        }
        private static MMDevice device;
        private void Set_VolumeLevel(int volumeLevel, bool muted)
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                if (muted)
                { 
                    ToolTipService.SetToolTip(volumeLevelContainer, "Muted");
                    volumeBackground.Visibility = Visibility.Collapsed;
                    this.volumeLevel.Glyph = "\uE74F";
                }
                else
                {
                    ToolTipService.SetToolTip(volumeLevelContainer, $"{volumeLevel}% volume");
                    volumeBackground.Visibility = Visibility.Visible;
                    this.volumeLevel.Glyph = volumeLevel switch
                    {
                        > 66 => "\uE995",
                        > 33 => "\uE994",
                        > 0 => "\uE993",
                        _ => "\uE992"
                    };
                }
            });
        }
        private void Set_BatteryLevel(int? remainingCapacity, int? totalCapacity, bool charging)
        {
            var percentCharged = (int)(((double)remainingCapacity / (double)totalCapacity) * 100);
            this.DispatcherQueue.TryEnqueue(() =>
            {
                ToolTipService.SetToolTip(batteryLevel, percentCharged == 100 ? "Fully charged 100%" : $"{percentCharged}% remaining");

                if (charging)
                {
                    batteryLevel.Glyph = percentCharged switch
                    {
                        >= 100 => "\uEBB5",
                        >= 90 => "\uEBB4",
                        >= 80 => "\uEBB3",
                        >= 70 => "\uEBB2",
                        >= 60 => "\uEBB1",
                        >= 50 => "\uEBB0",
                        >= 40 => "\uEBAF",
                        >= 30 => "\uEBAE",
                        >= 20 => "\uEBAD",
                        >= 10 => "\uEBAC",
                        0 => "\uEBAB",
                        _ => "\uEC02"
                    };
                }
                else
                {
                    batteryLevel.Glyph = percentCharged switch
                    {
                        >= 100 => "\uEBAA",
                        >= 90 => "\uEBA9",
                        >= 80 => "\uEBA8",
                        >= 70 => "\uEBA7",
                        >= 60 => "\uEBA6",
                        >= 50 => "\uEBA5",
                        >= 40 => "\uEBA4",
                        >= 30 => "\uEBA3",
                        >= 20 => "\uEBA2",
                        >= 10 => "\uEBA1",
                        0 => "\uEBA0",
                        _ => "\uEC02"
                    };
                }
            });
        }
        private void Set_NetworkInfo(NetworkType type, string name)
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                ToolTipService.SetToolTip(networkIcon, name);
                networkIcon.Glyph = type switch
                {
                    NetworkType.WiFi => "\uE701",
                    NetworkType.Ethernet => "\uE839",
                    NetworkType.Cellular => "\uEC3B",
                    NetworkType.None => "\uF384",
                    _ => "\uF384"
                };
            });
        }

        private async void PopulateStartMenu()
        {
            await Indexers.IndexSteamGames(allApps);
            await Indexers.IndexEGSGames(allApps);
            //await Indexers.IndexEAGames(allApps);
            await Indexers.IndexGOGGames(allApps);
            await Indexers.IndexPackagedApps(allApps);
            Indexers.IndexMCMods(mods);
            Indexers.IndexStartMenuFolder("C:\\Users\\" + Environment.UserName + "\\AppData\\Roaming\\Microsoft\\Windows\\Start Menu\\Programs", allApps);
            Indexers.IndexStartMenuFolder("C:\\ProgramData\\Microsoft\\Windows\\Start Menu\\Programs", allApps);
            AddAppsToStartMenu();
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

        private void Open_Time(object sender, RoutedEventArgs args) => Process.Start(new ProcessStartInfo("ms-actioncenter://") { UseShellExecute = true });
        private void Open_ControlCenter(object sender, RoutedEventArgs args) => Process.Start(new ProcessStartInfo("ms-actioncenter:controlcenter/&showFooter=true") { UseShellExecute = true });
        private void StartMenu_Click(object sender, RoutedEventArgs e) => startMenu.Translation = startMenu.Translation == new Vector3(0, 900, 40) ? new Vector3(0, 0, 40) : new Vector3(0, 900, 40);
        private void GameView_Click(object sender, RoutedEventArgs e) => taskViewBackground.Visibility = taskViewBackground.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;

        private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if ((NavigationViewItem)args.SelectedItem != mc_NavItem)
            {
                apps.ItemTemplate = (this.Content as Grid).Resources["StartMenuItemTemplate"] as DataTemplate;
                apps.SetBinding(ItemsControl.ItemsSourceProperty, new Binding() { Source = (NavigationViewItem)args.SelectedItem == games_NavItem ? gamesList : (NavigationViewItem)args.SelectedItem == launchers_NavItem ? launcherList : appsList });
                autoSuggestBox.PlaceholderText = (NavigationViewItem)args.SelectedItem == games_NavItem ? "Search games" : (NavigationViewItem)args.SelectedItem == launchers_NavItem ? "Search launchers" : "Search apps";
                autoSuggestBox.Text = null;
            }
            else
            {
                apps.ItemTemplate = (this.Content as Grid).Resources["ModTemplate"] as DataTemplate;
                apps.SetBinding(ItemsControl.ItemsSourceProperty, new Binding() { Source = mods });
            }
        }
        private void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            List<ApplicationKind> applicationTypes = (NavigationViewItem)startNV.SelectedItem == games_NavItem ? [ApplicationKind.SteamGame, ApplicationKind.EpicGamesGame, ApplicationKind.XboxGame] : (NavigationViewItem)startNV.SelectedItem == launchers_NavItem ? [ApplicationKind.Launcher, ApplicationKind.LauncherPackaged] : [ApplicationKind.Normal, ApplicationKind.Packaged];
            ObservableCollection<StartMenuItem> list = (NavigationViewItem)startNV.SelectedItem == games_NavItem ? gamesList : (NavigationViewItem)startNV.SelectedItem == launchers_NavItem ? launcherList : appsList;
            var filteredUnordered = allApps.Where(entry => entry.ItemName.Contains(sender.Text, StringComparison.InvariantCultureIgnoreCase));
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

        private bool? menuBarAnimated = null;
        private Windows.Foundation.Size menuBarOriginalSize;
        private void TopAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (menuBarAnimated == true && string.IsNullOrWhiteSpace(sender.Text))
            {
                launcherGrid.Visibility = controlCenter.Visibility = time.Visibility = Visibility.Visible;
                allSearchList.Visibility = Visibility.Collapsed;
                sender.CornerRadius = new CornerRadius(20);
                sender.Translation = Vector3.Zero;
                var animationBuilder = AnimationBuilder.Create();
                animationBuilder.Size(axis: Axis.X, to: menuBarOriginalSize.Width, from: 698, duration: TimeSpan.FromMilliseconds(500), easingType: EasingType.Default, easingMode: Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseOut, layer: FrameworkLayer.Xaml).Start(menuBar);
                animationBuilder.Size(axis: Axis.Y, to: menuBarOriginalSize.Height, from: menuBar.ActualHeight, duration: TimeSpan.FromMilliseconds(500), easingType: EasingType.Default, easingMode: Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseOut, layer: FrameworkLayer.Xaml).Start(menuBar);
                AnimationBuilder.Create().Size(axis: Axis.X, to: 400, from: 674, duration: TimeSpan.FromMilliseconds(500), easingType: EasingType.Default, easingMode: Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseOut, layer: FrameworkLayer.Xaml).Start(sender);
                menuBarAnimated = false;
            }
            else if (menuBarAnimated == false && !string.IsNullOrWhiteSpace(sender.Text))
            {
                launcherGrid.Visibility = controlCenter.Visibility = time.Visibility = Visibility.Collapsed;
                allSearchList.Visibility = Visibility.Visible;
                sender.CornerRadius = new CornerRadius(4);
                sender.Translation = new Vector3(0, 5, 0);
                var animationBuilder = AnimationBuilder.Create();
                animationBuilder.Size(axis: Axis.X, to: 698, from: menuBarOriginalSize.Width, duration: TimeSpan.FromMilliseconds(500), easingType: EasingType.Default, easingMode: Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseOut, layer: FrameworkLayer.Xaml).Start(menuBar);
                animationBuilder.Size(axis: Axis.Y, to: ((allSearch.Count * 36) + ((allSearch.Count - 1) * 4) + 70).Clamp(((int)startMenu.Height).Clamp(600, 400)), from: menuBarOriginalSize.Height, duration: TimeSpan.FromMilliseconds(500), easingType: EasingType.Default, easingMode: Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseOut, layer: FrameworkLayer.Xaml).Start(menuBar);
                AnimationBuilder.Create().Size(axis: Axis.X, to: 674, from: 400, duration: TimeSpan.FromMilliseconds(500), easingType: EasingType.Default, easingMode: Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseOut, layer: FrameworkLayer.Xaml).Start(sender);
                menuBarAnimated = true;
            }
            else if (menuBarAnimated is null)
            {
                menuBarOriginalSize = new(menuBar.ActualWidth, menuBar.ActualHeight);
                
                launcherGrid.Visibility = controlCenter.Visibility = time.Visibility = Visibility.Collapsed;
                allSearchList.Visibility = Visibility.Visible;
                sender.CornerRadius = new CornerRadius(4);
                sender.Translation = new Vector3(0, 5, 0);
                var animationBuilder = AnimationBuilder.Create();
                animationBuilder.Size(axis: Axis.X, to: 698, from: menuBarOriginalSize.Width, duration: TimeSpan.FromMilliseconds(500), easingType: EasingType.Default, easingMode: Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseOut, layer: FrameworkLayer.Xaml).Start(menuBar);
                animationBuilder.Size(axis: Axis.Y, to: ((allSearch.Count * 36) + ((allSearch.Count - 1) * 4) + 70).Clamp(((int)startMenu.Height).Clamp(600, 400)), from: menuBarOriginalSize.Height, duration: TimeSpan.FromMilliseconds(500), easingType: EasingType.Default, easingMode: Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseOut, layer: FrameworkLayer.Xaml).Start(menuBar);
                AnimationBuilder.Create().Size(axis: Axis.X, to: 674, from: 400, duration: TimeSpan.FromMilliseconds(500), easingType: EasingType.Default, easingMode: Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseOut, layer: FrameworkLayer.Xaml).Start(sender);
                menuBarAnimated = true;
            }

            var filteredUnordered = allApps.Where(entry => entry.ItemName.Contains(sender.Text, StringComparison.InvariantCultureIgnoreCase));
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
        private void MenuBar_HeightUpdate()
        {
            if (menuBarAnimated == true)
            {
                var newHeight = ((allSearch.Count * 36) + ((allSearch.Count - 1) * 4) + 70).Clamp(((int)startMenu.Height).Clamp(600, 400));
                var oldHeight = menuBar.ActualHeight;

                if (newHeight != oldHeight && newHeight != 64)
                    AnimationBuilder.Create().Size(axis: Axis.Y, to: newHeight, from: oldHeight, duration: TimeSpan.FromMilliseconds(500), easingType: EasingType.Default, easingMode: Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseOut, layer: FrameworkLayer.Xaml).Start(menuBar);
            }
        }
    }
}
