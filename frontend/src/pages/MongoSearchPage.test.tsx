import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import MongoSearchPage from './MongoSearchPage'

describe('MongoDB search page', () => {
  afterEach(() => vi.unstubAllGlobals())
  it('searches its provider endpoint with equivalent search controls', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ entity: 'Orders', items: [], totalCount: 0, page: 1, pageSize: 20, durationMilliseconds: 3 }), { status: 200, headers: { 'Content-Type': 'application/json' } }))
    vi.stubGlobal('fetch', fetchMock)
    render(<MongoSearchPage />)
    fireEvent.click(screen.getByRole('tab', { name: 'Orders' }))
    fireEvent.change(screen.getByLabelText('Search orders'), { target: { value: 'ORD-1001' } })
    fireEvent.click(screen.getByRole('button', { name: 'Search' }))
    await waitFor(() => expect(fetchMock).toHaveBeenCalledOnce())
    expect(String(fetchMock.mock.calls[0][0])).toContain('/api/search/mongo?')
    expect(String(fetchMock.mock.calls[0][0])).toContain('entity=Orders')
    expect(String(fetchMock.mock.calls[0][0])).not.toContain('/postgres')
  })
})
