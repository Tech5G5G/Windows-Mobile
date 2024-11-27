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
using CommunityToolkit.WinUI.Animations;
using Windows.Networking.Connectivity;
using Windows.Devices.Power;
using CoreAudio;
using Windows_Mobile.Indexing;
using Windows_Mobile.Networking;
using Windows_Mobile.MC;
using Windows.UI.Notifications;
using Windows.UI.Notifications.Management;
using Windows.UI.Input.Preview.Injection;
using Windows.System;

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
        ObservableCollection<Indexing.Notification> notifications = [];

        public MainWindow()
        {
            this.InitializeComponent();

            Title = "Windows Mobile";
            AppWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen);

            startMenu.Height = (AppWindow.Size.Height * 7) / 8;
            startNV.SelectedItem = games_NavItem;

            allSearch.CollectionChanged += (sender, e) => MenuBar_HeightUpdate();
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
            App.Settings.IsGlobalNotifCenterEnabledChanged += (args) =>
            {
                notifCenter.Visibility = Visibility.Collapsed;
                notifCenterButton.IsChecked = false;
            };

            wallpaperImage.ImageSource = new BitmapImage() { UriSource = new Uri("C:\\Users\\" + Environment.UserName + "\\AppData\\Roaming\\Microsoft\\Windows\\Themes\\TranscodedWallpaper") };
            if (App.Settings.IsGlobalNotifCenterEnabled) global_RadioButton.IsChecked = true;
            else builtin_RadioButton.IsChecked = true;

            PopulateStartMenu();
            SetControlCenterIcons();
            UpdateTime(true);
            SetUpNotificationListener();
            SetUpControllers();
        }

        private void SetUpControllers()
        {
            var injector = InputInjector.TryCreate();
            Windows.Gaming.Input.Gamepad.GamepadAdded += (sender, gamepad) =>
            {
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

                bool aEnabled = true;
                bool bEnabled = true;
                bool menuEnabled = true;

                var timer = new System.Timers.Timer() { Interval = 1 };
                timer.Elapsed += (sender, e) =>
                {
                    var inputList = new List<InjectedInputKeyboardInfo>();
                    var reading = gamepad.GetCurrentReading();

                    if (reading.LeftThumbstickY < 0.5 && reading.LeftThumbstickY > -0.5 && !leftYenabled)
                    {
                        leftYenabledChanged = null;
                        leftYenabled = true;

                        timer.Interval = 1;
                    }
                    else if (leftYenabledChanged is not null && DateTime.Now - leftYenabledChanged.Value > TimeSpan.FromMilliseconds(500))
                    {
                        if (reading.LeftThumbstickY > 0.5)
                            inputList.Add(new InjectedInputKeyboardInfo() { VirtualKey = (ushort)VirtualKey.GamepadLeftThumbstickUp });
                        else if (reading.LeftThumbstickY < -0.5)
                            inputList.Add(new InjectedInputKeyboardInfo() { VirtualKey = (ushort)VirtualKey.GamepadLeftThumbstickDown });

                        timer.Interval = 100;
                    }
                    else if (!(reading.LeftThumbstickY < 0.5) && leftYenabled)
                    {
                        inputList.Add(new InjectedInputKeyboardInfo() { VirtualKey = (ushort)VirtualKey.GamepadLeftThumbstickUp });
                        leftYenabled = false;
                        leftYenabledChanged = DateTime.Now;
                    }
                    else if (!(reading.LeftThumbstickY > -0.5) && leftYenabled)
                    {
                        inputList.Add(new InjectedInputKeyboardInfo() { VirtualKey = (ushort)VirtualKey.GamepadLeftThumbstickDown });
                        leftYenabled = false;
                        leftYenabledChanged = DateTime.Now;
                    }

                    if (reading.LeftThumbstickX < 0.5 && reading.LeftThumbstickX > -0.5 && !leftXenabled)
                    {
                        leftXenabledChanged = null;
                        leftXenabled = true;

                        timer.Interval = 1;
                    }
                    else if (leftXenabledChanged is not null && DateTime.Now - leftXenabledChanged.Value > TimeSpan.FromMilliseconds(500))
                    {
                        if (!(reading.LeftThumbstickX < 0.5))
                            inputList.Add(new InjectedInputKeyboardInfo() { VirtualKey = (ushort)VirtualKey.GamepadLeftThumbstickRight });
                        else if (!(reading.LeftThumbstickX > -0.5))
                            inputList.Add(new InjectedInputKeyboardInfo() { VirtualKey = (ushort)VirtualKey.GamepadLeftThumbstickLeft });

                        timer.Interval = 100;
                    }
                    else if (!(reading.LeftThumbstickX < 0.5) && leftXenabled)
                    {
                        inputList.Add(new InjectedInputKeyboardInfo() { VirtualKey = (ushort)VirtualKey.GamepadLeftThumbstickRight });
                        leftXenabled = false;
                        leftXenabledChanged = DateTime.Now;
                    }
                    else if (!(reading.LeftThumbstickX > -0.5) && leftXenabled)
                    {
                        inputList.Add(new InjectedInputKeyboardInfo() { VirtualKey = (ushort)VirtualKey.GamepadLeftThumbstickLeft });
                        leftXenabled = false;
                        leftXenabledChanged = DateTime.Now;
                    }

                    if (reading.Buttons.HasFlag(Windows.Gaming.Input.GamepadButtons.A) && aEnabled)
                    {
                        inputList.Add(new InjectedInputKeyboardInfo() { VirtualKey = (ushort)VirtualKey.GamepadA });
                        aEnabled = false;
                    }
                    else if (!reading.Buttons.HasFlag(Windows.Gaming.Input.GamepadButtons.A) && !aEnabled)
                    {
                        inputList.Add(new InjectedInputKeyboardInfo() { VirtualKey = (ushort)VirtualKey.GamepadA, KeyOptions = InjectedInputKeyOptions.KeyUp });
                        aEnabled = true;
                    }

                    if (reading.Buttons.HasFlag(Windows.Gaming.Input.GamepadButtons.B) && bEnabled)
                    {
                        inputList.Add(new InjectedInputKeyboardInfo() { VirtualKey = (ushort)VirtualKey.GamepadB });
                        bEnabled = false;
                    }
                    else if (!reading.Buttons.HasFlag(Windows.Gaming.Input.GamepadButtons.B) && !bEnabled)
                    {
                        inputList.Add(new InjectedInputKeyboardInfo() { VirtualKey = (ushort)VirtualKey.GamepadB, KeyOptions = InjectedInputKeyOptions.KeyUp });
                        bEnabled = true;
                    }

                    if (reading.Buttons.HasFlag(Windows.Gaming.Input.GamepadButtons.DPadDown) && downEnabledChanged is not null && DateTime.Now - downEnabledChanged.Value > TimeSpan.FromMilliseconds(500))
                    {
                        inputList.Add(new InjectedInputKeyboardInfo() { VirtualKey = (ushort)VirtualKey.GamepadDPadDown });
                        timer.Interval = 100;
                    }
                    else if (reading.Buttons.HasFlag(Windows.Gaming.Input.GamepadButtons.DPadDown) && downEnabled)
                    {
                        inputList.Add(new InjectedInputKeyboardInfo() { VirtualKey = (ushort)VirtualKey.GamepadDPadDown });
                        downEnabled = false;
                        downEnabledChanged = DateTime.Now;
                    }
                    else if (!reading.Buttons.HasFlag(Windows.Gaming.Input.GamepadButtons.DPadDown) && !downEnabled)
                    {
                        timer.Interval = 1;

                        downEnabledChanged = null;
                        downEnabled = true;
                    }

                    if (reading.Buttons.HasFlag(Windows.Gaming.Input.GamepadButtons.DPadLeft) && leftEnabledChanged is not null && DateTime.Now - leftEnabledChanged.Value > TimeSpan.FromMilliseconds(500))
                    {
                        inputList.Add(new InjectedInputKeyboardInfo() { VirtualKey = (ushort)VirtualKey.GamepadDPadLeft });
                        timer.Interval = 100;
                    }
                    else if (reading.Buttons.HasFlag(Windows.Gaming.Input.GamepadButtons.DPadLeft) && leftEnabled)
                    {
                        inputList.Add(new InjectedInputKeyboardInfo() { VirtualKey = (ushort)VirtualKey.GamepadDPadLeft });
                        leftEnabled = false;
                        leftEnabledChanged = DateTime.Now;
                    }
                    else if (!reading.Buttons.HasFlag(Windows.Gaming.Input.GamepadButtons.DPadLeft) && !leftEnabled)
                    {
                        timer.Interval = 1;

                        leftEnabledChanged = null;
                        leftEnabled = true;
                    }

                    if (reading.Buttons.HasFlag(Windows.Gaming.Input.GamepadButtons.DPadRight) && rightEnabledChanged is not null && DateTime.Now - rightEnabledChanged.Value > TimeSpan.FromMilliseconds(500))
                    {
                        inputList.Add(new InjectedInputKeyboardInfo() { VirtualKey = (ushort)VirtualKey.GamepadDPadRight });
                        timer.Interval = 100;
                    }
                    else if (reading.Buttons.HasFlag(Windows.Gaming.Input.GamepadButtons.DPadRight) && rightEnabled)
                    {
                        inputList.Add(new InjectedInputKeyboardInfo() { VirtualKey = (ushort)VirtualKey.GamepadDPadRight });
                        rightEnabled = false;
                        rightEnabledChanged = DateTime.Now;
                    }
                    else if (!reading.Buttons.HasFlag(Windows.Gaming.Input.GamepadButtons.DPadRight) && !rightEnabled)
                    {
                        timer.Interval = 1;

                        rightEnabledChanged = null;
                        rightEnabled = true;
                    }

                    if (reading.Buttons.HasFlag(Windows.Gaming.Input.GamepadButtons.DPadUp) && upEnabledChanged is not null && DateTime.Now - upEnabledChanged.Value > TimeSpan.FromMilliseconds(500))
                    {
                        inputList.Add(new InjectedInputKeyboardInfo() { VirtualKey = (ushort)VirtualKey.GamepadDPadUp });
                        timer.Interval = 100;
                    }
                    else if (reading.Buttons.HasFlag(Windows.Gaming.Input.GamepadButtons.DPadUp) && upEnabled)
                    {
                        inputList.Add(new InjectedInputKeyboardInfo() { VirtualKey = (ushort)VirtualKey.GamepadDPadUp });
                        upEnabled = false;
                        upEnabledChanged = DateTime.Now;
                    }
                    else if (!reading.Buttons.HasFlag(Windows.Gaming.Input.GamepadButtons.DPadUp) && !upEnabled)
                    {
                        timer.Interval = 1;

                        upEnabledChanged = null;
                        upEnabled = true;
                    }

                    if (reading.Buttons.HasFlag(Windows.Gaming.Input.GamepadButtons.Menu) && menuEnabled)
                    {
                        inputList.Add(new InjectedInputKeyboardInfo() { VirtualKey = (ushort)VirtualKey.Application });
                        menuEnabled = false;
                    }
                    else if (!reading.Buttons.HasFlag(Windows.Gaming.Input.GamepadButtons.Menu))
                        menuEnabled = true;

                    if (reading.LeftTrigger > 0.5 && leftTriggerEnabledChanged is not null && DateTime.Now - leftTriggerEnabledChanged.Value > TimeSpan.FromMilliseconds(500))
                    {
                        inputList.Add(new InjectedInputKeyboardInfo() { VirtualKey = (ushort)VirtualKey.GamepadLeftTrigger });
                        timer.Interval = 100;
                    }
                    else if (reading.LeftTrigger > 0.5 && leftTriggerEnabled)
                    {
                        inputList.Add(new InjectedInputKeyboardInfo() { VirtualKey = (ushort)VirtualKey.GamepadLeftTrigger });
                        leftTriggerEnabled = false;
                        leftTriggerEnabledChanged = DateTime.Now;
                    }
                    else if (reading.LeftTrigger < 0.5 && !leftTriggerEnabled)
                    {
                        timer.Interval = 1;

                        leftTriggerEnabledChanged = null;
                        leftTriggerEnabled = true;
                    }

                    if (reading.RightTrigger > 0.5 && rightTriggerEnabledChanged is not null && DateTime.Now - rightTriggerEnabledChanged.Value > TimeSpan.FromMilliseconds(500))
                    {
                        inputList.Add(new InjectedInputKeyboardInfo() { VirtualKey = (ushort)VirtualKey.GamepadRightTrigger });
                        timer.Interval = 100;
                    }
                    else if (reading.RightTrigger > 0.5 && rightTriggerEnabled)
                    {
                        inputList.Add(new InjectedInputKeyboardInfo() { VirtualKey = (ushort)VirtualKey.GamepadRightTrigger });
                        rightTriggerEnabled = false;
                        rightTriggerEnabledChanged = DateTime.Now;
                    }
                    else if (reading.RightTrigger < 0.5 && !rightTriggerEnabled)
                    {
                        timer.Interval = 1;

                        rightTriggerEnabledChanged = null;
                        rightTriggerEnabled = true;
                    }

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
                Process.Start(new ProcessStartInfo("ms-actioncenter://") { UseShellExecute = true });
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
                {
                    NotificationBinding binding = notification.Notification.Visual.GetBinding(KnownNotificationBindings.ToastGeneric);
                    var text = binding.GetTextElements();

                    string titleText = text.Count == 0 ? "New notification" : text.First().Text;
                    string bodyText = string.Empty;
                    for (int i = 1; i < text.Count; i++)
                    {
                        var textblock = text[i];
                        bodyText = bodyText + textblock.Text + "\n";
                    }

                    this.DispatcherQueue.TryEnqueue(async () =>
                    {
                        BitmapImage bmp = new();
                        if (!string.IsNullOrWhiteSpace(notification.AppInfo.PackageFamilyName))
                        {
                            try
                            {
                                var entry = allApps.First(i => i.ItemName.Contains(notification.AppInfo.DisplayInfo.DisplayName, StringComparison.InvariantCultureIgnoreCase));
                                bmp = entry.Icon;
                            }
                            catch { bmp.SetSource(await notification.AppInfo.DisplayInfo.GetLogo(new Windows.Foundation.Size(120, 120)).OpenReadAsync()); }
                            var notif = new Indexing.Notification() { Title = titleText, Body = bodyText, Id = notification.Id, AppIcon = bmp, AppDisplayName = notification.AppInfo.DisplayInfo.DisplayName, AppPackageFamilyName = notification.AppInfo.PackageFamilyName };
                            this.notifications.Insert(0, notif);
                        }
                        else
                        {
                            try
                            {
                                var entry = allApps.First(i => i.ItemName.Contains(notification.AppInfo.DisplayInfo.DisplayName, StringComparison.InvariantCultureIgnoreCase));
                                bmp = entry.Icon;
                            }
                            catch { }
                            var notif = new Indexing.Notification() { Title = titleText, Body = bodyText, Id = notification.Id, AppIcon = bmp, AppDisplayName = notification.AppInfo.DisplayInfo.DisplayName };
                            this.notifications.Insert(0, notif);
                        }
                    });
                }
            }
            else if (changeKind == UserNotificationChangedKind.Added)
            {
                var notifications = await sender.GetNotificationsAsync(NotificationKinds.Toast);
                UserNotification notification = null;
                try { notification = notifications.First(i => i.Id == changedId); }
                catch { }

                NotificationBinding binding = notification.Notification.Visual.GetBinding(KnownNotificationBindings.ToastGeneric);
                var text = binding.GetTextElements();

                string titleText = text.Count == 0 ? "New notification" : text.First().Text;
                string bodyText = string.Empty;
                for (int i = 1; i < text.Count; i++)
                {
                    var textblock = text[i];
                    bodyText = bodyText + textblock.Text + "\n";
                }

                this.DispatcherQueue.TryEnqueue(async () =>
                {
                    BitmapImage bmp = new();
                    if (!string.IsNullOrWhiteSpace(notification.AppInfo.PackageFamilyName))
                    {
                        try
                        {
                            var entry = allApps.First(i => i.ItemName.Contains(notification.AppInfo.DisplayInfo.DisplayName, StringComparison.InvariantCultureIgnoreCase));
                            bmp = entry.Icon;
                        }
                        catch { bmp.SetSource(await notification.AppInfo.DisplayInfo.GetLogo(new Windows.Foundation.Size(120, 120)).OpenReadAsync()); }
                        var notif = new Indexing.Notification() { Title = titleText, Body = bodyText, Id = notification.Id, AppIcon = bmp, AppDisplayName = notification.AppInfo.DisplayInfo.DisplayName, AppPackageFamilyName = notification.AppInfo.PackageFamilyName };
                        this.notifications.Insert(0, notif);
                    }
                    else
                    {
                        try
                        {
                            var entry = allApps.First(i => i.ItemName.Contains(notification.AppInfo.DisplayInfo.DisplayName, StringComparison.InvariantCultureIgnoreCase));
                            bmp = entry.Icon;
                        }
                        catch { }
                        var notif = new Indexing.Notification() { Title = titleText, Body = bodyText, Id = notification.Id, AppIcon = bmp, AppDisplayName = notification.AppInfo.DisplayInfo.DisplayName };
                        this.notifications.Insert(0, notif);
                    }
                });
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
        private void NotifSettingsButton_Click(object sender, RoutedEventArgs e) => Process.Start(new ProcessStartInfo("ms-settings:notifications") { UseShellExecute = true });
        private void DateTimeButton_Click(object sender, RoutedEventArgs e) => Process.Start(new ProcessStartInfo("ms-settings:dateandtime") { UseShellExecute = true });
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

        private void Open_ControlCenter(object sender, RoutedEventArgs args) => Process.Start(new ProcessStartInfo("ms-actioncenter:controlcenter/&showFooter=true") { UseShellExecute = true });
        private void StartMenu_Click(object sender, RoutedEventArgs e)
        {
            if (AppWindow.Size.Height - 70 - (AppWindow.Size.Height * 7 / 8).Clamp(725, 400) < 54)
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
        private void Open_Diagnostics(object sender, RoutedEventArgs e) => Process.Start(new ProcessStartInfo("ms-settings:troubleshoot") { UseShellExecute = true });
        private void Open_NetworkInternet(object sender, RoutedEventArgs e) => Process.Start(new ProcessStartInfo("ms-settings:network-status") { UseShellExecute = true });
        private void Open_VolumeMixer(object sender, RoutedEventArgs e) => Process.Start(new ProcessStartInfo("ms-settings:apps-volume") { UseShellExecute = true });
        private void Open_SoundSettings(object sender, RoutedEventArgs e) => Process.Start(new ProcessStartInfo("ms-settings:sound") { UseShellExecute = true });
        private void Open_PowerSleep(object sender, RoutedEventArgs e) => Process.Start(new ProcessStartInfo("ms-settings:powersleep") { UseShellExecute = true });

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
                    content.Children.Add(new Image() { Source = new BitmapImage() { UriSource = new Uri(heros.Length != 0 ? heros[0].FullImageUrl : "ms-appx:///Assets/Placeholder.png") } });
                    content.Children.Add(new Image() { MaxHeight = 90, Margin = new Thickness(40), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Stretch, Source = new BitmapImage() { UriSource = new Uri(logos.Length != 0 ? logos[0].FullImageUrl : "ms-appx:///Assets/Placeholder.png") } });
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
                animationBuilder.Size(axis: Axis.Y, to: ((allSearch.Count * 36) + ((allSearch.Count - 1) * 4) + 70).Clamp(((int)startMenu.Height).Clamp(600, 400)), from: menuBarOriginalSize.Height, duration: TimeSpan.FromMilliseconds(500), easingType: EasingType.Default, easingMode: Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseOut, layer: FrameworkLayer.Xaml).Start(menuBar);

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
                animationBuilder.Size(axis: Axis.Y, to: ((allSearch.Count * 36) + ((allSearch.Count - 1) * 4) + 70).Clamp(((int)startMenu.Height).Clamp(600, 400)), from: menuBarOriginalSize.Height, duration: TimeSpan.FromMilliseconds(500), easingType: EasingType.Default, easingMode: Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseOut, layer: FrameworkLayer.Xaml).Start(menuBar);

                var senderAnimationBuilder = AnimationBuilder.Create();
                senderAnimationBuilder.Size(axis: Axis.X, to: 674, from: 400, duration: TimeSpan.FromMilliseconds(500), easingType: EasingType.Default, easingMode: Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseOut, layer: FrameworkLayer.Xaml).Start(sender);
                senderAnimationBuilder.Translation(to: new Vector2(0, 5), from: Vector2.Zero, duration: TimeSpan.FromMilliseconds(300), easingMode: Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseOut, layer: FrameworkLayer.Xaml).Start(sender);

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

        private void SoundSwitch_Toggled(object sender, RoutedEventArgs e) => ElementSoundPlayer.State = (sender as ToggleSwitch).IsOn ? ElementSoundPlayerState.On : ElementSoundPlayerState.Off;
    }
}
