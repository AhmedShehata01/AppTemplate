using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using AppTemplate.BLL.Services.UserSessionServices;

namespace AppTemplate.BLL.Hubs
{
    [Authorize]
    public class PresenceHub : Hub
    {
        private readonly IUserSessionService _userSessionService;
        private readonly ILogger<PresenceHub> _logger;

        // هنا نخزن UserId -> ConnectionId
        public static ConcurrentDictionary<string, string> ConnectedUsers = new();

        public PresenceHub(
            IUserSessionService userSessionService,
            ILogger<PresenceHub> logger)
        {
            _userSessionService = userSessionService;
            _logger = logger;
        }

        /// <summary>
        /// يرجع الـ ConnectionId للفرونت عشان يتسجل في السيشن
        /// </summary>
        [AllowAnonymous]
        public Task<string> GetConnectionId()
        {
            _logger.LogInformation("ℹ️ [GetConnectionId] Returning ConnectionId: {ConnectionId}", Context.ConnectionId);
            return Task.FromResult(Context.ConnectionId);
        }

        /// <summary>
        /// يتم استدعائها عند أي اتصال جديد بالـ Hub
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst("nameid")?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                _logger.LogInformation("✅ [OnConnectedAsync] UserId: {UserId} connected. ConnectionId: {ConnectionId}", userId, Context.ConnectionId);

                // حدث السيشن على طول هنا
                await _userSessionService.UpdateConnectionIdAsync(userId, Context.ConnectionId);

                // حدث الديكشنري
                ConnectedUsers[userId] = Context.ConnectionId;
            }
            else
            {
                _logger.LogInformation("✅ [OnConnectedAsync] Anonymous connection established. ConnectionId: {ConnectionId}", Context.ConnectionId);
            }

            await base.OnConnectedAsync();
        }


        /// <summary>
        /// يتم استدعائها عند قطع الاتصال
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            _logger.LogWarning("❌ [OnDisconnectedAsync] Disconnected. ConnectionId: {ConnectionId}", Context.ConnectionId);

            var userId = FindUserIdByConnectionId(Context.ConnectionId);
            if (userId != null)
            {
                ConnectedUsers.TryRemove(userId, out _);
                _logger.LogInformation("🗑 [OnDisconnectedAsync] Removed mapping for user {UserId}.", userId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// يسجل الـ ConnectionId للـ User في الـ Dictionary وفي DB
        /// </summary>
        public async Task RegisterConnection(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("⚠️ [RegisterConnection] Called with empty userId.");
                return;
            }

            // تحديث الديكشنري
            ConnectedUsers[userId] = Context.ConnectionId;

            _logger.LogInformation("✅ [RegisterConnection] Registered UserId: {UserId} with ConnectionId: {ConnectionId}", userId, Context.ConnectionId);

            try
            {
                await _userSessionService.UpdateConnectionIdAsync(userId, Context.ConnectionId);
                _logger.LogInformation("✅ [RegisterConnection] Updated ConnectionId in UserSession table for UserId: {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [RegisterConnection] Error updating ConnectionId for UserId: {UserId}", userId);
            }
        }

        /// <summary>
        /// يجبر المستخدم على تسجيل الخروج من السيستم
        /// </summary>
        public async Task ForceLogout(string userId)
        {
            if (ConnectedUsers.TryGetValue(userId, out var connectionId))
            {
                _logger.LogInformation("⚡ [ForceLogout] Sending forceLogout to UserId: {UserId} on ConnectionId: {ConnectionId}", userId, connectionId);

                try
                {
                    await Clients.Client(connectionId)
                        .SendAsync("forceLogout");

                    // إزالة المستخدم من الـ Dictionary
                    ConnectedUsers.TryRemove(userId, out _);

                    // تحديث الـ DB ليتم تمييز الـ session كـ ForceLoggedOut
                    await _userSessionService.MarkForceLogoutAsync(userId);
                    _logger.LogInformation("✅ [ForceLogout] Marked UserId: {UserId} as ForceLoggedOut in DB.", userId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ [ForceLogout] Error while sending forceLogout to UserId: {UserId}", userId);
                }
            }
            else
            {
                _logger.LogInformation("ℹ️ [ForceLogout] UserId: {UserId} not connected. Cannot send forceLogout.", userId);
            }
        }

        /// <summary>
        /// يبحث عن UserId بناء على ConnectionId في الديكشنري
        /// </summary>
        private string? FindUserIdByConnectionId(string connectionId)
        {
            foreach (var kvp in ConnectedUsers)
            {
                if (kvp.Value == connectionId)
                    return kvp.Key;
            }
            return null;
        }
    }
}
