using AutoMapper;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Mapping
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // User Mapping
            CreateMap<User, UserResponse>()
                .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role != null ? src.Role.Name : string.Empty));
            CreateMap<RegisterRequest, User>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.Role, opt => opt.Ignore());

            // Paper Mapping
            CreateMap<Paper, PaperResponse>()
                .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src => src.Author != null ? src.Author.FullName : string.Empty));
            CreateMap<PaperCreateRequest, Paper>();
            CreateMap<PaperUpdateRequest, Paper>();
        }
    }
}
