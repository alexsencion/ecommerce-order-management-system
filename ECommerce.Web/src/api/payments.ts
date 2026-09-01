import { apiClient } from "./client";

export interface PaymentIntentResponse {
    paymentIntentId: string;
    clientSecret: string;
    publishableKey: string;
    amount: number;
    currency: string;
    status: string;
}

export enum PaymentStatus {
    Pending = 1,
    Succeeded = 2,
    Failed = 3,
    Refunded = 4,
}

export const PAYMENT_STATUS_LABELS: Record<PaymentStatus, string> = {
    [PaymentStatus.Pending]: 'Pending',
    [PaymentStatus.Succeeded]: 'Paid',
    [PaymentStatus.Failed]: 'Failed',
    [PaymentStatus.Refunded]: 'Refunded',
};

export interface PaymentResponse {
    id: string;
    orderId: string;
    stripePaymentIntentId: string;
    status: PaymentStatus;
    statusLabel: string;
    amount: number;
    paidAt?: string;
    createdAt: string;
}

export const paymentsApi = {
    createIntent(orderId: string) {
        return apiClient.post<PaymentIntentResponse>('/payments/intent', { orderId });
    },
    getByOrder(orderId: string) {
        return apiClient.get<PaymentResponse>(`/payments/order/${orderId}`);
    },
    refund(orderId: string) {
        return apiClient.post(`/payments/order/${orderId}/refund`);
    }
};