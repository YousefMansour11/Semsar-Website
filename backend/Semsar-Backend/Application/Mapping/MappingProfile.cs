using AutoMapper;
using Application.DTOs;
using Domain.Entities;
using Domain.Enums;

namespace Application.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Lead DTO ↔ Lead entity
            CreateMap<LeadDto, Lead>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Property, opt => opt.Ignore());

            // Property mapping (contact handled manually in service)
            CreateMap<CreatePropertyDto, Property>()
                .ForMember(dest => dest.ListingType, opt => opt.MapFrom(src => src.ListingType))
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Contact, opt => opt.Ignore())
                .ForMember(dest => dest.Code, opt => opt.Ignore())
                .ForMember(dest => dest.Images, opt => opt.Ignore())
                .ForMember(dest => dest.TitleEn, opt => opt.MapFrom(src => src.TitleEn))
                .ForMember(dest => dest.TitleAr, opt => opt.MapFrom(src => src.TitleAr))
                .ForMember(dest => dest.DescriptionEn, opt => opt.MapFrom(src => src.DescriptionEn))
                .ForMember(dest => dest.DescriptionAr, opt => opt.MapFrom(src => src.DescriptionAr))
                .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Price))
                .ForMember(dest => dest.Location, opt => opt.MapFrom(src => src.Location));
        }

        // No custom parsing needed: DTO already uses PropertyListingType enum
    }
}
