using Windows.UI.Notifications;

namespace Windows_Mobile.Notifications
{
    public class Notification
    {
        public string Title { get; set; }
        public string Body { get; set; }
        public uint Id { set; get; }

        public BitmapImage AppIcon { get; set; }
        public string AppDisplayName { get; set; }
        public string AppPackageFamilyName { get; set; }

        public async static Task<Notification> FromUserNotification(UserNotification notification, ObservableCollection<StartMenuItem> allApps)
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

            BitmapImage bmp = new();
            if (!string.IsNullOrWhiteSpace(notification.AppInfo.PackageFamilyName))
            {
                try
                {
                    var entry = allApps.First(i => i.ItemName.Contains(notification.AppInfo.DisplayInfo.DisplayName, StringComparison.InvariantCultureIgnoreCase));
                    bmp = entry.Icon;
                }
                catch { bmp.SetSource(await notification.AppInfo.DisplayInfo.GetLogo(new Windows.Foundation.Size(120, 120)).OpenReadAsync()); }
                var notif = new Notification() { Title = titleText, Body = bodyText, Id = notification.Id, AppIcon = bmp, AppDisplayName = notification.AppInfo.DisplayInfo.DisplayName, AppPackageFamilyName = notification.AppInfo.PackageFamilyName };
                return notif;
            }
            else
            {
                try
                {
                    var entry = allApps.First(i => i.ItemName.Contains(notification.AppInfo.DisplayInfo.DisplayName, StringComparison.InvariantCultureIgnoreCase));
                    bmp = entry.Icon;
                }
                catch { }
                var notif = new Notification() { Title = titleText, Body = bodyText, Id = notification.Id, AppIcon = bmp, AppDisplayName = notification.AppInfo.DisplayInfo.DisplayName };
                return notif;
            }
        }
    }
}
