import type { CreateOrderInput, FieldErrors } from './types';

export const MIN_CANTIDAD = 1;
export const MAX_CANTIDAD = 100;

// Validación rápida en el cliente; la existencia del SKU solo la valida el servidor.
export function validateOrder(input: CreateOrderInput): FieldErrors {
  const errors: FieldErrors = {};

  if (!input.clienteNombre.trim()) {
    errors.clienteNombre = ['El nombre del cliente es obligatorio.'];
  }

  if (!input.sku.trim()) {
    errors.sku = ['El SKU es obligatorio.'];
  }

  if (input.cantidad === null || Number.isNaN(input.cantidad)) {
    errors.cantidad = ['La cantidad es obligatoria.'];
  } else if (!Number.isInteger(input.cantidad) || input.cantidad < MIN_CANTIDAD || input.cantidad > MAX_CANTIDAD) {
    errors.cantidad = [`La cantidad debe ser un entero entre ${MIN_CANTIDAD} y ${MAX_CANTIDAD}.`];
  }

  return errors;
}

export const hasErrors = (errors: FieldErrors) => Object.keys(errors).length > 0;
