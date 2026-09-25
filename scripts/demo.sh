#!/usr/bin/env bash
# Walks through the "Definition of Done" against a running system (docker compose up).
# Also used by CI as the end-to-end smoke test. Requires: curl, jq.
set -euo pipefail

GATEWAY="${GATEWAY:-http://localhost:5000}"
RABBIT="${RABBIT:-http://localhost:15672}"
RABBIT_AUTH="deliver:deliver"
FLAKY_CUSTOMER="00000000-0000-0000-0000-0000000f1a4e"
UNREACHABLE_CUSTOMER="00000000-0000-0000-0000-00000000dead"

step() { printf '\n\033[1;36m▶ %s\033[0m\n' "$*"; }
ok()   { printf '  \033[32m✔ %s\033[0m\n' "$*"; }
fail() { printf '  \033[31m✘ %s\033[0m\n' "$*"; exit 1; }

# eventually <description> <timeout-seconds> '<condition>': re-evaluates the condition until it holds.
# (The condition is a string so that $(...) inside it runs on every attempt, not once up front.)
eventually() {
  local description=$1 timeout=$2 condition=$3
  local deadline=$((SECONDS + timeout))
  until eval "$condition" >/dev/null 2>&1; do
    (( SECONDS < deadline )) || fail "timed out: $description"
    sleep 1
  done
  ok "$description"
}

api()  { curl -fsS -H 'Content-Type: application/json' -H "X-Correlation-Id: ${CORRELATION_ID:-demo}" "$@"; }
get()  { api "$GATEWAY$1"; }
post() { api -X POST "$GATEWAY$1" -d "$2"; }

queue_messages() { curl -fsS -u "$RABBIT_AUTH" "$RABBIT/api/queues/%2F/$1" | jq '.messages'; }

create_shipment() { # customerId weightKg → shipment id
  post /api/shipments "$(jq -n --arg c "$1" --argjson w "$2" '{
      customerId: $c,
      pickupAddress:   { street: "Av. Providencia 1234", city: "Santiago",     postalCode: "7500000" },
      deliveryAddress: { street: "Calle Valparaíso 55",  city: "Viña del Mar", postalCode: "2520000" },
      items: [ { description: "Box", quantity: 1, unitWeightKg: $w } ] }')" | jq -r '.id'
}

shipment_field() { get "/api/shipments/$1" | jq -r "$2"; }

deliver_shipment() {
  local id=$1
  eventually "shipment $id assigned to a driver" 30 'test "$(shipment_field "$id" .status)" = Assigned'
  api -X POST "$GATEWAY/api/shipments/$id/pickup"  && ok "picked up"
  api -X POST "$GATEWAY/api/shipments/$id/transit" && ok "in transit"
  api -X POST "$GATEWAY/api/shipments/$id/deliver" && ok "delivered"
}

# --------------------------------------------------------------------------------------------
step "1. Waiting for the system to be up (gateway + all consumer queues declared)"
eventually "gateway healthy" 180 'curl -fsS "$GATEWAY/health"'
for q in shipping fleet dispatch billing notifications analytics; do
  eventually "queue '$q' declared" 180 'curl -fsS -u "$RABBIT_AUTH" "$RABBIT/api/queues/%2F/$q"'
done

step "2. Registering a driver and starting their shift (Fleet → driver.available → Dispatch projection)"
DRIVER_ID=$(post /api/drivers '{"name":"Carlos","phone":"+56 9 1234 5678","vehicle":{"plate":"ABCD12","type":"Van","capacityKg":800}}' | jq -r '.id')
post "/api/drivers/$DRIVER_ID/availability" '{"available":true}' >/dev/null
ok "driver Carlos = $DRIVER_ID"

step "3. Creating a shipment (201 immediately; everything else happens asynchronously)"
export CORRELATION_ID="demo-$(date +%s)"
CUSTOMER_ID=$(cat /proc/sys/kernel/random/uuid 2>/dev/null || uuidgen)
SHIPMENT_ID=$(create_shipment "$CUSTOMER_ID" 5.5)
ok "shipment $SHIPMENT_ID (correlation id $CORRELATION_ID)"
echo "  right after creation: $(get "/api/shipments/$SHIPMENT_ID" | jq -c '{status, price}')"

step "4. ShipmentCreated fans out: Billing prices it, Dispatch assigns a driver, Notifications tells the customer"
eventually "Billing priced the shipment (eventual consistency)" 30 'test "$(shipment_field "$SHIPMENT_ID" .price)" != null'
eventually "Dispatch assigned a driver" 30 'test "$(get "/api/dispatch/assignments/$SHIPMENT_ID" | jq -r .driverName)" != null'
ASSIGNMENT=$(get "/api/dispatch/assignments/$SHIPMENT_ID")
DRIVER_ID=$(jq -r .driverId <<<"$ASSIGNMENT"); DRIVER_NAME=$(jq -r .driverName <<<"$ASSIGNMENT")
ok "Dispatch chose $DRIVER_NAME (available, enough capacity, waited longest)"
eventually "Shipping recorded the assignment" 30 'test "$(shipment_field "$SHIPMENT_ID" .status)" = Assigned'
eventually "customer notified about $DRIVER_NAME" 30 '[[ "$(get "/api/notifications?shipmentId=$SHIPMENT_ID")" == *"driver $DRIVER_NAME"* ]]'
echo "  now: $(get "/api/shipments/$SHIPMENT_ID" | jq -c '{status, price, currency, driverId}')"

