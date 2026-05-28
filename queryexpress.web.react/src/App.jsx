import { useCallback, useEffect, useRef, useState } from 'react'
import { DataTable } from "primereact/datatable";
import { Column } from "primereact/column";
import { Calendar } from "primereact/calendar";
import { TriStateCheckbox } from "primereact/tristatecheckbox";
import { FilterMatchMode, FilterOperator } from 'primereact/api';
import { Skeleton } from 'primereact/skeleton';

import 'primereact/resources/themes/lara-dark-blue/theme.css';
import 'primereact/resources/primereact.min.css';

const PAGE_SIZE = 50
const API_URL = 'https://localhost:7233/api/person'
const DEBOUNCE_DELAY = 300
const isRowLoaded = (row) => row != null
const getPageFirst = (first = 0, pageSize = PAGE_SIZE) => Math.floor(first / pageSize) * pageSize

function App() {
    const [rows, setRows] = useState([]);
    const [loading, setLoading] = useState(false)
    const [error, setError] = useState(null)
    const [totalRecords, setTotalRecords] = useState(0);

    const [lazyState, setLazyState] = useState({
        first: 0,
        pageSize: PAGE_SIZE,
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
            isEligibile: {
                operator: FilterOperator.AND,
                constraints: [
                    {
                        value: null,
                        matchMode: FilterMatchMode.EQUALS,
                    },
                ],
            },
            isUtilized: {
                operator: FilterOperator.AND,
                constraints: [
                    {
                        value: null,
                        matchMode: FilterMatchMode.EQUALS,
                    },
                ],
            },
        },
    });

    const abortRef = useRef(null);
    const filterTimeout = useRef(null);
    const initialLazyState = useRef(lazyState);

    // --------------------------------------------
    // Fetch data from server
    // --------------------------------------------
    const loadData = useCallback(async ({
        first,
        pageSize,
        multiSortMeta,
        filters,
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

                for (const constraint of f.constraints) {
                    if (!constraint.value) continue;

                    filterData.push({
                        Operand: key,
                        Value: constraint.value,
                        Operation: opNameToEnumValue(constraint.matchMode ?? 'contains'),
                    });
                }
            });

            const pageFirst = getPageFirst(first, pageSize);

            const body = {
                pageData: { pageNum: Math.floor(pageFirst / pageSize) + 1, pageSize },
                sortData: (multiSortMeta || []).map((s) => ({ columnName: s.field, sortDirection: s.order === 1 ? 'Asc' : 'Desc' })),
                filterData,
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

            setTotalRecords(result.totalRecords);

            setRows((prev) => {
                const virtualRows =
                    prev.length === result.totalRecords
                        ? [...prev]
                        : Array.from({ length: result.totalRecords }, () => null);

                for (let i = 0; i < result.data.length; i++) {
                    virtualRows[pageFirst + i] = result.data[i];
                }

                return virtualRows;
            });
        } catch (err) {
            if (err.name !== "AbortError") {
                setError(err.message || "Error fetching data");
            }
        } finally {
            setLoading(false);
        }
    }, []);

    // Initial load
    useEffect(() => {
        const timeoutId = setTimeout(() => {
            loadData(initialLazyState.current);
        }, 0);

        return () => clearTimeout(timeoutId);
    }, [loadData]);

    // --------------------------------------------
    // Infinite scroll handler
    // --------------------------------------------
    const onVirtualScroll = async (event) => {
        const first = event.first ?? 0;
        const last = event.last ?? first + PAGE_SIZE;
        const needLoad = rows.slice(first, last).some((row) => !isRowLoaded(row));

        if (needLoad) {
            await loadData({
                first: getPageFirst(first, PAGE_SIZE),
                pageSize: PAGE_SIZE,
                multiSortMeta: lazyState.multiSortMeta,
                filters: lazyState.filters,
            });
        }
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

        await loadData(nextState);
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
            await loadData(nextState);
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
        if (!rowData) {
            return loadingTemplate();
        }
        return formatDate(rowData[field]);
    };

    const booleanBodyTemplate = (field) => (rowData) => {
        if (!rowData) {
            return loadingTemplate();
        }
        if (rowData[field] === true) {
            return 'Yes'
        }
        if (rowData[field] === false) {
            return 'No';
        }

        return '';
    };

    const textBodyTemplate = (field) => (rowData) => {
        if (!rowData) {
            return loadingTemplate();
        }

        return rowData[field] ?? '';
    };

    const dateFilterElement = (options) => {
        return (
            <Calendar
                value={options.value}
                onChange={(e) => options.filterApplyCallback(e.value)}
                showTime
                hourFormat="12"
            />
        );
    };

    const boolFilterElement = (options) => {
        return (
            <TriStateCheckbox
                value={options.value}
                onChange={(e) => options.filterApplyCallback(e.value)}
            />
        );
    };

    const loadingTemplate = () => {
        return (
            <div
                className="flex align-items-center"
                style={{
                    height: '17px',
                    flexGrow: '1',
                    overflow: 'hidden',
                }}
            >
                <Skeleton width="60%" height="1rem" />
            </div>
        );
    };
    

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
                    scrollable
                    scrollHeight="600px"
                    virtualScrollerOptions={{
                        lazy: true,
                        onLazyLoad: onVirtualScroll,
                        itemSize: 100,
                        delay: 150,
                        showLoader: false
                    }}
                    totalRecords={totalRecords}
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
                        body={textBodyTemplate("firstName")}
                    />

                    <Column
                        field="lastName"
                        header="Last Name"
                        sortable
                        filter
                        body={textBodyTemplate("lastName")}
                    />

                    <Column
                        field="email"
                        header="Email"
                        sortable
                        filter
                        body={textBodyTemplate("email")}
                    />

                    <Column
                        field="age"
                        header="Age"
                        sortable
                        filter
                        dataType="numeric"
                        body={textBodyTemplate("age")}
                    />

                    <Column
                        field="litersUsed"
                        header="Liters Used"
                        sortable
                        filter
                        dataType="numeric"
                        body={textBodyTemplate("litersUsed")}
                    />

                    <Column
                        field="createdAt"
                        header="Created"
                        sortable
                        body={dateBodyTemplate("createdAt")}
                        filter
                        dataType="date"
                        filterElement={dateFilterElement}
                    />

                    <Column
                        field="updatedAt"
                        header="Updated"
                        sortable
                        body={dateBodyTemplate("updatedAt")}
                        filter
                        dataType="date"
                        filterElement={dateFilterElement}
                    />

                    <Column
                        field="isEligibile"
                        header="Eligible"
                        sortable
                        body={booleanBodyTemplate("isEligibile")}
                        filter
                        dataType="boolean"
                        filterElement={boolFilterElement}
                    />

                    <Column
                        field="isUtilized"
                        header="Utilized"
                        sortable
                        body={booleanBodyTemplate("isUtilized")}
                        filter
                        dataType="boolean"
                        filterElement={boolFilterElement}
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
