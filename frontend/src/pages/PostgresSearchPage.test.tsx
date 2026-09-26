import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import PostgresSearchPage from './PostgresSearchPage'

describe('PostgreSQL search page', () => {
  afterEach(() => vi.unstubAllGlobals())
  it('searches its provider endpoint with the shared search controls', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ entity: 'Products', items: [], totalCount: 0, page: 1, pageSize: 20, durationMilliseconds: 2.5 }), { status: 200, headers: { 'Content-Type': 'application/json' } }))
    vi.stubGlobal('fetch', fetchMock)
    render(<PostgresSearchPage />)
    fireEvent.change(screen.getByLabelText('Search products'), { target: { value: 'keyboard' } })
    fireEvent.click(screen.getByRole('button', { name: 'Search' }))
    await waitFor(() => expect(fetchMock).toHaveBeenCalledOnce())
    expect(String(fetchMock.mock.calls[0][0])).toContain('/api/search/postgres?')
    expect(String(fetchMock.mock.calls[0][0])).toContain('query=keyboard')
    expect(String(fetchMock.mock.calls[0][0])).not.toContain('/mongo')
  })
})
