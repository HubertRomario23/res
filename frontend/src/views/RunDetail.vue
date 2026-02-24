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
            <option value="">All Results</option>
            <option value="Success">Success</option>
            <option value="Failure">Failure</option>
            <option value="Error">Error</option>
            <option value="Inconclusive">Inconclusive</option>
            <option value="Ignored">Ignored</option>
          </select>
          <select v-model="featureLevelFilter" class="filter-select">
            <option value="">All Features</option>
            <option value="Passed">Passed Features</option>
            <option value="Failed">Failed Features</option>
          </select>
        </div>

        <div v-if="filteredGroupedResults.length" class="feature-accordion">
          <div v-for="group in filteredGroupedResults" :key="group.feature" class="feature-group">
            <!-- Feature header -->
            <div class="feature-header" @click="toggleFeature(group.feature)">
              <span class="feature-toggle">{{ expandedFeatures.has(group.feature) ? '▼' : '►' }}</span>
              <span :class="['feature-status-icon', featureStatusClass(group)]">{{ featureStatusIcon(group) }}</span>
              <span class="feature-name"><span class="label-prefix">Feature:</span> {{ group.feature }}</span>
              <div class="feature-meta">
                <span class="feature-time">⏱ {{ formatTotalDuration(group.totalDurationMs) }}</span>
                <span :class="['feature-result-badge', featureResultBadgeClass(group)]">{{ featureResultLabel(group) }}</span>
                <span class="feature-counts">
                  <span class="count-total">{{ group.testCount }} tests</span>
                  <span class="count-scenarios">{{ group.scenarios.length }} scenarios</span>
                </span>
              </div>
            </div>

            <!-- Scenarios under this feature -->
            <div v-if="expandedFeatures.has(group.feature)" class="scenarios-container">
              
              <!-- BeforeFeature Hooks -->
              <div v-if="getFeatureHooks(group.feature).beforeLogs.length > 0" class="feature-hooks before-feature">
                <div class="hook-section-header">
                  <span class="hook-icon">⚙</span>
                  <span class="hook-title">BeforeFeature</span>
                  <span class="hook-count">({{ getFeatureHooks(group.feature).beforeLogs.length }} logs)</span>
                </div>
                <div class="hook-logs">
                  <table class="step-logs-table">
                    <thead>
                      <tr>
                        <th class="col-time">Time</th>
                        <th class="col-level">Level</th>
                        <th class="col-component">Component</th>
                        <th class="col-message">Message</th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr v-for="(log, idx) in getFeatureHooks(group.feature).beforeLogs" :key="idx" :class="logRowClass(log.level)">
                        <td class="log-time">{{ log.time }}</td>
                        <td><span :class="['log-level-pill', logLevelClass(log.level)]">{{ log.level }}</span></td>
                        <td class="log-component">{{ log.component }}</td>
                        <td class="log-message-full">{{ log.message }}</td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </div>
              
              <div v-for="scenario in group.scenarios" :key="scenario.name" class="scenario-group">
                <!-- Scenario header -->
                <div class="scenario-header" @click="toggleScenario(group.feature + '::' + scenario.name)">
                  <span class="scenario-toggle">{{ expandedScenarios.has(group.feature + '::' + scenario.name) ? '▼' : '►' }}</span>
                  <span :class="['scenario-status-icon', scenarioStatusClass(scenario)]">{{ scenarioStatusIcon(scenario) }}</span>
                  <span class="scenario-name"><span class="label-prefix">Scenario:</span> {{ scenario.name }}</span>
                  <div class="scenario-meta">
                    <span class="scenario-time">⏱ {{ formatTotalDuration(scenario.totalDurationMs) }}</span>
                    <span :class="['scenario-result-badge', scenarioResultBadgeClass(scenario)]">{{ scenarioResultLabel(scenario) }}</span>
                    <span v-if="scenario.tests.length > 1" class="count-variants">{{ scenario.tests.length }} variants</span>
                  </div>
                </div>

                <!-- Test cases and logs under this scenario -->
                <div v-if="expandedScenarios.has(group.feature + '::' + scenario.name)" class="scenario-details">
                  
                  <!-- SINGLE TEST: Show details directly without variant wrapper -->
                  <template v-if="scenario.tests.length === 1">
                    <div class="single-test-details">
                      <!-- Error Message -->
                      <div v-if="scenario.tests[0].errorMessage" class="variant-error">
                        <div class="error-title">Error:</div>
                        <div class="error-content">{{ scenario.tests[0].errorMessage }}</div>
                      </div>
                      
                      <!-- Test Info -->
                      <div class="single-test-info">
                        <span :class="['variant-result', resultClass(scenario.tests[0].result)]">{{ scenario.tests[0].result }}</span>
                        <span class="variant-duration">{{ formatDuration(scenario.tests[0].duration) }}</span>
                      </div>
                      
                      <!-- Execution Flow -->
                      <div v-if="getScenarioSteps(scenario.tests[0].name).length > 0 || getScenarioSteps(scenario.name).length > 0" class="scenario-steps">
                        <div class="section-title">Execution Flow</div>
                        <div class="steps-list">
                          <div v-for="(step, idx) in (getScenarioSteps(scenario.tests[0].name).length > 0 ? getScenarioSteps(scenario.tests[0].name) : getScenarioSteps(scenario.name))" :key="idx" class="step-wrapper">
                            <!-- Step Header (clickable to expand/collapse logs) -->
                            <div 
                              :class="['step-item', stepStatusClass(step), { 'step-hook': step.isHook, 'step-clickable': step.logs?.length > 0 }]"
                              @click="step.logs?.length > 0 && toggleStep(getStepKey(scenario.tests[0].id, idx))"
                            >
                              <span class="step-number">{{ idx + 1 }}</span>
                              <span :class="['step-keyword', stepKeywordClass(step.keyword)]">{{ step.keyword }}</span>
                              <span class="step-text">{{ step.text }}</span>
                              <span v-if="step.duration" class="step-duration">{{ step.duration }}</span>
                              <span class="step-time">{{ step.time }}</span>
                              <span :class="['step-status', stepStatusClass(step)]">{{ step.status }}</span>
                              <span v-if="step.logs?.length > 0" class="step-expand-icon">
                                {{ expandedSteps.has(getStepKey(scenario.tests[0].id, idx)) ? '▼' : '►' }}
                                <span class="log-count">({{ step.logs.length }} logs)</span>
                              </span>
                            </div>
                            
                            <!-- Step Logs (expandable) -->
                            <div v-if="step.logs?.length > 0 && expandedSteps.has(getStepKey(scenario.tests[0].id, idx))" class="step-logs">
                              <table class="step-logs-table">
                                <thead>
                                  <tr>
                                    <th class="col-time">Time</th>
                                    <th class="col-level">Level</th>
                                    <th class="col-component">Component</th>
                                    <th class="col-message">Message</th>
                                  </tr>
                                </thead>
                                <tbody>
                                  <tr v-for="(log, logIdx) in step.logs" :key="logIdx" :class="logRowClass(log.level)">
                                    <td class="log-time">{{ log.time }}</td>
                                    <td><span :class="['log-level-pill', logLevelClass(log.level)]">{{ log.level }}</span></td>
                                    <td class="log-component">{{ log.component }}</td>
                                    <td class="log-message-full">{{ log.message }}</td>
                                  </tr>
                                </tbody>
                              </table>
                            </div>
                          </div>
                        </div>
                      </div>
                      
                      <!-- Fallback: Show logs directly if no steps found -->
                      <div v-else-if="getScenarioLogs(scenario.tests[0].name).length > 0 || getScenarioLogs(scenario.name).length > 0" class="scenario-logs-fallback">
                        <div class="section-title">Execution Logs</div>
                        <div class="logs-table-wrapper">
                          <table class="step-logs-table">
                            <thead>
                              <tr>
                                <th class="col-time">Time</th>
                                <th class="col-level">Level</th>
                                <th class="col-component">Component</th>
                                <th class="col-message">Message</th>
                              </tr>
                            </thead>
                            <tbody>
                              <tr v-for="(log, logIdx) in (getScenarioLogs(scenario.tests[0].name).length > 0 ? getScenarioLogs(scenario.tests[0].name) : getScenarioLogs(scenario.name))" :key="logIdx" :class="logRowClass(log.level)">
                                <td class="log-time">{{ log.time }}</td>
                                <td><span :class="['log-level-pill', logLevelClass(log.level)]">{{ log.level }}</span></td>
                                <td class="log-component">{{ log.component }}</td>
                                <td class="log-message-full">{{ log.message }}</td>
                              </tr>
                            </tbody>
                          </table>
                        </div>
                      </div>
                    </div>
                  </template>
                  
                  <!-- MULTIPLE TESTS: Show with variant headers -->
                  <template v-else>
                    <div v-for="(test, testIdx) in scenario.tests" :key="test.id" class="variant-section">
                      <!-- Variant Header -->
                      <div class="variant-header" @click="toggleVariant(test.id)">
                        <span class="variant-toggle">{{ expandedVariants.has(test.id) ? '▼' : '►' }}</span>
                        <span class="variant-number">Variant {{ testIdx + 1 }}</span>
                        <span class="variant-name">{{ test.displayName || test.name }}</span>
                        <span :class="['variant-result', resultClass(test.result)]">{{ test.result }}</span>
                        <span class="variant-duration">{{ formatDuration(test.duration) }}</span>
                        <span v-if="test.errorMessage" class="variant-error-indicator">⚠</span>
                      </div>
                      
                      <!-- Variant Details (expandable) -->
                      <div v-if="expandedVariants.has(test.id)" class="variant-details">
                        <!-- Error Message -->
                        <div v-if="test.errorMessage" class="variant-error">
                          <div class="error-title">Error:</div>
                          <div class="error-content">{{ test.errorMessage }}</div>
                        </div>
                        
                        <!-- Variant Execution Flow - try test.name first, then scenario.name -->
                        <div v-if="getScenarioSteps(test.name).length > 0 || getScenarioSteps(scenario.name).length > 0" class="scenario-steps">
                          <div class="section-title">Execution Flow</div>
                          <div class="steps-list">
                            <div v-for="(step, idx) in (getScenarioSteps(test.name).length > 0 ? getScenarioSteps(test.name) : getScenarioSteps(scenario.name))" :key="idx" class="step-wrapper">
                              <!-- Step Header (clickable to expand/collapse logs) -->
                              <div 
                                :class="['step-item', stepStatusClass(step), { 'step-hook': step.isHook, 'step-clickable': step.logs?.length > 0 }]"
                                @click="step.logs?.length > 0 && toggleStep(getStepKey(test.id, idx))"
                              >
                                <span class="step-number">{{ idx + 1 }}</span>
                                <span :class="['step-keyword', stepKeywordClass(step.keyword)]">{{ step.keyword }}</span>
                                <span class="step-text">{{ step.text }}</span>
                                <span v-if="step.duration" class="step-duration">{{ step.duration }}</span>
                                <span class="step-time">{{ step.time }}</span>
                                <span :class="['step-status', stepStatusClass(step)]">{{ step.status }}</span>
                                <span v-if="step.logs?.length > 0" class="step-expand-icon">
                                  {{ expandedSteps.has(getStepKey(test.id, idx)) ? '▼' : '►' }}
                                  <span class="log-count">({{ step.logs.length }} logs)</span>
                                </span>
                              </div>
                              
                              <!-- Step Logs (expandable) -->
                              <div v-if="step.logs?.length > 0 && expandedSteps.has(getStepKey(test.id, idx))" class="step-logs">
                                <table class="step-logs-table">
                                  <thead>
                                    <tr>
                                      <th class="col-time">Time</th>
                                      <th class="col-level">Level</th>
                                      <th class="col-component">Component</th>
                                      <th class="col-message">Message</th>
                                    </tr>
                                  </thead>
                                  <tbody>
                                    <tr v-for="(log, logIdx) in step.logs" :key="logIdx" :class="logRowClass(log.level)">
                                      <td class="log-time">{{ log.time }}</td>
                                      <td><span :class="['log-level-pill', logLevelClass(log.level)]">{{ log.level }}</span></td>
                                      <td class="log-component">{{ log.component }}</td>
                                      <td class="log-message-full">{{ log.message }}</td>
                                    </tr>
                                  </tbody>
                                </table>
                              </div>
                            </div>
                          </div>
                        </div>
                        
                        <!-- Fallback: Show logs directly if no steps found -->
                        <div v-else-if="getScenarioLogs(test.name).length > 0 || getScenarioLogs(scenario.name).length > 0" class="scenario-logs-fallback">
                          <div class="section-title">Execution Logs</div>
                          <div class="logs-table-wrapper">
                            <table class="step-logs-table">
                              <thead>
                                <tr>
                                  <th class="col-time">Time</th>
                                  <th class="col-level">Level</th>
                                  <th class="col-component">Component</th>
                                  <th class="col-message">Message</th>
                                </tr>
                              </thead>
                              <tbody>
                                <tr v-for="(log, logIdx) in (getScenarioLogs(test.name).length > 0 ? getScenarioLogs(test.name) : getScenarioLogs(scenario.name))" :key="logIdx" :class="logRowClass(log.level)">
                                  <td class="log-time">{{ log.time }}</td>
                                  <td><span :class="['log-level-pill', logLevelClass(log.level)]">{{ log.level }}</span></td>
                                  <td class="log-component">{{ log.component }}</td>
                                  <td class="log-message-full">{{ log.message }}</td>
                                </tr>
                              </tbody>
                            </table>
                          </div>
                        </div>
                      </div>
                    </div>
                  </template>
                </div>
              </div>
              
              <!-- AfterFeature Hooks -->
              <div v-if="getFeatureHooks(group.feature).afterLogs.length > 0" class="feature-hooks after-feature">
                <div class="hook-section-header">
                  <span class="hook-icon">⚙</span>
                  <span class="hook-title">AfterFeature</span>
                  <span class="hook-count">({{ getFeatureHooks(group.feature).afterLogs.length }} logs)</span>
                </div>
                <div class="hook-logs">
                  <table class="step-logs-table">
                    <thead>
                      <tr>
                        <th class="col-time">Time</th>
                        <th class="col-level">Level</th>
                        <th class="col-component">Component</th>
                        <th class="col-message">Message</th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr v-for="(log, idx) in getFeatureHooks(group.feature).afterLogs" :key="idx" :class="logRowClass(log.level)">
                        <td class="log-time">{{ log.time }}</td>
                        <td><span :class="['log-level-pill', logLevelClass(log.level)]">{{ log.level }}</span></td>
                        <td class="log-component">{{ log.component }}</td>
                        <td class="log-message-full">{{ log.message }}</td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </div>
            </div>
          </div>
        </div>
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
            <option value="Success">Success</option>
            <option value="Failure">Failure</option>
            <option value="NotPerformed">Not Performed</option>
          </select>
        </div>
        <div class="table-responsive">
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
        </div>
        <div v-if="!filteredMeasurements.length" class="status">No measurements available for this run.</div>
      </div>
    </template>
  </div>
