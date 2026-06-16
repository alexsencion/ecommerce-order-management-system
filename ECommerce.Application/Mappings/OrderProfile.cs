using AutoMapper;
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
    public class OrderProfile : Profile
    {
        public OrderProfile()
        {
            CreateMap<Order, OrderResponse>()
                .ForMember(dest => dest.CustomerName,
                    opt => opt.MapFrom(src =>
                        src.Customer != null ? src.Customer.FullName : string.Empty))
                .ForMember(dest => dest.CustomerEmail,
                    opt => opt.MapFrom(src =>
                        src.Customer != null ? src.Customer.Email : string.Empty))
                .ForMember(dest => dest.ShippingAddress,
                    opt => opt.MapFrom(src =>
                        src.ShippingAddressJson != null
                            ? JsonSerializer.Deserialize<ShippingAddressDto>(
                                src.ShippingAddressJson,
                                (JsonSerializerOptions?)null)
                              ?? new ShippingAddressDto()
                            : new ShippingAddressDto()))
                .ForMember(dest => dest.Items,
                    opt => opt.MapFrom(src => src.Items))
                .ForMember(dest => dest.StatusHistory,
                    opt => opt.MapFrom(src =>
                        src.StatusHistory.OrderBy(h => h.ChangedAt)));

            CreateMap<OrderItem, OrderItemResponse>()
                .ForMember(dest => dest.ProductName,
                    opt => opt.MapFrom(src => 
                        src.Product != null ? src.Product.Name : string.Empty))
                .ForMember(dest => dest.ProductSku,
                    opt => opt.MapFrom(src =>
                        src.Product != null ? src.Product.Sku : string.Empty))
                .ForMember(dest => dest.LineTotal, 
                    opt => opt.MapFrom(src => src.LineTotal));

            CreateMap<OrderStatusHistory, OrderStatusHistoryResponse>();
        }
    }
}
