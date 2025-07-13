using AppTemplate.DAL.Database;
using AppTemplate.DAL.Entity;
using Microsoft.EntityFrameworkCore;

public class UserSessionRepository : IUserSessionRepository
{
    private readonly ApplicationContext _context;

    public UserSessionRepository(ApplicationContext context)
    {
        _context = context;
    }

    public async Task<UserSession?> GetByUserIdAsync(string userId)
    {
        return await _context.UserSessions
            .FirstOrDefaultAsync(s => s.UserId == userId);
    }

    public async Task AddAsync(UserSession session)
    {
        _context.UserSessions.Add(session);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveByUserIdAsync(string userId)
    {
        var session = await _context.UserSessions
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (session != null)
        {
            _context.UserSessions.Remove(session);
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdateAsync(UserSession session)
    {
        _context.UserSessions.Update(session);
        await _context.SaveChangesAsync();
    }

    // ✅ New method for SessionCleanup
    public async Task<List<UserSession>> GetAllSessionsAsync()
    {
        return await _context.UserSessions.ToListAsync();
    }
}

public interface IUserSessionRepository
{
    Task<UserSession?> GetByUserIdAsync(string userId);
    Task AddAsync(UserSession session);
    Task RemoveByUserIdAsync(string userId);
    Task UpdateAsync(UserSession session);

    // ✅ Interface method
    Task<List<UserSession>> GetAllSessionsAsync();
}
