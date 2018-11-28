using Orchard.ContentManagement;
using System.Collections.Generic;

namespace Orchard.Core.Common.Handlers {
    public interface IUpdateModelHandler : IUpdateModel, IDependency
    {
        IUpdateModelHandler SetData(object _root);
    }
}