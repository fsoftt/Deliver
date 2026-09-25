export function formatMoney(amount: number | null | undefined, currency: string | null | undefined): string {
  if (amount === null || amount === undefined) return '—'
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: currency ?? 'CLP',
    maximumFractionDigits: 0,
  }).format(amount)
}

export function formatDateTime(value: string | null | undefined): string {
  if (!value) return '—'
  return new Date(value).toLocaleString('en-GB', { dateStyle: 'medium', timeStyle: 'short' })
}

export function formatTime(value: string | null | undefined): string {
  if (!value) return ''
  return new Date(value).toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit', second: '2-digit' })
}

export function formatMinutes(minutes: number | null | undefined): string {
  if (minutes === null || minutes === undefined) return '—'
  if (minutes < 1) return `${Math.round(minutes * 60)} s`
  if (minutes < 60) return `${minutes.toFixed(1)} min`
  return `${(minutes / 60).toFixed(1)} h`
}

export function shortId(id: string | null | undefined): string {
  return id ? id.slice(-8) : '—'
}

/** Splits PascalCase status names for display: "InTransit" → "In transit". */
export function humanize(value: string): string {
  const spaced = value.replace(/([a-z])([A-Z])/g, '$1 $2').toLowerCase()
  return spaced.charAt(0).toUpperCase() + spaced.slice(1)
}
