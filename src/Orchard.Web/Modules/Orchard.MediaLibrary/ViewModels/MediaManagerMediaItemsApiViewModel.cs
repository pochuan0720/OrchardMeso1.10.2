using System.Collections.Generic;
using Orchard.MediaLibrary.Models;
using Orchard.UI.Navigation;

namespace Orchard.MediaLibrary.ViewModels {
    public class MediaManagerMediaItemsApiViewModel {
        public string FolderPath { get; set; }
        public IList<MediaPart> MediaItems { get; set; }
        public Pager Pager { get; set; }
    }
}