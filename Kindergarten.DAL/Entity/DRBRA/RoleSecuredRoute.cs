using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
