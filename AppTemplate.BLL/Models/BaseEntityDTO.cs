namespace AppTemplate.BLL.Models
{
    public class BaseEntityDTO
    {
        public bool? IsDeleted { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string? UpdatedBy { get; set; }


        public BaseEntityDTO()
        {
            // تعيين القيم الافتراضية عند إنشاء الكيان
            IsDeleted = false;
            CreatedOn = DateTime.UtcNow;
        }
    }
}
