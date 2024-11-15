using System;
using System.Collections.ObjectModel;
using System.IO;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows_Mobile.Types;

namespace Windows_Mobile
{
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

        public static BitmapImage ToBitmapImage(this Stream stream)
        {
            MemoryStream ms = new();
            stream.CopyTo(ms);
            ms.Position = 0;
            var bitmapImage = new BitmapImage();
            bitmapImage.SetSource(ms.AsRandomAccessStream());
            return bitmapImage;
        }

        public static int Clamp(this int input, int max, int min = 0)
        {
            if (input >= min && input <= max)
                return input;
            else if (input > max)
                return max;
            else if (input < min)
                return min;
            else
                throw new Exception("Unable to compare value of input");
        }
    }
}
