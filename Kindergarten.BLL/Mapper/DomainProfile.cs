using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Kindergarten.BLL.Models.BranchDTO;
using Kindergarten.BLL.Models.KindergartenDTO;
using Kindergarten.BLL.Models.UsersManagementDTO;
using Kindergarten.DAL.Entity;

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

            #endregion
        }
    }
}
