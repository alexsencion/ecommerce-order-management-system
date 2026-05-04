using AutoMapper;
using ECommerce.Application.DTOs.Request;
using ECommerce.Application.DTOs.Response;
using ECommerce.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ECommerce.Application.Mappings
{
    public class CustomerProfile : Profile
    {
        public CustomerProfile()
        {
            CreateMap<Customer, CustomerResponse>()
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => 
                    src.AddressJson != null 
                        ? JsonSerializer.Deserialize<AddressResponse>(src.AddressJson, (JsonSerializerOptions?)null)
                        : null));

            CreateMap<CreateCustomerRequest, Customer>()
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email.ToLower().Trim()))
                .ForMember(dest => dest.AddressJson, opt => opt.MapFrom(src => 
                    src.Address != null
                        ? JsonSerializer.Serialize(src.Address, (JsonSerializerOptions?)null)
                        : null));

            CreateMap<UpdateCustomerRequest, Customer>()
                .ForMember(dest => dest.AddressJson, opt => opt.MapFrom(src =>
                    src.Address != null
                        ? JsonSerializer.Serialize(src.Address, (JsonSerializerOptions?)null)
                        : null))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Email, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Orders, opt => opt.Ignore());

            CreateMap<AddressRequest, AddressResponse>();
        }
    }
}
