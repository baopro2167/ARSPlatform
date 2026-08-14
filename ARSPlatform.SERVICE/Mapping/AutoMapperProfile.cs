using AutoMapper;
using ARSPlatform.MODEL.Entities;
using ARSPlatform.SERVICE.DTOs.Request;
using ARSPlatform.SERVICE.DTOs.Response;

namespace ARSPlatform.SERVICE.Mapping
{
    public class AutoMapperProfile : AutoMapper.Profile
    {
        public AutoMapperProfile()
        {
            // =========================
            // User Mapping
            // =========================

            CreateMap<User, UserResponse>()
                .ForMember(
                    dest => dest.Id,
                    opt => opt.MapFrom(src => src.UserId))
                .ForMember(
                    dest => dest.RoleName,
                    opt => opt.MapFrom(src =>
                        src.UserRoles != null &&
                        src.UserRoles.Any() &&
                        src.UserRoles.First().Role != null
                            ? src.UserRoles.First().Role!.Name
                            : string.Empty));

            CreateMap<RegisterRequest, User>()
                .ForMember(
                    dest => dest.PasswordHash,
                    opt => opt.Ignore())
                .ForMember(
                    dest => dest.UserRoles,
                    opt => opt.Ignore());


            // =========================
            // Paper Mapping
            // =========================

            CreateMap<Paper, PaperResponse>()
                .ForMember(
                    dest => dest.Id,
                    opt => opt.MapFrom(src => src.PaperId))
                .ForMember(
                    dest => dest.AuthorId,
                    opt => opt.MapFrom(src => src.CreatorId))
                .ForMember(
                    dest => dest.AuthorName,
                    opt => opt.MapFrom(src =>
                        src.Creator != null
                            ? src.Creator.FullName
                            : string.Empty));

            CreateMap<PaperCreateRequest, Paper>();

            CreateMap<PaperUpdateRequest, Paper>();


            // =========================
            // Seminar Mapping
            // =========================

            CreateMap<Seminar, SeminarResponse>()
                .ForMember(
                    dest => dest.Participants,
                    opt => opt.MapFrom(
                        src => src.SeminarParticipants));

            CreateMap<SeminarCreateRequest, Seminar>();

            CreateMap<SeminarUpdateRequest, Seminar>();

            CreateMap<SeminarParticipant, SeminarParticipantResponse>();

            CreateMap<SeminarParticipantCreateRequest, SeminarParticipant>();

            CreateMap<SeminarParticipantUpdateRequest, SeminarParticipant>();
        }
    }
}