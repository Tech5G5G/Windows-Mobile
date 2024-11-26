using System;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Windows_Mobile.MC
{
    ///<summary>Used for identifying mods</summary>
    public enum ModKind
    {
        FabricQuilt,
        Forge,
        NeoForge,
        Unknown
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
        public string homepage { get; set; }
        public string sources { get; set; }
        public string issues { get; set; }
    }
}
