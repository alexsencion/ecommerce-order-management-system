import { apiClient } from "./client";

export enum ShipmentStatus {
    Created = 1,
    InTransit = 2,
    Delivered = 3,
    Failed = 4,
}

export const SHIPMENT_STATUS_LABELS: Record<ShipmentStatus, string> = {
    [ShipmentStatus.Created]: 'Created',
    [ShipmentStatus.InTransit]: 'In Transit',
    [ShipmentStatus.Delivered]: 'Delivered',
    [ShipmentStatus.Failed]: 'Failed',
};

export const SHIPMENT_STATUS_BADGE: Record<ShipmentStatus, string> = {
    [ShipmentStatus.Created]: 'bg-secondary-subtle text-secondary-emphasis',
    [ShipmentStatus.InTransit]: 'bg-info-subtle text-info-emphasis',
    [ShipmentStatus.Delivered]: 'bg-success-subtle text-success-emphasis',
    [ShipmentStatus.Failed]: 'bg-danger-subtle text-danger-emphasis',
};

export interface ShipmentResponse {
    id: string;
    orderId: string;
    carrier: string;
    trackingNumber: string;
    status: ShipmentStatus;
    statusLabel: string;
    notes?: string;
    shippedAt?: string;
    deliveredAt?: string;
    createdAt: string;
}

export interface CreateShipmentRequest {
    orderId: string;
    carrier: string;
    trackingNumber: string;
    notes?: string;
}

export interface UpdateShipmentRequest {
    carrier: string;
    trackingNumber: string;
    notes?: string;
}

export const shipmentsApi = {
    getByOrder(orderId: string) {
        return apiClient.get<ShipmentResponse>(`/shipments/order/${orderId}`);
    },
    create(data: CreateShipmentRequest) {
        return apiClient.post<ShipmentResponse>('/shipments', data);
    },
    update(shipmentId: string, data: UpdateShipmentRequest) {
        return apiClient.put<ShipmentResponse>(`/shipments/${shipmentId}`, data);
    },
    markDelivered(shipmentId: string) {
      return apiClient.patch<ShipmentResponse>(
        `/shipments/${shipmentId}/delivered`);
    },
}