</template>

<script setup>
import { computed, ref, reactive, onMounted } from 'vue'
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
const featureLevelFilter = ref('')
const measurementSearch = ref('')
const measurementResultFilter = ref('')
const expandedFeatures = reactive(new Set())
const expandedScenarios = reactive(new Set())
const expandedErrors = reactive(new Set())
const expandedSteps = reactive(new Set())
const expandedVariants = reactive(new Set())

/**
 * Parse SpecflowLog and index entries by scenario name AND feature name
 * Captures BeforeFeature/AfterFeature, BeforeScenario/AfterScenario hooks along with scenario steps
 */
const parsedSpecflowLog = computed(() => {
  const scenarioMap = new Map()
  const log = run.value?.specflowLog
  if (!log) {
    console.log('[LOG] No specflowLog available')
    return scenarioMap
  }
  console.log('[LOG] Parsing specflowLog, length:', log.length)

  // Split on both CRLF and LF for cross-platform compatibility
  const lines = log.split(/\r?\n/)
  let currentScenario = null
  let currentEntries = []
  let pendingBeforeEntries = [] // Capture logs before scenario starts (BeforeScenario hooks)

  // Pattern: 2026-01-23 21:11:36.173 INFO Setup.A_PreLoop.Beforetest.0 - >>Scenario Started: A_SystemPowerOn
  const logLinePattern = /^(\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2}\.\d{3})\s+(DEBUG|INFO|WARN|WARNING|ERROR|TRACE|FATAL)\s+(\S+)\s+-\s+(.*)$/i
  const scenarioStartPattern = />>Scenario Started:\s*(.+)$/i
  const scenarioEndPattern = />>Scenario Finished:\s*(.+)$/i
  // Patterns to identify Before/After hooks in component or message
  const hookPattern = /Before|After|SetUp|TearDown|Hook/i

  for (const line of lines) {
    const match = line.match(logLinePattern)
    if (!match) continue

    const [, timestamp, level, component, message] = match
    const entry = {
      time: timestamp.split(' ')[1],
      level: level.toUpperCase(),
      component,
      message,
      isHook: hookPattern.test(component) || hookPattern.test(message)
    }

    const startMatch = message.match(scenarioStartPattern)
    if (startMatch) {
      currentScenario = startMatch[1].trim()
      // Include pending "before" entries (BeforeScenario hooks)
      currentEntries = [...pendingBeforeEntries, entry]
      pendingBeforeEntries = []
      continue
    }

    const endMatch = message.match(scenarioEndPattern)
    if (endMatch) {
      if (currentScenario) {
        currentEntries.push(entry)
        // Capture any AfterScenario entries that follow immediately
        scenarioMap.set(currentScenario, [...currentEntries])
      }
      currentScenario = null
      currentEntries = []
      continue
    }

    if (currentScenario) {
      currentEntries.push(entry)
    } else {
      // Log entries between scenarios - could be BeforeScenario hooks for next scenario
      // or AfterScenario hooks from previous
      if (entry.isHook) {
        pendingBeforeEntries.push(entry)
      }
    }
  }

  console.log('[LOG] Parsed scenarios:', scenarioMap.size, 'scenarios')
  console.log('[LOG] Sample scenario names:', Array.from(scenarioMap.keys()).slice(0, 5))
  return scenarioMap
})