step "5. Driving the shipment through its lifecycle"
deliver_shipment "$SHIPMENT_ID"

step "6. Delivery fan-out: Billing charges, Fleet frees the driver, Analytics updates its read model"
eventually "invoice paid" 30 'test "$(get "/api/billing/shipments/$SHIPMENT_ID" | jq -r .status)" = Paid'
eventually "Shipping recorded the payment (saga completed)" 30 'test "$(shipment_field "$SHIPMENT_ID" .paymentStatus)" = Captured'
eventually "driver available again" 30 'test "$(get "/api/drivers/$DRIVER_ID" | jq -r .availability)" = Available'
eventually "analytics counted the delivery" 30 'test "$(get /api/analytics/deliveries | jq -r .delivered)" -ge 1'
echo "  invoice:   $(get "/api/billing/shipments/$SHIPMENT_ID/invoice" | jq -c '{amount, currency, status, payments: (.payments | length)}')"
echo "  analytics: $(get /api/analytics/deliveries | jq -c .)"

step "7. Duplicate-message protection: the same ShipmentDelivered message published twice"
DUPLICATE_ID=$(cat /proc/sys/kernel/random/uuid 2>/dev/null || uuidgen)
ENVELOPE=$(jq -nc --arg id "$DUPLICATE_ID" --arg s "$SHIPMENT_ID" --arg c "$CUSTOMER_ID" --arg d "$DRIVER_ID" \
  '{messageId: $id, eventType: "ShipmentDelivered", occurredAt: (now | todate), correlationId: "duplicate-demo",
    payload: {shipmentId: $s, customerId: $c, driverId: $d, deliveredAt: (now | todate)}}')
for _ in 1 2; do
  curl -fsS -u "$RABBIT_AUTH" -H 'Content-Type: application/json' -X POST "$RABBIT/api/exchanges/%2F/shipment.events/publish" \
    -d "$(jq -nc --arg p "$ENVELOPE" --arg id "$DUPLICATE_ID" '{routing_key: "shipment.delivered", payload: $p, payload_encoding: "string", properties: {message_id: $id}}')" >/dev/null
done
sleep 3
PAYMENTS=$(get "/api/billing/shipments/$SHIPMENT_ID/invoice" | jq '.payments | length')
[ "$PAYMENTS" = 1 ] && ok "customer charged exactly once ($PAYMENTS payment)" || fail "customer charged $PAYMENTS times"

step "8. Saga failure branch: a delivery whose payment is declined (card limit)"
HEAVY_ID=$(create_shipment "$(cat /proc/sys/kernel/random/uuid 2>/dev/null || uuidgen)" 100)
deliver_shipment "$HEAVY_ID"
eventually "invoice declined" 30 'test "$(get "/api/billing/shipments/$HEAVY_ID" | jq -r .status)" = PaymentFailed'
eventually "shipment marked ShipmentDeliveryPaymentFailed" 30 'test "$(shipment_field "$HEAVY_ID" .paymentStatus)" = Failed'

step "9. Retry: the notification provider fails twice for a 'flaky' customer, then succeeds"
FLAKY_ID=$(create_shipment "$FLAKY_CUSTOMER" 1)
eventually "notification delivered after retries" 40 '[[ "$(get "/api/notifications?shipmentId=$FLAKY_ID")" == *"We received your shipment"* ]]'

step "10. Dead letter: the provider always fails for an 'unreachable' customer"
BEFORE=$(queue_messages notifications.dlq)
create_shipment "$UNREACHABLE_CUSTOMER" 1 >/dev/null
eventually "message parked in notifications.dlq after 3 retries" 60 'test "$(queue_messages notifications.dlq)" -gt "$BEFORE"'

step "11. Distributed tracing: one trace spans several services through the outbox and RabbitMQ"
JAEGER="${JAEGER:-http://localhost:16686}"
# Jaeger API v3 returns complete traces as OTLP JSON: group spans by trace id and count distinct services.
traces_json() {
  curl -fsS -G "$JAEGER/api/v3/traces" \
    --data-urlencode "query.service_name=api-gateway" \
    --data-urlencode "query.start_time_min=$(date -u -d '-1 hour' +%Y-%m-%dT%H:%M:%SZ)" \
    --data-urlencode "query.start_time_max=$(date -u -d '+1 minute' +%Y-%m-%dT%H:%M:%SZ)" \
    --data-urlencode "query.search_depth=100"
}
services_per_trace='[.result.resourceSpans[]
  | (.resource.attributes[] | select(.key == "service.name") | .value.stringValue) as $service
  | .scopeSpans[].spans[] | {trace: .traceId, service: $service}]
  | group_by(.trace) | map([.[].service] | unique)'
echo "  services reporting to Jaeger: $(curl -fsS "$JAEGER/api/v3/services" | jq -c .services)"
eventually "a single trace crosses at least 4 services" 60 'test "$(traces_json | jq "$services_per_trace | map(length) | max // 0")" -ge 4'
echo "  widest trace: $(traces_json | jq -c "$services_per_trace | max_by(length)")"

step "Done. Explore:"
echo "  RabbitMQ  $RABBIT  (deliver / deliver) - queues, DLQs, retry tiers"
echo "  Jaeger    http://localhost:16686  - search service 'api-gateway' to follow one request across services"
echo "  Gateway   $GATEWAY/api/shipments/$SHIPMENT_ID"
