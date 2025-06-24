using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kindergarten.DAL.Entity.DRBRA
{
    public class SidebarItem
    {
        public int Id { get; set; }

        public string Label { get; set; } = null!;

        public string Icon { get; set; } = null!;

        public string Route { get; set; } = null!;

        public int? ParentId { get; set; } // Null = Root Item

        public int Order { get; set; } = 0;

        public bool IsDeleted { get; set; } = false;

        // Navigation Properties
        public SidebarItem? Parent { get; set; }

        public ICollection<SidebarItem> Children { get; set; } = new List<SidebarItem>();
    }
}
