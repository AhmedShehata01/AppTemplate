using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kindergarten.DAL.Enum
{
    public enum UserStatus
    {
        draft = 0,
        pendingApproval = 1,
        approved = 2,
        rejected = 3,
    }
}
