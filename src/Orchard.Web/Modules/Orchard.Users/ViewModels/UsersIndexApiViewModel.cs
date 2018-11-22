using System.Collections.Generic;
using Orchard.UI.Navigation;
using Orchard.Users.Models;

namespace Orchard.Users.ViewModels {

    public class UsersIndexApiViewModel  {
        public int? Id { get; set; }
        public IList<UserPart> Data { get; set; }
        public UserIndexOptions Options { get; set; }
        public Pager Pager { get; set; }
    }
}
