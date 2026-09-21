import DefaultTheme from 'vitepress/theme'
import './custom.css'
import RulesetBuilder from './components/RulesetBuilder.vue'

export default {
  extends: DefaultTheme,
  enhanceApp({ app }) {
    app.component('RulesetBuilder', RulesetBuilder)
  }
}
