using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace Kindergarten.DAL.Extend
{
    public class ApplicationUser : IdentityUser
    {
        public ApplicationUser()
        {
            IsAgree = true;
            IsDeleted = false;
            CreatedOn = DateTime.Now.ToShortDateString();
        }
        public bool IsAgree { get; set; }
        public string CreatedOn { get; set; }
        public bool IsDeleted { get; set; }
    }
}
