import { defineConfig } from 'vitepress'

const repo = 'https://github.com/fsoftt/Deliver'

export default defineConfig({
  title: 'Deliver',
  description: 'A last-mile delivery platform built with .NET 10 microservices, DDD, RabbitMQ and React: how it works and how it was built.',
  base: '/Deliver/',
  cleanUrls: true,
  // localhost URLs in "Run it locally" are meant for the reader's machine.
  ignoreDeadLinks: 'localhostLinks',
  lastUpdated: true,
  // Mermaid is large but loaded lazily, only on pages that contain diagrams.
  vite: { build: { chunkSizeWarningLimit: 3000 } },
  head: [
    ['link', { rel: 'icon', type: 'image/svg+xml', href: '/Deliver/favicon.svg' }],
    ['meta', { property: 'og:title', content: 'Deliver: distributed systems portfolio project' }],
    ['meta', { property: 'og:description', content: '.NET 10 microservices, DDD, RabbitMQ, outbox/inbox, sagas, OpenTelemetry and a React UI.' }],
  ],

  markdown: {
    // ```mermaid fences are rendered in the browser by the <Mermaid> component.
    config(md) {
      const fence = md.renderer.rules.fence!
      md.renderer.rules.fence = (tokens, idx, options, env, self) => {
        const token = tokens[idx]
        if (token.info.trim() === 'mermaid')
          return `<Mermaid code="${encodeURIComponent(token.content)}" />`
        return fence(tokens, idx, options, env, self)
      }
    },
  },

  themeConfig: {
    logo: '/favicon.svg',
    nav: [
      { text: 'Overview', link: '/guide/overview' },
      { text: 'Architecture', link: '/guide/architecture' },
      { text: 'Concepts', link: '/concepts/domain-driven-design' },
      { text: 'How it was built', link: '/guide/how-it-was-built' },
      { text: 'Source code', link: repo },
    ],
    sidebar: [
      {
        text: 'The project',
        items: [
          { text: 'Overview', link: '/guide/overview' },
          { text: 'Architecture', link: '/guide/architecture' },
          { text: 'Life of a shipment', link: '/guide/life-of-a-shipment' },
          { text: 'The web UI', link: '/guide/frontend' },
          { text: 'How it was built', link: '/guide/how-it-was-built' },
          { text: 'Testing and CI', link: '/guide/testing' },
          { text: 'Design decisions', link: '/guide/decisions' },
          { text: 'Run it locally', link: '/guide/run-locally' },
        ],
      },
      {
        text: 'Concepts demonstrated',
        items: [
          { text: 'Domain-Driven Design', link: '/concepts/domain-driven-design' },
          { text: 'Clean Architecture and vertical slices', link: '/concepts/clean-architecture' },
          { text: 'Event-driven architecture', link: '/concepts/event-driven-architecture' },
          { text: 'Transactional outbox and inbox', link: '/concepts/outbox-and-inbox' },
          { text: 'Retries and dead-letter queues', link: '/concepts/retries-and-dead-letters' },
          { text: 'Sagas and eventual consistency', link: '/concepts/sagas-and-eventual-consistency' },
          { text: 'Observability', link: '/concepts/observability' },
        ],
      },
    ],
    socialLinks: [{ icon: 'github', link: repo }],
    editLink: { pattern: `${repo}/edit/main/website/:path`, text: 'Edit this page on GitHub' },
    search: { provider: 'local' },
    outline: { level: [2, 3] },
    footer: {
      message: 'Released under the MIT License.',
      copyright: 'Deliver: a portfolio project about distributed systems with .NET',
    },
  },
})
