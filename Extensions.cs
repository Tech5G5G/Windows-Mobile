using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    }
}
