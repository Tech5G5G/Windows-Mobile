using System.Xml.Serialization;

//Custom types for retriving data
namespace Windows_Mobile.Helpers;

[XmlRoot("Game")]
public class Game
{
    [XmlElement("StoreId")]
    public string StoreId { get; set; }
}

public class SteamGameInfo
{
    public string appid { get; set; }
    public string name { get; set; }
    public string installdir { get; set; }
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