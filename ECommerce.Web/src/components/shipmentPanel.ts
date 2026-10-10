import {
    SHIPMENT_STATUS_BADGE,
    SHIPMENT_STATUS_LABELS,
    shipmentsApi,
    ShipmentStatus,
    type ShipmentResponse} from "../api/shipments";
import { showToast } from "./toast";
import { showConfirmModal } from "./modal";

export interface ShipmentPanelOptions {
    orderId: string;
    orderStatus: number;
    onUpdate: () => void;
}

export async function renderShipmentSection(
    container: HTMLElement,
    opts: ShipmentPanelOptions
): Promise<void> {
    container.innerHTML = 
      '<p class="text-muted small">Loading shipment...</p>';

    try {
    const { data: shipment } = await shipmentsApi.getByOrder(opts.orderId);
    container.innerHTML = renderExisting(shipment); 
    bindExistinEvents(container, shipment, opts);
    } catch {
        if (opts.orderStatus === 4) {
            container.innerHTML = renderCreateForm(opts.orderId);
            bindCreateFormEvents(container, opts);
            //
        } else {
            container.innerHTML =
                '<p class="text-muted small mb-0">No shipment recorded yet.</p>';
        }
    }
}

function renderExisting(
    s: ShipmentResponse
): string {
    const badge = SHIPMENT_STATUS_BADGE[s.status];
    const label = SHIPMENT_STATUS_LABELS[s.status];
    const shippedAt = s.shippedAt
      ? new Date(s.shippedAt).toLocaleString() : '-';
    const deliveredAt = s.deliveredAt
      ? new Date(s.deliveredAt).toLocaleString() : '-';

    const canUpdate = s.status !== ShipmentStatus.Delivered;
    const canDeliver = s.status !== ShipmentStatus.Delivered;

    return `
    <!-- Shipment summary -->
    <div class="d-flex justify-content-between align-items-center mb-3">
      <span class="badge ${badge}">${label}</span>
      <span class="text-muted small">
        Shipped: ${shippedAt}
      </span>
    </div>

    <div class="row g-2 mb-3 small">
      <div class="col-6">
        <div class="text-muted" style="font-size:.72rem">CARRIER</div>
        <div class="fw-semibold">${escHtml(s.carrier)}</div>
      </div>
      <div class="col-6">
        <div class="text-muted" style="font-size:.72rem">TRACKING</div>
        <div class="fw-semibold font-monospace">${escHtml(s.trackingNumber)}</div>
      </div>
      ${s.notes ? `
      <div class="col-12">
        <div class="text-muted" style="font-size:.72rem">NOTES</div>
        <div>${escHtml(s.notes)}</div>
      </div>` : ''}
      ${s.deliveredAt ? `
      <div class="col-12">
        <div class="text-muted" style="font-size:.72rem">DELIVERED AT</div>
        <div>${deliveredAt}</div>
      </div>` : ''}
    </div>

    <!-- Edit form — hidden by default -->
    <div id="edit-shipment-form" class="d-none">
      ${renderEditForm(s)}
    </div>

    <!-- Error feedback -->
    <div id="shipment-error" class="alert alert-danger d-none small py-2 mb-2">
    </div>

    <!-- Actions -->
    ${canUpdate || canDeliver ? `
    <div class="d-flex gap-2 flex-wrap">
      ${canUpdate ? `
        <button class="btn btn-outline-secondary btn-sm"
                id="btn-edit-shipment" data-id="${s.id}">
          Edit
        </button>` : ''}
      ${canDeliver ? `
        <button class="btn btn-success btn-sm"
                id="btn-mark-delivered" data-id="${s.id}">
          Mark Delivered
        </button>` : ''}
    </div>` : ''}`;
}

function renderEditForm(s: ShipmentResponse): string {
    return `
    <div class="mb-2">
      <label class="form-label small mb-1">Carrier</label>
      <input id="edit-carrier" class="form-control form-control-sm"
             value="${escHtml(s.carrier)}">
    </div>
    <div class="mb-2">
      <label class="form-label small mb-1">Tracking Number</label>
      <input id="edit-tracking" class="form-control form-control-sm font-monospace"
             value="${escHtml(s.trackingNumber)}">
    </div>
    <div class="mb-3">
      <label class="form-label small mb-1">Notes</label>
      <input id="edit-notes" class="form-control form-control-sm"
             value="${escHtml(s.notes ?? '')}">
    </div>
    <div class="d-flex gap-2">
      <button class="btn btn-primary btn-sm" id="btn-save-shipment"
              data-id="${s.id}">
        Save
      </button>
      <button class="btn btn-link btn-sm text-muted p-0"
              id="btn-cancel-edit">
        Cancel
      </button>
    </div>`;
}

