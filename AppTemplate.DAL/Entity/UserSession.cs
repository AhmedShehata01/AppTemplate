using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppTemplate.DAL.Entity
{
    public class UserSession
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string Token { get; set; }
        public DateTime ExpireAt { get; set; }
        public string ConnectionId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsLoggedOut { get; set; } = false;
        public bool ForceLoggedOut { get; set; } = false;
        public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    }

}
