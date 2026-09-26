import type { FormEvent } from 'react'
import { useState } from 'react'
import type { Entity, SearchRequest } from '../api/search'

export function SearchForm({ entity, onSearch, busy }: { entity: Entity; onSearch: (request: SearchRequest) => void; busy: boolean }) {
  const [query, setQuery] = useState('')
  const [category, setCategory] = useState('')
  const [status, setStatus] = useState('')
  const [active, setActive] = useState('')
  const [createdFrom, setCreatedFrom] = useState('')
  const [createdTo, setCreatedTo] = useState('')
  function submit(event: FormEvent) {
    event.preventDefault()
    onSearch({ entity, query, page: 1, pageSize: 20, ...(entity === 'Products' ? { category: category || undefined, active: active === '' ? undefined : active === 'true' } : { status: status || undefined, createdFrom: createdFrom ? new Date(createdFrom).toISOString() : undefined, createdTo: createdTo ? new Date(createdTo).toISOString() : undefined }) })
  }
  return <form className="search-form" onSubmit={submit}>
    <label className="search-label" htmlFor="query">Search {entity.toLowerCase()}</label>
    <div className="search-line"><div className="search-input-wrap"><span aria-hidden="true">⌕</span><input id="query" value={query} onChange={event => setQuery(event.target.value)} placeholder={entity === 'Products' ? 'Try “wireless keyboard”' : 'Try an order number or product'} maxLength={200} /></div><button className="primary-button" disabled={busy}>{busy ? 'Searching…' : 'Search'}</button></div>
    {entity === 'Products' ? <div className="filters"><label>Category<select value={category} onChange={event => setCategory(event.target.value)}><option value="">All categories</option><option>Accessories</option><option>Electronics</option><option>Furniture</option></select></label><label>Status<select value={active} onChange={event => setActive(event.target.value)}><option value="">Any status</option><option value="true">Active</option><option value="false">Inactive</option></select></label></div> : <div className="filters"><label>Order status<select value={status} onChange={event => setStatus(event.target.value)}><option value="">All statuses</option>{['Pending', 'Processing', 'Shipped', 'Delivered', 'Cancelled'].map(value => <option key={value}>{value}</option>)}</select></label><label>From<input type="date" value={createdFrom} onChange={event => setCreatedFrom(event.target.value)} /></label><label>To<input type="date" value={createdTo} onChange={event => setCreatedTo(event.target.value)} /></label></div>}
  </form>
}
