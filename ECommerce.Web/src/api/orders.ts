import { apiClient } from "./client";
import type { PagedResponse } from "./customers";

export enum OrderStatus {
    Pending = 1,
    Confirmed = 2,
    Processing = 3,
    Packed = 4,
    Shipped = 5,
    Delivered = 6,
    Cancelled = 7
}

export const ORDER_STATUS_LABELS: Record<OrderStatus, string> = {
    [OrderStatus.Pending]: 'Pending',
    [OrderStatus.Confirmed]: 'Confirmed',
    [OrderStatus.Processing]: 'Processing',
    [OrderStatus.Packed]: 'Packed',
    [OrderStatus.Shipped]: 'Shipped',
    [OrderStatus.Delivered]: 'Delivered',
    [OrderStatus.Cancelled]: 'Cancelled',
};

export const ALLOWED_TRANSITIONS: Record<OrderStatus, OrderStatus[]> = {
    [OrderStatus.Pending]: [OrderStatus.Confirmed, OrderStatus.Cancelled],
    [OrderStatus.Confirmed]: [OrderStatus.Processing, OrderStatus.Cancelled],
    [OrderStatus.Processing]: [OrderStatus.Packed, OrderStatus.Cancelled],
    [OrderStatus.Packed]: [OrderStatus.Shipped, OrderStatus.Cancelled],
    [OrderStatus.Shipped]: [OrderStatus.Delivered],
    [OrderStatus.Delivered]: [],
    [OrderStatus.Cancelled]: [],
};

export interface ShippingAddress {
    street: string;
    city: string;
    state: string;
    zipCode: string;
    country: string;
}

export interface OrderItemResponse {
    productId: string;
    productName: string;
    productSku: string;
    quantity: number;
    unitPrice: number;
    lineTotal: number;
}

export interface OrderStatusHistory {
    fromStatus: OrderStatus;
    toStatus: OrderStatus;
    notes?: string;
    changedAt: string;
}

export interface OrderResponse {
    id: string;
    customerId: string;
    customerName: string;
    customerEmail: string;
    status: OrderStatus;
    statusLabel: string;
    subtotal: number;
    tax: number;
    total: number;
    shippingAddress: ShippingAddress;
    notes?: string;
    items: OrderItemResponse[];
    statusHistory: OrderStatusHistory[];
    createdAt: string;
    updatedAt: string;
}

export interface OrderQueryParams {
    customerId?: string;
    status?: number;
    from?: string;
    to?: string;
    page?: number;
    pageSize?: number;
}

export interface CreateOrderItemRequest {
    productId: string;
    quantity: number;
}

export interface CreateOrderRequest {
    customerId: string;
    items: CreateOrderItemRequest[];
    shippingAddress: ShippingAddress;
    notes?: string;
}

export const ordersApi = {
    getAll(params: OrderQueryParams = {}) {
        return apiClient.get<PagedResponse<OrderResponse>>('/orders', { params });
    },
    getById(id: string) {
        return apiClient.get<OrderResponse>(`/orders/${id}`);
    },
    create(data: CreateOrderRequest) {
        return apiClient.post<OrderResponse>('/orders', data)
    },
    updateStatus(id: string, newStatus: OrderStatus, notes?: string) {
        return apiClient.patch<OrderResponse>(`/orders/${id}/status`, { newStatus, notes });
    },
    cancel(id: string, reason?: string) {
        const params = reason ? `reason=${encodeURIComponent(reason)}` : '';
        return apiClient.delete(`/orders/${id}${params}`);
    },
};