/**
 * Parse SpecflowLog for Feature-level hooks (BeforeFeature/AfterFeature)
 * Returns a Map with featureName -> { beforeLogs: [], afterLogs: [] }
 */
const parsedFeatureHooks = computed(() => {
  const featureHooksMap = new Map()
  const log = run.value?.specflowLog
  if (!log) return featureHooksMap

  const lines = log.split(/\r?\n/)
  
  // Pattern: 2026-01-23 21:11:36.173 INFO Setup.A_PreLoop.Beforetest.0 - >>Feature Started: FeatureName
  const logLinePattern = /^(\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2}\.\d{3})\s+(DEBUG|INFO|WARN|WARNING|ERROR|TRACE|FATAL)\s+(\S+)\s+-\s+(.*)$/i
  const featureStartPattern = />>Feature Started:\s*(.+)$/i
  const featureEndPattern = />>Feature Finished:\s*(.+)$/i
  const scenarioStartPattern = />>Scenario Started:/i
  const scenarioEndPattern = />>Scenario Finished:/i
  
  let currentFeature = null
  let beforeFeatureEntries = []
  let afterFeatureEntries = []
  let isCollectingBefore = true // Start by collecting before-feature logs
  let isInScenario = false

  for (const line of lines) {
    const match = line.match(logLinePattern)
    if (!match) continue

    const [, timestamp, level, component, message] = match
    const entry = {
      time: timestamp.split(' ')[1],
      level: level.toUpperCase(),
      component,
      message
    }

    // Check for Feature Start
    const featureStartMatch = message.match(featureStartPattern)
    if (featureStartMatch) {
      // Save previous feature's hooks if any
      if (currentFeature && (beforeFeatureEntries.length > 0 || afterFeatureEntries.length > 0)) {
        featureHooksMap.set(currentFeature, {
          beforeLogs: [...beforeFeatureEntries],
          afterLogs: [...afterFeatureEntries]
        })
      }
      
      currentFeature = featureStartMatch[1].trim()
      beforeFeatureEntries = [entry]
      afterFeatureEntries = []
      isCollectingBefore = true
      isInScenario = false
      continue
    }

    // Check for Scenario Start - stop collecting BeforeFeature
    if (message.match(scenarioStartPattern)) {
      isCollectingBefore = false
      isInScenario = true
      continue
    }

    // Check for Scenario End
    if (message.match(scenarioEndPattern)) {
      isInScenario = false
      continue
    }

    // Check for Feature End
    const featureEndMatch = message.match(featureEndPattern)
    if (featureEndMatch) {
      afterFeatureEntries.push(entry)
      if (currentFeature) {
        featureHooksMap.set(currentFeature, {
          beforeLogs: [...beforeFeatureEntries],
          afterLogs: [...afterFeatureEntries]
        })
      }
      currentFeature = null
      beforeFeatureEntries = []
      afterFeatureEntries = []
      isCollectingBefore = true
      continue
    }

    // Collect logs outside scenarios
    if (currentFeature && !isInScenario) {
      if (isCollectingBefore) {
        beforeFeatureEntries.push(entry)
      } else {
        afterFeatureEntries.push(entry)
      }
    }
  }

  // Handle last feature if not ended
  if (currentFeature && (beforeFeatureEntries.length > 0 || afterFeatureEntries.length > 0)) {
    featureHooksMap.set(currentFeature, {
      beforeLogs: beforeFeatureEntries,
      afterLogs: afterFeatureEntries
    })
  }

  console.log('[LOG] Parsed feature hooks:', featureHooksMap.size, 'features')
  return featureHooksMap
})

/**
 * Get BeforeFeature/AfterFeature logs for a feature
 */
function getFeatureHooks(featureName) {
  const hooksMap = parsedFeatureHooks.value
  
  if (!featureName || hooksMap.size === 0) {
    return { beforeLogs: [], afterLogs: [] }
  }
  
  // Try exact match first
  if (hooksMap.has(featureName)) {
    return hooksMap.get(featureName)
  }
  
  // Try partial/fuzzy match
  const normalizedName = featureName.toLowerCase().replace(/[_\s-]+/g, '')
  for (const [key, value] of hooksMap.entries()) {
    const normalizedKey = key.toLowerCase().replace(/[_\s-]+/g, '')
    if (normalizedKey.includes(normalizedName) || normalizedName.includes(normalizedKey)) {
      return value
    }
  }
  
  return { beforeLogs: [], afterLogs: [] }
}

/**
 * Get logs for a scenario/variant with flexible matching
 * Handles: ScenarioName, ScenarioName(param1, param2), Feature.ScenarioName, etc.
 */
