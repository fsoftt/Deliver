import { NavLink, Outlet } from 'react-router'

const navigation = [
  { to: '/', label: 'Overview', context: 'Analytics', end: true },
  { to: '/shipments', label: 'Shipments', context: 'Shipping' },
  { to: '/drivers', label: 'Drivers', context: 'Fleet' },
]

const tools = [
  { href: 'http://localhost:15672', label: 'RabbitMQ' },
  { href: 'http://localhost:16686', label: 'Jaeger traces' },
  { href: 'https://github.com/fsoftt/Deliver', label: 'Source code' },
]

export function Layout() {
  return (
    <div className="shell">
      <aside className="sidebar">
        <NavLink to="/" className="brand">
          <span className="brand-mark" aria-hidden>▲</span>
          Deliver
          <span className="brand-sub">control tower</span>
        </NavLink>
        <nav className="nav">
          {navigation.map((item) => (
            <NavLink key={item.to} to={item.to} end={item.end} className="nav-link">
              <span>{item.label}</span>
              <span className="nav-context">{item.context}</span>
            </NavLink>
          ))}
        </nav>
        <div className="sidebar-footer">
          <p className="sidebar-heading">Behind the scenes</p>
          {tools.map((tool) => (
            <a key={tool.href} href={tool.href} target="_blank" rel="noreferrer">{tool.label} ↗</a>
          ))}
        </div>
      </aside>
      <main className="content">
        <Outlet />
      </main>
    </div>
  )
}
