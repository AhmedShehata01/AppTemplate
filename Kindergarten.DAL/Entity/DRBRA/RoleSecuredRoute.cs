using Kindergarten.DAL.Extend;

namespace Kindergarten.DAL.Entity.DRBRA
{
    public class RoleSecuredRoute
    {
        public int Id { get; set; }

        public int SecuredRouteId { get; set; }
        public SecuredRoute SecuredRoute { get; set; }

        public string RoleId { get; set; }
        public ApplicationRole Role { get; set; }
    }
}