function getScenarioLogs(scenarioName) {
  const logMap = parsedSpecflowLog.value
  
  if (!scenarioName || logMap.size === 0) {
    console.log('[LOGS] No scenarioName or empty logMap')
    return []
  }
  
  console.log('[LOGS] Looking for logs for:', scenarioName)
  console.log('[LOGS] Available scenarios in log:', Array.from(logMap.keys()))
  
  // Direct match
  if (logMap.has(scenarioName)) {
    console.log('[LOGS] Direct match found')
    return logMap.get(scenarioName)
  }
  
  // Extract parts from test name
  // Test names can be: "Feature.ScenarioName(param1, param2)" or just "ScenarioName"
  const parts = scenarioName.split('.')
  const lastPart = parts[parts.length - 1]
  
  // Strip parameters from the test name
  const baseNameWithoutParams = lastPart.replace(/\([^)]*\)$/, '').trim()
  
  // Normalize for fuzzy matching - removes all non-alphanumeric, lowercase
  const normalize = (s) => s.toLowerCase().replace(/[^a-z0-9]/g, '')
  const normalizedTarget = normalize(baseNameWithoutParams)
  const normalizedFull = normalize(scenarioName)
  
  console.log('[LOGS] Normalized target:', normalizedTarget)
  
  // First pass: exact normalized match or containment
  for (const [logScenario, entries] of logMap.entries()) {
    const logWithoutParams = logScenario.replace(/\([^)]*\)$/, '').trim()
    const normalizedLog = normalize(logWithoutParams)
    
    if (normalizedTarget === normalizedLog || 
        normalizedFull === normalizedLog ||
        normalizedTarget.includes(normalizedLog) || 
        normalizedLog.includes(normalizedTarget)) {
      console.log('[LOGS] Match found:', logScenario)
      return entries
    }
  }
  
  // Second pass: try matching scenario name that contains the base name
  for (const [logScenario, entries] of logMap.entries()) {
    const logWithoutParams = logScenario.replace(/\([^)]*\)$/, '').trim()
    const normalizedLog = normalize(logWithoutParams)
    
    // Check if significant overlap
    const minLen = Math.min(normalizedTarget.length, normalizedLog.length)
    if (minLen > 5) {
      if (normalizedTarget.startsWith(normalizedLog.substring(0, Math.floor(minLen * 0.7))) ||
          normalizedLog.startsWith(normalizedTarget.substring(0, Math.floor(minLen * 0.7)))) {
        console.log('[LOGS] Partial match found:', logScenario)
        return entries
      }
    }
  }
  
  console.log('[LOGS] No match found for:', scenarioName)
  return []
}

/**
 * Extract SpecFlow steps with their logs, grouped by step type
 * Parses logs to find step boundaries using markers like:
 * - ">>Step Started: Given ..." or step keyword at start
 * - ">>Step Passed/Failed: ..." or "-> done/error: ..."
 * Structure: BeforeScenario → Given/When/Then steps → AfterScenario
 */
function getScenarioSteps(scenarioName) {
  const logs = getScenarioLogs(scenarioName)
  if (!logs.length) return []
  
  const steps = []
  
  // Step markers patterns
  const stepStartedPattern = />>Step\s*Started[:\s]+(.+)/i
  const stepPassedPattern = />>Step\s*Passed[:\s]+(.+)/i
  const stepFailedPattern = />>Step\s*Failed[:\s]+(.+)/i
  const stepSkippedPattern = />>Step\s*Skipped[:\s]+(.+)/i
  
  // Alternative patterns when step markers aren't present
  const stepKeywordPattern = /^(Given|When|Then|And|But)\s+(.+)$/i
  const stepDonePattern = /^->\s*(done|passed):\s*(.+?)\s*\((\d+\.?\d*s?)\)$/i
  const stepErrorPattern = /^->\s*(error|failed):\s*(.+?)\s*\((\d+\.?\d*s?)\)$/i
  const stepSkipPattern = /^->\s*(pending|skipped):\s*(.+?)\s*\((\d+\.?\d*s?)\)$/i
  
  // Scenario markers
  const scenarioStartPattern = />>Scenario\s*Started/i
  const scenarioEndPattern = />>Scenario\s*Finished/i
  
  // Hook patterns
  const beforeHookPattern = /Before|BeforeScenario|SetUp|PreLoop|BeforeTest/i
  const afterHookPattern = /After|AfterScenario|TearDown|PostLoop|AfterTest/i
  
  let currentStep = null
  let beforeHooks = { keyword: 'BeforeScenario', text: 'Setup Hooks', logs: [], time: null, status: 'Done', isHook: true }
  let afterHooks = { keyword: 'AfterScenario', text: 'Cleanup Hooks', logs: [], time: null, status: 'Done', isHook: true }
  let phase = 'before' // 'before' | 'steps' | 'after'
  let lastKeyword = 'Given'
  
  console.log('[STEPS] Processing', logs.length, 'logs for scenario:', scenarioName)
  
  // Helper to finalize current step
  function pushCurrentStep() {
    if (currentStep) {
      if (currentStep.status === 'Running') currentStep.status = 'Done'
      steps.push(currentStep)
      currentStep = null
    }
  }
  
  // Helper to create a new step
  function createStep(keyword, text, time) {
    pushCurrentStep()
    const kw = keyword.charAt(0).toUpperCase() + keyword.slice(1).toLowerCase()
    if (['Given', 'When', 'Then'].includes(kw)) lastKeyword = kw
    currentStep = {
      keyword: kw,
      text: text,
      time: time,
      status: 'Running',
      duration: null,
      logs: [],
      isHook: false
    }
    if (phase === 'before') phase = 'steps'
  }
  
  for (const log of logs) {
    const message = log.message
    const component = log.component
    
    // Check for scenario start marker
    if (scenarioStartPattern.test(message)) {
      if (beforeHooks.logs.length > 0) {
        beforeHooks.time = beforeHooks.logs[0].time
        beforeHooks.status = beforeHooks.logs.some(l => l.level === 'ERROR') ? 'Error' : 'Done'
      }
      phase = 'steps'
      console.log('[STEPS] Scenario started, BeforeScenario has', beforeHooks.logs.length, 'logs')
      continue
    }
    
    // Check for scenario end marker
    if (scenarioEndPattern.test(message)) {
      pushCurrentStep()
      phase = 'after'
      console.log('[STEPS] Scenario ended, found', steps.length, 'steps')
      continue
    }
    
    // Check for step started marker (explicit)
    let match = message.match(stepStartedPattern)
    if (match) {
      const stepText = match[1].trim()
      const kwMatch = stepText.match(/^(Given|When|Then|And|But)\s+(.+)$/i)
      if (kwMatch) {
        createStep(kwMatch[1], kwMatch[2], log.time)
      } else {
        createStep('Step', stepText, log.time)
      }
      currentStep.logs.push(log)
      continue
    }
    
    // Check for step passed marker
    match = message.match(stepPassedPattern)
    if (match && currentStep) {
      currentStep.status = 'Passed'
      currentStep.logs.push(log)
      pushCurrentStep()
      continue
    }
    
    // Check for step failed marker
    match = message.match(stepFailedPattern)
    if (match && currentStep) {
      currentStep.status = 'Failed'
      currentStep.logs.push(log)
      pushCurrentStep()
      continue
    }
    
    // Check for step skipped marker
    match = message.match(stepSkippedPattern)
    if (match && currentStep) {
      currentStep.status = 'Skipped'
      currentStep.logs.push(log)
      pushCurrentStep()
      continue
    }
    
    // Check for alternative step completion patterns "-> done/error: ..."
    match = message.match(stepDonePattern)
    if (match && currentStep) {
      currentStep.status = 'Passed'
      currentStep.duration = match[3]
      currentStep.logs.push(log)
      pushCurrentStep()
      continue
    }
    
    match = message.match(stepErrorPattern)
    if (match && currentStep) {
      currentStep.status = 'Failed'
      currentStep.duration = match[3]
      currentStep.logs.push(log)
      pushCurrentStep()
      continue
    }
    
    match = message.match(stepSkipPattern)
    if (match && currentStep) {
      currentStep.status = 'Skipped'
      currentStep.duration = match[3]
      currentStep.logs.push(log)
      pushCurrentStep()
      continue
    }
    
    // Check if this log line starts a new step (keyword pattern)
    match = message.match(stepKeywordPattern)
    if (match && phase !== 'after') {
      createStep(match[1], match[2], log.time)
      currentStep.logs.push(log)
      continue
    }
    
    // Assign log to appropriate bucket
    if (phase === 'before') {
      beforeHooks.logs.push(log)
      if (log.level === 'ERROR') beforeHooks.status = 'Error'
    } else if (phase === 'after') {
      afterHooks.logs.push(log)
      if (log.level === 'ERROR') afterHooks.status = 'Error'
      if (!afterHooks.time) afterHooks.time = log.time
    } else if (currentStep) {
      currentStep.logs.push(log)
      if (log.level === 'ERROR') currentStep.status = 'Error'
    }
  }
  
  // Finalize any remaining step
  pushCurrentStep()
  
  // Build final array: BeforeScenario → Steps → AfterScenario
  const result = []
  if (beforeHooks.logs.length > 0) {
    result.push(beforeHooks)
  }
  result.push(...steps)
  if (afterHooks.logs.length > 0) {
    result.push(afterHooks)
  }
  
  console.log('[STEPS] Final result:', result.length, 'items (before:', beforeHooks.logs.length, ', steps:', steps.length, ', after:', afterHooks.logs.length, ')')
  
  return result
}

