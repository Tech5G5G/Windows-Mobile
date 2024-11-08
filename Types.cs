using System.Xml.Serialization;

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