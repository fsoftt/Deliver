# Run it locally

You only need **Docker**. The .NET 10 SDK and Node.js are needed only to develop outside containers.

```bash
git clone git@github.com:fsoftt/Deliver.git
cd Deliver
docker compose up --build
```

| URL | What |
|---|---|
| http://localhost:3000 | The web UI (control tower) |
| http://localhost:5000 | The API gateway |
| http://localhost:15672 | RabbitMQ management (`deliver` / `deliver`): queues, retry tiers, DLQs |
| http://localhost:16686 | Jaeger: search service `api-gateway` and follow one request across every service |

## A guided tour

In the UI:

1. **Drivers** → register a driver → **Start shift**.
2. **Shipments** → **New shipment** → **Create**. Watch the price and the driver arrive a moment later.
3. Use **Mark picked up → Start transit → Mark delivered**, then watch the invoice become **Paid**.
4. Create a shipment for the **flaky customer**, and the notification succeeds after two automatic retries.
5. Create a shipment for the **unreachable customer**, and the notification ends in `notifications.dlq` in RabbitMQ.
6. Create a heavy shipment (100 kg), deliver it, and the payment is **declined**. That is the saga's failure branch.

Or run the whole Definition of Done as a script:

```bash
./scripts/demo.sh
```

## Tests

```bash
dotnet test --solution Deliver.slnx     # needs Docker for the Testcontainers suites
cd frontend && npm install && npm test   # component tests
npm run e2e                              # Playwright against a running docker compose
```
