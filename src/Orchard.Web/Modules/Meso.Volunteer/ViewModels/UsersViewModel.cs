
namespace Meso.Users.ViewModels {

    public enum Order {
        Name,
        Email,
        CreatedUtc,
        LastLoginUtc
    }

    public class Filter {
        public string[] UserRoles { get; set; }
    }
}
