import { apiClient } from "./client";
import { saveBlob } from "../utils/format";

export interface InvoiceResponse {
    id: string;
    orderId: string;
    invoiceNumber: string;
    issuedAt: string;
    billToName: string;
    billToEmail: string;
    subtotal: number;
    tax: number;
    total: number;
    fileName: string;
}

export const invoicesApi = {
    getByOrder(orderId: string) {
        return apiClient.get<InvoiceResponse>(`/invoices/order/${orderId}`);
    },
    generate(orderId: string) {
        return apiClient.post<InvoiceResponse>(`/invoices/order/${orderId}`);
    },
    async download(invoice: InvoiceResponse): Promise<void> {
        const { data } = await apiClient.get<Blob>(
            `/invoices/${invoice.id}/download`,
            { responseType: 'blob' }
        );
        saveBlob(data, invoice.fileName);
    },
}