function stepKeywordClass(keyword) {
  const kw = keyword?.toLowerCase()
  if (kw === 'given') return 'keyword-given'
  if (kw === 'when') return 'keyword-when'
  if (kw === 'then') return 'keyword-then'
  if (kw === 'and' || kw === 'but') return 'keyword-and'
  if (kw === 'beforescenario' || kw === 'before') return 'keyword-before'
  if (kw === 'afterscenario' || kw === 'after') return 'keyword-after'
  if (kw === 'step') return 'keyword-step'
  return ''
}

function stepStatusClass(step) {
  const status = step?.status?.toLowerCase()
  if (status === 'done' || status === 'passed') return 'step-passed'
  if (status === 'error' || status === 'failed') return 'step-failed'
  if (status === 'pending' || status === 'skipped') return 'step-skipped'
  return 'step-running'
}

/**
 * Extract feature name, scenario name, and display name from the full qualified test name
 */
function parseTestName(fullName) {
  const parenIdx = fullName.indexOf('(')
  const basePart = parenIdx >= 0 ? fullName.substring(0, parenIdx) : fullName
  const paramPart = parenIdx >= 0 ? fullName.substring(parenIdx) : ''

  const segments = basePart.split('.')
  if (segments.length >= 2) {
    const feature = segments[segments.length - 2]
    const scenarioName = segments[segments.length - 1]
    const displayName = paramPart || scenarioName
    return { feature, scenario: scenarioName, displayName }
  }
  return { feature: 'Other', scenario: fullName, displayName: fullName }
}

/**
 * Parse duration string to milliseconds
 */
function parseDurationToMs(dur) {
  if (!dur) return 0
  // Format: HH:MM:SS.fff or HH:MM:SS
  const parts = dur.split(':')
  if (parts.length !== 3) return 0
  const hours = Number.parseInt(parts[0], 10) || 0
  const minutes = Number.parseInt(parts[1], 10) || 0
  const secondsPart = parts[2].split('.')
  const seconds = Number.parseInt(secondsPart[0], 10) || 0
  const ms = secondsPart[1] ? Number.parseInt(secondsPart[1].padEnd(3, '0').substring(0, 3), 10) : 0
  return (hours * 3600 + minutes * 60 + seconds) * 1000 + ms
}

const filteredResults = computed(() => {
  if (!run.value?.indexedResults) return []
  return run.value.indexedResults.filter(r => {
    const matchName = !resultSearch.value || r.testName.toLowerCase().includes(resultSearch.value.toLowerCase())
    const matchResult = !resultFilter.value || r.result === resultFilter.value
    return matchName && matchResult
  })
})

const groupedResults = computed(() => {
  const results = filteredResults.value
  const featureMap = new Map()

  for (const r of results) {
    const { feature, scenario, displayName } = parseTestName(r.testName)
    const durationMs = parseDurationToMs(r.duration)

    if (!featureMap.has(feature)) {
      featureMap.set(feature, { 
        feature, 
        scenarioMap: new Map(), 
        testCount: 0, 
        passed: 0, 
        failed: 0, 
        skipped: 0,
        totalDurationMs: 0
      })
    }
    const fg = featureMap.get(feature)
    fg.testCount++
    fg.totalDurationMs += durationMs
    if (r.result === 'Success' || r.result === 'Passed') fg.passed++
    else if (r.result === 'Failure' || r.result === 'Failed' || r.result === 'Error') fg.failed++
    else fg.skipped++

    if (!fg.scenarioMap.has(scenario)) {
      fg.scenarioMap.set(scenario, { 
        name: scenario, 
        tests: [], 
        passed: 0, 
        failed: 0, 
        skipped: 0,
        totalDurationMs: 0
      })
    }
    const sg = fg.scenarioMap.get(scenario)
    sg.tests.push({ ...r, displayName })
    sg.totalDurationMs += durationMs
    if (r.result === 'Success' || r.result === 'Passed') sg.passed++
    else if (r.result === 'Failure' || r.result === 'Failed' || r.result === 'Error') sg.failed++
    else sg.skipped++
  }

  return Array.from(featureMap.values())
    .map(fg => ({
      ...fg,
      scenarios: Array.from(fg.scenarioMap.values()).sort((a, b) => a.name.localeCompare(b.name))
    }))
    .sort((a, b) => a.feature.localeCompare(b.feature))
})

const filteredGroupedResults = computed(() => {
  if (!featureLevelFilter.value) return groupedResults.value
  return groupedResults.value.filter(group => {
    if (featureLevelFilter.value === 'Passed') return group.failed === 0 && group.passed > 0
    if (featureLevelFilter.value === 'Failed') return group.failed > 0
    return true
  })
})

function toggleFeature(feature) {
  if (expandedFeatures.has(feature)) {
    expandedFeatures.delete(feature)
  } else {
    expandedFeatures.add(feature)
  }
}

function toggleScenario(key) {
  if (expandedScenarios.has(key)) {
    expandedScenarios.delete(key)
  } else {
    expandedScenarios.add(key)
  }
}

function toggleError(testId) {
  if (expandedErrors.has(testId)) {
    expandedErrors.delete(testId)
  } else {
    expandedErrors.add(testId)
  }
}

function toggleStep(stepKey) {
  if (expandedSteps.has(stepKey)) {
    expandedSteps.delete(stepKey)
  } else {
    expandedSteps.add(stepKey)
  }
}

function toggleVariant(variantId) {
  if (expandedVariants.has(variantId)) {
    expandedVariants.delete(variantId)
  } else {
    expandedVariants.add(variantId)
  }
}

function getStepKey(scenarioName, stepIdx) {
  return `${scenarioName}::step::${stepIdx}`
}

// Feature status
function featureStatusIcon(group) {
  if (group.failed > 0) return '✗'
  if (group.passed === group.testCount) return '✓'
  return '?'
}

function featureStatusClass(group) {
  if (group.failed > 0) return 'status-failed'
  if (group.passed === group.testCount) return 'status-passed'
  return 'status-mixed'
}

function featureResultLabel(group) {
  if (group.failed > 0) return `${group.failed} Failed`
  if (group.passed === group.testCount) return 'All Passed'
  return 'Mixed'
}

function featureResultBadgeClass(group) {
  if (group.failed > 0) return 'badge-failed'
  if (group.passed === group.testCount) return 'badge-passed'
  return 'badge-mixed'
}

// Scenario status
function scenarioStatusIcon(scenario) {
  if (scenario.failed > 0) return '✗'
  if (scenario.passed === scenario.tests.length) return '✓'
  return '?'
}

function scenarioStatusClass(scenario) {
  if (scenario.failed > 0) return 'status-failed'
  if (scenario.passed === scenario.tests.length) return 'status-passed'
  return 'status-mixed'
}

function scenarioResultLabel(scenario) {
  if (scenario.failed > 0) return 'Failed'
  if (scenario.skipped > 0 && scenario.passed === 0) return 'Skipped'
  if (scenario.passed === scenario.tests.length) return 'Passed'
  return 'Mixed'
}

function scenarioResultBadgeClass(scenario) {
  const label = scenarioResultLabel(scenario)
  if (label === 'Failed') return 'badge-failed'
  if (label === 'Passed') return 'badge-passed'
  if (label === 'Skipped') return 'badge-skipped'
  return 'badge-mixed'
}

