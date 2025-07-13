using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AppTemplate.BLL.Hubs;
using AppTemplate.BLL.Models;
using AppTemplate.DAL.Entity;
using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AppTemplate.BLL.Services.UserSessionServices
{
    public class UserSessionService : IUserSessionService
    {
        private readonly IUserSessionRepository _repo;
        private readonly IUserSessionCacheService _cache;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;
        private readonly IHubContext<PresenceHub> _hubContext;
        private readonly ILogger<UserSessionService> _logger;

        private readonly int _jwtDurationMinutes;
        private readonly int _forceLogoutMinutes;

        public UserSessionService(
            IUserSessionRepository repo,
            IUserSessionCacheService cache,
            IMapper mapper,
            IConfiguration configuration,
            IHubContext<PresenceHub> hubContext,
            ILogger<UserSessionService> logger)
        {
            _repo = repo;
            _cache = cache;
            _mapper = mapper;
            _configuration = configuration;
            _hubContext = hubContext;
            _logger = logger;

            _jwtDurationMinutes = _configuration.GetValue<int>("JWT:DurationInMinutes");
            _forceLogoutMinutes = _configuration.GetValue<int>("Session:ForceLogoutExpirationInMinutes", 60);
        }

        /// <summary>
        /// جلب الـ Session من Redis أو من الـ DB لو مش موجودة في الكاش
        /// </summary>
        public async Task<UserSessionDTO?> GetUserSessionAsync(string userId)
        {
            var session = await _cache.GetUserSessionAsync(userId);
            if (session != null)
            {
                _logger.LogInformation("✅ Session for user {UserId} fetched from cache.", userId);
                return session;
            }

            var dbSession = await _repo.GetByUserIdAsync(userId);
            if (dbSession != null)
            {
                var dto = _mapper.Map<UserSessionDTO>(dbSession);

                await _cache.SetUserSessionAsync(userId, dto, TimeSpan.FromMinutes(_jwtDurationMinutes));

                _logger.LogInformation("✅ Session for user {UserId} fetched from DB and cached.", userId);
                return dto;
            }

            _logger.LogInformation("ℹ️ No session found for user {UserId}.", userId);
            return null;
        }

        /// <summary>
        /// حفظ Session جديدة في DB و Redis
        /// </summary>
        public async Task SaveUserSessionAsync(UserSessionDTO dto)
        {
            // نحذف أي Session قديمة لنفس المستخدم
            await _repo.RemoveByUserIdAsync(dto.UserId);

            if (string.IsNullOrEmpty(dto.ConnectionId))
                dto.ConnectionId = null;

            dto.ExpireAt = DateTime.UtcNow.AddMinutes(_jwtDurationMinutes);
            dto.LastActivityAt = DateTime.UtcNow;

            var entity = _mapper.Map<UserSession>(dto);

            await _repo.AddAsync(entity);
            await _cache.SetUserSessionAsync(dto.UserId, dto, TimeSpan.FromMinutes(_jwtDurationMinutes));

            _logger.LogInformation("✅ Saved new session for user {UserId}.", dto.UserId);
        }

        /// <summary>
        /// حذف Session من DB و Redis
        /// </summary>
        public async Task RemoveSessionAsync(string userId)
        {
            await _repo.RemoveByUserIdAsync(userId);
            await _cache.RemoveUserSessionAsync(userId);

            _logger.LogInformation("✅ Removed session for user {UserId}.", userId);
        }

        /// <summary>
        /// تحديث الـ ConnectionId الخاص باليوزر في DB و Redis
        /// تُستخدم بعد الـ Reconnect من SignalR
        /// </summary>
        public async Task UpdateConnectionIdAsync(string userId, string newConnectionId)
        {
            if (string.IsNullOrEmpty(newConnectionId))
            {
                _logger.LogWarning("⚠️ newConnectionId is null or empty for user {UserId}.", userId);
                return;
            }

            var session = await GetUserSessionAsync(userId);
            if (session == null)
            {
                _logger.LogWarning("❌ No session found for user {UserId} to update ConnectionId.", userId);
                return;
            }

            var existingEntity = await _repo.GetByUserIdAsync(userId);
            if (existingEntity == null)
            {
                _logger.LogWarning("❌ No DB entity found for user {UserId} to update.", userId);
                return;
            }

            existingEntity.ConnectionId = newConnectionId;
            existingEntity.LastActivityAt = DateTime.UtcNow;

            await _repo.UpdateAsync(existingEntity);

            session.ConnectionId = newConnectionId;
            session.LastActivityAt = DateTime.UtcNow;
            await _cache.SetUserSessionAsync(userId, session, TimeSpan.FromMinutes(_jwtDurationMinutes));

            _logger.LogInformation("✅ Updated ConnectionId for user {UserId} to {ConnectionId}.", userId, newConnectionId);
        }


        /// <summary>
        /// تمييز الـ Session على أنها Force Logout
        /// وتحديث الـ Cache و DB
        /// </summary>
        public async Task MarkForceLogoutAsync(string userId)
        {
            var session = await GetUserSessionAsync(userId);
            if (session == null)
            {
                _logger.LogWarning("❌ No session found for user {UserId} to mark ForceLogout.", userId);
                return;
            }

            session.ForceLoggedOut = true;
            session.IsLoggedOut = true;
            session.LastActivityAt = DateTime.UtcNow;

            var entity = _mapper.Map<UserSession>(session);
            await _repo.UpdateAsync(entity);

            await _cache.SetUserSessionAsync(userId, session, TimeSpan.FromMinutes(_forceLogoutMinutes));

            _logger.LogInformation("✅ User {UserId} marked as ForceLoggedOut.", userId);
        }

        /// <summary>
        /// تنفيذ ForceLogout حقيقي عن طريق SignalR
        /// + تحديث الـ Session في DB و Cache
        /// </summary>
        public async Task ForceLogoutAsync(string userId)
        {
            var session = await GetUserSessionAsync(userId);
            if (session == null || string.IsNullOrEmpty(session.ConnectionId))
            {
                _logger.LogInformation("ℹ️ No active connection for user {UserId} to force logout.", userId);
                return;
            }

            _logger.LogInformation("⚡ Sending force logout to user {UserId} on connection {ConnectionId}.", userId, session.ConnectionId);

            try
            {
                await _hubContext.Clients.Client(session.ConnectionId)
                    .SendAsync("forceLogout");

                await MarkForceLogoutAsync(userId);

                _logger.LogInformation("✅ ForceLogout completed for user {UserId}.", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error while sending forceLogout to user {UserId}.", userId);
            }
        }
    }

    public interface IUserSessionService
    {
        Task<UserSessionDTO?> GetUserSessionAsync(string userId);
        Task SaveUserSessionAsync(UserSessionDTO dto);
        Task RemoveSessionAsync(string userId);
        Task UpdateConnectionIdAsync(string userId, string newConnectionId);
        Task MarkForceLogoutAsync(string userId);

        // ✅ Added for Force Logout
        Task ForceLogoutAsync(string userId);
    }
}
