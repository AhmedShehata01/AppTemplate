using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Kindergarten.BLL.Models;
using Kindergarten.BLL.Models.DRBRADTO;
using Kindergarten.DAL.Database;
using Kindergarten.DAL.Entity.DRBRA;
using Microsoft.EntityFrameworkCore;

namespace Kindergarten.BLL.Services
{
    public class SidebarService : ISidebarService
    {
        #region prop
        private readonly ApplicationContext _db;
        private readonly IMapper _mapper;
        #endregion

        #region CTOR
        public SidebarService(ApplicationContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }
        #endregion

        #region Actions 
        public async Task<PagedResult<SidebarItemDTO>> GetPagedAsync(PaginationFilter filter)
        {
            var query = _db.SidebarItem
                .Where(x => !x.IsDeleted && x.ParentId == null)
                .Include(x => x.Children.Where(c => !c.IsDeleted))
                .AsQueryable();

            // 🔍 Search
            if (!string.IsNullOrWhiteSpace(filter.SearchText))
            {
                var search = filter.SearchText.Trim().ToLower();
                query = query.Where(x => 
                        x.LabelAr.ToLower().Contains(search) ||
                        x.LabelEn.ToLower().Contains(search));
            }

            // 🔃 Sorting
            if (!string.IsNullOrWhiteSpace(filter.SortBy))
            {
                var isDesc = filter.SortDirection?.ToLower() == "desc";

                switch (filter.SortBy.ToLower())
                {
                    case "labelar":
                        query = isDesc ? query.OrderByDescending(x => x.LabelAr) : query.OrderBy(x => x.LabelAr);
                        break;

                    case "labelen":
                        query = isDesc ? query.OrderByDescending(x => x.LabelEn) : query.OrderBy(x => x.LabelEn);
                        break;

                    case "route":
                        query = isDesc ? query.OrderByDescending(x => x.Route) : query.OrderBy(x => x.Route);
                        break;

                    case "icon":
                        query = isDesc ? query.OrderByDescending(x => x.Icon) : query.OrderBy(x => x.Icon);
                        break;

                    case "order":
                        query = isDesc ? query.OrderByDescending(x => x.Order) : query.OrderBy(x => x.Order);
                        break;
                    default:
                        query = query.OrderBy(x => x.Order); // default
                        break;
                }
            }
            else
            {
                query = query.OrderBy(x => x.Order);
            }

            // 📊 Pagination
            var totalCount = await query.CountAsync();
            var data = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var result = new PagedResult<SidebarItemDTO>
            {
                Data = _mapper.Map<List<SidebarItemDTO>>(data),
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };

            return result;
        }


        public async Task<SidebarItemDTO?> GetByIdAsync(int id)
        {
            var item = await _db.SidebarItem
                .Include(x => x.Children.Where(c => !c.IsDeleted))
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            return item == null ? null : _mapper.Map<SidebarItemDTO>(item);
        }

        public async Task<int> CreateAsync(CreateSidebarItemDTO dto)
        {
            var entity = _mapper.Map<SidebarItem>(dto);
            await _db.SidebarItem.AddAsync(entity);
            await _db.SaveChangesAsync();
            return entity.Id;
        }

        public async Task<bool> UpdateAsync(UpdateSidebarItemDTO dto)
        {
            var item = await _db.SidebarItem.FindAsync(dto.Id);
            if (item == null || item.IsDeleted)
                return false;

            _mapper.Map(dto, item);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SoftDeleteAsync(int id)
        {
            var item = await _db.SidebarItem
                .Include(x => x.Children)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (item == null || item.IsDeleted)
                return false;

            item.IsDeleted = true;

            foreach (var child in item.Children)
            {
                child.IsDeleted = true;
            }

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<List<SidebarItemDTO>> GetParentItemsAsync()
        {
            var parents = await _db.SidebarItem
                .Where(x => x.ParentId == null && !x.IsDeleted)
                .OrderBy(x => x.Order)
                .ToListAsync();

            return _mapper.Map<List<SidebarItemDTO>>(parents);
        }

        #endregion
    }

    public interface ISidebarService
    {
        Task<PagedResult<SidebarItemDTO>> GetPagedAsync(PaginationFilter filter);
        Task<SidebarItemDTO?> GetByIdAsync(int id);
        Task<int> CreateAsync(CreateSidebarItemDTO dto);
        Task<bool> UpdateAsync(UpdateSidebarItemDTO dto);
        Task<bool> SoftDeleteAsync(int id);
        Task<List<SidebarItemDTO>> GetParentItemsAsync();
    }

}
