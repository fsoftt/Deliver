import { describe, expect, it } from 'vitest'
import { formatMinutes, formatMoney, humanize, shortId } from './format'

describe('format', () => {
  it('formats money without decimals (CLP has no cents)', () => {
    expect(formatMoney(13000, 'CLP')).toMatch(/^CLP\s13,000$/)
    expect(formatMoney(null, 'CLP')).toBe('—')
  })

  it('formats durations for humans', () => {
    expect(formatMinutes(0.5)).toBe('30 s')
    expect(formatMinutes(12.34)).toBe('12.3 min')
    expect(formatMinutes(90)).toBe('1.5 h')
    expect(formatMinutes(null)).toBe('—')
  })

  it('humanizes PascalCase statuses', () => {
    expect(humanize('InTransit')).toBe('In transit')
    expect(humanize('PaymentFailed')).toBe('Payment failed')
  })

  it('shortens ids to their last 8 characters', () => {
    expect(shortId('01a0d928-7f16-7c57-a7aa-29d0a901ad6c')).toBe('a901ad6c')
  })
})
