import axios from 'axios';

const API_BASE = import.meta.env.VITE_API_URL ?? 'https://localhost:5001/api'

export const apiClient = axios.create({
    baseURL: API_BASE,
    headers: { 'Content-Type': 'application/json' }
});

export class ApiError extends Error {
    constructor(message: string, public readonly status?: number) {
        super(message)
        this.name = 'ApiError';
    }
}

apiClient.interceptors.response.use(
    (response) => response,
    (error) => {
        const data = error.response?.data;
        const message = 
            data?.errors?.join?.(' ') ??
            data?.error ??
            data?.message ??
            error.message ??
            'An unexpected error occurred.';
        return Promise.reject(new ApiError(message, error.response?.status));
    }
);