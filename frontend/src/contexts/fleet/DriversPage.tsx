import { useState, type FormEvent } from 'react'
import { Link } from 'react-router'
import { Card, EmptyState, ErrorBanner, StatusBadge } from '../../shared/ui'
import { shortId } from '../../shared/format'
import { useChangeAvailability, useDrivers, useRegisterDriver, type VehicleType } from './api'

const capacityByType: Record<VehicleType, number> = { Motorcycle: 20, Car: 150, Van: 800 }

export function DriversPage() {
  const { data: drivers, error } = useDrivers()
  const changeAvailability = useChangeAvailability()

  return (
    <>
      <header className="page-header">
        <div>
          <h1>Drivers</h1>
          <p className="lead">
            Owned by the Fleet service. Starting a shift publishes <code>driver.available</code>, and Dispatch immediately
            offers that driver the oldest pending shipment their vehicle can carry.
          </p>
        </div>
      </header>

      <div className="grid-sidebar">
        <RegisterDriverForm />

        <Card title="Fleet">
          <ErrorBanner error={error ?? changeAvailability.error} />
          {drivers && drivers.length === 0 && <EmptyState>No drivers yet. Register one to start dispatching.</EmptyState>}
          {drivers && drivers.length > 0 && (
            <table className="table">
              <thead>
                <tr>
                  <th>Driver</th>
                  <th>Vehicle</th>
                  <th>Availability</th>
                  <th>Current shipment</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {drivers.map((driver) => (
                  <tr key={driver.id}>
                    <td><strong>{driver.name}</strong><br /><span className="muted">{driver.phone}</span></td>
                    <td>{driver.vehicle ? `${driver.vehicle.type} · ${driver.vehicle.plate} · ${driver.vehicle.capacityKg} kg` : '—'}</td>
                    <td><StatusBadge status={driver.availability} /></td>
                    <td>{driver.currentShipmentId
                      ? <Link className="mono" to={`/shipments/${driver.currentShipmentId}`}>#{shortId(driver.currentShipmentId)}</Link>
                      : '—'}</td>
                    <td className="right">
                      {driver.availability === 'Unavailable' && (
                        <button className="button" onClick={() => changeAvailability.mutate({ id: driver.id, available: true })}>Start shift</button>
                      )}
                      {driver.availability === 'Available' && (
                        <button className="button ghost" onClick={() => changeAvailability.mutate({ id: driver.id, available: false })}>End shift</button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </Card>
      </div>
    </>
  )
}

function RegisterDriverForm() {
  const register = useRegisterDriver()
  const [name, setName] = useState('Carlos')
  const [phone, setPhone] = useState('+56 9 1234 5678')
  const [type, setType] = useState<VehicleType>('Van')
  const [plate, setPlate] = useState('ABCD12')

  function submit(event: FormEvent) {
    event.preventDefault()
    register.mutate({ name, phone, vehicle: { type, plate, capacityKg: capacityByType[type] } })
  }

  return (
    <Card title="Register driver" subtitle="New drivers start off shift.">
      <form className="stack" onSubmit={submit}>
        <ErrorBanner error={register.error} />
        <label className="field"><span>Name</span><input name="driver-name" value={name} onChange={(e) => setName(e.target.value)} /></label>
        <label className="field"><span>Phone</span><input name="driver-phone" value={phone} onChange={(e) => setPhone(e.target.value)} /></label>
        <label className="field">
          <span>Vehicle</span>
          <select name="vehicle-type" value={type} onChange={(e) => setType(e.target.value as VehicleType)}>
            {Object.entries(capacityByType).map(([vehicle, capacity]) => (
              <option key={vehicle} value={vehicle}>{vehicle} (up to {capacity} kg)</option>
            ))}
          </select>
        </label>
        <label className="field"><span>Plate</span><input name="vehicle-plate" value={plate} onChange={(e) => setPlate(e.target.value)} /></label>
        <button className="button primary" type="submit" disabled={register.isPending}>Register</button>
      </form>
    </Card>
  )
}
