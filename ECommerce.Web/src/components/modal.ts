export function showConfirmModal(message: string): Promise<boolean> {
    return new Promise((resolve) => {
        const existing = document.getElementById('confirm-modal');
        if (existing) existing.remove();

        const modal = document.createElement('div');
        modal.id = 'confirm-modal'
        modal.style.cssText = 
            'position:fixed;inset:0;background:rgba(0,0,0,.5);display:flex;align-items:center;justify-content:center;z-index:10000';
        modal.innerHTML = `
            <div style="background:#fff;border-radius:8px;padding:1.5rem;max-width:400px;width:90%">
                <p style="margin:0 0 1.25rem;font-size:1rem">${message}</p>
                <div style="display:flex;gap:.75rem;justify-content:flex-end">
                    <button id="modal-cancel"  class="btn btn-secondary btn-sm">Cancel</button>
                    <button id="modal-confirm" class="btn btn-danger btn-sm">Confirm</button>
                </div>
            </div>`;

        document.body.appendChild(modal);

        modal.querySelector('#modal-confirm')!.addEventListener('click', () => {
            modal.remove();
            resolve(true);
        });
        modal.querySelector('#modal-cancel')!.addEventListener('click', () => {
            modal.remove();
            resolve(false);
        });
    });
}