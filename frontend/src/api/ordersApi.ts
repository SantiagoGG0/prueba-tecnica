import type { CreateOrderInput, FieldErrors, Order, Product } from '../types';

// nginx (o el proxy de Vite en dev) redirige /api y /hubs a Orders API
const API_BASE = import.meta.env.VITE_API_BASE_URL ?? '/api';
export const HUB_URL = import.meta.env.VITE_HUB_URL ?? '/hubs/orders';

export class ApiError extends Error {
  constructor(
    message: string,
    public readonly status: number,
    public readonly fieldErrors: FieldErrors = {},
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  let response: Response;
  try {
    response = await fetch(`${API_BASE}${path}`, {
      ...init,
      headers: { 'Content-Type': 'application/json', Accept: 'application/json', ...init?.headers },
    });
  } catch {
    throw new ApiError('No se pudo conectar con el servidor. Verifica que Orders API esté en ejecución.', 0);
  }

  if (!response.ok) {
    // ProblemDetails: { title, status, errors? }
    const problem = await response.json().catch(() => null);
    const message =
      response.status === 400
        ? problem?.title ?? 'Los datos enviados no son válidos.'
        : response.status === 404
          ? 'El recurso solicitado no existe.'
          : `Error del servidor (${response.status}). Intenta de nuevo.`;
    throw new ApiError(message, response.status, problem?.errors ?? {});
  }

  return (await response.json()) as T;
}

export const ordersApi = {
  list: () => request<Order[]>('/orders'),
  get: (id: string) => request<Order>(`/orders/${id}`),
  create: (input: CreateOrderInput) =>
    request<Order>('/orders', { method: 'POST', body: JSON.stringify(input) }),
  products: () => request<Product[]>('/products'),
};
