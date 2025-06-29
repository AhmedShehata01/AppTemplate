using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kindergarten.DAL.Enum;

namespace Kindergarten.BLL.Models.ActivityLogDTO
{
    public class ActivityLogDTO
    {
        public int Id { get; set; }

        public string EntityName { get; set; }
        public string EntityId { get; set; }
        public ActivityActionType ActionType { get; set; }

        public string? OldValues { get; set; }
        public string? NewValues { get; set; }

        [MaxLength(500)]
        public string? SystemComment { get; set; }              // Optional comment

        [MaxLength(500)]
        public string? UserComment { get; set; }              // Optional comment

        public string PerformedByUserId { get; set; }
        public string PerformedByUserName { get; set; }

        public DateTime PerformedAt { get; set; }
    }

    public class ActivityLogViewDTO
    {

        public string ActionType { get; set; }

        [MaxLength(500)]
        public string? UserComment { get; set; }              // Optional comment

        public string PerformedByUserName { get; set; }

        public DateTime PerformedAt { get; set; }
    }

    public class ActivityLogCreateDTO
    {
        public string EntityName { get; set; }
        public string EntityId { get; set; }
        public ActivityActionType ActionType { get; set; }

        public string? OldValues { get; set; }
        public string? NewValues { get; set; }

        [MaxLength(500)]
        public string? SystemComment { get; set; }              // Optional comment

        [MaxLength(500)]
        public string? UserComment { get; set; }              // Optional comment

        public string PerformedByUserId { get; set; }
        public string PerformedByUserName { get; set; }
    }
}
