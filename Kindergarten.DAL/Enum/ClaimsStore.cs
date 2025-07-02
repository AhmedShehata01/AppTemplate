using System.Security.Claims;

namespace Kindergarten.DAL.Enum
{
    public class ClaimsStore
    {
        public static List<Claim> AllClaims = new List<Claim>()
        {
            new Claim("View Role" , "View Role"),
            new Claim("Create Role" , "Create Role"),
            new Claim("Edit Role" , "Edit Role"),
            new Claim("Delete Role" , "Delete Role"),
        };
    }
}
