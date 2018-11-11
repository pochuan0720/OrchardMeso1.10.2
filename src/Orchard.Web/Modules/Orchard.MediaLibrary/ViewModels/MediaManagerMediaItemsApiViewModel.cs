using System.Collections.Generic;
using Orchard.MediaLibrary.Models;
using Orchard.UI.Navigation;

namespace Orchard.MediaLibrary.ViewModels {
    public class MediaManagerMediaItemsApiViewModel {
        public IList<MediaPart> MediaItems { get; set; }
        public Pager Pager { get; set; }
    }
}