import { defineConfig } from 'vitepress'

export default defineConfig({
  base: '/funkarr/',
  title: 'FunkArr',
  description: 'German public broadcaster Mediathek integration for the *arr ecosystem',
  head: [
    ['link', { rel: 'icon', href: '/funkarr/logo.svg' }]
  ],
  themeConfig: {
    logo: '/logo.svg',
    nav: [
      { text: 'Guide', link: '/getting-started' },
      { text: 'Config', link: '/configuration' },
      { text: 'Rulesets', link: '/rulesets/' }
    ],
    sidebar: [
      {
        text: 'Guide',
        items: [
          { text: 'Getting Started', link: '/getting-started' },
          { text: 'Configuration', link: '/configuration' }
        ]
      },
      {
        text: 'Rulesets',
        items: [
          { text: 'Overview', link: '/rulesets/' },
          { text: 'Community Catalog', link: '/rulesets/catalog' },
          { text: 'Custom Rulesets', link: '/rulesets/custom' }
        ]
      }
    ],
    socialLinks: [
      { icon: 'github', link: 'https://github.com/st0o0/funkarr' }
    ],
    search: {
      provider: 'local'
    }
  }
})
