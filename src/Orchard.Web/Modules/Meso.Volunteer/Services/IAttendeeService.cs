using Orchard.Schedule.Models;
using Orchard;
using System.Collections.Generic;
using Orchard.ContentManagement;
using Orchard.Security;
using Orchard.Projections.Models;
using Newtonsoft.Json.Linq;
using System.Web.Http.Routing;

namespace Meso.Volunteer.Services {
    public interface IAttendeeService : IDependency {
        IEnumerable<ContentItem> GetAttendees(IUser user, QueryModel query, string contentType = null);
        JObject GetAttendee(UrlHelper Url, IContent content, JObject inModel = null, bool withUser = true);
    }
}