// Log level styling
function logLevelClass(level) {
  const l = level.toUpperCase()
  if (l === 'ERROR' || l === 'FATAL') return 'level-error'
  if (l === 'WARN' || l === 'WARNING') return 'level-warn'
  if (l === 'INFO') return 'level-info'
  return 'level-debug'
}

function logRowClass(level) {
  const l = level.toUpperCase()
  if (l === 'ERROR' || l === 'FATAL') return 'log-row-error'
  if (l === 'WARN' || l === 'WARNING') return 'log-row-warn'
  return ''
}

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
  return dur
}

function formatTotalDuration(ms) {
  if (!ms || ms === 0) return '0s'
  const hours = Math.floor(ms / 3600000)
  const minutes = Math.floor((ms % 3600000) / 60000)
  const seconds = Math.floor((ms % 60000) / 1000)
  
  if (hours > 0) return `${hours}h ${minutes}m ${seconds}s`
  if (minutes > 0) return `${minutes}m ${seconds}s`
  return `${seconds}s`
}

function formatSpec(lower, upper) {
  if (!lower && !upper) return '—'
  return `${lower || '?'} – ${upper || '?'}`
}

function resultBadge(result) {
  return {
    badge: true,
    'badge-passed': result === 'Passed' || result === 'Success',
    'badge-failed': result === 'Failed' || result === 'Failure' || result === 'Error'
  }
}

function resultClass(result) {
  return {
    'result-passed': result === 'Passed' || result === 'Success',
    'result-failed': result === 'Failed' || result === 'Failure' || result === 'Error',
    'result-notperformed': result === 'NotPerformed' || result === 'Ignored' || result === 'Inconclusive' || result === 'Skipped'
  }
}

onMounted(() => store.fetchRun(props.host, props.pdc, props.runId))
</script>

<style scoped>
/* ========== RESPONSIVE BASE ========== */
.run-detail {
  width: 100%;
  max-width: 100%;
  box-sizing: border-box;
  min-height: calc(100vh - 100px);
  display: flex;
  flex-direction: column;
}

.tab-content {
  flex: 1;
  width: 100%;
  overflow-y: auto;
  max-height: calc(100vh - 300px);
}

.back-link {
  display: inline-block;
  margin-bottom: 1rem;
  color: #42b883;
  text-decoration: none;
  font-weight: 600;
}

/* ========== RUN HEADER ========== */
.run-header {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 1rem;
  margin-bottom: 1.5rem;
}
.run-header h1 { 
  margin: 0; 
  font-size: 1.3rem;
  word-break: break-word;
}

