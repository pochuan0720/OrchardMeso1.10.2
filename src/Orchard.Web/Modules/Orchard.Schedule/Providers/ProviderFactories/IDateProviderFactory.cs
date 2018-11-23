using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Orchard.Schedule.Providers.ProviderFactories
{
    public interface IDateProviderFactory
    {
        DateProvider Build();
    }
}
