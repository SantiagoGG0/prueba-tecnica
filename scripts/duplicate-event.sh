#!/usr/bin/env sh
# Publica dos veces el mismo OrderCreated (mismo eventId) usando la API de RabbitMQ
# y muestra el stock antes y después. Debe descontarse una sola vez.
#
# Uso: sh scripts/duplicate-event.sh [SKU] [CANTIDAD]
set -eu

SKU="${1:-ABC-02}"
CANTIDAD="${2:-5}"
USER="${RABBITMQ_USER:-orderflow}"
PASS="${RABBITMQ_PASSWORD:-orderflow_dev}"

# uuidgen no está en Git Bash
guid() { od -An -N16 -tx1 /dev/urandom | tr -d ' \n' | sed -E 's/(.{8})(.{4})(.{4})(.{4})(.{12})/\1-\2-\3-\4-\5/'; }
EVENT_ID="$(guid)"
ORDER_ID="$(guid)"
NOW="$(date -u +%Y-%m-%dT%H:%M:%SZ)"

stock() {
  docker compose exec -T postgres psql -U "${POSTGRES_USER:-orderflow}" -d inventory -tAc \
    "SELECT \"Disponible\" FROM stock WHERE \"Sku\" = '$SKU';"
}

PAYLOAD="{\\\"eventId\\\":\\\"$EVENT_ID\\\",\\\"orderId\\\":\\\"$ORDER_ID\\\",\\\"sku\\\":\\\"$SKU\\\",\\\"cantidad\\\":$CANTIDAD,\\\"ocurridoEn\\\":\\\"$NOW\\\"}"
BODY="{\"properties\":{\"message_id\":\"$EVENT_ID\",\"delivery_mode\":2,\"content_type\":\"application/json\"},\"routing_key\":\"order.created\",\"payload\":\"$PAYLOAD\",\"payload_encoding\":\"string\"}"

echo "Stock de $SKU antes: $(stock)"
for i in 1 2; do
  echo "Publicando evento $EVENT_ID (envío $i)…"
  curl -s -u "$USER:$PASS" -H "content-type: application/json" \
    -X POST "http://localhost:15672/api/exchanges/%2F/orderflow.events/publish" -d "$BODY"
  echo
done

sleep 2
echo "Stock de $SKU después: $(stock)   (esperado: antes - $CANTIDAD)"
echo "Registros de idempotencia para el evento:"
docker compose exec -T postgres psql -U "${POSTGRES_USER:-orderflow}" -d inventory -c \
  "SELECT \"EventId\", \"Reserved\", \"Motivo\" FROM processed_events WHERE \"EventId\" = '$EVENT_ID';"
