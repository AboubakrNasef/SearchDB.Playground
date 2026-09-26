import type { SearchResult } from '../api/search'

export function SearchResults({ result, loading, error, onPage }: { result: SearchResult | null; loading: boolean; error: string | null; onPage: (page: number) => void }) {
  if (loading) return <div className="state" role="status">Searching…</div>
  if (error) return <div className="state error" role="alert">{error}</div>
  if (!result) return <div className="state">Search products or orders to see results.</div>
  return <section aria-live="polite">
    <div className="result-summary"><strong>{result.totalCount ?? '—'} results</strong><span>{result.durationMilliseconds.toFixed(1)} ms provider time</span></div>
    {result.items.length === 0 ? <div className="state">No results found. Try changing your search or filters.</div> : <div className="result-list">
      {result.items.map(item => <article className="result-card" key={item.id}>
        <div className="result-icon" aria-hidden="true">{result.entity === 'Products' ? 'P' : 'O'}</div>
        <div className="result-copy"><div className="result-title">{item.primaryText}</div><div className="result-subtitle">{item.secondaryText}</div>
          <div className="chips">{item.category && <span className="chip">{item.category}</span>}{item.status && <span className="chip">{item.status}</span>}</div>
        </div>
        <div className="result-meta">{item.amount != null && <strong>${item.amount.toFixed(2)}</strong>}{item.createdAt && <time>{new Date(item.createdAt).toLocaleDateString()}</time>}</div>
      </article>)}
    </div>}
    {(result.totalCount ?? 0) > result.pageSize && <div className="pagination"><button disabled={loading || result.page <= 1} onClick={() => onPage(result.page - 1)}>Previous</button><span>Page {result.page} of {Math.ceil((result.totalCount ?? 0) / result.pageSize)}</span><button disabled={loading || result.page * result.pageSize >= (result.totalCount ?? 0)} onClick={() => onPage(result.page + 1)}>Next</button></div>}
  </section>
}
