import type { LiveMode } from '../hooks/useOrders';
import type { Order } from '../types';
import { StatusBadge } from './StatusBadge';

interface Props {
  orders: Order[];
  loading: boolean;
  error: string | null;
  mode: LiveMode;
  onRefresh: () => void;
}

const MODE_LABEL: Record<LiveMode, string> = {
  connecting: 'Conectando…',
  realtime: '● Tiempo real',
  polling: '↻ Polling cada 3 s',
};

const formatDate = (iso: string) => new Date(iso).toLocaleString('es-CO');

export function OrderList({ orders, loading, error, mode, onRefresh }: Props) {
  return (
    <section className="card">
      <div className="list-header">
        <h2>Pedidos</h2>
        <span className={`live live-${mode}`}>{MODE_LABEL[mode]}</span>
        <button type="button" className="secondary" onClick={onRefresh}>
          Actualizar
        </button>
      </div>

      {error && (
        <div className="alert alert-error" role="alert">
          {error}
        </div>
      )}

      {loading ? (
        <p className="muted">Cargando pedidos…</p>
      ) : orders.length === 0 ? (
        <p className="muted">Aún no hay pedidos. Crea uno desde “Nuevo pedido”.</p>
      ) : (
        <div className="table-wrapper">
          <table>
            <thead>
              <tr>
                <th>Pedido</th>
                <th>Cliente</th>
                <th>SKU</th>
                <th>Cant.</th>
                <th>Estado</th>
                <th>Creado</th>
                <th>Detalle</th>
              </tr>
            </thead>
            <tbody>
              {orders.map((o) => (
                <tr key={o.id}>
                  <td>
                    <code title={o.id}>{o.id.slice(0, 8)}</code>
                  </td>
                  <td>{o.clienteNombre}</td>
                  <td>{o.sku}</td>
                  <td>{o.cantidad}</td>
                  <td>
                    <StatusBadge status={o.estado} />
                  </td>
                  <td>{formatDate(o.creadoEn)}</td>
                  <td className="muted">{o.motivoRechazo ?? ''}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}
