export type Entity = 'Products' | 'Orders'
export type SearchRequest = { entity: Entity; query: string; page: number; pageSize: number; category?: string; active?: boolean; status?: string; createdFrom?: string; createdTo?: string }
export type SearchResultItem = { id: string; primaryText: string; secondaryText: string; category: string | null; status: string | null; amount: number | null; createdAt: string | null; score: number }
export type SearchResult = { entity: Entity; items: SearchResultItem[]; totalCount: number | null; page: number; pageSize: number; durationMilliseconds: number }

export async function searchAt(path: 'postgres' | 'mongo', request: SearchRequest): Promise<SearchResult> {
  const params = new URLSearchParams({ entity: request.entity, query: request.query, page: String(request.page), pageSize: String(request.pageSize) })
  for (const key of ['category', 'active', 'status', 'createdFrom', 'createdTo'] as const) {
    const value = request[key]
    if (value !== undefined && value !== '') params.set(key, String(value))
  }
  const response = await fetch(`/api/search/${path}?${params}`)
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { title?: string; detail?: string } | null
    throw new Error(problem?.detail || problem?.title || `Search failed (${response.status})`)
  }
  return response.json() as Promise<SearchResult>
}

export const searchPostgres = (request: SearchRequest) => searchAt('postgres', request)
export const searchMongo = (request: SearchRequest) => searchAt('mongo', request)
