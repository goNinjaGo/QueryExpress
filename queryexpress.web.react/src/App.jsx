import { useEffect, useRef, useState, useCallback } from 'react'
import { DataTable } from "primereact/datatable";
import { Column } from "primereact/column";
import { InputText } from "primereact/inputtext";
import { Calendar } from "primereact/calendar";
import { TriStateCheckbox } from "primereact/tristatecheckbox";
import { FilterMatchMode, FilterOperator } from 'primereact/api';
import './App.css'
import 'primereact/resources/themes/lara-dark-blue/theme.css';
import 'primereact/resources/primereact.min.css';

const PAGE_SIZE = 50
const API_URL = 'https://localhost:7233/api/person'
const DEBOUNCE_DELAY = 300

function App() {
    const [rows, setRows] = useState([]);
    const [loading, setLoading] = useState(false)
    const [error, setError] = useState(null)
    const [totalRecords, setTotalRecords] = useState(0);

    const [lazyState, setLazyState] = useState({
        first: 0,
        rows: PAGE_SIZE,
        multiSortMeta: [],
        filters: {
            firstName: {
                operator: FilterOperator.AND,
                constraints: [
                    {
                        value: null,
                        matchMode: FilterMatchMode.CONTAINS,
                    },
                ],
            },
            lastName: {
                operator: FilterOperator.AND,
                constraints: [
                    {
                        value: null,
                        matchMode: FilterMatchMode.CONTAINS,
                    },
                ],
            },
            email: {
                operator: FilterOperator.AND,
                constraints: [
                    {
                        value: null,
                        matchMode: FilterMatchMode.CONTAINS,
                    },
                ],
            },
            age: {
                operator: FilterOperator.AND,
                constraints: [
                    {
                        value: null,
                        matchMode: FilterMatchMode.EQUALS,
                    },
                ],
            },
            litersUsed: {
                operator: FilterOperator.AND,
                constraints: [
                    {
                        value: null,
                        matchMode: FilterMatchMode.EQUALS,
                    },
                ],
            },
            createdAt: {
                operator: FilterOperator.AND,
                constraints: [
                    {
                        value: null,
                        matchMode: FilterMatchMode.DATE_IS,
                    },
                ],
            },
            updatedAt: {
                operator: FilterOperator.AND,
                constraints: [
                    {
                        value: null,
                        matchMode: FilterMatchMode.DATE_IS,
                    },
                ],
            },
            isEligibile: { value: null, matchMode: FilterMatchMode.EQUALS },
            isUtilized: { value: null, matchMode: FilterMatchMode.EQUALS },
        },
    });

    const abortRef = useRef(null);
    const filterTimeout = useRef(null);

    // --------------------------------------------
    // Fetch data from server
    // --------------------------------------------
    const loadData = async ({
        first,
        rows: pageSize,
        multiSortMeta,
        filters,
        append = false,
    }) => {
        try {
            setLoading(true);

            // cancel previous request
            if (abortRef.current) {
                abortRef.current.abort();
            }

            abortRef.current = new AbortController();

            const filterData = [];
            Object.entries(filters || {}).forEach(([key, f]) => {
                if (!f) return;

                // Flat filter
                if (f.matchMode !== undefined) {
                    if (f.value === null || f.value === undefined) return;
                    filterData.push({
                        Operand: key,
                        Value: f.value,
                        Operation: opNameToEnumValue(f.matchMode ?? 'equals'),
                    });
                    return;
                }

                // Constraints
                if (f.constraints) {
                    for (const constraint of f.constraints) {
                        if (constraint.value === null || constraint.value === undefined) continue;
                        filterData.push({
                            Operand: key,
                            Value: constraint.value,
                            Operation: opNameToEnumValue(constraint.matchMode ?? 'contains'),
                        });
                    }
                }
            });

            const body = {
                pageData: { pageNum: Math.round(first / PAGE_SIZE) + 1, pageSize: PAGE_SIZE },
                sortData: (multiSortMeta || []).map((s) => ({ columnName: s.field, sortDirection: s.order === 1 ? 'Asc' : 'Desc' })),
                filterData: filterData,
            };

            const response = await fetch(
                API_URL,
                {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(body),
                    signal: abortRef.current.signal,
                }
            );

            const result = await response.json();

            const mapped = result.data.map((row) => ({
                ...row,
                createdAt: row.createdAt ? new Date(row.createdAt) : null,
                updatedAt: row.updatedAt ? new Date(row.updatedAt) : null,
            }));

            setRows((prev) =>
                append ? [...prev, ...mapped] : mapped
            );

            setTotalRecords(result.totalRecords);
        } catch (err) {
            if (err.name !== "AbortError") {
                setError(err.message || "Error fetching data");
            }
        } finally {
            setLoading(false);
        }
    };

    // Initial load
    useEffect(() => {
        loadData({
            ...lazyState,
            append: false,
        });
    }, []);

    // --------------------------------------------
    // Infinite scroll handler
    // --------------------------------------------
    const onVirtualScroll = async (event) => {
        const nextFirst = event.first;

        // Already loaded enough rows
        if (nextFirst < rows.length) {
            return;
        }

        const nextState = {
            ...lazyState,
            first: nextFirst,
        };

        setLazyState(nextState);

        await loadData({
            ...nextState,
            append: true,
        });
    };

    // --------------------------------------------
    // Server-side sorting
    // --------------------------------------------
    const onSort = async (event) => {
        const nextState = {
            ...lazyState,
            first: 0,
            multiSortMeta: event.multiSortMeta,
        };

        setLazyState(nextState);

        await loadData({
            ...nextState,
            append: false,
        });
    };

    // --------------------------------------------
    // Server-side filtering
    // --------------------------------------------
    const onFilter = async (event) => {
        const nextState = {
            ...lazyState,
            first: 0,
            filters: event.filters,
        };

        setLazyState(nextState);

        if (filterTimeout.current) {
            clearTimeout(filterTimeout.current);
        }

        filterTimeout.current = setTimeout(async () => {
            await loadData({
                ...nextState,
                append: false,
            });
        }, DEBOUNCE_DELAY);
    };

    // --------------------------------------------
    // Templates
    // --------------------------------------------
    const formatDate = (value) => {
        if (!value) return "";

        return new Intl.DateTimeFormat("en-US", {
            dateStyle: "medium",
            timeStyle: "short",
        }).format(new Date(value));
    };

    const dateBodyTemplate = (field) => (rowData) => {
        return formatDate(rowData[field]);
    };

    const booleanBodyTemplate = (field) => (rowData) => {
        if (rowData[field] === true) {
            return 'Yes'
        }

        if (rowData[field] === false) {
            return 'No';
        }

        return '';
    };

    const booleanFilterTemplate = (options) => (
        <TriStateCheckbox
            value={options.value}
            onChange={(e) => options.filterCallback(e.value)}
        />
    );

    const dateFilterTemplate = (options) => (
        <Calendar
            value={options.value}
            onChange={(e) => options.filterCallback(e.value, options.index)}
            showTime
            hourFormat="12"
        />
    );

    

    return (
        <div className="app-container">
            <h1>People</h1>

            <div style={{ height: 8 }} />

            {error && <div className="error">Error: {error}</div>}

            <div className="grid-viewport">
                <DataTable
                    className="data-grid"
                    value={rows}
                    lazy
                    dataKey="id"
                    scrollable
                    scrollHeight="600px"
                    virtualScrollerOptions={{
                        lazy: true,
                        onLazyLoad: onVirtualScroll,
                        itemSize: 46,
                        delay: 150,
                        showLoader: true,
                        loading,
                    }}
                    totalRecords={totalRecords}
                    loading={loading}
                    filterDisplay="menu"
                    filters={lazyState.filters}
                    onFilter={onFilter}
                    sortMode="multiple"
                    multiSortMeta={lazyState.multiSortMeta}
                    onSort={onSort}
                    removableSort
                >
                    <Column
                        field="firstName"
                        header="First Name"
                        sortable
                        filter
                    />

                    <Column
                        field="lastName"
                        header="Last Name"
                        sortable
                        filter
                    />

                    <Column
                        field="email"
                        header="Email"
                        sortable
                        filter
                    />

                    <Column
                        field="age"
                        header="Age"
                        sortable
                        filter
                        dataType="number"
                    />

                    <Column
                        field="litersUsed"
                        header="Liters Used"
                        sortable
                        filter
                        dataType="number"
                    />

                    <Column
                        field="createdAt"
                        header="Created"
                        sortable
                        body={dateBodyTemplate("createdAt")}
                        filter
                        filterElement={dateFilterTemplate}
                    />

                    <Column
                        field="updatedAt"
                        header="Updated"
                        sortable
                        body={dateBodyTemplate("updatedAt")}
                        filter
                        filterElement={dateFilterTemplate}
                    />

                    <Column
                        field="isEligibile"
                        header="Eligible"
                        sortable
                        body={booleanBodyTemplate("isEligibile")}
                        filter
                        filterElement={booleanFilterTemplate}
                    />

                    <Column
                        field="isUtilized"
                        header="Utilized"
                        sortable
                        body={booleanBodyTemplate("isUtilized")}
                        filter
                        filterElement={booleanFilterTemplate}
                    />
                </DataTable>
            </div>

            {loading && <div className="loading-indicator">Loading...</div>}
        </div>
    )
}

export default App

function opNameToEnumValue(name) {
    switch (name) {
        case 'equals': return 'Equals'
        case 'notEquals': return 'NotEquals'
        case 'lt': return 'LessThan'
        case 'lte': return 'LessThanOrEqual'
        case 'gt': return 'GreaterThan'
        case 'gte': return 'GreaterThanOrEqual'
        case 'contains': return 'Contains'
        case 'notContains': return 'DoesNotContain'
        case 'startsWith': return 'StartsWith'
        case 'endsWith': return 'EndsWith'
        case 'dateIs': return 'Equals'
        case 'dateIsNot': return 'NotEquals'
        case 'dateBefore': return 'LessThan'
        case 'dateAfter': return 'GreaterThan'
        default: return 'Equals'
    }
}