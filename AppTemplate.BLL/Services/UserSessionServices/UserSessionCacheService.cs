using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AppTemplate.BLL.Models;
using Microsoft.Extensions.Caching.Distributed;

namespace AppTemplate.BLL.Services.UserSessionServices
{
    public class UserSessionCacheService : IUserSessionCacheService
    {
        private readonly IDistributedCache _cache;
        private readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        public UserSessionCacheService(IDistributedCache cache)
        {
            _cache = cache;
        }

        private string GetKey(string userId) => $"UserSession:{userId}";

        public async Task<UserSessionDTO?> GetUserSessionAsync(string userId)
        {
            var data = await _cache.GetStringAsync(GetKey(userId));
            if (data == null)
                return null;

            try
            {
                return JsonSerializer.Deserialize<UserSessionDTO>(data, _serializerOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error deserializing UserSession for {userId}: {ex.Message}");
                return null;
            }
        }

        public async Task SetUserSessionAsync(string userId, UserSessionDTO session, TimeSpan expiry)
        {
            var json = JsonSerializer.Serialize(session, _serializerOptions);
            await _cache.SetStringAsync(GetKey(userId), json,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiry
                });
        }

        public async Task RemoveUserSessionAsync(string userId)
        {
            await _cache.RemoveAsync(GetKey(userId));
        }
    }
    public interface IUserSessionCacheService
    {
        Task<UserSessionDTO?> GetUserSessionAsync(string userId);

        Task SetUserSessionAsync(string userId, UserSessionDTO session, TimeSpan expiry);

        Task RemoveUserSessionAsync(string userId);
    }

}
