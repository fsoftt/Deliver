<script setup lang="ts">
import { useData } from 'vitepress'
import { onMounted, ref, watch } from 'vue'

const props = defineProps<{ code: string }>()
const { isDark } = useData()
const svg = ref('')
let counter = 0

// Diagrams follow the site identity: asphalt/concrete surfaces, signal-green routes.
const palettes = {
  dark: {
    background: '#141816',
    primaryColor: '#1b201d',
    primaryBorderColor: '#19c37d',
    primaryTextColor: '#edefea',
    secondaryColor: '#16241d',
    secondaryBorderColor: '#3a433e',
    tertiaryColor: '#141816',
    tertiaryBorderColor: '#3a433e',
    lineColor: '#9aa39c',
    textColor: '#edefea',
    mainBkg: '#1b201d',
    nodeBorder: '#19c37d',
    clusterBkg: '#111412',
    clusterBorder: '#3a433e',
    titleColor: '#edefea',
    edgeLabelBackground: '#141816',
    actorBkg: '#1b201d',
    actorBorder: '#19c37d',
    actorTextColor: '#edefea',
    actorLineColor: '#56615a',
    signalColor: '#c9cfc8',
    signalTextColor: '#edefea',
    labelBoxBkgColor: '#1b201d',
    labelBoxBorderColor: '#3a433e',
    labelTextColor: '#edefea',
    loopTextColor: '#edefea',
    noteBkgColor: '#2b2610',
    noteBorderColor: '#f5b700',
    noteTextColor: '#edefea',
    activationBkgColor: '#16241d',
    activationBorderColor: '#19c37d',
    sequenceNumberColor: '#04130b',
  },
  light: {
    background: '#ffffff',
    primaryColor: '#ffffff',
    primaryBorderColor: '#0b7a4b',
    primaryTextColor: '#121513',
    secondaryColor: '#eaf5ef',
    secondaryBorderColor: '#b9beb5',
    tertiaryColor: '#f7f8f4',
    tertiaryBorderColor: '#b9beb5',
    lineColor: '#4a524c',
    textColor: '#121513',
    mainBkg: '#ffffff',
    nodeBorder: '#0b7a4b',
    clusterBkg: '#f7f8f4',
    clusterBorder: '#b9beb5',
    titleColor: '#121513',
    edgeLabelBackground: '#ffffff',
    actorBkg: '#ffffff',
    actorBorder: '#0b7a4b',
    actorTextColor: '#121513',
    actorLineColor: '#8a948c',
    signalColor: '#2c322e',
    signalTextColor: '#121513',
    labelBoxBkgColor: '#ffffff',
    labelBoxBorderColor: '#b9beb5',
    labelTextColor: '#121513',
    loopTextColor: '#121513',
    noteBkgColor: '#fff6d6',
    noteBorderColor: '#c79500',
    noteTextColor: '#121513',
    activationBkgColor: '#eaf5ef',
    activationBorderColor: '#0b7a4b',
    sequenceNumberColor: '#ffffff',
  },
}

async function render() {
  // Loaded lazily in the browser only: keeps the static build light and SSR-safe.
  const { default: mermaid } = await import('mermaid')
  mermaid.initialize({
    startOnLoad: false,
    theme: 'base',
    securityLevel: 'strict',
    fontFamily: 'Inter, ui-sans-serif, system-ui, sans-serif',
    themeVariables: {
      ...(isDark.value ? palettes.dark : palettes.light),
      fontFamily: 'Inter, ui-sans-serif, system-ui, sans-serif',
      fontSize: '15px',
    },
  })
  const id = `mermaid-${Math.random().toString(36).slice(2)}-${counter++}`
  svg.value = (await mermaid.render(id, decodeURIComponent(props.code))).svg
}

onMounted(render)
watch(isDark, render)
</script>

<template>
  <div class="mermaid-diagram" v-html="svg" />
</template>
