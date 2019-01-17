using Newtonsoft.Json;
using TYMetro.Management.Api.Models.Permission;
using TYMetro.Management.Api.Models.Time;

namespace Meso.TyMetro.ViewModels
{
    [JsonObject(MemberSerialization.OptIn)]
    public class UserViewModel
    {
        [JsonProperty]
        public int Id { get; set; }
        [JsonProperty]
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }


        public UserViewModel()
        {
        }

        public UserViewModel(GoTimeListDataModel inMoidel)
        {
            Id = (int)inMoidel.ID;
        }

        public UserLoginModel ToLoginModel()
        {
            return new UserLoginModel
            {
                Email = UserName,
                Password = Password
            };
        }
    }
}