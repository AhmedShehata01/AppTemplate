namespace AppTemplate.DAL.Entity.DRBRA
{
    public class SidebarItem
    {
        public int Id { get; set; }

        public string LabelAr { get; set; } = null!;
        public string LabelEn { get; set; } = null!;

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
