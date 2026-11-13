import DefaultTheme from 'vitepress/theme'
import './custom.css'
import ArchitectureView from './components/ArchitectureView.vue'
import RulesetBuilder from './components/RulesetBuilder.vue'

export default {
  extends: DefaultTheme,
  enhanceApp({ app }) {
    app.component('ArchitectureView', ArchitectureView)
    app.component('RulesetBuilder', RulesetBuilder)
  }
}
