import { useEffect, useMemo, useState, useCallback } from 'react'
import {
    useReactTable,
    getCoreRowModel,
    getSortedRowModel,
    getFilteredRowModel,
} from '@tanstack/react-table'
import { TriStateCheckbox } from 'primereact/tristatecheckbox'
import { Calendar } from 'primereact/calendar'
import './App.css'
import 'primereact/resources/themes/lara-dark-blue/theme.css';
import 'primereact/resources/primereact.min.css';

const PAGE_SIZE = 20
const API_URL = 'https://localhost:7233/api/person'

function App() {
    const [data, setData] = useState([])
    const [loading, setLoading] = useState(false)
    const [error, setError] = useState(null)
    const [pageNum, setPageNum] = useState(1)
    const [hasMore, setHasMore] = useState(true)

    const [sorting, setSorting] = useState([]) // [{id, desc}]
    const [filters, setFilters] = useState({}) // {columnId: value}
    // UI input state for filters (updated on every keystroke). Applied filters are debounced into `filters`.
    const [filterInputs, setFilterInputs] = useState({})

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

            function opNameToEnumValue(name) {
                switch (name) {
                    case 'Equals': return 'Equals'
                    case 'Not Equals': return 'NotEquals'
                    case 'Between': return 'Between'
                    case '<': return 'LessThan'
                    case '<=': return 'LessThanOrEqual'
                    case '>': return 'GreaterThan'
                    case '>=': return 'GreaterThanOrEqual'
                    case 'Contains': return 'Contains'
                    case 'Does Not Contain': return 'DoesNotContain'
                    case 'Starts With': return 'StartsWith'
                    case 'Ends With': return 'EndsWith'
                    default: return 'Equals'
                }
            }

            dq.FilterData = dq.FilterData.map(fd => ({
                ...fd,
                Operation: opNameToEnumValue(fd.Operation)
            }))

            try {
                const res = await fetch(API_URL, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(dq),
                })
                if (!res.ok) throw new Error(`${res.status} ${res.statusText}`)
                const items = await res.json()                
                setData(items)

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

    // Apply UI inputs to the actual filters with a debounce so requests aren't made on every keystroke
    useEffect(() => {
        const t = setTimeout(() => {
            setFilters(filterInputs)
        }, 300)
        return () => clearTimeout(t)
    }, [filterInputs])

    useEffect(() => {
        if (pageNum === 1) return
        fetchPage(pageNum)
    }, [pageNum, fetchPage])


    // Position an appended calendar overlay so it appears directly below the input
    const positionCalendarOverlay = (colId) => {
        try {
            const input = document.getElementById(`calendar-input-${colId}`)
            const panel = document.querySelector(`.calendar-overlay-${colId}`)
            if (!input || !panel) return
            const rect = input.getBoundingClientRect()
            panel.style.position = 'absolute'
            panel.style.left = `${rect.left + window.scrollX}px`
            panel.style.top = `${rect.bottom + window.scrollY}px`
        } catch {
            // ignore positioning errors
        }
    }

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
                                    const inputState = (filterInputs[colId] ?? filters[colId]) || { op: defaultOpForType(colDef.dataType), value: '', secondaryValue: '' }
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
                                                    value={inputState.op}
                                                    onChange={(e) => {
                                                        const val = e.target.value
                                                        setFilterInputs((fs) => ({ ...(fs || {}), [colId]: { ...(fs?.[colId] || {}), op: val } }))
                                                        setFilters((fs) => ({ ...(fs || {}), [colId]: { ...(fs?.[colId] || {}), op: val } }))
                                                    }}
                                                >
                                                    {operatorOptionsForType(colDef.dataType).map((op) => (
                                                        <option key={op} value={op}>
                                                            {op}
                                                        </option>
                                                    ))}
                                                </select>
                                                {inputState.op === 'Between' ? (
                                                    colDef.dataType === 'date' ? (
                                                        // For date-between use a single range calendar
                                                        <Calendar
                                                            value={(() => {
                                                                const v = []
                                                                if (inputState.value) v.push(new Date(inputState.value))
                                                                if (inputState.secondaryValue) v.push(new Date(inputState.secondaryValue))
                                                                return v.length ? v : null
                                                            })()}
                                                            selectionMode="range"
                                                            onChange={(e) => {
                                                                const vals = e.value || []
                                                                const start = vals[0] ? vals[0].toISOString() : ''
                                                                const end = vals[1] ? vals[1].toISOString() : ''
                                                                setFilterInputs((fs) => ({ ...(fs || {}), [colId]: { ...(fs?.[colId] || {}), value: start, secondaryValue: end } }))
                                                            }}
                                                            showTime
                                                            hourFormat="12"
                                                            appendTo={document.body}
                                                                    inputId={colId ? `calendar-input-${colId}` : undefined}
                                                                    panelClassName={colId ? `calendar-overlay-${colId}` : undefined}
                                                                    onShow={() => colId && positionCalendarOverlay && positionCalendarOverlay(colId)}
                                                        />
                                                    ) : (
                                                        <>
                                                            {renderFilterInput(colDef.dataType, inputState.value ?? '', (val) =>
                                                                setFilterInputs((fs) => ({ ...(fs || {}), [colId]: { ...(fs?.[colId] || {}), value: val } })), colId, positionCalendarOverlay
                                                            )}
                                                            {renderFilterInput(colDef.dataType, inputState.secondaryValue ?? '', (val) =>
                                                                setFilterInputs((fs) => ({ ...(fs || {}), [colId]: { ...(fs?.[colId] || {}), secondaryValue: val } })), colId, positionCalendarOverlay
                                                            )}
                                                        </>
                                                    )
                                                ) : (
                                                    renderFilterInput(colDef.dataType, inputState.value ?? '', (val) =>
                                                        setFilterInputs((fs) => ({ ...(fs || {}), [colId]: { ...(fs?.[colId] || {}), value: val } })), colId, positionCalendarOverlay
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
            return ['Equals', 'Not Equals', '<', '<=', '>', '>=', 'Between']
        case 'boolean':
            return ['Equals', 'Not Equals']
        default:
            return ['Contains', 'Does Not Contain', 'Starts With', 'Ends With', 'Equals', 'Not Equals']
    }
}

function renderFilterInput(type, value, onChange, colId, positionCalendarOverlay) {
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
            <Calendar
                value={v}
                onChange={(e) => onChange(e.value ? e.value.toISOString() : '')}
                showTime
                hourFormat="12"
                appendTo={document.body}
                inputId={colId ? `calendar-input-${colId}` : undefined}
                panelClassName={colId ? `calendar-overlay-${colId}` : undefined}
                onShow={() => colId && positionCalendarOverlay && positionCalendarOverlay(colId)}
            />
        )
    }
    if (type === 'boolean') {
        // TriStateCheckbox accepts true / false / null (indeterminate)
        const normalized = value === true || value === 'true' ? true : value === false || value === 'false' ? false : null
        return (
            <TriStateCheckbox
                value={normalized}
                onChange={(e) => onChange(e.value + '')}
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

