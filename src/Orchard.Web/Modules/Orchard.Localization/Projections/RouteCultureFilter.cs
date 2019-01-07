using System;
using System.Globalization;
using System.Web;
using Orchard.ContentManagement;
using Orchard.Events;
using Orchard.Localization.Models;
using Orchard.Localization.Services;

namespace Orchard.Localization.Projections {

    public class RouteCultureFilter : IFilterProvider {
        private readonly IWorkContextAccessor _workContextAccessor;
        private readonly ICultureManager _cultureManager;

        public RouteCultureFilter(IWorkContextAccessor workContextAccessor, ICultureManager cultureManager) {
            _workContextAccessor = workContextAccessor;
            _cultureManager = cultureManager;
            T = NullLocalizer.Instance;
        }

        public Localizer T { get; set; }

        public void Describe(dynamic describe) {
            describe.For("Localization", T("Localization"), T("Localization"))
                .Element("ForRouteCulture", T("For Route culture"), T("Localized content items for Route culture"),
                    (Action<dynamic>)ApplyFilter,
                    (Func<dynamic, LocalizedString>)DisplayFilter,
                    null
                );
        }

        public void ApplyFilter(dynamic context) {
            HttpContextBase _context = _workContextAccessor.GetContext().HttpContext;
            var routeCulture = _context.Request.RequestContext.RouteData.Values["culture"] ??
                _context.Request.RequestContext.HttpContext.Request.Params["culture"];

            if (routeCulture == null || string.IsNullOrWhiteSpace(routeCulture.ToString()))
                routeCulture = _workContextAccessor.GetContext().CurrentCulture;

            try
            {
                var currentCulture = CultureInfo.GetCultureInfo(routeCulture.ToString());
                //string currentCulture = _workContextAccessor.GetContext().HttpContext;
                var currentCultureId = _cultureManager.GetCultureByName(currentCulture.Name).Id;

                var query = (IHqlQuery)context.Query;
                context.Query = query.Where(x => x.ContentPartRecord<LocalizationPartRecord>(), x => x.Eq("CultureId", currentCultureId));
            }
            catch { }
        }

        public LocalizedString DisplayFilter(dynamic context) {
            return T("For Route culture");
        }
    }
}