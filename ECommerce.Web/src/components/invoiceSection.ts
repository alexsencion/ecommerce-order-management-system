import { invoicesApi, type InvoiceResponse } from "../api/invoices";
import { ApiError } from "../api/client";
import { showToast } from "./toast";
import { escHtml, money } from "../utils/format";

export async function renderInvoiceSection(
    container: HTMLElement,
    orderId: string
): Promise<void> {
    container.innerHTML = '<p class="text-muted small mb-0">Loading invoice…</p>';

    try {
        const { data } = await invoicesApi.getByOrder(orderId);
        showExisting(container, data);
    } catch (err) {
        if (err instanceof ApiError && err.status === 404) {
            showGenerate(container, orderId);
        } else {
            container.innerHTML = 
                `<div class="alert alert-danger small py-2 mb-0">
                    ${escHtml((err as Error).message)}
                </div>`;
        }
    }
}

function showExisting(container: HTMLElement, inv: InvoiceResponse): void {
    container.innerHTML = `
        <div class="d-flex justify-content-between align-items-center">
            <div>
                <div class="fw-semibold font-monospace small">${escHtml(inv.invoiceNumber)}</div>
                <div class="text-muted" style="font-size:.75rem">
                Issued ${new Date(inv.issuedAt).toLocaleDateString()} · ${money(inv.total)}
                </div>
            </div>
            <button class="btn btn-outline-primary btn-sm" data-action="download">
                Download PDF
            </button>
        </div>
        <div class="alert alert-danger d-none small py-2 mt-2 mb-0" data-role="error"></div>`;

    const btn = container.querySelector<HTMLButtonElement>('[data-action="download"]')!;
    const errorEl = container.querySelector<HTMLElement>('[data-role="error"]')!;

    btn.addEventListener('click', async () => {
        btn.disabled = true;
        errorEl.classList.add('d-none');
        try {
            await invoicesApi.download(inv);
        } catch (err) {
            errorEl.textContent = (err as Error).message;
            errorEl.classList.remove('d-none');
        } finally {
            btn.disabled = false;
        }
    });
}

function showGenerate(container: HTMLElement, orderId: string): void {
    container.innerHTML = `
        <div class="d-flex justify-content-between align-items-center">
            <span class="text-muted small">No invoice issued yet.</span>
            <button class="btn btn-primary btn-sm" data-action="generate">
                Generate Invoice
            </button>
            </div>
        <div class="alert alert-danger d-none small py-2 mt-2 mb-0" data-role="error"></div>`;

    const btn = container.querySelector<HTMLButtonElement>('[data-action="generate"]')!;
    const errorEl = container.querySelector<HTMLElement>('[data-role="error"]')!;

    btn.addEventListener('click', async () => {
        btn.disabled = true;
        btn.textContent = "Generating...";
        errorEl.classList.add('d-none');
        try {
            const { data } = await invoicesApi.generate(orderId);
            showToast(`Invoice ${data.invoiceNumber} issued.`, 'success');
            showExisting(container, data)
        } catch (err) {
            errorEl.textContent = (err as Error).message;
            errorEl.classList.remove('d-none');
            btn.disabled = false;
            btn.textContent = 'Generate Invoice';
        }
    });
}