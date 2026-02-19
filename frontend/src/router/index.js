import { createRouter, createWebHashHistory } from 'vue-router'
import RunsList from '../views/RunsList.vue'
import RunDetail from '../views/RunDetail.vue'

const routes = [
  {
    path: '/',
    name: 'RunsList',
    component: RunsList
  },
  {
    path: '/testrun/:host/:pdc/:runId',
    name: 'RunDetail',
    component: RunDetail,
    props: true
  }
]

const router = createRouter({
  history: createWebHashHistory(),
  routes
})

export default router
