using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kindergarten.BLL.Models.BranchDTO;
using Kindergarten.BLL.Models.KindergartenDTO;

namespace Kindergarten.BLL.Models.KGBranchDTO
{
    public class KGBranchUpdateDTO
    {
        
        public KindergartenUpdateDTO Kg { get; set; }
        public List<BranchUpdateDTO> Branches { get; set; }

    }
}
