using AutoMapper;
using LMPWebService.Models;
using LMPWebService.DTO;

namespace LMPWebService.Profiles
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<OuterMessage, CreateLeadDataDto>().ReverseMap();

            CreateMap<CreateLeadDataDto, OuterMessage>();
        }
    }
}
