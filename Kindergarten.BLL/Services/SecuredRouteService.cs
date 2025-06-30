using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Kindergarten.BLL.Extensions;
using Kindergarten.BLL.Models;
using Kindergarten.BLL.Models.DRBRADTO;
using Kindergarten.DAL.Database;
using Kindergarten.DAL.Entity.DRBRA;
using Microsoft.EntityFrameworkCore;

namespace Kindergarten.BLL.Services
{
    public class SecuredRouteService : ISecuredRouteService
    {
        #region Prop
        private readonly ApplicationContext _context;
        private readonly IMapper _mapper;

        #endregion

        #region CTOR
        public SecuredRouteService(ApplicationContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        #endregion

        #region Actions 
        public async Task<PagedResult<SecuredRouteDTO>> GetAllRoutesAsync(PaginationFilter filter)
        {
            var searchText = filter.SearchText?.Trim().ToLower();

            // 📦 بداية الاستعلام من DbSet
            var query = _context.SecuredRoutes
                .Include(r => r.CreatedBy) // لو عايز اسم المستخدم
                .Include(r => r.RoleSecuredRoutes)
                    .ThenInclude(rr => rr.Role)
                .Where(r =>
                    string.IsNullOrEmpty(searchText) ||
                    r.BasePath.ToLower().Contains(searchText) ||
                    r.Description.ToLower().Contains(searchText) ||
                    r.CreatedBy.FullName.ToLower().Contains(searchText)
                );

            // 🌀 ترتيب ديناميكي باستخدام ApplySorting
            query = query.ApplySorting(filter.SortBy, filter.SortDirection);

            // 🔢 إجمالي العناصر
            var totalCount = await query.CountAsync();

            // 📄 تطبيق Pagination
            var pagedData = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .AsNoTracking()
                .ToListAsync();

            // 🗺️ تحويل إلى DTO
            var mapped = _mapper.Map<List<SecuredRouteDTO>>(pagedData);

            // 📤 النتيجة النهائية
            return new PagedResult<SecuredRouteDTO>
            {
                Data = mapped,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }

        public async Task<SecuredRouteDTO> GetRouteByIdAsync(int id)
        {
            var route = await _context.SecuredRoutes
                .Include(r => r.RoleSecuredRoutes)
                    .ThenInclude(rr => rr.Role)
                .Include(r => r.CreatedBy)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (route == null) return null;
            return _mapper.Map<SecuredRouteDTO>(route);
        }

        public async Task<int> CreateRouteAsync(CreateSecuredRouteDTO dto, string createdById)
        {
            var route = new SecuredRoute
            {
                BasePath = dto.BasePath,
                Description = dto.Description,
                CreatedById = createdById,
                CreatedOn = DateTime.Now,
                RoleSecuredRoutes = dto.RoleIds?.Select(roleId => new RoleSecuredRoute
                {
                    RoleId = roleId
                }).ToList()
            };

            _context.SecuredRoutes.Add(route);
            await _context.SaveChangesAsync();
            return route.Id;
        }

        public async Task<bool> UpdateRouteAsync(UpdateSecuredRouteDTO dto)
        {
            var existing = await _context.SecuredRoutes
                .Include(r => r.RoleSecuredRoutes)
                .FirstOrDefaultAsync(r => r.Id == dto.Id);

            if (existing == null) return false;

            // تأكد مفيش تعارض في BasePath
            if (await _context.SecuredRoutes.AnyAsync(r => r.Id != dto.Id && r.BasePath == dto.BasePath))
                throw new Exception("Another route already uses the same base path.");

            // ✏️ تحديث البيانات
            existing.BasePath = dto.BasePath;
            existing.Description = dto.Description;

            // 🔄 تحديث الأدوار: حذف القديمة وإضافة الجديدة
            _context.RoleSecuredRoutes.RemoveRange(existing.RoleSecuredRoutes);

            if (dto.RoleIds != null && dto.RoleIds.Any())
            {
                existing.RoleSecuredRoutes = dto.RoleIds.Select(roleId => new RoleSecuredRoute
                {
                    RoleId = roleId,
                    SecuredRouteId = existing.Id
                }).ToList();
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteRouteAsync(int id)
        {
            var entity = await _context.SecuredRoutes
                .Include(r => r.RoleSecuredRoutes)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (entity == null) return false;

            _context.RoleSecuredRoutes.RemoveRange(entity.RoleSecuredRoutes);
            _context.SecuredRoutes.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AssignRolesAsync(AssignRolesToRouteDTO dto)
        {
            var route = await _context.SecuredRoutes
                .Include(r => r.RoleSecuredRoutes)
                .FirstOrDefaultAsync(r => r.Id == dto.SecuredRouteId);
            if (route == null) return false;

            var existingRoleIds = route.RoleSecuredRoutes.Select(r => r.RoleId).ToHashSet();
            var newRoleIds = dto.RoleIds.Except(existingRoleIds);

            foreach (var roleId in newRoleIds)
            {
                route.RoleSecuredRoutes.Add(new RoleSecuredRoute
                {
                    SecuredRouteId = dto.SecuredRouteId,
                    RoleId = roleId
                });
            }

            if (!newRoleIds.Any())
                return true;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UnassignRoleAsync(UnassignRoleFromRouteDTO dto)
        {
            var record = await _context.RoleSecuredRoutes
                .FirstOrDefaultAsync(r => r.SecuredRouteId == dto.SecuredRouteId && r.RoleId == dto.RoleId);
            if (record == null) return false;

            _context.RoleSecuredRoutes.Remove(record);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<RouteWithRolesDTO>> GetRoutesWithRolesAsync()
        {
            var routes = await _context.SecuredRoutes
                .Include(r => r.RoleSecuredRoutes)
                    .ThenInclude(rr => rr.Role)
                .ToListAsync();

            return _mapper.Map<List<RouteWithRolesDTO>>(routes);
        }

        public async Task<bool> IsRoleAssignedToAnySecuredRouteAsync(string roleId)
        {
            return await _context.RoleSecuredRoutes
                                 .AnyAsync(r => r.RoleId == roleId);
        }

        #endregion


    }



    public interface ISecuredRouteService
    {
        Task<PagedResult<SecuredRouteDTO>> GetAllRoutesAsync(PaginationFilter filter);
        Task<SecuredRouteDTO> GetRouteByIdAsync(int id);
        Task<int> CreateRouteAsync(CreateSecuredRouteDTO dto, string createdById);
        Task<bool> UpdateRouteAsync(UpdateSecuredRouteDTO dto);
        Task<bool> DeleteRouteAsync(int id);
        Task<bool> AssignRolesAsync(AssignRolesToRouteDTO dto);
        Task<bool> UnassignRoleAsync(UnassignRoleFromRouteDTO dto);
        Task<List<RouteWithRolesDTO>> GetRoutesWithRolesAsync();
        Task<bool> IsRoleAssignedToAnySecuredRouteAsync(string roleId);

    }
}
