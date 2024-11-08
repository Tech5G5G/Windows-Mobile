using System.Xml.Serialization;
using Microsoft.UI.Xaml.Media.Imaging;
using craftersmine.SteamGridDBNet;

//Custom types for retriving data
namespace Windows_Mobile.Types;

[XmlRoot("Game")]
public class Game
{
    [XmlElement("StoreId")]
    public string StoreId { get; set; }
}

public class EGSGameInfo
{
    public string LaunchExecutable { get; set; }
    public string InstallLocation { get; set; }
    public string DisplayName { get; set; }
    public string CatalogNamespace { get; set; }
    public string CatalogItemId { get; set; }
    public string AppName { get; set; }
}

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