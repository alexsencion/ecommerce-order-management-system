export type ToastType = 'success' | 'danger' | 'warning' | 'info';

export function showToast(message: string, type: ToastType = 'info'): void {
    const container = getOrCreateContainer();

    const toast = document.createElement('div');
    toast.className = `toast align-items-center text-bg-${type} border-0 show mb-2`;
    toast.setAttribute('role', 'alert')
    toast.innerHTML = `
        <div class="d-flex">
            <div class="toast-body">${message}</div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto"
                data-bs-dismiss="toast"></button>
        </div>`;

    toast.querySelector('button')!.addEventListener('click', () => toast.remove());
    container.appendChild(toast);
    setTimeout(() => toast.remove(), 4000);
}

function getOrCreateContainer(): HTMLElement {
    let c = document.getElementById("toast-container");
    if (!c) {
        c = document.createElement('div');
        c.id = 'toast-container';
        c.style.cssText = 'position:fixed;top:1rem;right:1rem;z-index:9999;min-width:280px';
        document.body.appendChild(c);
    }
    return c;
}