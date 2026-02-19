<template>
  <div class="run-detail">
    <router-link to="/" class="back-link">← Back to list</router-link>

    <div v-if="store.loading" class="status">Loading…</div>
    <div v-else-if="store.error" class="status error">{{ store.error }}</div>

    <template v-else-if="run">
      <div class="run-header">
        <h1>{{ run.host }} / {{ run.pdc }} / {{ run.runId }}</h1>
        <span :class="resultBadge(run.overallResult)">{{ run.overallResult }}</span>
      </div>

      <div class="summary-grid">
        <div class="summary-card">
          <div class="label">Total Tests</div>
          <div class="value">{{ run.testCount }}</div>
        </div>
        <div class="summary-card passed">
          <div class="label">Passed</div>
          <div class="value">{{ run.passedCount }}</div>
        </div>
        <div class="summary-card failed">
          <div class="label">Failed</div>
          <div class="value">{{ run.failedCount }}</div>
        </div>
        <div class="summary-card skipped">
          <div class="label">Skipped</div>
          <div class="value">{{ run.skippedCount }}</div>
        </div>
        <div class="summary-card">
          <div class="label">Start</div>
          <div class="value small">{{ formatDate(run.startTime) }}</div>
        </div>
        <div class="summary-card">
          <div class="label">End</div>
          <div class="value small">{{ formatDate(run.endTime) }}</div>
        </div>
      </div>

      <!-- Tabs -->
      <div class="tabs">
        <button :class="['tab', { active: activeTab === 'results' }]" @click="activeTab = 'results'">
          Test Results ({{ run.indexedResults?.length || 0 }})
        </button>
        <button :class="['tab', { active: activeTab === 'systeminfo' }]" @click="activeTab = 'systeminfo'">
          System Info
        </button>
        <button :class="['tab', { active: activeTab === 'measurements' }]" @click="activeTab = 'measurements'">
          Measurements ({{ run.measurements?.length || 0 }})
        </button>
      </div>

      <!-- Tab: Test Results -->
      <div v-if="activeTab === 'results'" class="tab-content">
        <!-- Filter bar -->
        <div class="result-filter">
          <input v-model="resultSearch" placeholder="Filter by test name…" class="filter-input" />
          <select v-model="resultFilter" class="filter-select">
            <option value="">All</option>
            <option value="Passed">Passed</option>
            <option value="Failed">Failed</option>
            <option value="Inconclusive">Inconclusive</option>
            <option value="Skipped">Skipped</option>
          </select>
        </div>
        <table v-if="filteredResults.length" class="data-table">
          <thead>
            <tr>
              <th>#</th>
              <th>Test Name</th>
              <th>Result</th>
              <th>Duration</th>
              <th>Error Message</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="(r, i) in filteredResults" :key="r.id">
              <td class="num">{{ i + 1 }}</td>
              <td>{{ r.testName }}</td>
              <td :class="resultClass(r.result)">{{ r.result }}</td>
              <td class="mono">{{ formatDuration(r.duration) }}</td>
              <td class="error-msg">{{ r.errorMessage || '—' }}</td>
            </tr>
          </tbody>
        </table>
        <div v-else class="status">No test results match the current filter.</div>
      </div>

      <!-- Tab: System Info -->
      <div v-if="activeTab === 'systeminfo'" class="tab-content">
        <div v-if="run.systemInfo" class="sysinfo-grid">
          <div class="sysinfo-row" v-for="(value, key) in systemInfoEntries" :key="key">
            <span class="sysinfo-label">{{ key }}</span>
            <span class="sysinfo-value">{{ value }}</span>
          </div>
        </div>
        <div v-else class="status">No system information available for this run.</div>
      </div>

      <!-- Tab: Measurements -->
      <div v-if="activeTab === 'measurements'" class="tab-content">
        <div class="result-filter">
          <input v-model="measurementSearch" placeholder="Filter by test or measurement name…" class="filter-input" />
          <select v-model="measurementResultFilter" class="filter-select">
            <option value="">All Results</option>
            <option value="Passed">Passed</option>
            <option value="Failed">Failed</option>
            <option value="NotPerformed">Not Performed</option>
          </select>
        </div>
        <table v-if="filteredMeasurements.length" class="data-table measurements-table">
          <thead>
            <tr>
              <th>Test Name</th>
              <th>Measurement</th>
              <th>Result</th>
              <th>Value</th>
              <th>Unit</th>
              <th>Spec (Error)</th>
              <th>Spec (Warning)</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="m in filteredMeasurements" :key="m.measurementName + m.testName">
              <td>{{ m.testName }}</td>
              <td>{{ m.measurementName }}</td>
              <td :class="resultClass(m.result)">{{ m.result }}</td>
              <td class="mono">{{ m.measuredValue || '—' }}</td>
              <td>{{ m.measurementUnit || '—' }}</td>
              <td class="mono spec">{{ formatSpec(m.specErrorLower, m.specErrorUpper) }}</td>
              <td class="mono spec">{{ formatSpec(m.specWarningLower, m.specWarningUpper) }}</td>
            </tr>
          </tbody>
        </table>
        <div v-else class="status">No measurements available for this run.</div>
      </div>
    </template>
  </div>
