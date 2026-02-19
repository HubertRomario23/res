<template>
  <div class="runs-list">
    <h1>Test Runs</h1>

    <!-- Paste Link -->
    <div class="link-bar">
      <input
        v-model="pastedLink"
        placeholder="Paste a ResultViewer link (e.g. http://igts-resultviewer.ta.philips.com/#/testrun/Host/Pdc/RunId)"
        class="link-input"
        @keyup.enter="goFromLink"
      />
      <button class="btn btn-primary" @click="goFromLink">Go</button>
    </div>
    <div v-if="linkError" class="link-error">{{ linkError }}</div>

    <!-- Loading / Error -->
    <div v-if="store.loading" class="status">Loading…</div>
    <div v-else-if="store.error" class="status error">{{ store.error }}</div>

    <!-- Table -->
    <table v-else-if="store.runs.length" class="runs-table">
      <thead>
        <tr>
          <th>Host</th>
          <th>PDC</th>
          <th>Run ID</th>
          <th>Start Time</th>
          <th>Result</th>
          <th>Tests</th>
          <th>Passed</th>
          <th>Failed</th>
        </tr>
      </thead>
      <tbody>
        <tr
          v-for="run in store.runs"
          :key="`${run.host}-${run.pdc}-${run.runId}`"
          class="run-row"
          @click="goToDetail(run)"
        >
          <td>{{ run.host }}</td>
          <td>{{ run.pdc }}</td>
          <td>{{ run.runId }}</td>
          <td>{{ formatDate(run.startTime) }}</td>
          <td :class="resultClass(run.overallResult)">{{ run.overallResult }}</td>
          <td>{{ run.testCount }}</td>
          <td>{{ run.passedCount }}</td>
          <td>{{ run.failedCount }}</td>
        </tr>
      </tbody>
    </table>

    <div v-else class="status">No test runs found.</div>

    <!-- Pagination -->
    <div v-if="store.totalPages > 1" class="pagination">
      <button :disabled="store.page <= 1" @click="store.setPage(store.page - 1)">← Prev</button>
      <span>Page {{ store.page }} of {{ store.totalPages }} ({{ store.totalCount }} total)</span>
      <button :disabled="store.page >= store.totalPages" @click="store.setPage(store.page + 1)">Next →</button>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useRunsStore } from '../stores/runsStore'

const store = useRunsStore()
const router = useRouter()

const pastedLink = ref('')
const linkError = ref('')

function goToDetail(run) {
  router.push({ name: 'RunDetail', params: { host: run.host, pdc: run.pdc, runId: run.runId } })
}

function goFromLink() {
  linkError.value = ''
  const text = pastedLink.value.trim()
  if (!text) return

  // Extract host/pdc/runId from link or plain path
  // Supports: http://.../#/testrun/Host/Pdc/RunId  OR  /testrun/Host/Pdc/RunId  OR  Host/Pdc/RunId
  const patterns = [
    /[#/]*testrun\/([^/]+)\/([^/]+)\/([^/]+)/i,
    /^([^/]+)\/([^/]+)\/([^/]+)$/
  ]

  for (const pattern of patterns) {
    const match = text.match(pattern)
    if (match) {
      const [, host, pdc, runId] = match
      pastedLink.value = ''
      router.push({ name: 'RunDetail', params: { host, pdc, runId } })
      return
    }
  }

  linkError.value = 'Could not parse link. Expected format: http://.../#/testrun/Host/Pdc/RunId'
}

function formatDate(iso) {
  if (!iso) return '—'
  return new Date(iso).toLocaleString()
}

function resultClass(result) {
  return {
    'result-passed': result === 'Passed',
    'result-failed': result === 'Failed'
  }
}

onMounted(() => store.fetchRuns())
</script>

<style scoped>
.runs-list h1 { margin-bottom: 1rem; }

.link-bar {
  display: flex;
  gap: 0.5rem;
  margin-bottom: 0.5rem;
}
.link-input {
  flex: 1;
  padding: 0.6rem;
  border: 2px solid #42b883;
  border-radius: 6px;
  font-size: 0.95rem;
}
.link-input:focus { outline: none; border-color: #2e9d6e; }
.link-error {
  color: #e74c3c;
  font-size: 0.85rem;
  margin-bottom: 0.5rem;
}

.btn {
  padding: 0.5rem 1rem;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-weight: 600;
}
.btn-primary { background: #42b883; color: #fff; }

.status { padding: 2rem; text-align: center; color: #666; }
.error { color: #e74c3c; }

.runs-table {
  width: 100%;
  border-collapse: collapse;
}
.runs-table th, .runs-table td {
  padding: 0.6rem 0.8rem;
  border-bottom: 1px solid #eee;
  text-align: left;
}
.runs-table th { background: #f5f5f5; font-weight: 600; }
.run-row { cursor: pointer; }
.run-row:hover { background: #f0faf5; }

.result-passed { color: #27ae60; font-weight: 600; }
.result-failed { color: #e74c3c; font-weight: 600; }

.pagination {
  display: flex;
  justify-content: center;
  align-items: center;
  gap: 1rem;
  margin-top: 1.5rem;
}
.pagination button {
  padding: 0.4rem 0.8rem;
  border: 1px solid #ccc;
  border-radius: 4px;
  background: #fff;
  cursor: pointer;
}
.pagination button:disabled { opacity: 0.4; cursor: default; }
</style>
