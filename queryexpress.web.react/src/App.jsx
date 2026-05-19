import React, { useEffect, useMemo, useState, useRef, useCallback } from 'react'
import {
  useReactTable,
  getCoreRowModel,
  getSortedRowModel,
  getFilteredRowModel,
} from '@tanstack/react-table'
import { useVirtualizer } from '@tanstack/react-virtual'
import './App.css'

const PAGE_SIZE = 20

function App() {
  const [data, setData] = useState([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState(null)
  const [pageNum, setPageNum] = useState(1)
  const [hasMore, setHasMore] = useState(true)

  const [sorting, setSorting] = useState([]) // [{id, desc}]
  const [filters, setFilters] = useState({}) // {columnId: value}

  const tableColumns = useMemo(
    () => [
      { accessorKey: 'firstName', header: 'First Name' },
      { accessorKey: 'lastName', header: 'Last Name' },
      { accessorKey: 'email', header: 'Email' },
      { accessorKey: 'age', header: 'Age' },
      { accessorKey: 'litersUsed', header: 'Liters Used' },
      { accessorKey: 'createdAt', header: 'Created At' },
      { accessorKey: 'updatedAt', header: 'Updated At' },
      { accessorKey: 'isEligibile', header: 'Is Eligible' },
      { accessorKey: 'isUtilized', header: 'Is Utilized' },
    ],
    []
  )

  const table = useReactTable({
    data,
    columns: tableColumns,
    state: { sorting },
    onSortingChange: setSorting,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: getSortedRowModel(),
    getFilteredRowModel: getFilteredRowModel(),
    manualPagination: true,
    manualSorting: true,
    manualFiltering: true,
    pageCount: -1,
  })

  // Fetch page of data from server using DataQuery POST body
  const fetchPage = useCallback(
    async (pNum) => {
      setLoading(true)
      const dq = {
        PageData: { PageNum: pNum, PageSize: PAGE_SIZE },
        SortData: (sorting || []).map((s) => ({ ColumnName: s.id, SortDirection: s.desc ? 'Desc' : 'Asc' })),
        FilterData: Object.entries(filters).map(([col, val]) => ({ Operand: col, Value: String(val), Operation: 'Contains' })),
      }

      try {
          const res = await fetch('https://localhost:7233/api/person', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(dq),
        })
        if (!res.ok) throw new Error(`${res.status} ${res.statusText}`)
        const items = await res.json()

        if (pNum === 1) setData(items)
        else setData((prev) => [...prev, ...items])

        setHasMore(items.length >= PAGE_SIZE)
        setError(null)
      } catch (err) {
        setError(err.message)
      } finally {
        setLoading(false)
      }
    },
    [sorting, filters]
  )

  // Reset and fetch when sorting or filters change
  useEffect(() => {
    setPageNum(1)
    setData([])
    setHasMore(true)
    fetchPage(1)
  }, [sorting, filters, fetchPage])

  useEffect(() => {
    if (pageNum === 1) return
    fetchPage(pageNum)
  }, [pageNum, fetchPage])

  // Virtualization for infinite scroll
  const parentRef = useRef()
  const rowVirtualizer = useVirtualizer({
    count: data.length + (hasMore ? 1 : 0),
    getScrollElement: () => parentRef.current,
    estimateSize: () => 50,
    overscan: 5,
  })

  // Watch scroll to load more
  useEffect(() => {
    const el = parentRef.current
    if (!el) return
    const onScroll = () => {
      const { scrollTop, scrollHeight, clientHeight } = el
      if (scrollTop + clientHeight >= scrollHeight - 200 && !loading && hasMore) {
        setPageNum((p) => p + 1)
      }
    }
    el.addEventListener('scroll', onScroll)
    return () => el.removeEventListener('scroll', onScroll)
  }, [loading, hasMore])

  return (
    <div className="app-container">
      <h1>People (server-side)</h1>

      <div className="controls">
        {tableColumns.map((c) => (
          <div key={c.accessorKey} className="filter">
            <label>{c.header}</label>
            <input
              value={filters[c.accessorKey] ?? ''}
              onChange={(e) => setFilters((f) => ({ ...f, [c.accessorKey]: e.target.value }))}
              placeholder={`Filter ${c.header}`}
            />
          </div>
        ))}
      </div>

      {error && <div className="error">Error: {error}</div>}

      <div ref={parentRef} className="grid-viewport">
        <table className="data-grid">
          <thead>
            {table.getHeaderGroups().map((hg) => (
              <tr key={hg.id}>
                {hg.headers.map((h) => (
                  <th
                    key={h.id}
                    onClick={() => {
                      const id = h.column.id
                      const current = sorting.find((s) => s.id === id)
                      if (!current) setSorting([{ id, desc: false }])
                      else if (!current.desc) setSorting([{ id, desc: true }])
                      else setSorting([])
                    }}
                  >
                    {h.column.columnDef.header}
                    {sorting.find((s) => s.id === h.column.id) ? (
                      sorting.find((s) => s.id === h.column.id).desc ? ' 🔽' : ' 🔼'
                    ) : null}
                  </th>
                ))}
              </tr>
            ))}
          </thead>

          <tbody>
            <tr style={{ height: rowVirtualizer.getTotalSize ? `${rowVirtualizer.getTotalSize()}px` : undefined }}>
              <td colSpan={tableColumns.length} style={{ padding: 0, border: 0 }}>
                <div style={{ height: rowVirtualizer.totalSize, position: 'relative' }}>
                  {rowVirtualizer.getVirtualItems().map((virtualRow) => {
                    const index = virtualRow.index
                    const isLoaderRow = index > data.length - 1
                    const item = data[index]
                    return (
                      <div
                        key={virtualRow.index}
                        style={{
                          position: 'absolute',
                          top: 0,
                          left: 0,
                          width: '100%',
                          transform: `translateY(${virtualRow.start}px)`,
                        }}
                      >
                        {isLoaderRow ? (
                          <div className="loading-row">{loading ? 'Loading...' : hasMore ? 'Load more' : 'No more'}</div>
                        ) : (
                          <table className="inner-row">
                            <tbody>
                              <tr>
                                {tableColumns.map((c) => (
                                  <td key={c.accessorKey}>{formatCell(item, c.accessorKey)}</td>
                                ))}
                              </tr>
                            </tbody>
                          </table>
                        )}
                      </div>
                    )
                  })}
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      {loading && <div className="loading-indicator">Loading...</div>}
    </div>
  )
}

function formatCell(item, key) {
  if (!item) return ''
  const v = item[key] ?? item[camelToPascal(key)] ?? item[pascalToCamel(key)]
  if (v === null || v === undefined) return ''
  if (typeof v === 'boolean') return v ? 'Yes' : 'No'
  return String(v)
}

function camelToPascal(s) {
  if (!s) return s
  return s.charAt(0).toUpperCase() + s.slice(1)
}

function pascalToCamel(s) {
  if (!s) return s
  return s.charAt(0).toLowerCase() + s.slice(1)
}

export default App
