import { useState, type FormEvent, type ReactNode } from 'react';
import { ApiError, ordersApi } from '../api/ordersApi';
import { useProducts } from '../hooks/useProducts';
import type { CreateOrderInput, FieldErrors, Order } from '../types';
import { MAX_CANTIDAD, MIN_CANTIDAD, hasErrors, validateOrder } from '../validation';

interface Props {
  onCreated: (order: Order) => void;
}

const EMPTY: CreateOrderInput = { clienteNombre: '', sku: '', cantidad: 1 };

export function OrderForm({ onCreated }: Props) {
  const products = useProducts();
  const [form, setForm] = useState<CreateOrderInput>(EMPTY);
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [generalError, setGeneralError] = useState<string | null>(null);
  const [created, setCreated] = useState<Order | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const update = <K extends keyof CreateOrderInput>(field: K, value: CreateOrderInput[K]) => {
    setForm((f) => ({ ...f, [field]: value }));
    setFieldErrors((e) => ({ ...e, [field]: undefined }));
  };

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault();
    setGeneralError(null);
    setCreated(null);

    const clientErrors = validateOrder(form);
    setFieldErrors(clientErrors);
    if (hasErrors(clientErrors)) return;

    setSubmitting(true);
    try {
      const order = await ordersApi.create({ ...form, sku: form.sku.trim() });
      setCreated(order);
      setForm(EMPTY);
      onCreated(order);
    } catch (e) {
      // los 400 traen errores por campo, igual que la validación del cliente
      if (e instanceof ApiError && hasErrors(e.fieldErrors)) {
        setFieldErrors(e.fieldErrors);
        setGeneralError(e.message);
      } else {
        setGeneralError(e instanceof ApiError ? e.message : 'Error inesperado al crear el pedido.');
      }
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <form className="card form" onSubmit={handleSubmit} noValidate>
      <h2>Nuevo pedido</h2>

      {generalError && (
        <div className="alert alert-error" role="alert">
          {generalError}
        </div>
      )}
      {created && (
        <div className="alert alert-success" role="status">
          Pedido <code>{created.id.slice(0, 8)}</code> creado en estado <strong>Pendiente</strong>. Su estado se
          actualizará en la lista cuando Inventario responda.
        </div>
      )}

      <Field label="Nombre del cliente" id="clienteNombre" errors={fieldErrors.clienteNombre}>
        <input
          id="clienteNombre"
          value={form.clienteNombre}
          onChange={(e) => update('clienteNombre', e.target.value)}
          aria-invalid={!!fieldErrors.clienteNombre}
          maxLength={200}
        />
      </Field>

      <Field label="SKU" id="sku" errors={fieldErrors.sku} hint="Elige uno del catálogo o escríbelo.">
        <input
          id="sku"
          list="sku-options"
          value={form.sku}
          onChange={(e) => update('sku', e.target.value)}
          aria-invalid={!!fieldErrors.sku}
          autoComplete="off"
        />
        <datalist id="sku-options">
          {products.map((p) => (
            <option key={p.sku} value={p.sku}>
              {p.nombre}
            </option>
          ))}
        </datalist>
      </Field>

      <Field label="Cantidad" id="cantidad" errors={fieldErrors.cantidad} hint={`Entre ${MIN_CANTIDAD} y ${MAX_CANTIDAD}.`}>
        <input
          id="cantidad"
          type="number"
          value={form.cantidad ?? ''}
          onChange={(e) => update('cantidad', e.target.value === '' ? null : Number(e.target.value))}
          aria-invalid={!!fieldErrors.cantidad}
        />
      </Field>

      <button type="submit" disabled={submitting}>
        {submitting ? 'Creando…' : 'Crear pedido'}
      </button>
    </form>
  );
}

function Field(props: { label: string; id: string; errors?: string[]; hint?: string; children: ReactNode }) {
  return (
    <div className="field">
      <label htmlFor={props.id}>{props.label}</label>
      {props.children}
      {props.errors?.length ? (
        <p className="field-error">{props.errors.join(' ')}</p>
      ) : (
        props.hint && <p className="field-hint">{props.hint}</p>
      )}
    </div>
  );
}
