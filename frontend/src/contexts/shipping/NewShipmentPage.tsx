import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router'
import { Card, ErrorBanner } from '../../shared/ui'
import { useCreateShipment, type Address, type ShipmentItem } from './api'

/**
 * Demo customers wired to the fake notification provider in docker compose, so the UI can trigger the
 * retry and dead-letter paths without touching any code.
 */
const customers = [
  { id: 'new', label: 'New customer (random id)' },
  { id: '00000000-0000-0000-0000-0000000f1a4e', label: 'Flaky customer: notifications fail twice, then retry succeeds' },
  { id: '00000000-0000-0000-0000-00000000dead', label: 'Unreachable customer: notifications end in the dead-letter queue' },
]

const emptyItem: ShipmentItem = { description: '', quantity: 1, unitWeightKg: 1 }

export function NewShipmentPage() {
  const navigate = useNavigate()
  const createShipment = useCreateShipment()
  const [customer, setCustomer] = useState(customers[0].id)
  const [pickup, setPickup] = useState<Address>({ street: 'Av. Providencia 1234', city: 'Santiago', postalCode: '7500000' })
  const [delivery, setDelivery] = useState<Address>({ street: 'Calle Valparaíso 55', city: 'Viña del Mar', postalCode: '2520000' })
  const [items, setItems] = useState<ShipmentItem[]>([{ description: 'Books', quantity: 2, unitWeightKg: 1.5 }])

  const totalWeight = items.reduce((sum, item) => sum + item.quantity * item.unitWeightKg, 0)

  function updateItem(index: number, patch: Partial<ShipmentItem>) {
    setItems((current) => current.map((item, i) => (i === index ? { ...item, ...patch } : item)))
  }

  async function submit(event: FormEvent) {
    event.preventDefault()
    const { id } = await createShipment.mutateAsync({
      customerId: customer === 'new' ? crypto.randomUUID() : customer,
      pickupAddress: pickup,
      deliveryAddress: delivery,
      items,
    })
    navigate(`/shipments/${id}`)
  }

  return (
    <>
      <header className="page-header">
        <div>
          <h1>New shipment</h1>
          <p className="lead">
            The request returns <code>201</code> as soon as Shipping has stored the shipment and its
            <code>ShipmentCreated</code> event (one transaction). Everything else happens asynchronously.
          </p>
        </div>
      </header>

      <form className="stack" onSubmit={submit}>
        <ErrorBanner error={createShipment.error} />

        <Card title="Customer">
          <label className="field">
            <span>Customer</span>
            <select value={customer} onChange={(e) => setCustomer(e.target.value)} name="customer">
              {customers.map((c) => <option key={c.id} value={c.id}>{c.label}</option>)}
            </select>
          </label>
        </Card>

        <div className="grid-2">
          <AddressCard title="Pickup" address={pickup} onChange={setPickup} name="pickup" />
          <AddressCard title="Delivery" address={delivery} onChange={setDelivery} name="delivery" />
        </div>

        <Card
          title="Items"
          subtitle={`Total weight ${totalWeight.toFixed(2)} kg. Billing prices every started kilogram, and Dispatch needs a vehicle that can carry it.`}
          actions={<button type="button" className="button" onClick={() => setItems([...items, emptyItem])}>Add item</button>}
        >
          {items.map((item, index) => (
            <div className="item-row" key={index}>
              <label className="field grow">
                <span>Description</span>
                <input name={`item-${index}-description`} value={item.description} onChange={(e) => updateItem(index, { description: e.target.value })} />
              </label>
              <label className="field">
                <span>Quantity</span>
                <input name={`item-${index}-quantity`} type="number" min={1} value={item.quantity} onChange={(e) => updateItem(index, { quantity: Number(e.target.value) })} />
              </label>
              <label className="field">
                <span>Unit weight (kg)</span>
                <input name={`item-${index}-weight`} type="number" min={0.1} step={0.1} value={item.unitWeightKg} onChange={(e) => updateItem(index, { unitWeightKg: Number(e.target.value) })} />
              </label>
              <button type="button" className="button ghost" aria-label="Remove item" disabled={items.length === 1}
                onClick={() => setItems(items.filter((_, i) => i !== index))}>✕</button>
            </div>
          ))}
        </Card>

        <div className="form-actions">
          <button type="submit" className="button primary" disabled={createShipment.isPending}>
            {createShipment.isPending ? 'Creating…' : 'Create shipment'}
          </button>
        </div>
      </form>
    </>
  )
}

function AddressCard({ title, address, onChange, name }: {
  title: string
  address: Address
  onChange: (address: Address) => void
  name: string
}) {
  return (
    <Card title={title}>
      <label className="field">
        <span>Street</span>
        <input name={`${name}-street`} value={address.street} onChange={(e) => onChange({ ...address, street: e.target.value })} />
      </label>
      <div className="grid-2 tight">
        <label className="field">
          <span>City</span>
          <input name={`${name}-city`} value={address.city} onChange={(e) => onChange({ ...address, city: e.target.value })} />
        </label>
        <label className="field">
          <span>Postal code</span>
          <input name={`${name}-postal`} value={address.postalCode} onChange={(e) => onChange({ ...address, postalCode: e.target.value })} />
        </label>
      </div>
    </Card>
  )
}
