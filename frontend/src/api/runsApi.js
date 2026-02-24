import axios from 'axios'

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || '/api',
  timeout: 90000,
  headers: {
    'Content-Type': 'application/json'
  }
})

// Request interceptor — attach correlation ID
apiClient.interceptors.request.use(config => {
  config.headers['X-Correlation-Id'] = crypto.randomUUID()
  return config
})

// Response interceptor — unwrap errors
apiClient.interceptors.response.use(
  response => response,
  error => {
    const message = error.response?.data?.message || error.message || 'Unknown error'
    console.error('[API Error]', message, error.response?.data)
    return Promise.reject(error)
  }
)

export default {
  /**
   * GET /api/runs?host=&pdc=&runId=
   */
  async getTestRun(host, pdc, runId) {
    const { data } = await apiClient.get('/runs', {
      params: { host, pdc, runId }
    })
    return data
  },

  /**
   * GET /api/runs/list?page=&pageSize=&host=&pdc=&result=&fromDate=&toDate=
   */
  async getTestRuns({ page = 1, pageSize = 20, host, pdc, result, fromDate, toDate } = {}) {
    const { data } = await apiClient.get('/runs/list', {
      params: { page, pageSize, host, pdc, result, fromDate, toDate }
    })
    return data
  }
}
