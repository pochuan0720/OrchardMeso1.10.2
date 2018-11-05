﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Orchard.Blogs.ViewModels
{
    public class BlogPostEditApiViewModel
    {
        public string Title { get; set; }
        public string Text { get; set; }
        public bool? Publish { get; set; }
        public object Data { get; set; }
    }
}