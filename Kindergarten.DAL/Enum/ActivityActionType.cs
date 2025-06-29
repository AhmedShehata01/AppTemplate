using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kindergarten.DAL.Enum
{
    public enum ActivityActionType
    {
        Created = 1,
        Updated = 2,
        Deleted = 3,
        SoftDeleted = 4,
        Viewed = 5,
        AddedChildEntity = 6,
        DeletedChildEntity = 7,
        SoftDeletedChildEntity = 8,
        UpdatedChildEntity = 9,
        Approved = 10,
        Rejected = 11,
        Activated = 12,
        Deactivated = 13,
        Archived = 14,




        // ✅ Auth Specific Actions
        Login = 50,
        ExternalLogin = 51,
        Register = 52,
        ChangePassword = 53,
        ForgetPassword = 54,
        FirstExternalLogin = 55
    }
}
