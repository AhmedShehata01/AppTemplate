using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kindergarten.DAL.Enum;
using Microsoft.AspNetCore.Identity;

namespace Kindergarten.DAL.Extend
{
    public class ApplicationUser : IdentityUser
    {
        public ApplicationUser()
        {
            IsAgree = true;
            IsDeleted = false;
            CreatedOn = DateTime.Now;
        }

        public UserType UserType { get; set; }

        public bool IsAgree { get; set; }
        public DateTime CreatedOn { get; set; }
        public bool IsDeleted { get; set; }
    }
}
