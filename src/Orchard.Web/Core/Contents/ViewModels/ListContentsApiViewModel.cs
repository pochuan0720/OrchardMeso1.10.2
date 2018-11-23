using System.Collections.Generic;
using Orchard.ContentManagement;
using Orchard.UI.Navigation;

namespace Orchard.Core.Contents.ViewModels {
    public class ListContentsApiViewModel  {
        public ListContentsApiViewModel() {
            Options = new ContentOptions();
        }

        public string Id { get; set; }

        public string TypeName {
            get { return Id; }
        }

        public string TypeDisplayName { get; set; }
        //public int? Page { get; set; }
        public IList<Entry> Entries { get; set; }
        public ContentOptions Options { get; set; }
        public Pager Pager { get; set; }

        #region Nested type: Entry

        public class Entry {
            public ContentItem ContentItem { get; set; }
            public ContentItemMetadata ContentItemMetadata { get; set; }
        }

        #endregion
    }

}