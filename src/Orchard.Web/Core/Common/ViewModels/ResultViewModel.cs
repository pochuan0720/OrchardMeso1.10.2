using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Orchard.Core.Common.ViewModels
{
    public class ResultViewModel
    {
        public object Content { get; set; }
        public string Code { get; set; }
        public string Message { get; set; }
    }
}