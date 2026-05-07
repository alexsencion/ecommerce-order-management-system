import { apiClient } from "./client";

export interface Address {
    street: string;
    city: string;
    state: string;
    zipCode: string;
    country: string;
}

export interface CustomerResponse {
    id: string;
    firstName: string;
    lastName: string;
    fullName: string;
    email: string;
    phone?: string;
    address?: Address;
    isActive: boolean;
    createdAt: string 
}

export interface PagedResponse<T> {
    data: T[];
    page: number;
    totalCount: number;
    totalPages: number;
    hasNextPage: boolean;
    hasPreviousPage: boolean;
}

export interface CreateCustomerRequest {
    firstName: string;
    lastName: string;
    email: string;
    phone?: string;
    address?: Address;
}

export interface UpdateCustomerRequest {
    firstName: string;
    lastName: string;
    phone?: string;
    address?: Address;
}

export interface CustomerQueryParams {
    search?: string;
    isActive?: boolean;
    page?: number;
    pageSize?: number;
}

export const customersApi = {
    getAll(params: CustomerQueryParams = {}) {
        return apiClient.get<PagedResponse<CustomerResponse>>('/customers', { params });
    },
    getById(id: string) {
        return apiClient.get<CustomerResponse>(`/customers/${id}`);
    },
    create(data: CreateCustomerRequest) {
        return apiClient.post<CustomerResponse>(`/customers`, data)
    },
    update(id: string, data: UpdateCustomerRequest) {
        return apiClient.put<CustomerResponse>(`/customers/${id}`, data)
    },
    deactivate(id: string) {
        return apiClient.patch<CustomerResponse>(`/customers/${id}/deactivate`)
    },
    delete(id: string) {
        return apiClient.delete<CustomerResponse>(`/customers/${id}`)
    },
}