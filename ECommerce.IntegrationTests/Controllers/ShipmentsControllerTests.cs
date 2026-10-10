using ECommerce.Application.DTOs.Request;
using ECommerce.Application.DTOs.Response;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.IntegrationTests.Common;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.IntegrationTests.Controllers
{
    public class ShipmentsControllerTests : IntegrationTestBase
    {
        public ShipmentsControllerTests(CustomWebApplicationFactory factory) : base(factory)
        {
        }

        private CreateShipmentRequest ValidRequest(Guid orderId) => new()
        {
            OrderId = orderId,
            Carrier = "FedEx",
            TrackingNumber = "FEDEX-123456",
            Notes = "Handle with care"
        };

        private async Task<ShipmentResponse> CreateShipmentFor(Guid orderId)
        {
            var response = await Client.PostAsJsonAsync(
            "/api/shipments", ValidRequest(orderId));

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            return (await response.Content
                .ReadFromJsonAsync<ShipmentResponse>())!;
        }

        [Fact]
        public async Task GetByOrder_ExistingShipment_Returns200WithShipmentDetails()
        {
            var (order, _) = await SeedPackedOrderAsync();
            await CreateShipmentFor(order.Id);

            var response = await Client.GetAsync(
                $"/api/shipments/order/{order.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<ShipmentResponse>();
            body!.OrderId.Should().Be(order.Id);
            body.Carrier.Should().Be("FedEx");
            body.TrackingNumber.Should().Be("FEDEX-123456");
            body.Status.Should().Be(ShipmentStatus.Created);
            body.ShippedAt.Should().NotBeNull();
            body.DeliveredAt.Should().BeNull();
        }

        [Fact]
        public async Task GetByOrder_NoShipmentExists_Returns404()
        {
            var (order, _) = await SeedPackedOrderAsync();

            var response = await Client.GetAsync(
                $"/api/shipments/order/{order.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetByOrder_NonExistentOrder_Returns404()
        {
            var response = await Client.GetAsync(
                $"/api/shipments/order/{Guid.NewGuid()}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Create_PackedOrder_Returns201AndTransitionsOrderToShipped()
        {
            var (order, _) = await SeedPackedOrderAsync();

            var response = await Client.PostAsJsonAsync(
                "/api/shipments", ValidRequest(order.Id));

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var body = await response.Content.ReadFromJsonAsync<ShipmentResponse>();
            body!.Carrier.Should().Be("FedEx");
            body.TrackingNumber.Should().Be("FEDEX-123456");
            body.Status.Should().Be(ShipmentStatus.Created);
            body.ShippedAt.Should().NotBeNull();

            var getOrderResponse = await Client.GetAsync($"/api/orders/{order.Id}");
            var updatedOrder = (await getOrderResponse.Content
                .ReadFromJsonAsync<OrderResponse>())!;
            updatedOrder!.Status.Should().Be(OrderStatus.Shipped);
        }

        [Fact]
        public async Task Create_OrderNotInPackedStatus_Returns400()
        {
            var (_, _, order) = await SeedOrderAsync(status: OrderStatus.Confirmed);

            var response = await Client.PostAsJsonAsync(
                "/api/shipments", ValidRequest(order.Id));

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var unchanged = await DbContext.Orders.FindAsync(order.Id);
            unchanged!.Status.Should().Be(OrderStatus.Confirmed);
        }

        [Fact]
        public async Task Create_DuplicateShipment_Returns409()
        {
            var (order, _) = await SeedPackedOrderAsync();
            await CreateShipmentFor(order.Id);

            var response = await Client.PostAsJsonAsync(
                "/api/shipments", ValidRequest(order.Id));

            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task Create_NonExistentOrder_Returns404()
        {
            var response = await Client.PostAsJsonAsync(
                "/api/shipments", ValidRequest(Guid.NewGuid()));

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Create_InvalidTrackingFormat_Returns400()
        {
            var (order, _) = await SeedPackedOrderAsync();
            var request = ValidRequest(order.Id);
            request.TrackingNumber = "lowercase-not-allowed";

            var response = await Client.PostAsJsonAsync("/api/shipments", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("errors");
        }

        [Fact]
        public async Task Create_EmptyCarrier_Returns400()
        {
            var (order, _) = await SeedPackedOrderAsync();
            var request = ValidRequest(order.Id);
            request.Carrier = "";

            var response = await Client.PostAsJsonAsync("/api/shipments", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Create_AddsShippedStatusHistoryEntry()
        {
            var (order, _) = await SeedPackedOrderAsync();
            await CreateShipmentFor(order.Id);

            var history = DbContext.OrderStatusHistories
                .Where(h => h.OrderId ==  order.Id)
                .ToList();

            history.Should().ContainSingle();
            history[0].FromStatus.Should().Be(OrderStatus.Packed);
            history[0].ToStatus.Should().Be(OrderStatus.Shipped);
            history[0].Notes.Should().Contain("FedEx");
            history[0].Notes.Should().Contain("FEDEX-123456");
        }

        [Fact]
        public async Task Update_ActiveShipment_Returns200WithUpdatedDetails()
        {
            var (order, _) = await SeedPackedOrderAsync();
            var shipment = await CreateShipmentFor(order.Id);

            var response = await Client.PutAsJsonAsync(
                $"/api/shipments/{shipment.Id}",
                new UpdateShipmentRequest
                {
                    Carrier = "UPS",
                    TrackingNumber = "UPS-987654",
                    Notes = "Updated notes"
                });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<ShipmentResponse>();
            body!.Carrier.Should().Be("UPS");
            body.TrackingNumber.Should().Be("UPS-987654");
            body.Notes.Should().Be("Updated notes");
        }

        [Fact]
        public async Task Update_TrimsCarrierAndTrackingWhitespace()
        {
            var (order, _) = await SeedPackedOrderAsync();
            var shipment = await CreateShipmentFor(order.Id);

            var response = await Client.PutAsJsonAsync(
                $"/api/shipments/{shipment.Id}",
                new UpdateShipmentRequest
                {
                    Carrier = "  DHL  ",
                    TrackingNumber = "DHL-001"
                });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadFromJsonAsync<ShipmentResponse>();
            body!.Carrier.Should().Be("DHL");
        }

        [Fact]
        public async Task Update_DeliveredShipment_Returns400()
        {
            var (order, _) = await SeedPackedOrderAsync();
            var shipment = await CreateShipmentFor(order.Id);

            await Client.PatchAsync(
                $"/api/shipments/{shipment.Id}/delivered", null);

            var response = await Client.PutAsJsonAsync(
                $"/api/shipments/{shipment.Id}",
                new UpdateShipmentRequest
                {
                    Carrier = "UPS",
                    TrackingNumber = "UPS-000"
                });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Update_NonExistentShipment_Returns404()
        {
            var response = await Client.PutAsJsonAsync(
                $"/api/shipments/{Guid.NewGuid()}",
                new UpdateShipmentRequest
                {
                    Carrier = "FedEx",
                    TrackingNumber = "FEDEX-001"
                });

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task MarkDelivered_ActiveShipment_Returns200AndTransitionsOrder()
        {
            var (order, _) = await SeedPackedOrderAsync();
            var shipment = await CreateShipmentFor(order.Id);

            var response = await Client.PatchAsync(
                $"/api/shipments/{shipment.Id}/delivered", null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadFromJsonAsync<ShipmentResponse>();
            body!.Status.Should().Be(ShipmentStatus.Delivered);
            body.DeliveredAt.Should().NotBeNull();

            var getOrderResponse = await Client.GetAsync($"/api/orders/{order.Id}");
            var updatedOrder = (await getOrderResponse.Content
                .ReadFromJsonAsync<OrderResponse>())!;
            updatedOrder.Status.Should().Be(OrderStatus.Delivered);
        }

        [Fact]
        public async Task MarkDelivered_SetsDeliveredAtTimestamp()
        {
            var (order, _) = await SeedPackedOrderAsync();
            var shipment = await CreateShipmentFor(order.Id);

            var before = DateTime.UtcNow;
            var response = await Client.PatchAsync(
                $"/api/shipments/{shipment.Id}/delivered", null);
            var after = DateTime.UtcNow;

            var body = await response.Content.ReadFromJsonAsync<ShipmentResponse>();
            body!.DeliveredAt.Should().NotBeNull();
            body.DeliveredAt!.Value.Should().BeOnOrAfter(before);
            body.DeliveredAt!.Value.Should().BeOnOrBefore(after);
        }

        [Fact]
        public async Task MarkDelivered_AddsDeliveredStatusHistoryEntry()
        {
            var (order, _) = await SeedPackedOrderAsync();
            var shipment = await CreateShipmentFor(order.Id);

            await Client.PatchAsync(
                $"/api/shipments/{shipment.Id}/delivered", null);

            var history = DbContext.OrderStatusHistories
                .Where(h => h.OrderId == order.Id)
                .OrderBy(h => h.ChangedAt)
                .ToList();

            history.Should().HaveCount(2);
            history[0].FromStatus.Should().Be(OrderStatus.Packed);
            history[0].ToStatus.Should().Be(OrderStatus.Shipped);
            history[1].FromStatus.Should().Be(OrderStatus.Shipped);
            history[1].ToStatus.Should().Be(OrderStatus.Delivered);
        }

        [Fact]
        public async Task MarkDelivered_AlreadyDelivered_Returns400()
        {
            var (order, _) = await SeedPackedOrderAsync();
            var response = await Client.PostAsJsonAsync(
                "/api/shipments", ValidRequest(order.Id));

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var shipment = (await response.Content
                .ReadFromJsonAsync<ShipmentResponse>())!;

            var getOrderResponse = await Client.GetAsync($"/api/orders/{order.Id}");
            var shippedOrder = (await getOrderResponse.Content
                .ReadFromJsonAsync<OrderResponse>())!;
            shippedOrder.Status.Should().Be(OrderStatus.Shipped);

            var deliverResponse = await Client.PatchAsync(
                $"api/shipments/{shipment.Id}/delivered", null);

            deliverResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var deliveredShipment = (await deliverResponse.Content
                .ReadFromJsonAsync<ShipmentResponse>())!;
            deliveredShipment.Status.Should().Be(ShipmentStatus.Delivered);
            deliveredShipment.DeliveredAt.Should().NotBeNull();

            var getFinalResponse = await Client.GetAsync($"/api/orders/{order.Id}");
            var deliveredOrder = (await getFinalResponse.Content
                .ReadFromJsonAsync<OrderResponse>())!;
            deliveredOrder.Status.Should().Be(OrderStatus.Delivered);
        }

        [Fact]
        public async Task MarkDelivered_NonExistentShipment_Returns404()
        {
            var response = await Client.PatchAsync(
                $"/api/shipments/{Guid.NewGuid()}/delivered", null);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task FullShipmentLifecycle_PackedToDelivered_CompletesCorrectly()
        {
            var (order, _) = await SeedPackedOrderAsync();

            var createResponse = await Client.PostAsJsonAsync(
                "/api/shipments", ValidRequest(order.Id));
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var shipment = (await createResponse.Content
                .ReadFromJsonAsync<ShipmentResponse>())!;

            var getShippedResponse = await Client.GetAsync($"/api/orders/{order.Id}");
            var shippedOrder = (await getShippedResponse.Content
                .ReadFromJsonAsync<OrderResponse>())!;
            shippedOrder.Status.Should().Be(OrderStatus.Shipped);

            var updateResponse = await Client.PutAsJsonAsync(
                $"/api/shipments/{shipment.Id}",
                new UpdateShipmentRequest
                {
                    Carrier = "DHL",
                    TrackingNumber = "DHL-EXPRESS-001",
                    Notes = "Out for delivery"
                });
            updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var deliveredResponse = await Client.PatchAsync(
                $"/api/shipments/{shipment.Id}/delivered", null);
            deliveredResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var getShipmentResponse = await Client.GetAsync(
                $"/api/shipments/order/{order.Id}");
            var finalShipment = (await getShipmentResponse.Content
                .ReadFromJsonAsync<ShipmentResponse>())!;

            finalShipment.Carrier.Should().Be("DHL");
            finalShipment.TrackingNumber.Should().Be("DHL-EXPRESS-001");
            finalShipment.Status.Should().Be(ShipmentStatus.Delivered);
            finalShipment.ShippedAt.Should().NotBeNull();
            finalShipment.DeliveredAt.Should().NotBeNull();

            var getFinalOrderResponse = await Client.GetAsync($"/api/orders/{order.Id}");
            var finalOrder = (await getFinalOrderResponse.Content
                .ReadFromJsonAsync<OrderResponse>())!;
            finalOrder!.Status.Should().Be(OrderStatus.Delivered);
        }
    }
}
