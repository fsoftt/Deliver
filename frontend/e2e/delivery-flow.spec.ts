import { expect, test, type Page } from '@playwright/test'

const screenshot = (page: Page, name: string) =>
  page.screenshot({ path: `e2e/screenshots/${name}.png`, fullPage: true })

test('a shipment goes from creation to paid delivery, with every service reacting', async ({ page }) => {
  const driverName = `Ana ${Date.now().toString().slice(-4)}`

  // Fleet: register a driver and start the shift → driver.available → Dispatch's local projection.
  await page.goto('/drivers')
  await page.getByLabel('Name').fill(driverName)
  await page.getByLabel('Vehicle').selectOption('Car')
  await page.getByRole('button', { name: 'Register' }).click()
  const driverRow = page.getByRole('row', { name: new RegExp(driverName) })
  await driverRow.getByRole('button', { name: 'Start shift' }).click()
  await expect(driverRow.getByTestId('status-badge')).toHaveText('Available')

  // Shipping: create a shipment. The response comes back before any other service has reacted.
  await page.goto('/shipments/new')
  await page.getByRole('button', { name: 'Create shipment' }).click()
  await expect(page).toHaveURL(/\/shipments\/[0-9a-f-]{36}$/)

  // Billing and Dispatch fill in their parts asynchronously.
  await expect(page.getByTestId('shipment-price')).toContainText('CLP')
  await expect(page.getByTestId('assigned-driver')).not.toBeEmpty()
  await expect(page.getByRole('button', { name: 'Mark picked up' })).toBeVisible()
  await screenshot(page, 'shipment-assigned')

  // The driver moves the shipment through its lifecycle; the aggregate only allows the next step.
  await page.getByRole('button', { name: 'Mark picked up' }).click()
  await page.getByRole('button', { name: 'Start transit' }).click()
  await page.getByRole('button', { name: 'Mark delivered' }).click()

  // Delivery fan-out: Billing charges exactly once, Notifications tells the customer.
  await expect(page.getByTestId('invoice-status')).toHaveText('Paid')
  await expect(page.getByTestId('notifications')).toContainText('delivered')
  await screenshot(page, 'shipment-delivered')

  await page.goto('/shipments')
  await expect(page.getByRole('table')).toBeVisible()
  await screenshot(page, 'shipments')

  await page.goto('/drivers')
  await expect(page.getByRole('table')).toBeVisible()
  await screenshot(page, 'drivers')

  await page.goto('/')
  await expect(page.getByText('Revenue captured')).toBeVisible()
  await page.waitForTimeout(3500) // let the Analytics read model catch up with the last events
  await screenshot(page, 'overview')
})

test('invalid input is rejected with every field error at once', async ({ page }) => {
  await page.goto('/shipments/new')
  await page.getByLabel('Street').first().fill('')
  await page.getByLabel('Description').fill('')
  await page.getByRole('button', { name: 'Create shipment' }).click()

  const alert = page.getByRole('alert')
  await expect(alert).toContainText('Validation failed')
  await expect(alert).toContainText('PickupAddress.Street')
  await expect(alert).toContainText('Items[0].Description')
  await screenshot(page, 'validation-errors')
})
