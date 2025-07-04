using System.ComponentModel.DataAnnotations;
using AppTemplate.DAL.Enum;
using AppTemplate.DAL.Extend;

namespace AppTemplate.DAL.Entity
{
    public class ActivityLog
    {
        public int Id { get; set; }

        public string EntityName { get; set; }            // ex: Kindergarten, Branch
        public string EntityId { get; set; }              // ex: 12

        public ActivityActionType ActionType { get; set; }   // Enum

        public string? OldValues { get; set; }            // JSON serialized object
        public string? NewValues { get; set; }            // JSON serialized object

        [MaxLength(500)]
        public string? SystemComment { get; set; }              // Optional comment

        [MaxLength(500)]
        public string? UserComment { get; set; }              // Optional comment

        public string PerformedByUserId { get; set; }
        public string PerformedByUserName { get; set; }

        // Nullable Navigation Property for emergencies:
        public ApplicationUser? PerformedByUser { get; set; }

        public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
    }

}
