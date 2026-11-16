import { useState } from 'react';
import { OrderForm } from './components/OrderForm';
import { OrderList } from './components/OrderList';
import { useOrders } from './hooks/useOrders';

type View = 'form' | 'list';

// useOrders vive aquí y no en la lista para no perder datos ni la conexión al cambiar de pestaña
export default function App() {
  const [view, setView] = useState<View>('form');
  const { orders, loading, error, mode, refresh, upsert } = useOrders();
  const pending = orders.filter((o) => o.estado === 'Pending').length;

  return (
    <div className="app">
      <header>
        <h1>OrderFlow</h1>
        <nav>
          <button className={view === 'form' ? 'tab active' : 'tab'} onClick={() => setView('form')}>
            Nuevo pedido
          </button>
          <button className={view === 'list' ? 'tab active' : 'tab'} onClick={() => setView('list')}>
            Pedidos {pending > 0 && <span className="counter">{pending} pendientes</span>}
          </button>
        </nav>
      </header>

      <main>
        {view === 'form' ? (
          <OrderForm onCreated={upsert} />
        ) : (
          <OrderList orders={orders} loading={loading} error={error} mode={mode} onRefresh={refresh} />
        )}
      </main>
    </div>
  );
}
