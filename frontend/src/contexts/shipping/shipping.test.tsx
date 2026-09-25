import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, within } from '@testing-library/react'
import { createMemoryRouter, RouterProvider } from 'react-router'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { isSettled, nextAction, type ShipmentDetails } from './api'
import { ShipmentDetailPage } from './ShipmentDetailPage'
import { ShipmentTimeline } from './ShipmentTimeline'

const shipment: ShipmentDetails = {
  id: '01a0d928-7f16-7c57-a7aa-29d0a901ad6c',
  customerId: '5f0c6a8e-3d1b-4c55-9d0e-2a4b7c9e1f00',
  status: 'Created',
  driverId: null,
  pickupAddress: { street: 'Av. Providencia 1234', city: 'Santiago', postalCode: '7500000' },
  deliveryAddress: { street: 'Calle Valparaíso 55', city: 'Viña del Mar', postalCode: '2520000' },
  items: [{ description: 'Books', quantity: 2, unitWeightKg: 1.5 }],
  totalWeightKg: 3,
  price: null,
  currency: null,
  paymentStatus: 'Pending',
  createdAt: '2026-01-01T12:00:00Z',
  assignedAt: null,
  pickedUpAt: null,
  inTransitAt: null,
  deliveredAt: null,
  cancelledAt: null,
  cancellationReason: null,
}

describe('lifecycle helpers', () => {
  it('offers only the next valid step', () => {
    expect(nextAction('Created')).toBeNull()
    expect(nextAction('Assigned')).toBe('pickup')
    expect(nextAction('PickedUp')).toBe('transit')
    expect(nextAction('InTransit')).toBe('deliver')
    expect(nextAction('Delivered')).toBeNull()
  })

  it('keeps polling a delivered shipment until Billing reports the payment', () => {
    expect(isSettled({ ...shipment, status: 'Delivered', paymentStatus: 'Pending' })).toBe(false)
    expect(isSettled({ ...shipment, status: 'Delivered', paymentStatus: 'Captured' })).toBe(true)
    expect(isSettled({ ...shipment, status: 'Cancelled' })).toBe(true)
  })
})

describe('ShipmentTimeline', () => {
  it('marks reached steps and the current one', () => {
    render(<ShipmentTimeline shipment={{ ...shipment, status: 'Assigned', assignedAt: '2026-01-01T12:01:00Z' }} />)

    const steps = within(screen.getByRole('list', { name: 'Shipment lifecycle' })).getAllByRole('listitem')
    expect(steps.map((step) => step.className)).toEqual([
      'timeline-step done',
      'timeline-step current',
      'timeline-step todo',
      'timeline-step todo',
      'timeline-step todo',
    ])
  })

  it('ends at Cancelled for cancelled shipments', () => {
    render(<ShipmentTimeline shipment={{ ...shipment, status: 'Cancelled', cancelledAt: '2026-01-01T12:05:00Z' }} />)

    const labels = screen.getAllByRole('listitem').map((step) => step.querySelector('.timeline-label')?.textContent)
    expect(labels).toEqual(['Created', 'Cancelled'])
    expect(screen.queryByText('Delivered')).not.toBeInTheDocument()
  })
})

describe('ShipmentDetailPage', () => {
  afterEach(() => vi.unstubAllGlobals())

  function renderPage(responses: Record<string, unknown>) {
    vi.stubGlobal('fetch', vi.fn(async (url: string) => {
      const path = url.split('?')[0]
      const body = responses[path]
      return body === undefined
        ? new Response(null, { status: 404 })
        : new Response(JSON.stringify(body), { status: 200, headers: { 'Content-Type': 'application/json' } })
    }))

    const router = createMemoryRouter([{ path: '/shipments/:id', element: <ShipmentDetailPage /> }], {
      initialEntries: [`/shipments/${shipment.id}`],
    })
    render(
      <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
        <RouterProvider router={router} />
      </QueryClientProvider>,
    )
  }

  it('shows the price as pending until Billing has priced the shipment', async () => {
    renderPage({ [`/api/shipments/${shipment.id}`]: shipment, '/api/notifications': [] })

    expect(await screen.findByTestId('shipment-price')).toHaveTextContent('Billing is pricing it')
    expect(await screen.findByText('Billing has not priced this shipment yet')).toBeInTheDocument()
    expect(await screen.findByText('Dispatch has not received ShipmentCreated yet')).toBeInTheDocument()
  })

  it('composes data owned by Shipping, Dispatch and Billing once they have reacted', async () => {
    renderPage({
      [`/api/shipments/${shipment.id}`]: { ...shipment, status: 'Assigned', price: 13000, currency: 'CLP', assignedAt: '2026-01-01T12:01:00Z' },
      [`/api/dispatch/assignments/${shipment.id}`]: {
        id: 'a', shipmentId: shipment.id, status: 'Assigned', driverId: 'd', driverName: 'Carlos',
        requiredCapacityKg: 3, rejectedDriverIds: [], createdAt: '2026-01-01T12:00:00Z', assignedAt: '2026-01-01T12:01:00Z',
      },
      [`/api/billing/shipments/${shipment.id}/invoice`]: {
        invoiceId: 'i', shipmentId: shipment.id, customerId: shipment.customerId, amount: 13000, currency: 'CLP',
        status: 'Open', issuedAt: '2026-01-01T12:00:00Z', paidAt: null, voidedAt: null, payments: [],
      },
      '/api/notifications': [],
    })

    expect(await screen.findByTestId('assigned-driver')).toHaveTextContent('Carlos')
    expect(await screen.findByTestId('shipment-price')).toHaveTextContent('13,000')
    expect(screen.getByRole('button', { name: 'Mark picked up' })).toBeEnabled()
  })
})
