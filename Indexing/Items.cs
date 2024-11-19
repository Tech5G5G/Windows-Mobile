using System;
using System.Collections.ObjectModel;
using System.Xml.Serialization;
using craftersmine.SteamGridDBNet;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Windows_Mobile.Indexing
{
    [XmlRoot("Game")]
    public class Game
    {
        [XmlElement("StoreId")]
        public string StoreId { get; set; }
    }

    /// <summary>Represents a game from EGS</summary>
    public class EGSGameInfo
    {
        public string LaunchExecutable { get; set; }
        public string InstallLocation { get; set; }
        public string DisplayName { get; set; }
        public string CatalogNamespace { get; set; }
        public string CatalogItemId { get; set; }
        public string AppName { get; set; }
    }

    ///<summary>Used for sorting and starting applications</summary>
    public enum ApplicationKind
    {
        Normal,
        SteamGame,
        EpicGamesGame,
        GOGGame,
        XboxGame,
        Launcher,
        LauncherPackaged,
        Packaged
    }

    /// <summary>Represents an item in the start menu</summary>
    public class StartMenuItem
    {
        public string ItemName { get; set; }
        public ApplicationKind ItemKind { get; set; }
        public string ItemStartURI { get; set; }
        public BitmapImage Icon { get; set; }
        public SteamGridDbGame GameInfo { get; set; }
        public string Id { get; set; }
    }

    public class Notification
    {
        public string Title { get; set; }
        public string Body { get; set; }

        public BitmapImage AppIcon { get; set; }
        public string AppDisplayName { get; set; }
        public string AppPackageFamilyName { get; set; }
    }

    public static class Extensions
    {
        public static bool IsDuplicate(this StartMenuItem item, ObservableCollection<StartMenuItem> collection)
        {
            foreach (var collectionItem in collection)
            {
                if (collectionItem.ItemKind == item.ItemKind && (collectionItem.Id is null || item.Id is null || collectionItem.Id.Equals(item.Id, StringComparison.InvariantCultureIgnoreCase)) && collectionItem.ItemName.Equals(item.ItemName, StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}
