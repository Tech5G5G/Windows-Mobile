using System.Xml.Serialization;
using Microsoft.UI.Xaml.Media.Imaging;
using craftersmine.SteamGridDBNet;
using System.Collections.Generic;

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
    public string ItemName { get; set;  }
    public ApplicationKind ItemKind { get; set; }
    public string ItemStartURI { get; set; }
    public BitmapImage Icon { get; set; }
    public SteamGridDbGame GameInfo { get; set; }
    public string Id { get; set; }
}

public class MCModInfo
{
    public string name { get; set; }
    public string version { get; set; }
    public string description { get; set; }
    public string license { get; set; }
    public MCModContact contact { get; set; }
    public ModKind kind { get; set; }

    /// <summary>Path to icon</summary>
    public string icon { get; set; }
    public BitmapImage image { get; set; }
}

public class MCModContact
{
    public string homepage { get; set;}
    public string sources { get; set;}
    public string issues { get; set;}
}

///<summary>Used for identifying mods</summary>
public enum ModKind
{
    FabricQuilt,
    Forge,
    NeoForge
}

///<summary>Represents two doubles via X and Y</summary>
public class DoubleSize(double X, double Y)
{
    public double X { get; set; } = X;
    public double Y { get; set; } = Y;
}