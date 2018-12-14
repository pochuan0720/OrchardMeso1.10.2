using Newtonsoft.Json.Linq;
using System.ComponentModel.DataAnnotations;

namespace Orchard.Schedule.ViewModels
{
    public class AttendeeCancelViewMode
    {
        public string Title { get; set; }
        public string Owner { get; set; }
        public JObject Data { get; set; }
    }
}