.badge {
  padding: 0.3rem 0.8rem;
  border-radius: 12px;
  font-weight: 700;
  font-size: 0.85rem;
  white-space: nowrap;
}
.badge-passed { background: #d4edda; color: #155724; }
.badge-failed { background: #f8d7da; color: #721c24; }
.badge-mixed { background: #fff3cd; color: #856404; }
.badge-skipped { background: #ffeeba; color: #856404; }

/* ========== SUMMARY CARDS ========== */
.summary-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(120px, 1fr));
  gap: 0.75rem;
  margin-bottom: 1.5rem;
}
.summary-card {
  background: #f9f9f9;
  border-radius: 8px;
  padding: 0.75rem;
  text-align: center;
}
.summary-card .label { font-size: 0.75rem; color: #888; }
.summary-card .value { font-size: 1.4rem; font-weight: 700; }
.summary-card .value.small { font-size: 0.8rem; }
.summary-card.passed .value { color: #27ae60; }
.summary-card.failed .value { color: #e74c3c; }
.summary-card.skipped .value { color: #f39c12; }

/* ========== TABS ========== */
.tabs {
  display: flex;
  flex-wrap: wrap;
  gap: 0;
  border-bottom: 2px solid #e0e0e0;
  margin-bottom: 1rem;
}
.tab {
  padding: 0.5rem 1rem;
  border: none;
  background: none;
  cursor: pointer;
  font-weight: 600;
  font-size: 0.9rem;
  color: #666;
  border-bottom: 3px solid transparent;
  margin-bottom: -2px;
  transition: color 0.2s, border-color 0.2s;
  white-space: nowrap;
}
.tab:hover { color: #333; }
.tab.active {
  color: #42b883;
  border-bottom-color: #42b883;
}

/* ========== FILTER BAR ========== */
.result-filter {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  margin-bottom: 1rem;
}
.filter-input {
  flex: 1 1 200px;
  min-width: 150px;
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
  min-width: 120px;
}

/* ========== FEATURE ACCORDION ========== */
.feature-accordion {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  width: 100%;
}
.feature-group {
  border: 1px solid #e0e0e0;
  border-radius: 8px;
  overflow: hidden;
  width: 100%;
}
.feature-header {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.5rem;
  padding: 0.75rem 1rem;
  background: #f5f7f5;
  cursor: pointer;
  user-select: none;
  transition: background 0.15s;
}
.feature-header:hover {
  background: #eaf5ed;
}
.feature-toggle {
  font-size: 0.75rem;
  color: #888;
  flex-shrink: 0;
}
.feature-status-icon {
  font-size: 1rem;
  font-weight: bold;
  flex-shrink: 0;
}
.feature-status-icon.status-passed { color: #27ae60; }
.feature-status-icon.status-failed { color: #e74c3c; }
.feature-status-icon.status-mixed { color: #f39c12; }

.feature-name {
  font-weight: 600;
  font-size: 0.95rem;
  color: #333;
  flex: 1 1 auto;
  min-width: 0;
  word-break: break-word;
}
.label-prefix {
  color: #888;
  font-weight: 500;
  font-size: 0.85em;
}
.feature-meta {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.8rem;
}
.feature-time {
  color: #666;
  font-weight: 500;
}
.feature-result-badge {
  padding: 0.2rem 0.5rem;
  border-radius: 10px;
  font-weight: 600;
  font-size: 0.75rem;
}

.feature-counts {
  display: flex;
  gap: 0.4rem;
}
.count-total, .count-scenarios {
  background: #e9ecef;
  color: #555;
  padding: 0.15rem 0.4rem;
  border-radius: 8px;
  font-size: 0.75rem;
}

/* ========== FEATURE HOOKS (BeforeFeature/AfterFeature) ========== */
.feature-hooks {
  margin: 0.5rem 0;
  padding: 0.75rem;
  border-radius: 6px;
}
.feature-hooks.before-feature {
  background: #f0f9f0;
  border: 1px solid #c3e6c3;
}
.feature-hooks.after-feature {
  background: #f0f4f9;
  border: 1px solid #c3d4e6;
}
.hook-section-header {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  margin-bottom: 0.5rem;
  font-weight: 600;
  font-size: 0.9rem;
}
.hook-icon {
  font-size: 1rem;
}
.hook-title {
  color: #2c5282;
}
.before-feature .hook-title {
  color: #276749;
}
.after-feature .hook-title {
  color: #2c5282;
}
.hook-count {
  font-weight: 400;
  font-size: 0.8rem;
  color: #666;
}
.hook-logs {
  max-height: 200px;
  overflow-y: auto;
  border-radius: 4px;
  border: 1px solid #e0e0e0;
}

/* ========== SCENARIOS ========== */
.scenarios-container {
  border-top: 1px solid #e0e0e0;
}
.scenario-group {
  border-bottom: 1px solid #efefef;
}
.scenario-group:last-child {
  border-bottom: none;
}
.scenario-header {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.5rem;
  padding: 0.6rem 1rem 0.6rem 1.5rem;
  background: #fafcfa;
  cursor: pointer;
  user-select: none;
  transition: background 0.15s;
}
.scenario-header:hover {
  background: #f0f7f2;
}
.scenario-toggle {
  font-size: 0.65rem;
  color: #aaa;
  flex-shrink: 0;
}
.scenario-status-icon {
  font-size: 0.9rem;
  font-weight: bold;
  flex-shrink: 0;
}
.scenario-status-icon.status-passed { color: #27ae60; }
.scenario-status-icon.status-failed { color: #e74c3c; }
.scenario-status-icon.status-mixed { color: #f39c12; }

.scenario-name {
  font-weight: 500;
  font-size: 0.9rem;
  color: #444;
  flex: 1 1 auto;
  min-width: 0;
  word-break: break-word;
}
.scenario-meta {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.4rem;
  font-size: 0.8rem;
}
.scenario-time {
  color: #666;
  font-weight: 500;
}
.scenario-result-badge {
  padding: 0.15rem 0.4rem;
  border-radius: 8px;
  font-weight: 600;
  font-size: 0.7rem;
}
.count-variants {
  color: #888;
  font-size: 0.75rem;
}

/* ========== SCENARIO DETAILS ========== */
.scenario-details {
  padding: 1rem;
  background: #fff;
  border-top: 1px solid #eee;
}
.section-title {
  font-weight: 600;
  font-size: 0.85rem;
  color: #555;
  margin-bottom: 0.5rem;
  padding-bottom: 0.25rem;
  border-bottom: 1px solid #eee;
}

/* ========== VARIANT SECTION ========== */
.variant-section {
  margin-bottom: 0.75rem;
  border: 1px solid #e0e0e0;
  border-radius: 6px;
  overflow: hidden;
}
.variant-header {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.5rem;
  padding: 0.6rem 0.75rem;
  background: #f8f9fa;
  cursor: pointer;
  user-select: none;
  transition: background 0.15s;
}
.variant-header:hover {
  background: #eef0f2;
}
.variant-toggle {
  font-size: 0.7rem;
  color: #888;
  flex-shrink: 0;
}
.variant-number {
  font-weight: 600;
  font-size: 0.8rem;
  color: #666;
  background: #e9ecef;
  padding: 0.15rem 0.5rem;
  border-radius: 4px;
}
.variant-name {
  flex: 1;
  font-size: 0.85rem;
  color: #333;
  word-break: break-word;
}
.variant-result {
  font-weight: 600;
  font-size: 0.8rem;
  padding: 0.15rem 0.5rem;
  border-radius: 4px;
}
.variant-result.result-passed {
  background: #d4edda;
  color: #155724;
}
.variant-result.result-failed {
  background: #f8d7da;
  color: #721c24;
}
.variant-duration {
  font-family: 'Consolas', 'Courier New', monospace;
  font-size: 0.75rem;
  color: #666;
}
.variant-error-indicator {
  color: #e74c3c;
  font-size: 0.9rem;
}
.variant-details {
  padding: 0.75rem;
  background: #fff;
  border-top: 1px solid #eee;
}
.variant-error {
  background: #fff3f3;
  border: 1px solid #f5c6cb;
  border-radius: 6px;
  padding: 0.75rem;
  margin-bottom: 0.75rem;
}
.variant-error .error-title {
  font-weight: 600;
  color: #721c24;
  margin-bottom: 0.25rem;
  font-size: 0.85rem;
}
.variant-error .error-content {
  color: #721c24;
  font-size: 0.8rem;
  font-family: 'Consolas', 'Courier New', monospace;
  white-space: pre-wrap;
  word-break: break-word;
}

/* ========== SINGLE TEST (NO VARIANTS) ========== */
.single-test-details {
  padding: 0.75rem;
  background: #fff;
}
.single-test-info {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  margin-bottom: 0.75rem;
  padding: 0.5rem;
  background: #f8f9fa;
  border-radius: 4px;
}

/* ========== FALLBACK LOGS (NO STEPS) ========== */
.scenario-logs-fallback {
  margin-top: 0.5rem;
}
.scenario-logs-fallback .section-title {
  font-weight: 600;
  font-size: 0.9rem;
  color: #333;
  margin-bottom: 0.5rem;
}
.scenario-logs-fallback .logs-table-wrapper {
  max-height: 400px;
  overflow-y: auto;
  border: 1px solid #e0e0e0;
  border-radius: 6px;
}

/* ========== LOGS TABLE ========== */
.scenario-logs {
  margin-bottom: 1.5rem;
}
.logs-table-wrapper {
  overflow-x: auto;
  max-height: 300px;
  overflow-y: auto;
  border: 1px solid #e0e0e0;
  border-radius: 6px;
}
.logs-table {
  width: 100%;
  min-width: 500px;
  border-collapse: collapse;
  font-size: 0.8rem;
}
.logs-table th, .logs-table td {
  padding: 0.4rem 0.6rem;
  border-bottom: 1px solid #f0f0f0;
  text-align: left;
}
.logs-table th {
  background: #f8f9fa;
  font-weight: 600;
  position: sticky;
  top: 0;
  z-index: 1;
}
.logs-table .col-time { width: 100px; }
.logs-table .col-type { width: 60px; }
.logs-table .col-level { width: 70px; }
.logs-table .col-message { min-width: 300px; }

.log-time {
  font-family: 'Consolas', 'Courier New', monospace;
  font-size: 0.75rem;
  color: #666;
}
.log-type-pill {
  display: inline-block;
  padding: 0.1rem 0.4rem;
  border-radius: 4px;
  font-size: 0.65rem;
  font-weight: 600;
}
.type-hook { background: #e8daef; color: #6c3483; }
.type-step { background: #d5f5e3; color: #1e8449; }

.hook-row {
  background: #faf5ff;
}
.hook-row td {
  font-style: italic;
}

.log-level-pill {
  display: inline-block;
  padding: 0.1rem 0.4rem;
  border-radius: 4px;
  font-size: 0.7rem;
  font-weight: 600;
}
.level-error { background: #f8d7da; color: #721c24; }
.level-warn { background: #fff3cd; color: #856404; }
.level-info { background: #d1ecf1; color: #0c5460; }
.level-debug { background: #e9ecef; color: #495057; }

.log-message {
  font-size: 0.8rem;
  color: #333;
  max-width: 600px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.log-row-error { background: #fff5f5; }
.log-row-warn { background: #fffbeb; }

.no-logs {
  padding: 1rem;
  background: #f8f9fa;
  border-radius: 6px;
  color: #666;
  font-style: italic;
  font-size: 0.85rem;
  text-align: center;
  margin-bottom: 1rem;
}

/* ========== SCENARIO STEPS ========== */
.scenario-steps {
  margin-bottom: 1.5rem;
}
.steps-list {
  display: flex;
  flex-direction: column;
}
.step-item {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.6rem 0.75rem;
  background: #f9f9f9;
  border-radius: 6px;
  border-left: 4px solid #ddd;
  transition: all 0.2s ease;
}
.step-item.step-clickable {
  cursor: pointer;
}
.step-item.step-clickable:hover {
  background: #f0f0f0;
  filter: brightness(0.97);
}
.step-item:not(.step-clickable) {
  cursor: default;
}
.step-item.step-passed {
  border-left-color: #27ae60;
  background: #f0fdf4;
}
.step-item.step-failed {
  border-left-color: #e74c3c;
  background: #fef2f2;
}
.step-item.step-skipped {
  border-left-color: #f39c12;
  background: #fffbeb;
}
.step-item.step-running {
  border-left-color: #3498db;
  background: #eff6ff;
}
.step-number {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 24px;
  height: 24px;
  background: #e0e0e0;
  border-radius: 50%;
  font-size: 0.75rem;
  font-weight: 600;
  color: #555;
  flex-shrink: 0;
}
.step-keyword {
  display: inline-block;
  padding: 0.2rem 0.5rem;
  border-radius: 4px;
  font-size: 0.75rem;
  font-weight: 700;
  text-transform: uppercase;
  min-width: 55px;
  text-align: center;
  flex-shrink: 0;
}
.keyword-given {
  background: #d1ecf1;
  color: #0c5460;
}
.keyword-when {
  background: #e2d5f1;
  color: #5a2d82;
}
.keyword-then {
  background: #d4edda;
  color: #155724;
}
.keyword-and {
  background: #e2e3e5;
  color: #383d41;
}
.keyword-before {
  background: #ffeeba;
  color: #856404;
  min-width: 100px;
}
.keyword-after {
  background: #f5c6cb;
  color: #721c24;
  min-width: 100px;
}
.keyword-step {
  background: #d1d1d1;
  color: #333;
}
.step-wrapper {
  margin-bottom: 0.5rem;
}
.step-hook {
  border-left-width: 4px;
  border-left-style: dashed;
}
.step-duration {
  font-family: 'Consolas', 'Courier New', monospace;
  font-size: 0.75rem;
  color: #27ae60;
  background: #e8f8f0;
  padding: 0.1rem 0.4rem;
  border-radius: 4px;
  flex-shrink: 0;
}
.step-expand-icon {
  font-size: 0.75rem;
  color: #666;
  margin-left: 0.5rem;
  flex-shrink: 0;
}
.log-count {
  font-size: 0.7rem;
  color: #888;
  margin-left: 0.25rem;
}
.step-logs {
  margin-left: 2rem;
  margin-top: 0.25rem;
  margin-bottom: 0.5rem;
  border: 1px solid #e0e0e0;
  border-radius: 6px;
  overflow: hidden;
  background: #fff;
}
.step-logs-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.75rem;
}
.step-logs-table th,
.step-logs-table td {
  padding: 0.35rem 0.5rem;
  border-bottom: 1px solid #f0f0f0;
  text-align: left;
}
.step-logs-table th {
  background: #f8f9fa;
  font-weight: 600;
  font-size: 0.7rem;
}
.step-logs-table .col-time { width: 90px; }
.step-logs-table .col-level { width: 60px; }
.step-logs-table .col-component { width: 150px; }
.step-logs-table .col-message { min-width: 200px; }
.log-component {
  font-family: 'Consolas', 'Courier New', monospace;
  font-size: 0.7rem;
  color: #666;
  max-width: 150px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.log-message-full {
  font-size: 0.75rem;
  color: #333;
  word-break: break-word;
  white-space: pre-wrap;
}
.step-text {
  flex: 1;
  font-size: 0.85rem;
  color: #333;
  word-break: break-word;
}
.step-time {
  font-family: 'Consolas', 'Courier New', monospace;
  font-size: 0.75rem;
  color: #888;
  flex-shrink: 0;
}
.step-status {
  display: inline-block;
  padding: 0.15rem 0.5rem;
  border-radius: 10px;
  font-size: 0.7rem;
  font-weight: 600;
  flex-shrink: 0;
  min-width: 50px;
  text-align: center;
}
.step-status.step-passed {
  background: #d4edda;
  color: #155724;
}
.step-status.step-failed {
  background: #f8d7da;
  color: #721c24;
}
.step-status.step-skipped {
  background: #fff3cd;
  color: #856404;
}
.step-status.step-running {
  background: #cce5ff;
  color: #004085;
}

/* ========== TEST CASES TABLE ========== */
.test-cases-wrapper {
  overflow-x: auto;
}
.data-table {
  width: 100%;
  min-width: 400px;
  border-collapse: collapse;
  font-size: 0.85rem;
}
.data-table th, .data-table td {
  padding: 0.5rem 0.75rem;
  border-bottom: 1px solid #eee;
  text-align: left;
}
.data-table th { 
  background: #f5f5f5; 
  font-weight: 600; 
  position: sticky; 
  top: 0; 
}
.data-table .col-num { width: 40px; }
.data-table .col-name { min-width: 150px; max-width: 300px; }
.data-table .col-result { width: 80px; }
.data-table .col-duration { width: 100px; }
.data-table .col-error { min-width: 200px; }
.data-table .num { color: #aaa; }
.mono { font-family: 'Consolas', 'Courier New', monospace; font-size: 0.8rem; }
.spec { font-size: 0.75rem; color: #666; }

.test-name {
  max-width: 300px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.error-msg-cell {
  min-width: 200px;
  max-width: 100%;
}
.error-msg-container {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}
.error-msg-content {
  font-size: 0.8rem;
  color: #c0392b;
  background: #fdf2f2;
  padding: 0.4rem 0.6rem;
  border-radius: 4px;
  border-left: 3px solid #e74c3c;
  cursor: pointer;
  transition: all 0.2s ease;
  /* Collapsed state */
  max-height: 3.6em;
  overflow: hidden;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  word-break: break-word;
  white-space: pre-wrap;
}
.error-msg-content.expanded {
  max-height: none;
  -webkit-line-clamp: unset;
  display: block;
}
.error-msg-content:hover {
  background: #fce8e8;
}
.error-toggle-btn {
  align-self: flex-start;
  background: none;
  border: none;
  color: #3498db;
  font-size: 0.75rem;
  cursor: pointer;
  padding: 0.1rem 0.3rem;
  text-decoration: underline;
}
.error-toggle-btn:hover {
  color: #2980b9;
}
.no-error {
  color: #999;
  font-size: 0.8rem;
}

.result-passed { color: #27ae60; font-weight: 600; }
.result-failed { color: #e74c3c; font-weight: 600; }
.result-notperformed { color: #999; }

/* ========== SYSTEM INFO ========== */
.sysinfo-grid {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
  width: 100%;
  max-height: calc(100vh - 300px);
  overflow-y: auto;
  padding-right: 0.5rem;
}
.sysinfo-row {
  display: flex;
  flex-wrap: wrap;
  justify-content: space-between;
  padding: 0.6rem 0.75rem;
  background: #f9f9f9;
  border-radius: 6px;
  gap: 0.5rem;
  border-left: 3px solid #42b883;
}
.sysinfo-row:hover {
  background: #f0f0f0;
}
.sysinfo-label {
  font-weight: 600;
  color: #555;
  font-size: 0.85rem;
  min-width: 180px;
}
.sysinfo-value {
  font-family: 'Consolas', 'Courier New', monospace;
  font-size: 0.85rem;
  color: #333;
  word-break: break-word;
  flex: 1;
  text-align: right;
}

/* ========== MEASUREMENTS ========== */
.table-responsive {
  overflow-x: auto;
}
.measurements-table { min-width: 700px; }
.measurements-table td { font-size: 0.8rem; }

.status { padding: 2rem; text-align: center; color: #666; }
.error { color: #e74c3c; }

/* ========== RESPONSIVE BREAKPOINTS ========== */
@media (max-width: 768px) {
  .run-header h1 { font-size: 1.1rem; }
  .summary-grid { grid-template-columns: repeat(3, 1fr); }
  .summary-card .value { font-size: 1.2rem; }
  .feature-header, .scenario-header { padding: 0.6rem 0.75rem; }
  .feature-meta, .scenario-meta { width: 100%; justify-content: flex-start; margin-top: 0.25rem; }
  .scenario-details { padding: 0.75rem; }
  .error-msg-cell { min-width: 150px; }
  .error-msg-content { font-size: 0.75rem; }
  .step-item { flex-wrap: wrap; gap: 0.5rem; }
  .step-text { flex: 1 1 100%; order: 10; margin-top: 0.25rem; }
}

@media (max-width: 480px) {
  .summary-grid { grid-template-columns: repeat(2, 1fr); }
  .summary-card { padding: 0.5rem; }
  .summary-card .label { font-size: 0.65rem; }
  .summary-card .value { font-size: 1rem; }
  .tabs { overflow-x: auto; flex-wrap: nowrap; }
  .tab { font-size: 0.8rem; padding: 0.4rem 0.75rem; }
  .filter-input { flex: 1 1 100%; }
  .filter-select { flex: 1 1 45%; }
  .error-msg-cell { min-width: 120px; }
  .error-msg-content { 
    font-size: 0.7rem; 
    padding: 0.3rem 0.4rem;
    -webkit-line-clamp: 3;
  }
  .test-cases-table { font-size: 0.75rem; }
  .test-cases-table th, .test-cases-table td { padding: 0.3rem 0.4rem; }
  .step-item { padding: 0.5rem; }
  .step-keyword { min-width: 45px; font-size: 0.65rem; padding: 0.15rem 0.35rem; }
  .step-text { font-size: 0.8rem; }
  .step-number { width: 20px; height: 20px; font-size: 0.65rem; }
}
</style>
