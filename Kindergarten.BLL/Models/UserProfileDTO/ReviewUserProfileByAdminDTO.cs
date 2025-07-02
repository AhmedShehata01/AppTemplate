namespace Kindergarten.BLL.Models.UserProfileDTO
{
    public class ReviewUserProfileByAdminDTO
    {
        public string UserId { get; set; }
        public bool IsApproved { get; set; }
        public string? RejectionReason { get; set; }
    }
}
