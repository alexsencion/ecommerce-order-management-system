import axios from 'axios';

const API_BASE = import.meta.env.VITE_API_URL ?? 'https://localhost:5001/api'

export const apiClient = axios.create({
    baseURL: API_BASE,
    headers: { 'Content-Type': 'application/json' }
});

apiClient.interceptors.response.use(
    (response) => response,
    (error) => {
        const message = 
            error.response?.data?.error ??
            error.response?.data?.errors?.[0] ??
            'An unexpected error occurred.';
        return Promise.reject(new Error(message));
    }
);