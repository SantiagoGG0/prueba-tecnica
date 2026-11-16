import { useCallback, useEffect, useState } from 'react';
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { ApiError, HUB_URL, ordersApi } from '../api/ordersApi';
import type { Order } from '../types';

export type LiveMode = 'connecting' | 'realtime' | 'polling';

const POLL_INTERVAL_MS = 3000;

// Lista de pedidos: carga por REST y actualizaciones por SignalR.
// Si SignalR no conecta o se cae, se usa polling hasta que se reconecte.
export function useOrders() {
  const [orders, setOrders] = useState<Order[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [mode, setMode] = useState<LiveMode>('connecting');

  const refresh = useCallback(async () => {
    try {
      setOrders(await ordersApi.list());
      setError(null);
    } catch (e) {
      // se mantienen los datos anteriores
      setError(e instanceof ApiError ? e.message : 'Error inesperado al cargar los pedidos.');
    } finally {
      setLoading(false);
    }
  }, []);

  const upsert = useCallback((order: Order) => {
    setOrders((prev) => {
      const index = prev.findIndex((o) => o.id === order.id);
      if (index === -1) return [order, ...prev];
      const next = [...prev];
      next[index] = order;
      return next;
    });
  }, []);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  useEffect(() => {
    const connection = new HubConnectionBuilder()
      .withUrl(HUB_URL)
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    connection.on('orderChanged', upsert);
    connection.onreconnecting(() => setMode('polling'));
    connection.onreconnected(() => {
      setMode('realtime');
      void refresh(); // pueden haberse perdido eventos mientras estuvo desconectado
    });
    connection.onclose(() => setMode('polling'));

    connection
      .start()
      .then(() => setMode('realtime'))
      .catch(() => setMode('polling'));

    return () => {
      void connection.stop();
    };
  }, [upsert, refresh]);

  useEffect(() => {
    if (mode !== 'polling') return;
    const id = window.setInterval(() => void refresh(), POLL_INTERVAL_MS);
    return () => window.clearInterval(id);
  }, [mode, refresh]);

  return { orders, loading, error, mode, refresh, upsert };
}
