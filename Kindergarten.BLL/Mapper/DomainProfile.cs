using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Kindergarten.BLL.Models.BranchDTO;
using Kindergarten.BLL.Models.KGBranchDTO;
using Kindergarten.BLL.Models.KindergartenDTO;
using Kindergarten.DAL.Entity;

namespace Kindergarten.BLL.Mapper
{
    public class DomainProfile : Profile
    {
        public DomainProfile()
        {
            #region Mapping configurations for Kindergarten and Branch entities
            CreateMap<KG, KindergartenDTO>().ReverseMap();
            CreateMap<KindergartenCreateDTO, KG>().ReverseMap();
            CreateMap<KindergartenUpdateDTO, KG>().ReverseMap();

            CreateMap<Branch, BranchDTO>().ReverseMap();
            CreateMap<BranchCreateDTO, Branch>()
                .ForMember(dest => dest.Kindergarten, opt => opt.Ignore());
            CreateMap<Branch, BranchCreateDTO>();  // العكس حسب الحاجة
            CreateMap<BranchUpdateDTO, Branch>().ReverseMap();
            #endregion

            #region Mapping KG + Branches into KGBranchDTO
            CreateMap<KG, KGBranchDTO>()
                .ForMember(dest => dest.Branches, opt => opt.MapFrom(src => src.Branches));

            CreateMap<KGBranchDTO, KG>()
                .ForMember(dest => dest.Branches, opt => opt.MapFrom(src => src.Branches));

            CreateMap<KG, KGBranchCreateDTO>().ReverseMap();
            CreateMap<KG, KGBranchUpdateDTO>().ReverseMap();
            #endregion
        }
    }
}
