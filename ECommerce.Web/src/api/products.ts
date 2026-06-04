import { apiClient } from "./client";
import type { PagedResponse } from "./customers";

export interface CategoryResponse {
    id: string;
    name: string;
    slug: string;
    productCount: number;
}

export interface ProductResponse {
    id: string;
    categoryId: string;
    categoryName: string;
    name: string;
    sku: string;
    description?: string;
    price: number;
    stockQuantity: number;
    reservedQuantity: number;
    availableStock: number;
    imageUrl?: string;
    isActive: boolean;
    createdAt: string;
}

export interface CreateProductRequest {
    categoryId: string;
    name: string;
    sku: string;
    description?: string;
    price: number;
    stockQuantity: number;
    imageUrl?: string;
}

export interface UpdateProductRequest {
    categoryId: string;
    name: string;
    description?: string;
    price: number;
    imageUrl?: string;
}

export interface ProductQueryParams {
    search?: string;
    categoryId?: string;
    minPrice?: number;
    maxPrice?: number;
    isActive?: boolean;
    inStockOnly?: boolean;
    page?: number;
    pageSize?: number;
}

export const categoriesApi = {
    getAll() {
        return apiClient.get<CategoryResponse[]>('.categories');
    },
    create(data: { name: string; slug: string }) {
        return apiClient.post<CategoryResponse>('/categories', data);
    },
    delete(id: string) {
        return apiClient.delete(`/categories/${id}`);
    },
};

export const productsApi = {
    getAll(params: ProductQueryParams = {}) {
        return apiClient.get<PagedResponse<ProductResponse>>('/products', { params });
    },
    getById(id: string) {
        return apiClient.get<ProductResponse>(`/products/${id}`);
    },
    create(data: CreateProductRequest) {
        return apiClient.post<ProductResponse>('/products/', data)
    },
    update(id: string, data: UpdateProductRequest) {
        return apiClient.put<ProductResponse>(`/products/${id}`, data)
    },
    adjustStock(id: string, quantity: number, reason: string) {
        return apiClient.patch(`/products/${id}/stock`, { quantity, reason });
    },
    deactivate(id: string) {
        return apiClient.patch(`/products/${id}/deactivate`);
    },
    delete(id: string) {
        return apiClient.delete(`/products/${id}`)
    }
}