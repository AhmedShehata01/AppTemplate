using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Kindergarten.BLL.Models.ActivityLogDTO;
using Kindergarten.BLL.Models.BranchDTO;
using Kindergarten.BLL.Models.DRBRADTO;
using Kindergarten.BLL.Models.KindergartenDTO;
using Kindergarten.BLL.Models.RoleManagementDTO;
using Kindergarten.BLL.Models.UserManagementDTO;
using Kindergarten.BLL.Models.UserProfileDTO;
using Kindergarten.DAL.Entity;
using Kindergarten.DAL.Entity.DRBRA;
using Kindergarten.DAL.Extend;

namespace Kindergarten.BLL.Mapper
{
    public class DomainProfile : Profile
    {
        public DomainProfile()
        {
            #region Kindergarten Mappings

            // Entity → DTO مع الفروع النشطة فقط
            CreateMap<KG, KindergartenDTO>()
                .ForMember(dest => dest.Branches,
                           opt => opt.MapFrom(src => src.Branches.Where(b => b.IsDeleted == false)));

            CreateMap<KG, KindergartenFullDTO>()
               .ForMember(dest => dest.Branches,
                          opt => opt.MapFrom(src => src.Branches));

            // Create DTO → Entity
            CreateMap<KindergartenCreateDTO, KG>()
                .ForMember(dest => dest.KGCode, opt => opt.Ignore()) // نولد الكود يدويًا في الباك اند
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore()) // نعطيها يدويًا في الخدمة
                .ForMember(dest => dest.CreatedOn, opt => opt.Ignore());

            // Update DTO → Entity
            //CreateMap<KindergartenUpdateDTO, KG>().ReverseMap();
            CreateMap<KindergartenUpdateDTO, KG>()
                // إذا عندك حقول تتبع التعديل اضف تجاهلها هنا
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedOn, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedOn, opt => opt.Ignore());

            #endregion

            #region Branch Mappings

            CreateMap<Branch, BranchDTO>().ReverseMap();
            CreateMap<Branch, BranchCreateDTO>().ReverseMap();

            CreateMap<Branch, BranchUpdateDTO>();

            CreateMap<BranchUpdateDTO, Branch>()
                .ForMember(dest => dest.Kindergarten, opt => opt.Ignore()) // نتجنب ربط الـ Navigation Property بشكل تلقائي
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore()) // نعطيها يدويًا
                .ForMember(dest => dest.CreatedOn, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedOn, opt => opt.Ignore());

            #endregion

            #region Mapping for User Profile
            CreateMap<CompleteBasicProfileDTO, UserBasicProfile>()
                .ForMember(dest => dest.UserId, opt => opt.Ignore()) // نعطيها يدويًا
                .ForMember(dest => dest.Status, opt => opt.Ignore()) // نعطيها يدويًا
                .ForMember(dest => dest.SubmittedAt, opt => opt.Ignore()); // نعطيها يدويًا

            // العكس من الـ Entity إلى DTO
            CreateMap<UserBasicProfile, CompleteBasicProfileDTO>();
            CreateMap<GetUsersProfilesDTO, UserBasicProfile>().ReverseMap();

            CreateMap<ApplicationUser, UserListDTO>();
            #endregion

            #region DRBRA Mappings
            // Create => Entity
            CreateMap<CreateSecuredRouteDTO, SecuredRoute>()
                .ForMember(dest => dest.RoleSecuredRoutes, opt => opt.Ignore()); // لأنها هتتعمل يدويًا

            // Update => Entity
            CreateMap<UpdateSecuredRouteDTO, SecuredRoute>()
                .ForMember(dest => dest.Id, opt => opt.Ignore()); // عشان ما نعدلش الـ Id بالغلط

            // Entity => DTO (عرض المسار مع الرولز)
            CreateMap<SecuredRoute, SecuredRouteDTO>()
                .ForMember(dest => dest.CreatedByName, opt => opt.MapFrom(src => src.CreatedBy.UserName))
                .ForMember(dest => dest.AssignedRoles, opt => opt.MapFrom(src =>
                    src.RoleSecuredRoutes.Select(r => r.Role.Name).ToList()
                ));

            // Entity => RouteWithRolesDTO
            CreateMap<SecuredRoute, RouteWithRolesDTO>()
                .ForMember(dest => dest.RouteId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.BasePath, opt => opt.MapFrom(src => src.BasePath))
                .ForMember(dest => dest.Roles, opt => opt.MapFrom(src => src.RoleSecuredRoutes.Select(r => r.Role.Name)));


            #endregion

            #region Role Management Mappings

            // Entity → DTO
            CreateMap<ApplicationRole, ApplicationRoleDTO>();

            // Create DTO → Entity
            CreateMap<CreateRoleDTO, ApplicationRole>()
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.IsExternal, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.CreatedOn, opt => opt.MapFrom(src => DateTime.Now))
                .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => false));

            // Update DTO → لا نربطه مباشرة لأنه هيتم التعديل يدوي في الخدمة
            // Toggle DTO → نفس الفكرة، نعدل القيمة يدوي

            // Entity → RoleWithRoutesDTO
            CreateMap<ApplicationRole, RoleWithRoutesDTO>()
                .ForMember(dest => dest.RoleId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.AllowedRoutes, opt => opt.MapFrom(src =>
                    src.RoleSecuredRoutes.Select(rr => rr.SecuredRoute.BasePath).ToList()
                ));

            // Entity → DropdownRoleDTO
            CreateMap<ApplicationRole, DropdownRoleDTO>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name));


            #endregion

            #region User Management Mappings

            // Entity → DTO
            CreateMap<ApplicationUser, ApplicationUserDTO>()
                .ForMember(dest => dest.IsAgree, opt => opt.MapFrom(src => !src.LockoutEnabled));

            // Create DTO → Entity
            CreateMap<CreateApplicationUserDTO, ApplicationUser>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.EmailConfirmed, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.IsAgree, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.CreatedOn, opt => opt.MapFrom(src => DateTime.Now));

            // Update DTO → Entity
            CreateMap<UpdateApplicationUserDTO, ApplicationUser>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.UserName))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
                .ForMember(dest => dest.LockoutEnabled, opt => opt.MapFrom(src => !src.IsAgree));

            #endregion

            #region Sidebar Items and sub items 
            CreateMap<SidebarItem, SidebarItemDTO>()
                .ForMember(dest => dest.Children, opt => opt.MapFrom(src => src.Children.OrderBy(c => c.Order)));

            CreateMap<CreateSidebarItemDTO, SidebarItem>();
            CreateMap<UpdateSidebarItemDTO, SidebarItem>();
            #endregion

            #region Activity Log Mappings

            CreateMap<ActivityLog, ActivityLogDTO>()
                .ForMember(dest => dest.PerformedByUserName,
                           opt => opt.MapFrom(src => src.PerformedByUser != null
                                ? src.PerformedByUser.UserName
                                : src.PerformedByUserName));

            CreateMap<ActivityLog, ActivityLogViewDTO>()
                .ForMember(dest => dest.PerformedByUserName,
                           opt => opt.MapFrom(src => src.PerformedByUser != null
                                ? src.PerformedByUser.UserName
                                : src.PerformedByUserName))
                .ForMember(dest => dest.ActionType,
                    opt => opt.MapFrom(src => src.ActionType.ToString()));


            CreateMap<ActivityLogCreateDTO, ActivityLog>()
                .ForMember(dest => dest.PerformedAt, opt => opt.Ignore());

            #endregion

        }
    }
}
