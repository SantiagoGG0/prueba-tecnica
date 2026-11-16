export type OrderStatus = 'Pending' | 'Confirmed' | 'Rejected';

export interface Order {
  id: string;
  clienteNombre: string;
  sku: string;
  cantidad: number;
  estado: OrderStatus;
  creadoEn: string;
  actualizadoEn: string | null;
  motivoRechazo: string | null;
}

export interface Product {
  sku: string;
  nombre: string;
}

export interface CreateOrderInput {
  clienteNombre: string;
  sku: string;
  cantidad: number | null;
}

/** Errores por campo, mismo formato que ValidationProblemDetails de ASP.NET Core. */
export type FieldErrors = Partial<Record<keyof CreateOrderInput, string[]>>;
