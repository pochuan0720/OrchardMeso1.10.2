using System.Collections.Generic;
using System.Linq;

namespace Orchard.Schedule.Providers
{
    public abstract class CollectionProvider : DateProvider
    {
        protected CollectionProvider()
        {
            Providers = new List<DateProvider>();
        }

        protected CollectionProvider(ICollection<DateProvider> providers) {
            Providers = providers;
        }

        public ICollection<DateProvider> Providers { get; private set; }

        public void Add(DateProvider provider)
        {
            Providers.Add(provider);
        }

        public DateProvider Simplified()
        {
            if (Providers.Count == 1)
            {
                return Providers.First();
            }
            else
            {
                return this;
            }
        }
    }
}