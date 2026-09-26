import { useState } from 'react'
import { searchMongo, searchPostgres, type Entity, type SearchRequest, type SearchResult } from '../api/search'
import { SearchForm } from '../components/SearchForm'
import { SearchResults } from '../components/SearchResults'

export function SearchPage({ backend }: { backend: 'postgres' | 'mongo' }) {
  const [entity, setEntity] = useState<Entity>('Products')
  const [result, setResult] = useState<SearchResult | null>(null)
  const [lastRequest, setLastRequest] = useState<SearchRequest | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  async function runSearch(request: SearchRequest) {
    setLastRequest(request)
    setLoading(true); setError(null)
    try { setResult(await (backend === 'postgres' ? searchPostgres(request) : searchMongo(request))) }
    catch (exception) { setResult(null); setError(exception instanceof Error ? exception.message : 'Search failed. Please try again.') }
    finally { setLoading(false) }
  }
  const isMongo = backend === 'mongo'
  return <main className="page-shell">
    <header className="page-heading"><div><div className="eyebrow">DATABASE SEARCH COMPARISON</div><h1>{isMongo ? 'MongoDB Atlas Search' : 'PostgreSQL Search'}</h1><p>Explore your catalog and orders using {isMongo ? 'MongoDB’s relevance search' : 'PostgreSQL full-text search'}.</p></div><span className={`backend-badge ${backend}`}>{isMongo ? 'MongoDB' : 'PostgreSQL'}</span></header>
    <section className="search-panel"><div className="entity-switch" role="tablist" aria-label="Search data"><button role="tab" aria-selected={entity === 'Products'} onClick={() => setEntity('Products')}>Products</button><button role="tab" aria-selected={entity === 'Orders'} onClick={() => setEntity('Orders')}>Orders</button></div><SearchForm key={entity} entity={entity} onSearch={runSearch} busy={loading} /></section>
    <div className="results-heading"><h2>Results</h2>{result && <span>Page {result.page}</span>}</div><SearchResults result={result} loading={loading} error={error} onPage={page => lastRequest && runSearch({ ...lastRequest, page })} />
  </main>
}
