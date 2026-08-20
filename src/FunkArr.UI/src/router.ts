import { createRouter, createWebHashHistory } from 'vue-router'

const routes = [
  { path: '/', component: () => import('./views/QueueView.vue') },
  { path: '/history', component: () => import('./views/HistoryView.vue') },
  { path: '/rulesets', component: () => import('./views/RulesetsView.vue') },
  { path: '/rulesets/new', component: () => import('./views/RulesetEditor.vue') },
  { path: '/rulesets/:topic', component: () => import('./views/RulesetDetail.vue') },
  { path: '/rulesets/:topic/edit', component: () => import('./views/RulesetEditor.vue') },
  { path: '/matches', component: () => import('./views/MatchesView.vue') },
  { path: '/settings', component: () => import('./views/SettingsView.vue') },
  { path: '/setup', component: () => import('./views/SetupWizard.vue') },
]

export const router = createRouter({
  history: createWebHashHistory(),
  routes,
})

router.beforeEach((to) => {
  const apiKey = localStorage.getItem('funkarr-apikey')
  if (!apiKey && to.path !== '/setup') {
    return '/setup'
  }
})
