
namespace Meso.Volunteer.ViewModels {

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
