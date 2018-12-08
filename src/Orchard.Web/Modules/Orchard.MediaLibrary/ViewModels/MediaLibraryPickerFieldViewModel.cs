using System.Collections.Generic;
using Orchard.ContentManagement;
using Orchard.MediaLibrary.Fields;
using Orchard.MediaLibrary.Models;

namespace Orchard.MediaLibrary.ViewModels {

    public class MediaLibraryPickerFieldViewModel {

        public ICollection<ContentItem> ContentItems { get; set; }
        public ICollection<object> Objects { get; set; }
        public string SelectedIds { get; set; }
        public MediaLibraryPickerField Field { get; set; }
        public ContentPart Part { get; set; }
    }
}