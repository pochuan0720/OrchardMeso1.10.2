using Orchard.Settings;
using Newtonsoft.Json;
using System;

namespace Orchard.UI.Navigation {
    [JsonObject(MemberSerialization.OptIn)]
    public class Pager {
        /// <summary>
        /// The default page number.
        /// </summary>
        public const int PageDefault = 1;

        public Pager()
        {

        }

        /// <summary>
        /// Constructs a new pager.
        /// </summary>
        /// <param name="site">The site settings.</param>
        /// <param name="pagerParameters">The pager parameters.</param>
        public Pager(ISite site, PagerParameters pagerParameters) 
            : this(site, pagerParameters.Page, pagerParameters.PageSize) {
        }

        /// <summary>
        /// Constructs a new pager.
        /// </summary>
        /// <param name="site">The site settings.</param>
        /// <param name="page">The page parameter.</param>
        /// <param name="pageSize">The page size parameter.</param>
        public Pager(ISite site, int? page, int? pageSize) {
            Page = page == null || page == 0 ? PageDefault : page.Value;

            PageSize = pageSize ?? site.PageSize;
            if (site.MaxPageSize > 0 && (PageSize == 0 || PageSize > site.MaxPageSize)) {
                PageSize = site.MaxPageSize;
            }
        }

        public Pager(ISite site, int? page, int? pageSize, int totalCount) : this(site, page, pageSize)
        {
            TotalCount = totalCount;
            TotalPage = (int)Math.Ceiling((double)TotalCount / (double)PageSize);
        }
        /// <summary>
        /// Gets or sets the current page number or the default page number if none is specified.
        /// </summary>
        [JsonProperty]
        public int Page { get; set; }

        /// <summary>
        /// Gets or sets the current page size or the site default size if none is specified.
        /// </summary>
        [JsonProperty]
        public int PageSize { get; set; }

        [JsonProperty]
        public int TotalPage { get; set; }

        [JsonProperty]
        public int TotalCount { get; set; }

        /// <summary>
        /// Gets the current page start index.
        /// </summary>
        /// <param name="page">The current page number.</param>
        /// <returns>The index in which the page starts.</returns>
        public int GetStartIndex(int? page = null) {
            return ((page ?? Page) - PageDefault) * PageSize;
        }
    }
}