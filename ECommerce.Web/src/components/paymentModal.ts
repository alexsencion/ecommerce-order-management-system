import { loadStripe, type Stripe, type StripeElements } from '@stripe/stripe-js';
import { paymentsApi } from '../api/payments';
import { showToast } from './toast';

let stripeInstance: Stripe | null = null;
let elements: StripeElements | null = null;

interface PaymentModalOptions {
  orderId: string;
  amount: number;
  onSuccess: () => void;
  onDismiss: () => void;
}

export async function openPaymentModal(opts: PaymentModalOptions): Promise<void> {
    elements = null;

  // Create and render the modal container
  const modal = createModalElement();
  document.body.appendChild(modal);

  try {
    const { data: intent } = await paymentsApi.createIntent(opts.orderId);

    console.log('Stripe intent returned by backend:', {
        paymentIntentId: intent.paymentIntentId,
        clientSecret: intent.clientSecret,
        status: intent.status
    });

    if (!stripeInstance) {
      stripeInstance = await loadStripe(intent.publishableKey);
    }
    if (!stripeInstance) {
      throw new Error('Failed to load Stripe');
    }

    elements = stripeInstance.elements({ 
        clientSecret: intent.clientSecret
    });

    const paymentElement = elements.create('payment');

    const formContainer = modal.querySelector('#payment-form-container')! as HTMLElement;
    formContainer.innerHTML = '';

    try {
      paymentElement.mount(formContainer);
    } catch (mountError: any) {
      console.error('Error mounting payment element:', mountError);
      throw new Error('Failed to mount payment form: ' + mountError.message);
    }

    const submitBtn = modal.querySelector('#btn-pay') as HTMLButtonElement;
    submitBtn.textContent = `Pay $${opts.amount.toFixed(2)}`;
    submitBtn.addEventListener('click', async () => {
      await handlePaymentSubmit(opts, submitBtn, modal);
    });

    modal.querySelector('#btn-close-payment-modal')!
      .addEventListener('click', () => {
        opts.onDismiss();
        modal.remove();
      });

    modal.addEventListener('click', (e) => {
      if (e.target === modal) {
        opts.onDismiss();
        modal.remove();
      }
    });
  } catch (error: any) {
    const container = modal.querySelector('#payment-form-container')!;
    container.innerHTML =
      `<div class="alert alert-danger small mb-0">${error.message}</div>`;
  }
}

async function handlePaymentSubmit(
  opts: PaymentModalOptions,
  submitBtn: HTMLButtonElement,
  modal: HTMLElement
): Promise<void> {
  if (!stripeInstance || !elements) return;

  const errorEl = modal.querySelector('#payment-error')! as HTMLElement;
  errorEl.classList.add('d-none');
  submitBtn.disabled = true;
  submitBtn.textContent = 'Processing…';

  try {
    const { error } = await stripeInstance.confirmPayment({
      elements,
      confirmParams: {
        return_url: window.location.href,
      },
      redirect: 'if_required',
    });

    if (error) {
      errorEl.textContent = error.message ?? 'Payment failed. Please try again.';
      errorEl.classList.remove('d-none');
      submitBtn.disabled = false;
      submitBtn.textContent = `Pay $${opts.amount.toFixed(2)}`;
      return;
    }

    showToast('Payment successful!', 'success');

    await new Promise(resolve => setTimeout(resolve, 2000));

    modal.remove();
    opts.onSuccess();
  } catch (err: any) {
    errorEl.textContent = err.message ?? 'An unexpected error occurred.';
    errorEl.classList.remove('d-none');
    submitBtn.disabled = false;
    submitBtn.textContent = `Pay $${opts.amount.toFixed(2)}`;
  }
}

function createModalElement(): HTMLElement {
  const modal = document.createElement('div');
  modal.style.cssText = `
    position: fixed;
    inset: 0;
    background: rgba(0, 0, 0, 0.5);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 2000;
  `;
  modal.className = 'd-flex';

  modal.innerHTML = `
    <div style="background: white; border-radius: 12px; padding: 2rem;
                width: 90%; max-width: 500px; max-height: 90vh;
                overflow-y: auto; box-shadow: 0 20px 60px rgba(0,0,0,0.3)">
      <div class="d-flex justify-content-between align-items-center mb-4">
        <h5 class="mb-0">Complete Payment</h5>
        <button class="btn-close" id="btn-close-payment-modal"></button>
      </div>

      <div id="payment-error" class="alert alert-danger d-none small py-2 mb-3"></div>

      <div id="payment-form-container" class="mb-4 min-height-200">
        <p class="text-muted small text-center">Loading payment form…</p>
      </div>

      <button class="btn btn-primary w-100" id="btn-pay">Pay</button>
    </div>`;

  return modal;
}