using System.Collections.Generic;
using Orchard.UI.Navigation;
using Orchard.Users.Models;

namespace Orchard.Users.ViewModels {

    public class UsersIndexApiViewModel  {
        public IList<UserPartRecord> Users { get; set; }
        public UserIndexOptions Options { get; set; }
        public Pager Pager { get; set; }
    }
}