function renderCreateForm(orderId: string): string {
  return `
    <p class="text-muted small mb-3">
      Record tracking details to ship this order.
    </p>
    <div class="mb-2">
      <label class="form-label small mb-1">
        Carrier <span class="text-danger">*</span>
      </label>
      <input id="new-carrier" class="form-control form-control-sm"
             placeholder="e.g. FedEx, UPS, DHL">
    </div>
    <div class="mb-2">
      <label class="form-label small mb-1">
        Tracking Number <span class="text-danger">*</span>
      </label>
      <input id="new-tracking" class="form-control form-control-sm font-monospace"
             placeholder="UPPERCASE-ONLY">
    </div>
    <div class="mb-3">
      <label class="form-label small mb-1">Notes</label>
      <input id="new-notes" class="form-control form-control-sm"
             placeholder="Optional handling instructions">
    </div>
    <div id="shipment-error"
         class="alert alert-danger d-none small py-2 mb-2">
    </div>
    <button class="btn btn-primary btn-sm w-100"
            id="btn-create-shipment"
            data-orderid="${orderId}">
      Create Shipment
    </button>`;
}

function bindExistinEvents(
    container: HTMLElement,
    shipment: ShipmentResponse,
    opts: ShipmentPanelOptions
): void {
    const errorEl = container.querySelector('#shipment-error') as HTMLElement;

    container.querySelector('#btn-edit-shipment')
      ?.addEventListener('click', () => {
        const form = container.querySelector('#edit-shipment-form')!;
        form.classList.toggle('d-none')
      });

      container.querySelector('#btn-cancel-edit')
        ?.addEventListener('click', () => {
            container.querySelector('#edit-shipment-form')!
              .classList.add('d-none');
        });

      container.querySelector('#btn-save-shipment')
       ?.addEventListener('click', async () => {
         const carrier = (container.querySelector('#edit-carrier') as HTMLInputElement)
           .value.trim();
         const tracking = (container.querySelector('#edit-tracking') as HTMLInputElement)
           .value.trim().toUpperCase();
         const notes = (container.querySelector('#edit-notes') as HTMLInputElement)
           .value.trim() || undefined;

         if (!carrier || !tracking) {
            errorEl.textContent = 'Carrier and tracking number are required.';
            errorEl.classList.remove('d-none');
            return;
         }

         try {
            errorEl.classList.add('d-none');
            await shipmentsApi.update(shipment.id, {carrier, trackingNumber: tracking, notes });
            showToast('Shipment updated.', 'success');
            opts.onUpdate();
         } catch (err: any) {
            errorEl.textContent = err.message;
            errorEl.classList.remove('d-none');
         }
       });

       // Mark delivered
       container.querySelector('#btn-mark-delivered')
         ?.addEventListener('click', async () => {
          const ok = await showConfirmModal(
            'Mark this shipment as delivered? This will close the order.');
          if (!ok) return;

          try {
            errorEl.classList.add('d-none');
            await shipmentsApi.markDelivered(shipment.id);
            showToast('Order marked as delivered.', 'success');
            opts.onUpdate();
          } catch (err: any) {
            errorEl.textContent = err.message;
            errorEl.classList.remove('d-none');
          }
        });
}

function bindCreateFormEvents(
  container: HTMLElement,
  opts: ShipmentPanelOptions
): void {
  const errorEl = container.querySelector('#shipment-error') as HTMLElement;

  container.querySelector('#btn-create-shipment')
    ?.addEventListener('click', async (e) => {
      const btn = e.currentTarget as HTMLButtonElement;

      const carrier = (container.querySelector('#new-carrier') as HTMLInputElement)
        .value.trim();
      const tracking = (container.querySelector('#new-tracking') as HTMLInputElement)
        .value.trim().toUpperCase();
      const notes = (container.querySelector('#new-notes') as HTMLInputElement)
        .value.trim() || undefined;

      if (!carrier || !tracking) {
        errorEl.textContent = 'Carrier and tracking number are required.';
        errorEl.classList.remove('d-none');
        return;
      }

      errorEl.classList.add('d-none');
      btn.disabled = true;
      btn.textContent = 'Creating...';

      try {
        await shipmentsApi.create({
          orderId: opts.orderId,
          carrier,
          trackingNumber: tracking,
          notes,
        });
        showToast('Shipment created. Order is now Shipped.', 'success');
        opts.onUpdate();
      } catch (err: any) {
        errorEl.textContent = err.message;
        errorEl.classList.remove('d-none');
        btn.disabled = false;
        btn.textContent = 'Create Shipment';
      }
    });
}

function escHtml(str: string): string {
    return str
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;');
}
