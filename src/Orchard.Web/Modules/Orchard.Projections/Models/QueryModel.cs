using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Orchard.Projections.Models
{
    public class QueryModel
    {
        public string  Name { get; set; }
        public QueryPartRecord Param { get; set; }
    }
}