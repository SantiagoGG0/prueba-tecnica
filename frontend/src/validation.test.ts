import { describe, expect, it } from 'vitest';
import { validateOrder } from './validation';

describe('validateOrder', () => {
  it('acepta un pedido válido', () => {
    expect(validateOrder({ clienteNombre: 'Ana', sku: 'ABC-01', cantidad: 5 })).toEqual({});
  });

  it('reporta cada campo inválido por separado', () => {
    const errors = validateOrder({ clienteNombre: '   ', sku: '', cantidad: 0 });
    expect(Object.keys(errors).sort()).toEqual(['cantidad', 'clienteNombre', 'sku']);
  });

  it.each([null, 101, 2.5])('rechaza cantidad %s', (cantidad) => {
    expect(validateOrder({ clienteNombre: 'Ana', sku: 'ABC-01', cantidad }).cantidad).toBeDefined();
  });
});
