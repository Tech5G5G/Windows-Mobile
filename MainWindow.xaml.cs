using CommunityToolkit.WinUI.Animations;
using CommunityToolkit.WinUI.Collections;
using CommunityToolkit.WinUI.Controls;
using Windows.Networking.Connectivity;
using Windows.Devices.Power;
using CoreAudio;
using Windows.UI.Notifications;
using Windows.UI.Notifications.Management;
using Windows.UI.Input.Preview.Injection;
using Windows.Gaming.Input;
using CommunityToolkit.WinUI;

namespace Windows_Mobile
{
    public sealed partial class MainWindow : Window
    {
        static ObservableCollection<StartMenuItem> allApps = [];
        AdvancedCollectionView games = new(allApps, true) { Filter = i => ((StartMenuItem)i).ItemKind == ApplicationKind.SteamGame || ((StartMenuItem)i).ItemKind == ApplicationKind.EpicGamesGame || ((StartMenuItem)i).ItemKind == ApplicationKind.GOGGame || ((StartMenuItem)i).ItemKind == ApplicationKind.XboxGame || ((StartMenuItem)i).ItemKind == ApplicationKind.RobloxPlayer };
        AdvancedCollectionView launchers = new (allApps, true) { Filter = i => ((StartMenuItem)i).ItemKind == ApplicationKind.Launcher || ((StartMenuItem)i).ItemKind == ApplicationKind.LauncherPackaged };
        AdvancedCollectionView apps = new(allApps, true) { Filter = i => ((StartMenuItem)i).ItemKind == ApplicationKind.Normal || ((StartMenuItem)i).ItemKind == ApplicationKind.Packaged };
        AdvancedCollectionView search = new(allApps, true);

        ObservableCollection<MCModInfo> mods = [];
        ObservableCollection<Notifications.Notification> notifications = [];

        public MainWindow()
        {
            this.InitializeComponent();

            Title = "Windows Mobile";
            AppWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen);

            startMenu.Height = (AppWindow.Size.Height * 7) / 8;
            startNV.SelectedItem = games_NavItem;

