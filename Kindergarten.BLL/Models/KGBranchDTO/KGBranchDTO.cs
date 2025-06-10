using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kindergarten.BLL.Models.KGBranchDTO
{
    public class KGBranchDTO
    {
        public KindergartenDTO.KindergartenDTO Kg { get; set; }

        // Fix: Ensure BranchDTO is correctly referenced as a type  
        public List<BranchDTO.BranchDTO>? Branches { get; set; }

    }
}
