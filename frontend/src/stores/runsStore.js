import { defineStore } from 'pinia'
import runsApi from '../api/runsApi'

// Cache configuration
const CACHE_TTL_MS = 5 * 60 * 1000 // 5 minutes cache TTL
const CACHE_STORAGE_KEY = 'resultviewer_run_cache'
const CACHE_TIMESTAMPS_KEY = 'resultviewer_run_cache_ts'

// Load cache from sessionStorage
function loadCacheFromStorage() {
  try {
    const cache = sessionStorage.getItem(CACHE_STORAGE_KEY)
    const timestamps = sessionStorage.getItem(CACHE_TIMESTAMPS_KEY)
    return {
      runCache: cache ? JSON.parse(cache) : {},
      runCacheTimestamps: timestamps ? JSON.parse(timestamps) : {}
    }
  } catch {
    return { runCache: {}, runCacheTimestamps: {} }
  }
}

// Save cache to sessionStorage
function saveCacheToStorage(cache, timestamps) {
  try {
    sessionStorage.setItem(CACHE_STORAGE_KEY, JSON.stringify(cache))
    sessionStorage.setItem(CACHE_TIMESTAMPS_KEY, JSON.stringify(timestamps))
  } catch {
    // Storage full or unavailable - ignore
  }
}

const initialCache = loadCacheFromStorage()

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
    },
    // Cache for run details (loaded from sessionStorage)
    runCache: initialCache.runCache,
    runCacheTimestamps: initialCache.runCacheTimestamps
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
      const cacheKey = `${host}/${pdc}/${runId}`
      const now = Date.now()
      
      // Check if we have valid cached data
      const cachedData = this.runCache[cacheKey]
      const cacheTime = this.runCacheTimestamps[cacheKey]
      const isCacheValid = cachedData && cacheTime && (now - cacheTime < CACHE_TTL_MS)
      
      // If cache is valid, use it immediately
      if (isCacheValid) {
        this.currentRun = cachedData
        this.error = null
        this.loading = false
        return
      }
      
      // If we have stale cache, show it while loading fresh data
      if (cachedData) {
        this.currentRun = cachedData
        this.error = null
      }
      
      this.loading = true
      this.error = null
      try {
        const freshData = await runsApi.getTestRun(host, pdc, runId)
        this.currentRun = freshData
        // Update cache
        this.runCache[cacheKey] = freshData
        this.runCacheTimestamps[cacheKey] = now
        // Persist to sessionStorage
        saveCacheToStorage(this.runCache, this.runCacheTimestamps)
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
        // If no cached data, clear currentRun
        if (!cachedData) {
          this.currentRun = null
        }
      } finally {
        this.loading = false
      }
    },
    
    // Clear specific run from cache
    clearRunCache(host, pdc, runId) {
      const cacheKey = `${host}/${pdc}/${runId}`
      delete this.runCache[cacheKey]
      delete this.runCacheTimestamps[cacheKey]
      saveCacheToStorage(this.runCache, this.runCacheTimestamps)
    },
    
    // Clear all caches
    clearAllCache() {
      this.runCache = {}
      this.runCacheTimestamps = {}
      sessionStorage.removeItem(CACHE_STORAGE_KEY)
      sessionStorage.removeItem(CACHE_TIMESTAMPS_KEY)
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
