using Orchard.Data.Conventions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Orchard.Comments.ViewModels
{
    public class CommentEditApiViewModel
    {
        [Required]
        [StringLengthMax]
        public string CommentText { get; set; }

        public int? CommentedOn { get; set; }
        public int? RepliedOn { get; set; }
    }
}