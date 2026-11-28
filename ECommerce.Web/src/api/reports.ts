import { apiClient } from "./client";
import type { OrderStatus } from "./orders";

export interface SalesSummary {
    orderCount: number;
    grossRevenue: number;
    taxCollected: number;
    netRevenue: number;
    averageOrderValue: number;
}

export interface DailySalesPoint {
    date: string;
    orderCount: number;
    revenue: number;
}

export interface TopProductItem {
    productId: string;
    name: string;
    sku: string;
    unitsSold: number;
    revenue: number
}

export interface StatusBreakdownItem {
    status: OrderStatus;
    label: string;
    count: number;
}

export interface DashboardResponse {
    from: string;
    to: string;
    summary: SalesSummary;
    dailySales: DailySalesPoint[];
    topProducts: TopProductItem[];
    statusBreakdown: StatusBreakdownItem[];
}

export interface DashboardParams {
    from?: string;
    to?: string;
    top?: number;
}

export const reportsApi = {
    getDashboard(params: DashboardParams = {}) {
        return apiClient.get<DashboardResponse>('/reports/dashboard', { params });
    },
};