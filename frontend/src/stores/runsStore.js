import { defineStore } from 'pinia'
import runsApi from '../api/runsApi'

export const useRunsStore = defineStore('runs', {
  state: () => ({
    runs: [],
    currentRun: null,
    totalCount: 0,
    totalPages: 0,
    page: 1,
    pageSize: 20,
    loading: false,
    error: null,
    filters: {
      host: '',
      pdc: '',
      result: '',
      fromDate: '',
      toDate: ''
    }
  }),

  actions: {
    async fetchRuns() {
      this.loading = true
      this.error = null
      try {
        const result = await runsApi.getTestRuns({
          page: this.page,
          pageSize: this.pageSize,
          host: this.filters.host || undefined,
          pdc: this.filters.pdc || undefined,
          result: this.filters.result || undefined,
          fromDate: this.filters.fromDate || undefined,
          toDate: this.filters.toDate || undefined
        })
        this.runs = result.items
        this.totalCount = result.totalCount
        this.totalPages = result.totalPages
      } catch (err) {
        this.error = err.response?.data?.message || err.message
      } finally {
        this.loading = false
      }
    },

    async fetchRun(host, pdc, runId) {
      this.loading = true
      this.error = null
      try {
        this.currentRun = await runsApi.getTestRun(host, pdc, runId)
      } catch (err) {
        const status = err.response?.status
        const detail = err.response?.data?.detail || ''
        if (status === 404) {
          this.error = `Run not found: ${host}/${pdc}/${runId}. ${detail}`
        } else if (!err.response) {
          this.error = `Network error – is the backend running? (${err.message})`
        } else {
          this.error = err.response?.data?.message || err.message
        }
        this.currentRun = null
      } finally {
        this.loading = false
      }
    },

    setPage(page) {
      this.page = page
      this.fetchRuns()
    },

    applyFilters(filters) {
      this.filters = { ...this.filters, ...filters }
      this.page = 1
      this.fetchRuns()
    },

    clearFilters() {
      this.filters = { host: '', pdc: '', result: '', fromDate: '', toDate: '' }
      this.page = 1
      this.fetchRuns()
    }
  }
})
