using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kindergarten.BLL.Models.DRBRADTO
{
    public class SidebarItemDTO
    {
        public int Id { get; set; }
        public string LabelAr { get; set; } = null!;
        public string LabelEn { get; set; } = null!;
        public string Icon { get; set; } = null!;
        public string Route { get; set; } = null!;
        public int? ParentId { get; set; }
        public int Order { get; set; }

        public List<SidebarItemDTO> Children { get; set; } = new();
    }

    public class CreateSidebarItemDTO
    {
        public string LabelAr { get; set; } = null!;
        public string LabelEn { get; set; } = null!;
        public string Icon { get; set; } = null!;
        public string Route { get; set; } = null!;
        public int? ParentId { get; set; } // null => Main item
        public int Order { get; set; } = 0;
    }

    public class UpdateSidebarItemDTO
    {
        public int Id { get; set; }
        public string LabelAr { get; set; } = null!;
        public string LabelEn { get; set; } = null!;
        public string Icon { get; set; } = null!;
        public string Route { get; set; } = null!;
        public int? ParentId { get; set; }
        public int Order { get; set; } = 0;
    }
}