            search.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == "Count")
                    MenuBar_HeightUpdate();
            };
            clearAllButton.Click += (sender, e) =>
            {
                var count = notifications.Count - 1;
                for (int i = 0; i <= count; i++)
                    Dismiss_Notification(notifications[0].Id);
            };
            notifications.CollectionChanged += (sender, e) =>
            {
                var status = notifications.Count == 0;
                notificationsPlaceholder.Visibility = status ? Visibility.Visible : Visibility.Collapsed;
                clearAllButton.Visibility = status ? Visibility.Collapsed : Visibility.Visible;
            };
            // App.Settings.IsGlobalNotifCenterEnabledChanged += (args) =>
            // {
            //     notifCenter.Visibility = Visibility.Collapsed;
            //     notifCenterButton.IsChecked = false;
            // };

            wallpaperImage.ImageSource = new BitmapImage() { UriSource = new Uri("C:\\Users\\" + Environment.UserName + "\\AppData\\Roaming\\Microsoft\\Windows\\Themes\\TranscodedWallpaper") };
            // if (App.Settings.IsGlobalNotifCenterEnabled) global_RadioButton.IsChecked = true;
            // else builtin_RadioButton.IsChecked = true;

            PopulateStartMenu();
            SetControlCenterIcons();
            UpdateTime(true);
            SetUpNotificationListener();
            SetUpControllers();
            SetUpInputPane();
        }

        private void SetUpInputPane()
        {
            var inputPane = Windows.UI.ViewManagement.InputPaneInterop.GetForWindow(WinRT.Interop.WindowNative.GetWindowHandle(this));
            inputPane.Showing += (sender, e) =>
            {
                if (e.OccludedRect.Height > 0)
                {
                    if ((AppWindow.Size.Height / this.Content.XamlRoot.RasterizationScale) - (10 + e.OccludedRect.Height) - (AppWindow.Size.Height * 7 / 8).Clamp(725, 400) < 54)
                        topAutoSuggestBox.Visibility = menuBar.Visibility = startMenu.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;

                    startMenu.Margin = new Thickness(0, 10, 0, 10 + e.OccludedRect.Height);
                    startMenu.Height = (this.AppWindow.Size.Height / this.Content.XamlRoot.RasterizationScale) - e.OccludedRect.Height - 20;
                    startMenu.MinHeight = 0;

                    notifCenter.Margin = new Thickness(0, 74, 8, 10 + e.OccludedRect.Height);
                }
            };

            inputPane.Hiding += (sender, e) =>
            {
                if ((AppWindow.Size.Height / this.Content.XamlRoot.RasterizationScale) - 80 - (AppWindow.Size.Height * 7 / 8).Clamp(725, 400) > 54)
                    topAutoSuggestBox.Visibility = menuBar.Visibility = Visibility.Visible;

                startMenu.Margin = new Thickness(0, 10, 0, 80);
                startMenu.Height = (AppWindow.Size.Height * 7) / 8;
                startMenu.MinHeight = 400;

                notifCenter.Margin = new Thickness(0, 74, 8, 80);
            };
        }

        private void SetUpControllers()
        {
            var injector = InputInjector.TryCreate();
            Gamepad.GamepadAdded += (sender, gamepad) =>
            {
                bool aEnabled = true;
                bool bEnabled = true;
                bool menuEnabled = true;

                bool leftYenabled = true;
                DateTime? leftYenabledChanged = null;
                bool leftXenabled = true;
                DateTime? leftXenabledChanged = null;

                bool downEnabled = true;
                DateTime? downEnabledChanged = null;
                bool leftEnabled = true;
                DateTime? leftEnabledChanged = null;
                bool rightEnabled = true;
                DateTime? rightEnabledChanged = null;
                bool upEnabled = true;
                DateTime? upEnabledChanged = null;

                bool rightTriggerEnabled = true;
                DateTime? rightTriggerEnabledChanged = null;
                bool leftTriggerEnabled = true;
                DateTime? leftTriggerEnabledChanged = null;

                var timer = new System.Timers.Timer() { Interval = 1 };
                timer.Elapsed += (sender, e) =>
                {
                    var reading = gamepad.GetCurrentReading();
                    List<InjectedInputKeyboardInfo> inputList = [];

                    var aCheck = GamepadReadingChecker.CheckButton(GamepadButtons.A, VirtualKey.GamepadA, aEnabled, reading);
                    if (aCheck.InputInfo is not null)
                        inputList.Add(aCheck.InputInfo);
                    aEnabled = aCheck.ButtonEnabled;
                    var bCheck = GamepadReadingChecker.CheckButton(GamepadButtons.B, VirtualKey.GamepadB, bEnabled, reading);
                    if (bCheck.InputInfo is not null)
                        inputList.Add(bCheck.InputInfo);
                    bEnabled = bCheck.ButtonEnabled;
                    var menuCheck = GamepadReadingChecker.CheckButton(GamepadButtons.Menu, VirtualKey.Application, menuEnabled, reading);
                    if (menuCheck.InputInfo is not null)
                        inputList.Add(menuCheck.InputInfo);
                    menuEnabled = menuCheck.ButtonEnabled;

                    var downCheck = GamepadReadingChecker.CheckPadButton(GamepadButtons.DPadDown, VirtualKey.GamepadDPadDown, downEnabledChanged, downEnabled, reading, timer.Interval);
                    if (downCheck.InputInfo is not null)
                        inputList.Add(downCheck.InputInfo);
                    downEnabled = downCheck.ButtonEnabled;
                    downEnabledChanged = downCheck.ButtonEnabledChanged;
                    timer.Interval = downCheck.TimerInterval;
                    var leftCheck = GamepadReadingChecker.CheckPadButton(GamepadButtons.DPadLeft, VirtualKey.GamepadDPadLeft, leftEnabledChanged, leftEnabled, reading, timer.Interval);
                    if (leftCheck.InputInfo is not null)
                        inputList.Add(leftCheck.InputInfo);
                    leftEnabled = leftCheck.ButtonEnabled;
                    leftEnabledChanged = leftCheck.ButtonEnabledChanged;
                    timer.Interval = leftCheck.TimerInterval;
                    var rightCheck = GamepadReadingChecker.CheckPadButton(GamepadButtons.DPadRight, VirtualKey.GamepadDPadRight, rightEnabledChanged, rightEnabled, reading, timer.Interval);
                    if (rightCheck.InputInfo is not null)
                        inputList.Add(rightCheck.InputInfo);
                    rightEnabled = rightCheck.ButtonEnabled;
                    rightEnabledChanged = rightCheck.ButtonEnabledChanged;
                    timer.Interval = rightCheck.TimerInterval;
                    var upCheck = GamepadReadingChecker.CheckPadButton(GamepadButtons.DPadUp, VirtualKey.GamepadDPadUp, upEnabledChanged, upEnabled, reading, timer.Interval);
                    if (upCheck.InputInfo is not null)
                        inputList.Add(upCheck.InputInfo);
                    upEnabled = upCheck.ButtonEnabled;
                    upEnabledChanged = upCheck.ButtonEnabledChanged;
                    timer.Interval = upCheck.TimerInterval;

                    var leftYCheck = GamepadReadingChecker.CheckLeftStick(false, leftYenabledChanged, leftYenabled, reading, timer.Interval);
                    if (leftYCheck.InputInfo is not null)
                        inputList.Add(leftYCheck.InputInfo);
                    leftYenabled = leftYCheck.ButtonEnabled;
                    leftYenabledChanged = leftYCheck.ButtonEnabledChanged;
                    timer.Interval = leftYCheck.TimerInterval;

                    var leftXCheck = GamepadReadingChecker.CheckLeftStick(true, leftXenabledChanged, leftXenabled, reading, timer.Interval);
                    if (leftXCheck.InputInfo is not null)
                        inputList.Add(leftXCheck.InputInfo);
                    leftXenabled = leftXCheck.ButtonEnabled;
                    leftXenabledChanged = leftXCheck.ButtonEnabledChanged;
                    timer.Interval = leftXCheck.TimerInterval;

                    var lTriggerCheck = GamepadReadingChecker.CheckTrigger(true, VirtualKey.GamepadLeftTrigger, leftTriggerEnabledChanged, leftTriggerEnabled, reading, timer.Interval);
                    if (lTriggerCheck.InputInfo is not null)
                        inputList.Add(lTriggerCheck.InputInfo);
                    leftTriggerEnabled = lTriggerCheck.ButtonEnabled;
                    leftTriggerEnabledChanged = lTriggerCheck.ButtonEnabledChanged;
                    timer.Interval = lTriggerCheck.TimerInterval;
                    var rTriggerCheck = GamepadReadingChecker.CheckTrigger(false, VirtualKey.GamepadRightTrigger, rightTriggerEnabledChanged, rightTriggerEnabled, reading, timer.Interval);
                    if (rTriggerCheck.InputInfo is not null)
                        inputList.Add(rTriggerCheck.InputInfo);
                    rightTriggerEnabled = rTriggerCheck.ButtonEnabled;
                    rightTriggerEnabledChanged = rTriggerCheck.ButtonEnabledChanged;
                    timer.Interval = rTriggerCheck.TimerInterval;

                    if (inputList.Count > 0)
                        injector?.InjectKeyboardInput(inputList);
                };
                timer.Start();
            };
        }

        private void UpdateTime(bool setupTimer = false)
        {
            if (setupTimer)
            {
                System.Timers.Timer timer = new() { Interval = 1000 };
                timer.Elapsed += (s, e) => this.DispatcherQueue?.TryEnqueue(() => UpdateTime());
                timer.Start();
            }
            try
            {
                var dateTime = DateTime.Now;
                time.SetBinding(TextBlock.TextProperty, new Binding() { Source = string.Format("{0:HH:mm:ss tt}", dateTime) });
                timeToolTip.SetBinding(TextBlock.TextProperty, new Binding() { Source = $"{dateTime.ToLongDateString()}\n\n{string.Format("{0:HH:mm:ss tt}", dateTime)}" });
                date.SetBinding(TextBlock.TextProperty, new Binding() { Source = string.Format("{0:MM/dd/yyyy}", dateTime) });
                longDate.SetBinding(TextBlock.TextProperty, new Binding() { Source = $"{dateTime.DayOfWeek}, {dateTime.Month.ToMonthName()} {dateTime.Day}" });
            }
            catch { }
        }
        private void NotifCenter_Open(object sender, RoutedEventArgs args)
        {
            if (App.Settings.IsGlobalNotifCenterEnabled) 
            {
                (sender as ToggleButton).IsChecked = false;
                ApplicationStarter.FromFileName("ms-actioncenter://");
                ElementSoundPlayer.Play(ElementSoundKind.Invoke);
            }
            else
            {
                notifCenter.Visibility = notifCenter.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
                ElementSoundPlayer.Play(notifCenter.Visibility == Visibility.Visible ? ElementSoundKind.MovePrevious : ElementSoundKind.MoveNext);
            }
        }

        private static UserNotificationListener listener;
        private void SetUpNotificationListener()
        {
            listener = UserNotificationListener.Current;
            try { listener.NotificationChanged += (sender, e) => UpdateNotifications(sender, e.ChangeKind, e.UserNotificationId); }
            catch { }
            UpdateNotifications(listener, UserNotificationChangedKind.Added, 0, true);
        }
        private void Dismiss_Notification(uint notifId)
        {
            try { notifications.Remove(notifications.First(i => i.Id == notifId)); }
            catch { }
            listener.RemoveNotification(notifId);
        }
        private async void UpdateNotifications(UserNotificationListener sender, UserNotificationChangedKind changeKind, uint changedId, bool getAll = false)
        {
            if (getAll)
            {
                var notifications = await sender.GetNotificationsAsync(NotificationKinds.Toast);
                foreach (var notification in notifications)
                    this.DispatcherQueue.TryEnqueue(async () => this.notifications.Insert(0, await Notifications.Notification.FromUserNotification(notification, allApps)));
            }
            else if (changeKind == UserNotificationChangedKind.Added)
            {
                var notifications = await sender.GetNotificationsAsync(NotificationKinds.Toast);
                UserNotification notification = ValueAssigner.TryAssign(() => notifications.First(i => i.Id == changedId));
                this.DispatcherQueue.TryEnqueue(async () => this.notifications.Insert(0, await Notifications.Notification.FromUserNotification(notification, allApps)));
            }
            else if (changeKind == UserNotificationChangedKind.Removed)
            {
                this.DispatcherQueue.TryEnqueue(() =>
                {
                    try
                    {
                        var notif = notifications.First(i => i.Id == changedId);
                        notifications.Remove(notif);
                    }
                    catch { }
                });
            }
        }
        private void CalendarCollapseButton_Click(object sender, RoutedEventArgs e)
        {
            var senderButton = sender as Button;
            AnimationBuilder.Create().Size(axis: Axis.Y, to: calendar.Height == 377 ? 0 : 377, from: calendar.Height == 377 ? 377 : 0, duration: TimeSpan.FromMilliseconds(500), easingType: EasingType.Default, easingMode: Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseOut, layer: FrameworkLayer.Xaml).Start(calendar);
            senderButton.Content = new FontIcon() { Glyph = calendar.Height == 377 ? "\uE70E" : "\uE70D", FontSize = 11 };
        }
        private void SwipeItem_Invoked(SwipeItem sender, SwipeItemInvokedEventArgs args) => Dismiss_Notification((uint)sender.CommandParameter);
        private void Notif_DismissButton_Click(object sender, RoutedEventArgs e) => Dismiss_Notification((uint)(sender as Button).Tag);
        private void NotifSettingsButton_Click(object sender, RoutedEventArgs e) => ApplicationStarter.FromFileName("ms-settings:notifications");
        private void DateTimeButton_Click(object sender, RoutedEventArgs e) => ApplicationStarter.FromFileName("ms-settings:dateandtime");
        private void NotifRadioContext_Click(object sender, RoutedEventArgs e) => App.Settings.IsGlobalNotifCenterEnabled = bool.Parse((string)(sender as Control).Tag);

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
                ToolTipService.SetToolTip(networkIcon, type == NetworkType.None ? "No internet access" : name);
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
            games.SortDescriptions.Add(new SortDescription("ItemName", SortDirection.Ascending));
            launchers.SortDescriptions.Add(new SortDescription("ItemName", SortDirection.Ascending));
            apps.SortDescriptions.Add(new SortDescription("ItemName", SortDirection.Ascending));
            search.SortDescriptions.Add(new SortDescription("ItemName", SortDirection.Ascending));

            await Indexers.IndexSteamGames(allApps);
            await Indexers.IndexEGSGames(allApps);
            //await Indexers.IndexEAGames(allApps);
            await Indexers.IndexGOGGames(allApps);
            await Indexers.IndexPackagedApps(allApps);
            Indexers.IndexMCMods(mods);
            Indexers.IndexStartMenuFolder("C:\\Users\\" + Environment.UserName + "\\AppData\\Roaming\\Microsoft\\Windows\\Start Menu\\Programs", allApps);
            Indexers.IndexStartMenuFolder("C:\\ProgramData\\Microsoft\\Windows\\Start Menu\\Programs", allApps);
        }

        private void Open_ControlCenter(object sender, RoutedEventArgs args) => ApplicationStarter.FromFileName("ms-actioncenter:controlcenter/&showFooter=true");
        private void StartMenu_Click(object sender, RoutedEventArgs e)
        {
            if ((AppWindow.Size.Height / this.Content.XamlRoot.RasterizationScale) - 80 - (AppWindow.Size.Height * 7 / 8).Clamp(725, 400) < 54)
                topAutoSuggestBox.Visibility = menuBar.Visibility = startMenu.Visibility == Visibility.Visible ? Visibility.Visible : Visibility.Collapsed;
            startMenu.Visibility = startMenu.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            ElementSoundPlayer.Play(startMenu.Visibility == Visibility.Visible ? ElementSoundKind.MovePrevious : ElementSoundKind.MoveNext);
        }
        private void GameView_Open(object sender, RoutedEventArgs e)
        {
            ElementSoundPlayer.Play(gameView.Visibility == Visibility.Visible ? ElementSoundKind.Hide : ElementSoundKind.Show);
            gameView.Visibility = gameView.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;

            topAutoSuggestBox.Visibility = menuBar.Visibility = gameView.Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;

            notifCenter.Visibility = Visibility.Collapsed;
            notifCenterButton.IsChecked = false;

            startMenu.Visibility = Visibility.Collapsed;
            startMenuButton.IsChecked = false;
        }
        private void Open_Diagnostics(object sender, RoutedEventArgs e) => ApplicationStarter.FromFileName("ms-settings:troubleshoot");
        private void Open_NetworkInternet(object sender, RoutedEventArgs e) => ApplicationStarter.FromFileName("ms-settings:network-status");
        private void Open_VolumeMixer(object sender, RoutedEventArgs e) => ApplicationStarter.FromFileName("ms-settings:apps-volume");
        private void Open_SoundSettings(object sender, RoutedEventArgs e) => ApplicationStarter.FromFileName("ms-settings:sound");
        private void Open_PowerSleep(object sender, RoutedEventArgs e) => ApplicationStarter.FromFileName("ms-settings:powersleep");

        private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if ((NavigationViewItem)args.SelectedItem != mc_NavItem)
            {
                appsView.ItemTemplate = (this.Content as Grid).Resources["StartMenuItemTemplate"] as DataTemplate;
                appsView.SetBinding(ItemsControl.ItemsSourceProperty, new Binding() { Source = (NavigationViewItem)args.SelectedItem == games_NavItem ? games : (NavigationViewItem)args.SelectedItem == launchers_NavItem ? launchers : apps });
                autoSuggestBox.PlaceholderText = (NavigationViewItem)args.SelectedItem == games_NavItem ? "Search games" : (NavigationViewItem)args.SelectedItem == launchers_NavItem ? "Search launchers" : "Search apps";
                autoSuggestBox.Text = null;
            }
            else
            {
                appsView.ItemTemplate = (this.Content as Grid).Resources["ModTemplate"] as DataTemplate;
                appsView.SetBinding(ItemsControl.ItemsSourceProperty, new Binding() { Source = mods });
            }
        }
        private void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            ApplicationKind appKind = (NavigationViewItem)startNV.SelectedItem == games_NavItem ? ApplicationKind.SteamGame | ApplicationKind.EpicGamesGame | ApplicationKind.GOGGame | ApplicationKind.RobloxPlayer | ApplicationKind.XboxGame : (NavigationViewItem)startNV.SelectedItem == launchers_NavItem ? ApplicationKind.Launcher | ApplicationKind.LauncherPackaged : ApplicationKind.Normal | ApplicationKind.Packaged;
            AdvancedCollectionView list = (NavigationViewItem)startNV.SelectedItem == games_NavItem ? games : (NavigationViewItem)startNV.SelectedItem == launchers_NavItem ? launchers : apps;
            list.Filter = i => appKind.HasFlag(((StartMenuItem)i).ItemKind) && ((StartMenuItem)i).ItemName.Contains(sender.Text, StringComparison.InvariantCultureIgnoreCase);
        }
        private async void Apps_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is not null)
            {
                var selectedItemInfo = e.ClickedItem as StartMenuItem;

                if (selectedItemInfo.ItemKind == ApplicationKind.Normal || selectedItemInfo.ItemKind == ApplicationKind.Launcher || selectedItemInfo.ItemKind == ApplicationKind.Packaged || selectedItemInfo.ItemKind == ApplicationKind.LauncherPackaged)
                    ApplicationStarter.FromStartMenuItem(selectedItemInfo);
                else
                {
                    var content = new Grid() { Margin = new Thickness(-24) };
                    var heros = await App.db.GetHeroesByGameIdAsync(selectedItemInfo.GameInfo.Id);
                    var logos = await App.db.GetLogosForGameAsync(selectedItemInfo.GameInfo);
                    content.Children.Add(new Image() { Source = new BitmapImage() { UriSource = new Uri(heros.Length != 0 ? heros[0].FullImageUrl : "ms-appx:///Assets/Placeholder.png") } });
                    content.Children.Add(new Image() { MaxHeight = 90, Margin = new Thickness(40), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Stretch, Source = new BitmapImage() { UriSource = new Uri(logos.Length != 0 ? logos[0].FullImageUrl : "ms-appx:///Assets/Placeholder.png") } });
                    var dialog = new ContentDialog() { XamlRoot = this.Content.XamlRoot, Content = content, PrimaryButtonText = "Play", CloseButtonText = "Cancel", DefaultButton = ContentDialogButton.Primary };

                    if (selectedItemInfo.ItemKind == ApplicationKind.SteamGame)
                    {
                        dialog.SecondaryButtonText = "View in Steam";
                        var selection = await dialog.ShowAsync();

                        if (selection == ContentDialogResult.Primary)
                            ApplicationStarter.FromStartMenuItem(selectedItemInfo);
                        else if (selection == ContentDialogResult.Secondary)
                            ApplicationStarter.FromFileName($"steam://openurl/https://store.steampowered.com/app/{selectedItemInfo.Id}");
                    }
                    else if (selectedItemInfo.ItemKind == ApplicationKind.EpicGamesGame || selectedItemInfo.ItemKind == ApplicationKind.RobloxPlayer)
                    {
                        var selection = await dialog.ShowAsync();

                        if (selection == ContentDialogResult.Primary)
                            ApplicationStarter.FromStartMenuItem(selectedItemInfo);
                    }
                    else if (selectedItemInfo.ItemKind == ApplicationKind.XboxGame)
                    {
                        dialog.SecondaryButtonText = selectedItemInfo.Id is not null ? "View in the Xbox app" : null;
                        var selection = await dialog.ShowAsync();

                        if (selection == ContentDialogResult.Primary)
                            ApplicationStarter.FromStartMenuItem(selectedItemInfo);
                        else if (selection == ContentDialogResult.Secondary)
                            ApplicationStarter.FromFileName($"msxbox://game/?productId={selectedItemInfo.Id}");
                    }
                    else if (selectedItemInfo.ItemKind == ApplicationKind.GOGGame)
                    {
                        dialog.SecondaryButtonText = "View in GOG Galaxy";
                        var selection = await dialog.ShowAsync();

                        if (selection == ContentDialogResult.Primary)
                            ApplicationStarter.FromStartMenuItem(selectedItemInfo);
                        else if (selection == ContentDialogResult.Secondary)
                            ApplicationStarter.FromFileName($"goggalaxy://openGameView/{selectedItemInfo.Id}");
                    }
                }
            }
        }
        private void StartMenuItem_Holding(object sender, HoldingRoutedEventArgs e)
        {
            if (e.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Touch && e.HoldingState == Microsoft.UI.Input.HoldingState.Started)
                StartMenuItem_ContextRequested(sender as StackPanel, e.GetPosition(sender as UIElement));
        }
        private void StartMenuItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (e.PointerDeviceType != Microsoft.UI.Input.PointerDeviceType.Touch)
                StartMenuItem_ContextRequested(sender as StackPanel, e.GetPosition(sender as UIElement));
        }
        private void StartMenuItem_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Application)
                StartMenuItem_ContextRequested((e.OriginalSource as ListViewItem).ContentTemplateRoot as StackPanel, null);
        }
        private void StartMenuItem_ContextRequested(StackPanel senderPanel, Windows.Foundation.Point? point)
        {
            var appType = (senderPanel.Tag as StartMenuItem).ItemKind;
            var flyout = new MenuFlyout();

            var openButton = new MenuFlyoutItem();
            openButton.Click += (sender, args) => ApplicationStarter.FromStartMenuItem(allApps.First(i => i.Icon == (senderPanel.Tag as StartMenuItem).Icon));

            var adminButton = new MenuFlyoutItem() { Text = "Open as admin", Icon = new FontIcon() { Glyph = "\uE7EF" }, Visibility = Visibility.Collapsed };
            adminButton.Click += (sender, args) => ApplicationStarter.FromStartMenuItem(allApps.First(i => i.Icon == (senderPanel.Tag as StartMenuItem).Icon), true);

            var locationButton = new MenuFlyoutItem() { Text = "Open file location", Icon = new FontIcon() { Glyph = "\uED43" }, Visibility = Visibility.Collapsed };
            locationButton.Click += (sender, args) => ApplicationStarter.FromFileName("explorer.exe", $"/select, {(senderPanel.Tag as StartMenuItem).ItemStartURI}");

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
                    uninstallButton.Click += (sender, args) => ApplicationStarter.FromFileName("ms-settings:appsfeatures");
                    break;
                case ApplicationKind.Packaged:
                case ApplicationKind.LauncherPackaged:
                    openButton.Text = "Open";
                    openButton.Icon = new FontIcon() { Glyph = "\uE737" };
                    uninstallButton.Click += (sender, args) => ApplicationStarter.FromFileName("ms-settings:appsfeatures");
                    break;
                case ApplicationKind.SteamGame:
                    openButton.Text = "Play";
                    openButton.Icon = new FontIcon() { Glyph = "\uE768" };
                    uninstallButton.Click += (sender, args) => ApplicationStarter.FromStartMenuItem((StartMenuItem)launchers.First(i => ((StartMenuItem)i).ItemName.Contains("Steam", StringComparison.InvariantCultureIgnoreCase)));
                    break;
                case ApplicationKind.EpicGamesGame:
                    openButton.Text = "Play";
                    openButton.Icon = new FontIcon() { Glyph = "\uE768" };
                    uninstallButton.Click += (sender, args) => ApplicationStarter.FromStartMenuItem((StartMenuItem)launchers.First(i => ((StartMenuItem)i).ItemName.Contains("Epic", StringComparison.InvariantCultureIgnoreCase)));
                    break;
                case ApplicationKind.GOGGame:
                    openButton.Text = "Play";
                    openButton.Icon = new FontIcon() { Glyph = "\uE768" };
                    uninstallButton.Click += (sender, args) => ApplicationStarter.FromStartMenuItem((StartMenuItem)launchers.First(i => ((StartMenuItem)i).ItemName.Equals("GOG GALAXY", StringComparison.InvariantCultureIgnoreCase)));
                    break;
                case ApplicationKind.RobloxPlayer:
                case ApplicationKind.XboxGame:
                    openButton.Text = "Play";
                    openButton.Icon = new FontIcon() { Glyph = "\uE768" };
                    uninstallButton.Click += (sender, args) => ApplicationStarter.FromFileName("ms-settings:appsfeatures");
                    break;
            }

            flyout.Items.Add(openButton);
            if (adminButton.Visibility == Visibility.Visible)
                flyout.Items.Add(adminButton);

            flyout.Items.Add(new MenuFlyoutSeparator());

            if (locationButton.Visibility == Visibility.Visible)
                flyout.Items.Add(locationButton);

            flyout.Items.Add(uninstallButton);

            flyout.ShowAt(senderPanel, showOptions: new() { Position = point, Placement = FlyoutPlacementMode.BottomEdgeAlignedLeft });
        }

        private bool? menuBarAnimated = null;
        private Windows.Foundation.Size menuBarOriginalSize;
        private void TopAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (menuBarAnimated == true && string.IsNullOrWhiteSpace(sender.Text))
            {
                launcherGrid.Visibility = menuBarTray.Visibility = Visibility.Visible;
                allSearchList.Visibility = Visibility.Collapsed;
                sender.CornerRadius = new CornerRadius(20);

                var animationBuilder = AnimationBuilder.Create();
                animationBuilder.Size(axis: Axis.X, to: menuBarOriginalSize.Width, from: 698, duration: TimeSpan.FromMilliseconds(500), easingType: EasingType.Default, easingMode: Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseOut, layer: FrameworkLayer.Xaml).Start(menuBar);
                animationBuilder.Size(axis: Axis.Y, to: menuBarOriginalSize.Height, from: menuBar.ActualHeight, duration: TimeSpan.FromMilliseconds(500), easingType: EasingType.Default, easingMode: Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseOut, layer: FrameworkLayer.Xaml).Start(menuBar);

                var senderAnimationBuilder = AnimationBuilder.Create();
                senderAnimationBuilder.Size(axis: Axis.X, to: 400, from: 674, duration: TimeSpan.FromMilliseconds(500), easingType: EasingType.Default, easingMode: Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseOut, layer: FrameworkLayer.Xaml).Start(sender);
                senderAnimationBuilder.Translation(to: Vector2.Zero, from: new Vector2(0, 5), duration: TimeSpan.FromMilliseconds(300), easingMode: Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseOut, layer: FrameworkLayer.Xaml).Start(sender);
                
                menuBarAnimated = false;
            }
            else if (menuBarAnimated == false && !string.IsNullOrWhiteSpace(sender.Text))
            {
                launcherGrid.Visibility = menuBarTray.Visibility = Visibility.Collapsed;
                allSearchList.Visibility = Visibility.Visible;
                sender.CornerRadius = new CornerRadius(4);

                var animationBuilder = AnimationBuilder.Create();
                animationBuilder.Size(axis: Axis.X, to: 698, from: menuBarOriginalSize.Width, duration: TimeSpan.FromMilliseconds(500), easingType: EasingType.Default, easingMode: Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseOut, layer: FrameworkLayer.Xaml).Start(menuBar);
                animationBuilder.Size(axis: Axis.Y, to: ((search.Count * 36) + ((search.Count - 1) * 4) + 70).Clamp(((int)startMenu.Height).Clamp(600, 400)), from: menuBarOriginalSize.Height, duration: TimeSpan.FromMilliseconds(500), easingType: EasingType.Default, easingMode: Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseOut, layer: FrameworkLayer.Xaml).Start(menuBar);

                var senderAnimationBuilder = AnimationBuilder.Create();
                senderAnimationBuilder.Size(axis: Axis.X, to: 674, from: 400, duration: TimeSpan.FromMilliseconds(500), easingType: EasingType.Default, easingMode: Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseOut, layer: FrameworkLayer.Xaml).Start(sender);
                senderAnimationBuilder.Translation(to: new Vector2(0, 5), from: Vector2.Zero, duration: TimeSpan.FromMilliseconds(300), easingMode: Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseOut, layer: FrameworkLayer.Xaml).Start(sender);
                
                menuBarAnimated = true;
            }
            else if (menuBarAnimated is null && !string.IsNullOrWhiteSpace(sender.Text))
            {
                menuBarOriginalSize = new(menuBar.ActualWidth, menuBar.ActualHeight);
                
                launcherGrid.Visibility = menuBarTray.Visibility = Visibility.Collapsed;
                allSearchList.Visibility = Visibility.Visible;
                sender.CornerRadius = new CornerRadius(4);

                var animationBuilder = AnimationBuilder.Create();
                animationBuilder.Size(axis: Axis.X, to: 698, from: menuBarOriginalSize.Width, duration: TimeSpan.FromMilliseconds(500), easingType: EasingType.Default, easingMode: Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseOut, layer: FrameworkLayer.Xaml).Start(menuBar);
                animationBuilder.Size(axis: Axis.Y, to: ((search.Count * 36) + ((search.Count - 1) * 4) + 70).Clamp(((int)startMenu.Height).Clamp(600, 400)), from: menuBarOriginalSize.Height, duration: TimeSpan.FromMilliseconds(500), easingType: EasingType.Default, easingMode: Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseOut, layer: FrameworkLayer.Xaml).Start(menuBar);

                var senderAnimationBuilder = AnimationBuilder.Create();
                senderAnimationBuilder.Size(axis: Axis.X, to: 674, from: 400, duration: TimeSpan.FromMilliseconds(500), easingType: EasingType.Default, easingMode: Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseOut, layer: FrameworkLayer.Xaml).Start(sender);
                senderAnimationBuilder.Translation(to: new Vector2(0, 5), from: Vector2.Zero, duration: TimeSpan.FromMilliseconds(300), easingMode: Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseOut, layer: FrameworkLayer.Xaml).Start(sender);

                menuBarAnimated = true;
            }

            search.Filter = i => ((StartMenuItem)i).ItemName.Contains(sender.Text, StringComparison.InvariantCultureIgnoreCase);
        }
        private void MenuBar_HeightUpdate()
        {
            if (menuBarAnimated == true)
            {
                var newHeight = ((search.Count * 36) + ((search.Count - 1) * 4) + 70).Clamp(((int)startMenu.Height).Clamp(600, 400));
                var oldHeight = menuBar.ActualHeight;

                if (newHeight != oldHeight && newHeight != 64)
                    AnimationBuilder.Create().Size(axis: Axis.Y, to: newHeight, from: oldHeight, duration: TimeSpan.FromMilliseconds(500), easingType: EasingType.Default, easingMode: Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseOut, layer: FrameworkLayer.Xaml).Start(menuBar);
            }
        }

        private void SoundSwitch_Toggled(object sender, RoutedEventArgs e) => ElementSoundPlayer.State = (sender as ToggleSwitch).IsOn ? ElementSoundPlayerState.On : ElementSoundPlayerState.Off;

        private async void PowerMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog() { XamlRoot = this.Content.XamlRoot, PrimaryButtonText = "Yes", CloseButtonText = "No", DefaultButton = ContentDialogButton.Primary };
            string process = null;
            string args = null;

            switch ((sender as MenuFlyoutItem).Text)
            {
                case "Lock":
                    App.LockWorkStation();
                    return;
                case "Shut down":
                    process = "shutdown";
                    args = "/s /t 0";
                    
                    dialog.Title = "Shut down";
                    dialog.Content = "Are you sure you want to shut down?";
                    break;
                case "Restart":
                    process = "shutdown";
                    args = "/r /t 0";

                    dialog.Title = "Restart";
                    dialog.Content = "Are you sure you want to restart?";
                    break;
            }

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                ApplicationStarter.FromFileName(process, args, true);
        }
        private async void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var combo = new ComboBox();
            combo.Items.Add("Desktop");
            combo.Items.Add("Custom");
            combo.SelectedIndex = 0;

            var content = new StackPanel() { Spacing = 4 };
            content.Children.Add(new SettingsCard() { MinWidth = 400, HorizontalAlignment = HorizontalAlignment.Stretch, HeaderIcon = new FontIcon() { Glyph = "\uE7E8" }, Header = "Start at startup", Description = "Automatically use Windows Mobile at startup", Content = new ToggleSwitch() });
            content.Children.Add(new SettingsCard() { MinWidth = 400, HorizontalAlignment = HorizontalAlignment.Stretch, HeaderIcon = new FontIcon() { Glyph = "\uE7F9" /*Change this icon to Set Background icon from File Explorer*/ }, Header = "Wallpaper", Description = "Use the desktop wallpaper or a custom wallpaper", Content = combo });

            var dialog = new ContentDialog() { Content = content, Title = "Settings", CloseButtonText = "Done", XamlRoot = this.Content.XamlRoot };
            await dialog.ShowAsync();
        }
    }
}