</template>

<script setup>
import { computed, ref, onMounted } from 'vue'
import { useRunsStore } from '../stores/runsStore'

const props = defineProps({
  host: String,
  pdc: String,
  runId: String
})

const store = useRunsStore()
const run = computed(() => store.currentRun)

const activeTab = ref('results')
const resultSearch = ref('')
const resultFilter = ref('')
const measurementSearch = ref('')
const measurementResultFilter = ref('')

const filteredResults = computed(() => {
  if (!run.value?.indexedResults) return []
  return run.value.indexedResults.filter(r => {
    const matchName = !resultSearch.value || r.testName.toLowerCase().includes(resultSearch.value.toLowerCase())
    const matchResult = !resultFilter.value || r.result === resultFilter.value
    return matchName && matchResult
  })
})

const systemInfoEntries = computed(() => {
  if (!run.value?.systemInfo) return {}
  const si = run.value.systemInfo
  return {
    'System Name': si.systemName,
    'STM': si.stm,
    'MSI Version': si.msiVersion,
    'PDC Version': si.pdcVersion,
    'Monoplane / Biplane': si.monoplaneOrBiplane,
    'Frontal Stand Type': si.frontalStandType,
    'Table Type': si.tableType,
    'Table Top Type': si.tableTopType,
    'Detector Frontal': si.detectorNameFrontal,
    'Detector Lateral': si.detectorNameLateral,
    'System Type': si.systemType,
    'Product Family': si.productFamily,
    'Detector Type': si.detectorType,
    'Lateral Stand Type': si.lateralStandType,
    'System Config Type': si.systemConfigType
  }
})

const filteredMeasurements = computed(() => {
  if (!run.value?.measurements) return []
  return run.value.measurements.filter(m => {
    const matchName = !measurementSearch.value ||
      m.testName.toLowerCase().includes(measurementSearch.value.toLowerCase()) ||
      m.measurementName.toLowerCase().includes(measurementSearch.value.toLowerCase())
    const matchResult = !measurementResultFilter.value || m.result === measurementResultFilter.value
    return matchName && matchResult
  })
})

function formatDate(iso) {
  if (!iso) return '—'
  return new Date(iso).toLocaleString()
}

function formatDuration(dur) {
  if (!dur) return '—'
  // dur comes as "HH:MM:SS" or "HH:MM:SS.fff" from TimeSpan serialization
  return dur
}

function formatSpec(lower, upper) {
  if (!lower && !upper) return '—'
  return `${lower || '?'} – ${upper || '?'}`
}

function resultBadge(result) {
  return {
    badge: true,
    'badge-passed': result === 'Passed',
    'badge-failed': result === 'Failed'
  }
}

