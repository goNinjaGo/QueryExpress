import { useEffect, useMemo, useState, useCallback } from 'react'
import {
  useReactTable,
  getCoreRowModel,
  getSortedRowModel,
  getFilteredRowModel,
} from '@tanstack/react-table'
import { DateTimePicker } from 'react-datetime-picker'
import './App.css'
import 'react-datetime-picker/dist/DateTimePicker.css';
import 'react-calendar/dist/Calendar.css';
import 'react-clock/dist/Clock.css';

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
      { accessorKey: 'firstName', header: 'First Name', dataType: 'string' },
      { accessorKey: 'lastName', header: 'Last Name', dataType: 'string' },
      { accessorKey: 'email', header: 'Email', dataType: 'string' },
      { accessorKey: 'age', header: 'Age', dataType: 'number' },
      { accessorKey: 'litersUsed', header: 'Liters Used', dataType: 'number' },
      { accessorKey: 'createdAt', header: 'Created At', dataType: 'date' },
      { accessorKey: 'updatedAt', header: 'Updated At', dataType: 'date' },
      { accessorKey: 'isEligibile', header: 'Is Eligible', dataType: 'boolean' },
      { accessorKey: 'isUtilized', header: 'Is Utilized', dataType: 'boolean' },
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
      const filterData = []
      for (const col of tableColumns) {
        const key = col.accessorKey
        const f = filters[key]
        if (!f) continue
        // include filter entry even if value is empty when an operator was explicitly chosen
        filterData.push({
          Operand: key,
          Value: f.value ?? '',
          SecondaryValue: f.secondaryValue ?? null,
          Operation: f.op ?? defaultOpForType(col.dataType),
          IsCaseSensitive: f.isCaseSensitive ?? false,
        })
      }

      const dq = {
        PageData: { PageNum: pNum, PageSize: PAGE_SIZE },
        SortData: (sorting || []).map((s) => ({ ColumnName: s.id, SortDirection: s.desc ? 'Desc' : 'Asc' })),
        FilterData: filterData,
      }

      // Debug the outgoing DataQuery
      console.debug('Sending DataQuery:', dq)
      // Map operation names to enum numeric values to avoid string enum mapping issues
      function opNameToEnumValue(name) {
        switch (name) {
          case 'Equals': return 0
          case 'NotEquals': return 1
          case 'Between': return 2
          case 'LessThan': return 3
          case 'LessThanOrEqual': return 4
          case 'GreaterThan': return 5
          case 'GreaterThanOrEqual': return 6
          case 'Contains': return 7
          case 'DoesNotContain': return 8
          case 'StartsWith': return 9
          case 'EndsWith': return 10
          default: return 0
        }
      }

      // convert Operation strings to numeric enum values for reliable deserialization
      dq.FilterData = dq.FilterData.map(fd => ({
        ...fd,
        Operation: opNameToEnumValue(fd.Operation)
      }))

      try {
          const res = await fetch('https://localhost:7233/api/person', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(dq),
        })
        if (!res.ok) throw new Error(`${res.status} ${res.statusText}`)
        const items = await res.json()

        if (pNum === 1) setData(items)
        else {
          // deduplicate by simple key combination to avoid repeats
          setData((prev) => {
            const combined = [...prev, ...items]
            const seen = new Set()
            const dedup = []
            for (const it of combined) {
              const key = `${it.firstName ?? it.FirstName ?? ''}|${it.lastName ?? it.LastName ?? ''}|${it.email ?? it.Email ?? ''}|${it.age ?? it.Age ?? ''}`
              if (!seen.has(key)) {
                seen.add(key)
                dedup.push(it)
              }
            }
            return dedup
          })
        }

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
  // infinite scroll handled by pageNum increments; rendering without virtualization for correct alignment

  return (
    <div className="app-container">
      <h1>People</h1>

      <div style={{ height: 8 }} />

      {error && <div className="error">Error: {error}</div>}

      <div className="grid-viewport">
        <table className="data-grid">
          <thead>
            {table.getHeaderGroups().map((hg) => (
              <tr key={hg.id}>
                {hg.headers.map((h) => {
                  const colId = h.column.id
                  const colDef = tableColumns.find((c) => c.accessorKey === colId) || {}
                  const f = filters[colId] || { op: defaultOpForType(colDef.dataType), value: '', secondaryValue: '' }
                  return (
                    <th key={h.id}>
                      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                        <div className="column-header-title">{h.column.columnDef.header}</div>
                        <div
                          style={{ cursor: 'pointer', marginLeft: 8 }}
                          onClick={() => {
                            const id = colId
                            const current = sorting.find((s) => s.id === id)
                            if (!current) setSorting([{ id, desc: false }])
                            else if (!current.desc) setSorting([{ id, desc: true }])
                            else setSorting([])
                          }}
                        >
                          {sorting.find((s) => s.id === h.column.id)
                            ? sorting.find((s) => s.id === h.column.id).desc
                              ? ' 🔽'
                              : ' 🔼'
                            : ' ⇅'}
                        </div>
                      </div>
                      <div className="filter-in-header">
                        <select
                          value={f.op}
                          onChange={(e) =>
                            setFilters((fs) => ({ ...fs, [colId]: { ...(fs[colId] || {}), op: e.target.value } }))
                          }
                        >
                          {operatorOptionsForType(colDef.dataType).map((op) => (
                            <option key={op} value={op}>
                              {op}
                            </option>
                          ))}
                        </select>
                        {f.op === 'Between' ? (
                          <>
                            {renderFilterInput(colDef.dataType, f.value ?? '', (val) =>
                              setFilters((fs) => ({ ...fs, [colId]: { ...(fs[colId] || {}), value: val } }))
                            )}
                            {renderFilterInput(colDef.dataType, f.secondaryValue ?? '', (val) =>
                              setFilters((fs) => ({ ...fs, [colId]: { ...(fs[colId] || {}), secondaryValue: val } }))
                            )}
                          </>
                        ) : (
                          renderFilterInput(colDef.dataType, f.value ?? '', (val) =>
                            setFilters((fs) => ({ ...fs, [colId]: { ...(fs[colId] || {}), value: val } }))
                          )
                        )}
                      </div>
                    </th>
                  )
                })}
              </tr>
            ))}
          </thead>

          <tbody>
            {data.map((item, idx) => (
              <tr key={idx}>
                {tableColumns.map((c) => (
                  <td key={c.accessorKey}>{formatCell(item, c.accessorKey)}</td>
                ))}
              </tr>
            ))}
            {loading && (
              <tr>
                <td colSpan={tableColumns.length} className="loading-row">
                  Loading...
                </td>
              </tr>
            )}
            {!loading && !hasMore && data.length === 0 && (
              <tr>
                <td colSpan={tableColumns.length} className="loading-row">
                  No results
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {loading && <div className="loading-indicator">Loading...</div>}
    </div>
  )
}

function formatCell(item, key) {
  if (!item) return ''
  const v = item[key]
  if (v === null || v === undefined) return ''
  if (typeof v === 'boolean') return v ? 'Yes' : 'No'
  // format dates
  const dateKeys = ['createdAt', 'CreatedAt', 'updatedAt', 'UpdatedAt']
  if (dateKeys.includes(key) || /^[0-9]{4}-[0-9]{2}-[0-9]{2}T/.test(String(v))) {
    const d = new Date(v)
    if (!isNaN(d.getTime())) {
      return formatDateMMDD(d)
    }
  }
  return String(v)
}

export default App

function defaultOpForType(type) {
  switch (type) {
    case 'string':
      return 'Contains'      
    default:
      return 'Equals'
  }
}

function operatorOptionsForType(type) {
  switch (type) {
    case 'number':
    case 'date':
      return ['Equals', 'NotEquals', 'LessThan', 'LessThanOrEqual', 'GreaterThan', 'GreaterThanOrEqual', 'Between']
    case 'boolean':
      return ['Equals', 'NotEquals']
    default:
      return ['Contains', 'DoesNotContain', 'StartsWith', 'EndsWith', 'Equals', 'NotEquals']
  }
}

function renderFilterInput(type, value, onChange) {
  if (type === 'number') {
    return (
      <input
        type="number"
        value={value ?? ''}
        onChange={(e) => onChange(e.target.value)}
        style={{ width: 100 }}
      />
    )
  }
  if (type === 'date') {
    const v = value ? new Date(value) : null
    return (
      <DateTimePicker
        value={v}
        onChange={(dt) => onChange(dt ? dt.toISOString() : '')}
        format="MM/dd/yyyy HH:mm"
      />
    )
  }
  if (type === 'boolean') {
    return (
      <input
        type="checkbox"
        checked={value === true || value === 'true'}
        onChange={(e) => onChange(e.target.checked)}
      />
    )
  }

  return (
    <input
      value={value ?? ''}
      onChange={(e) => onChange(e.target.value)}
      placeholder="..."
      style={{ width: 100 }}
    />
  )
}

function formatDateMMDD(d) {
  const mm = String(d.getMonth() + 1).padStart(2, '0')
  const dd = String(d.getDate()).padStart(2, '0')
  const yyyy = d.getFullYear()
  const hh = String(d.getHours()).padStart(2, '0')
  const min = String(d.getMinutes()).padStart(2, '0')
  return `${mm}/${dd}/${yyyy} ${hh}:${min}`
}
