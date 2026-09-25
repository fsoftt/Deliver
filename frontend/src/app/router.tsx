import { createBrowserRouter } from 'react-router'
import { OverviewPage } from '../contexts/analytics/OverviewPage'
import { DriversPage } from '../contexts/fleet/DriversPage'
import { NewShipmentPage } from '../contexts/shipping/NewShipmentPage'
import { ShipmentDetailPage } from '../contexts/shipping/ShipmentDetailPage'
import { ShipmentsPage } from '../contexts/shipping/ShipmentsPage'
import { Layout } from './Layout'

export const router = createBrowserRouter([
  {
    element: <Layout />,
    children: [
      { index: true, element: <OverviewPage /> },
      { path: 'shipments', element: <ShipmentsPage /> },
      { path: 'shipments/new', element: <NewShipmentPage /> },
      { path: 'shipments/:id', element: <ShipmentDetailPage /> },
      { path: 'drivers', element: <DriversPage /> },
    ],
  },
])