function resultClass(result) {
  return {
    'result-passed': result === 'Passed',
    'result-failed': result === 'Failed',
    'result-notperformed': result === 'NotPerformed'
  }
}

onMounted(() => store.fetchRun(props.host, props.pdc, props.runId))
</script>

<style scoped>
.back-link {
  display: inline-block;
  margin-bottom: 1rem;
  color: #42b883;
  text-decoration: none;
  font-weight: 600;
}
.run-header {
  display: flex;
  align-items: center;
  gap: 1rem;
  margin-bottom: 1.5rem;
}
.run-header h1 { margin: 0; font-size: 1.3rem; }

.badge {
  padding: 0.3rem 0.8rem;
  border-radius: 12px;
  font-weight: 700;
  font-size: 0.85rem;
}
.badge-passed { background: #d4edda; color: #155724; }
.badge-failed { background: #f8d7da; color: #721c24; }

.summary-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(150px, 1fr));
  gap: 1rem;
  margin-bottom: 1.5rem;
}
.summary-card {
  background: #f9f9f9;
  border-radius: 8px;
  padding: 1rem;
  text-align: center;
}
.summary-card .label { font-size: 0.8rem; color: #888; }
.summary-card .value { font-size: 1.6rem; font-weight: 700; }
.summary-card .value.small { font-size: 0.9rem; }
.summary-card.passed .value { color: #27ae60; }
.summary-card.failed .value { color: #e74c3c; }
.summary-card.skipped .value { color: #f39c12; }

/* Tabs */
.tabs {
  display: flex;
  gap: 0;
  border-bottom: 2px solid #e0e0e0;
  margin-bottom: 1rem;
}
.tab {
  padding: 0.6rem 1.2rem;
  border: none;
  background: none;
  cursor: pointer;
  font-weight: 600;
  font-size: 0.95rem;
  color: #666;
  border-bottom: 3px solid transparent;
  margin-bottom: -2px;
  transition: color 0.2s, border-color 0.2s;
}
.tab:hover { color: #333; }
.tab.active {
  color: #42b883;
  border-bottom-color: #42b883;
}
.tab-content { margin-top: 0.5rem; }

/* Filter bar */
.result-filter {
  display: flex;
  gap: 0.5rem;
  margin-bottom: 1rem;
}
.filter-input {
  flex: 1;
  padding: 0.5rem;
  border: 1px solid #ccc;
  border-radius: 4px;
  font-size: 0.9rem;
}
.filter-select {
  padding: 0.5rem;
  border: 1px solid #ccc;
  border-radius: 4px;
  font-size: 0.9rem;
}

/* Tables */
.data-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.9rem;
}
.data-table th, .data-table td {
  padding: 0.5rem 0.8rem;
  border-bottom: 1px solid #eee;
  text-align: left;
}
.data-table th { background: #f5f5f5; font-weight: 600; position: sticky; top: 0; }
.data-table .num { width: 40px; color: #aaa; }
.mono { font-family: 'Consolas', 'Courier New', monospace; font-size: 0.85rem; }
.spec { font-size: 0.8rem; color: #666; }

.result-passed { color: #27ae60; font-weight: 600; }
.result-failed { color: #e74c3c; font-weight: 600; }
.result-notperformed { color: #999; }

.error-msg {
  max-width: 400px;
  font-size: 0.85rem;
  color: #888;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

/* System Info */
.sysinfo-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: 0.5rem;
}
.sysinfo-row {
  display: flex;
  justify-content: space-between;
  padding: 0.6rem 1rem;
  background: #f9f9f9;
  border-radius: 6px;
}
.sysinfo-label {
  font-weight: 600;
  color: #555;
  font-size: 0.9rem;
}
.sysinfo-value {
  font-family: 'Consolas', 'Courier New', monospace;
  font-size: 0.9rem;
  color: #333;
}

/* Measurements table */
.measurements-table td { font-size: 0.85rem; }

.status { padding: 2rem; text-align: center; color: #666; }
.error { color: #e74c3c; }
</style>
