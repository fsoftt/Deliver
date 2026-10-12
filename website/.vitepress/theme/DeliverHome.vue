<script setup lang="ts">
import { useData, withBase } from 'vitepress'
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'

interface Action { theme: 'brand' | 'alt'; text: string; link: string }
interface Feature { title: string; details: string; link: string }
interface StackItem { label: string; text: string }

const { frontmatter } = useData()
const hero = computed(() => frontmatter.value.hero as { name: string; text: string; tagline: string; actions: Action[] })
const features = computed(() => (frontmatter.value.features ?? []) as Feature[])
const stack = computed(() => (frontmatter.value.stack ?? []) as StackItem[])
const shot = computed(() => frontmatter.value.screenshot as { src: string; alt: string })

const isExternal = (link: string) => /^https?:\/\//.test(link)
const href = (link: string) => (isExternal(link) ? link : withBase(link))
const pad = (n: number) => String(n).padStart(2, '0')

// Animations only start after hydration and only when the reader has not asked for reduced motion.
const animate = ref(false)
let media: MediaQueryList | undefined
const sync = () => { animate.value = !media?.matches }
onMounted(() => {
  media = window.matchMedia('(prefers-reduced-motion: reduce)')
  sync()
  media.addEventListener('change', sync)
})
onBeforeUnmount(() => media?.removeEventListener('change', sync))

/* ----------------------------------------------------------- route map */
const ROUTE_1 = 'M50 190 H190 L290 90 H560'
const ROUTE_2 = 'M190 190 L290 290 H560'
const BUS = 'M190 190 H574'

const stations = [
  { code: 'GW', name: 'API Gateway', x: 50, y: 190, label: 'above', kind: 'terminus' },
  { code: 'SHP', name: 'Shipping', x: 150, y: 190, label: 'below', kind: 'interchange' },
  { code: 'DSP', name: 'Dispatch', x: 340, y: 90, label: 'above', kind: 'stop' },
  { code: 'FLT', name: 'Fleet', x: 460, y: 90, label: 'above', kind: 'stop' },
  { code: 'ANL', name: 'Analytics', x: 460, y: 190, label: 'below', kind: 'bus' },
  { code: 'BIL', name: 'Billing', x: 340, y: 290, label: 'below', kind: 'stop' },
  { code: 'NTF', name: 'Notifications', x: 460, y: 290, label: 'below', kind: 'stop' },
]

/* ------------------------------------------------------- shipping label */
const tracking = 'DLV-2026-000184'
const steps = [
  { name: 'Pending', time: '16:27:55', state: 'done' },
  { name: 'Assigned', time: '16:28:00', state: 'done' },
  { name: 'Picked up', time: '16:28:01', state: 'current' },
  { name: 'Delivered', time: 'ETA 16:41', state: 'next' },
] as const
const stateText = { done: 'Done', current: 'Now', next: 'Next' } as const

// A deterministic, decorative barcode derived from the tracking number.
const barcode = computed(() => {
  const bars: { x: number; w: number }[] = []
  let x = 0
  const push = (w: number, gap: number) => { bars.push({ x, w }); x += w + gap }
  push(2, 1); push(1, 1); push(2, 2)
  for (const ch of tracking) {
    const v = ch.charCodeAt(0)
    push(1 + (v % 3), 1 + ((v >> 2) % 2))
    push(1 + ((v >> 1) % 2), 1 + ((v >> 3) % 3))
    push(1 + ((v >> 4) % 3), 2)
  }
  push(2, 1); push(1, 1); push(2, 0)
  return { bars, width: x }
})
</script>

<template>
  <div class="dh">
    <!-- ============================================================ HERO -->
    <section class="dh-hero">
      <div class="dh-wrap dh-hero-grid">
        <div class="dh-hero-copy">
          <p class="dh-eyebrow"><span class="dh-eyebrow-dot" aria-hidden="true" />00 — Dispatch center</p>
          <h1 class="dh-name">{{ hero.name }}</h1>
          <p class="dh-text">{{ hero.text }}</p>
          <p class="dh-tagline">{{ hero.tagline }}</p>
          <div class="dh-actions">
            <a
              v-for="a in hero.actions"
              :key="a.link"
              :class="['dh-btn', a.theme === 'brand' ? 'dh-btn-brand' : 'dh-btn-alt']"
              :href="href(a.link)"
              v-bind="isExternal(a.link) ? { target: '_blank', rel: 'noreferrer' } : {}"
            >
              {{ a.text }}
              <span aria-hidden="true" class="dh-btn-arrow">{{ isExternal(a.link) ? '↗' : '→' }}</span>
            </a>
          </div>
        </div>

        <figure class="dh-board" aria-labelledby="dh-board-caption">
          <div class="dh-board-bar">
            <span class="dh-mono">Line 1 · Shipment route</span>
            <span class="dh-chip-transit"><span class="dh-chip-icon" aria-hidden="true">▸</span>In transit</span>
          </div>

          <svg class="dh-map" viewBox="0 40 600 300" role="img" aria-labelledby="dh-map-title dh-map-desc">
            <title id="dh-map-title">Route map of the Deliver services</title>
            <desc id="dh-map-desc">
              The API gateway leads to Shipping. The shipment route continues to Dispatch and Fleet, a second line
              goes to Billing and Notifications, and a dashed RabbitMQ event bus runs to Analytics. A parcel travels
              along the shipment route.
            </desc>

            <!-- event bus: dashed lane marking -->
            <path :d="BUS" class="dh-map-bus-bed" />
            <path :d="BUS" class="dh-map-bus" />
            <text x="574" y="172" text-anchor="end" class="dh-map-tag">RABBITMQ EVENT BUS</text>

            <!-- lines -->
            <path :d="ROUTE_2" class="dh-map-line dh-map-line-2" />
            <path :d="ROUTE_1" class="dh-map-line dh-map-line-1" />
            <path d="M560 78 v24" class="dh-map-end" />
            <path d="M560 278 v24" class="dh-map-end dh-map-end-2" />

            <!-- event pulses -->
            <g v-if="animate" aria-hidden="true">
              <circle r="3.5" class="dh-map-pulse">
                <animateMotion :path="BUS" dur="3.2s" repeatCount="indefinite" />
              </circle>
              <circle r="3.5" class="dh-map-pulse">
                <animateMotion :path="BUS" dur="3.2s" begin="1.6s" repeatCount="indefinite" />
              </circle>
              <circle r="3.5" class="dh-map-pulse dh-map-pulse-2">
                <animateMotion :path="ROUTE_2" dur="4.4s" begin="0.8s" repeatCount="indefinite" />
              </circle>
            </g>

            <!-- stations -->
            <g v-for="s in stations" :key="s.code" :class="['dh-st', `dh-st-${s.kind}`]">
              <rect v-if="s.kind === 'terminus'" :x="s.x - 13" :y="s.y - 13" width="26" height="26" rx="6" />
              <rect v-else-if="s.kind === 'interchange'" :x="s.x - 11" :y="s.y - 11" width="22" height="22" rx="11" />
              <circle v-else :cx="s.x" :cy="s.y" r="8" />
              <text
                :x="s.kind === 'terminus' ? s.x - 13 : s.x"
                :y="s.label === 'above' ? s.y - (s.kind === 'terminus' ? 26 : 22) : s.y + 34"
                :text-anchor="s.kind === 'terminus' ? 'start' : 'middle'"
                class="dh-map-label"
              >
                <tspan class="dh-map-code">{{ s.code }}</tspan>
                <tspan class="dh-map-name" dx="6">{{ s.name }}</tspan>
              </text>
            </g>

            <!-- the parcel -->
            <g aria-hidden="true">
              <g v-if="animate">
                <rect x="-8" y="-8" width="16" height="16" rx="3" class="dh-parcel-halo" />
                <rect x="-6" y="-6" width="12" height="12" rx="2.5" class="dh-parcel" />
                <animateMotion :path="ROUTE_1" dur="8s" repeatCount="indefinite" keyPoints="0;0.18;0.18;0.6;0.6;0.82;0.82;1;1" keyTimes="0;0.18;0.28;0.5;0.58;0.74;0.8;0.92;1" calcMode="linear" />
              </g>
              <g v-else transform="translate(400 90)">
                <rect x="-8" y="-8" width="16" height="16" rx="3" class="dh-parcel-halo" />
                <rect x="-6" y="-6" width="12" height="12" rx="2.5" class="dh-parcel" />
              </g>
            </g>
          </svg>

          <figcaption id="dh-board-caption" class="dh-legend">
            <span><i class="dh-key dh-key-1" aria-hidden="true" />Shipment route</span>
            <span><i class="dh-key dh-key-2" aria-hidden="true" />Billing &amp; notify</span>
            <span><i class="dh-key dh-key-bus" aria-hidden="true" />Event bus</span>
            <span><i class="dh-key dh-key-parcel" aria-hidden="true" />Parcel in transit</span>
          </figcaption>
          <ul class="dh-station-key" aria-label="Station codes">
            <li v-for="s in stations" :key="s.code"><b>{{ s.code }}</b>{{ s.name }}</li>
          </ul>
        </figure>
      </div>
    </section>

    <!-- =========================================================== STOPS -->
    <section class="dh-section" aria-labelledby="dh-stops-title">
      <div class="dh-wrap">
        <header class="dh-section-head">
          <p class="dh-eyebrow">01 — Route stops</p>
          <h2 id="dh-stops-title" class="dh-h2">Six stops through the concepts it demonstrates</h2>
        </header>
        <ol class="dh-stops">
          <li v-for="(f, i) in features" :key="f.link" class="dh-stop">
            <a :href="href(f.link)" class="dh-stop-link">
              <span class="dh-stop-marker" aria-hidden="true">{{ pad(i + 1) }}</span>
              <span class="dh-stop-code dh-mono">Stop {{ pad(i + 1) }}</span>
              <span class="dh-stop-title">{{ f.title }}</span>
              <span class="dh-stop-details">{{ f.details }}</span>
              <span class="dh-stop-more dh-mono">Read <span aria-hidden="true">→</span></span>
            </a>
          </li>
        </ol>
      </div>
    </section>

    <!-- ======================================================== GLANCE -->
    <section class="dh-section" aria-labelledby="dh-glance-title">
      <div class="dh-wrap">
        <header class="dh-section-head">
          <p class="dh-eyebrow">02 — Manifest</p>
          <h2 id="dh-glance-title" class="dh-h2">At a glance</h2>
        </header>
        <dl class="dh-manifest">
          <div v-for="(s, i) in stack" :key="s.label" class="dh-manifest-row">
            <dt><span class="dh-manifest-n" aria-hidden="true">{{ pad(i + 1) }}</span>{{ s.label }}</dt>
            <dd>{{ s.text }}</dd>
          </div>
        </dl>
      </div>
    </section>

    <!-- ======================================================= TRACKING -->
    <section class="dh-section dh-section-last" aria-labelledby="dh-pod-title">
      <div class="dh-wrap">
        <header class="dh-section-head">
          <p class="dh-eyebrow">03 — Proof of delivery</p>
          <h2 id="dh-pod-title" class="dh-h2">One shipment, four services, one page</h2>
        </header>

        <div class="dh-pod">
          <article class="dh-label" aria-label="Example shipping label">
            <header class="dh-label-head">
              <span class="dh-label-brand">Deliver</span>
              <span class="dh-label-svc">Last-mile · Std</span>
            </header>
            <div class="dh-label-body">
              <div class="dh-label-fields">
                <div><span>From</span>Santiago</div>
                <div><span>To</span>Viña del Mar</div>
                <div><span>Weight</span>3 kg</div>
                <div><span>Route</span>GW › SHP › DSP › FLT</div>
              </div>

              <div class="dh-label-track">
                <span class="dh-label-k">Tracking no.</span>
                <span class="dh-label-no">{{ tracking }}</span>
                <svg
                  class="dh-barcode"
                  :viewBox="`0 0 ${barcode.width} 40`"
                  preserveAspectRatio="none"
                  aria-hidden="true"
                >
                  <rect v-for="(b, i) in barcode.bars" :key="i" :x="b.x" y="0" :width="b.w" height="40" />
                </svg>
              </div>

              <ol class="dh-timeline" aria-label="Shipment status">
                <li v-for="s in steps" :key="s.name" :class="['dh-tl', `dh-tl-${s.state}`]" :aria-current="s.state === 'current' ? 'step' : undefined">
                  <span class="dh-tl-icon" aria-hidden="true">
                    <svg v-if="s.state === 'done'" viewBox="0 0 16 16"><path d="M3.5 8.5 6.5 11.5 12.5 5" /></svg>
                    <svg v-else-if="s.state === 'current'" viewBox="0 0 16 16"><path d="M2 5h7v6H2zM9 7h3l2 2v2H9z" /><circle cx="5" cy="12" r="1.4" /><circle cx="12" cy="12" r="1.4" /></svg>
                  </span>
                  <span class="dh-tl-name">{{ s.name }}</span>
                  <span class="dh-tl-time">{{ s.time }}</span>
                  <span class="dh-tl-state">{{ stateText[s.state] }}</span>
                </li>
              </ol>
            </div>
          </article>

          <figure class="dh-window">
            <div class="dh-window-bar" aria-hidden="true">
              <span class="dh-window-lamps"><i /><i /><i /></span>
              <span class="dh-window-url">localhost:5173/shipments/92d1ee42</span>
            </div>
            <img :src="withBase(shot.src)" :alt="shot.alt" width="1440" height="1142" loading="lazy" decoding="async" />
            <figcaption class="dh-window-caption">{{ shot.alt }}</figcaption>
          </figure>
        </div>
      </div>
    </section>
  </div>
</template>

<style scoped>
.dh {
  --dh-max: 1200px;
  color: var(--dlv-text);
  background: var(--dlv-bg);
  overflow-x: clip;
}

.dh-wrap {
  max-width: var(--dh-max);
  margin: 0 auto;
  padding: 0 24px;
}

.dh-mono {
  font-family: var(--dlv-font-mono);
}

.dh-eyebrow {
  display: inline-flex;
  align-items: center;
  gap: 10px;
  margin: 0 0 18px;
  font-family: var(--dlv-font-mono);
  font-size: 12px;
  font-weight: 600;
  letter-spacing: 0.18em;
  text-transform: uppercase;
  color: var(--dlv-green);
}

.dh-eyebrow::after {
  content: '';
  width: 40px;
  height: 2px;
  background: repeating-linear-gradient(90deg, currentColor 0 6px, transparent 6px 10px);
}

.dh-eyebrow-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: var(--dlv-green);
  box-shadow: 0 0 0 4px var(--dlv-green-soft);
}

/* ------------------------------------------------------------------ hero */
.dh-hero {
  position: relative;
  padding: 56px 0 72px;
  border-bottom: 1px solid var(--dlv-line);
  background-image:
    linear-gradient(var(--dlv-line) 1px, transparent 1px),
    linear-gradient(90deg, var(--dlv-line) 1px, transparent 1px);
  background-size: 48px 48px;
  background-position: -1px -1px;
}

.dh-hero::before {
  content: '';
  position: absolute;
  inset: 0;
  background: radial-gradient(ellipse 90% 80% at 30% 40%, transparent 0%, var(--dlv-bg) 75%);
  pointer-events: none;
}

.dh-hero-grid {
  position: relative;
  display: grid;
  grid-template-columns: minmax(0, 1fr) minmax(0, 1.05fr);
  gap: 56px;
  align-items: center;
}

.dh-name {
  margin: 0;
  font-family: var(--dlv-font-display);
  font-weight: 900;
  font-stretch: 125%;
  text-transform: uppercase;
  font-size: clamp(46px, 6.8vw, 92px);
  line-height: 0.92;
  white-space: nowrap;
  letter-spacing: -0.01em;
  color: var(--dlv-text);
}

.dh-text {
  margin: 22px 0 0;
  max-width: 30ch;
  font-family: var(--dlv-font-display);
  font-weight: 700;
  font-stretch: 100%;
  font-size: clamp(22px, 2.5vw, 30px);
  line-height: 1.2;
  letter-spacing: -0.01em;
  color: var(--dlv-text);
}

.dh-tagline {
  margin: 18px 0 0;
  max-width: 54ch;
  font-size: 16.5px;
  line-height: 1.65;
  color: var(--dlv-muted);
}

.dh-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
  margin-top: 32px;
}

.dh-btn {
  display: inline-flex;
  align-items: center;
  gap: 10px;
  min-height: 46px;
  padding: 0 20px;
  border-radius: 8px;
  font-family: var(--dlv-font-display);
  font-weight: 700;
  font-size: 15px;
  text-decoration: none;
  transition: background-color 0.2s, border-color 0.2s, color 0.2s, transform 0.2s;
}

.dh-btn:focus-visible {
  outline: 2px solid var(--dlv-green);
  outline-offset: 3px;
}

.dh-btn-brand {
  background: var(--dlv-green);
  color: var(--dlv-on-green);
}

.dh-btn-brand:hover {
  background: var(--dlv-green-hi);
}

.dh-btn-alt {
  border: 1px solid var(--dlv-line-strong);
  background: var(--dlv-surface);
  color: var(--dlv-text);
}

.dh-btn-alt:hover {
  border-color: var(--dlv-green);
  color: var(--dlv-green);
}

.dh-btn-arrow {
  font-family: var(--dlv-font-mono);
  transition: transform 0.2s;
}

.dh-btn:hover .dh-btn-arrow {
  transform: translateX(3px);
}

/* dispatch board */
.dh-board {
  margin: 0;
  border: 1px solid var(--dlv-line-strong);
  border-radius: 14px;
  background: var(--dlv-surface);
  box-shadow: var(--dlv-shadow);
  overflow: hidden;
}

.dh-board-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 12px 16px;
  border-bottom: 1px solid var(--dlv-line);
  background: var(--dlv-surface-2);
  font-size: 11.5px;
  font-weight: 600;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  color: var(--dlv-muted);
}

.dh-chip-transit {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 4px 9px;
  border-radius: 999px;
  border: 1px solid var(--dlv-amber);
  background: var(--dlv-amber-soft);
  color: var(--dlv-amber-ink);
  font-family: var(--dlv-font-mono);
  font-size: 11px;
  font-weight: 700;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  white-space: nowrap;
}

.dh-chip-icon {
  animation: dh-blink 1.6s steps(2, jump-none) infinite;
}

.dh-map {
  display: block;
  width: 100%;
  height: auto;
  padding: 8px 8px 0;
  box-sizing: border-box;
}

.dh-map-line {
  fill: none;
  stroke-width: 8;
  stroke-linecap: round;
  stroke-linejoin: round;
}

.dh-map-line-1 {
  stroke: var(--dlv-green);
}

.dh-map-line-2 {
  stroke: var(--dlv-route-2);
}

.dh-map-end {
  stroke: var(--dlv-green);
  stroke-width: 5;
  stroke-linecap: round;
}

.dh-map-end-2 {
  stroke: var(--dlv-route-2);
}

.dh-map-bus-bed {
  fill: none;
  stroke: var(--dlv-surface-2);
  stroke-width: 14;
  stroke-linecap: round;
}

.dh-map-bus {
  fill: none;
  stroke: var(--dlv-muted);
  stroke-width: 2.5;
  stroke-dasharray: 12 9;
  animation: dh-lane 1.4s linear infinite;
}

.dh-map-tag {
  font-family: var(--dlv-font-mono);
  font-size: 10px;
  font-weight: 600;
  letter-spacing: 0.16em;
  fill: var(--dlv-faint);
}

.dh-map-pulse {
  fill: var(--dlv-green);
}

.dh-map-pulse-2 {
  fill: var(--dlv-text);
  opacity: 0.7;
}

.dh-st rect,
.dh-st circle {
  fill: var(--dlv-station-fill);
  stroke: var(--dlv-station-ring);
  stroke-width: 3.5;
}

.dh-st-terminus rect {
  fill: var(--dlv-green);
  stroke: var(--dlv-station-ring);
}

.dh-st-bus circle {
  stroke-dasharray: 3 2.4;
  stroke-width: 3;
}

.dh-map-label {
  font-size: 14px;
}

.dh-map-code {
  font-family: var(--dlv-font-mono);
  font-weight: 700;
  fill: var(--dlv-text);
  letter-spacing: 0.04em;
}

.dh-map-name {
  font-family: var(--vp-font-family-base);
  fill: var(--dlv-muted);
  font-size: 13px;
}

.dh-parcel {
  fill: var(--dlv-amber);
  stroke: #121513;
  stroke-width: 1.5;
}

.dh-parcel-halo {
  fill: var(--dlv-amber);
  opacity: 0.25;
  transform-box: fill-box;
  transform-origin: center;
  animation: dh-halo 1.6s ease-out infinite;
}

.dh-legend {
  display: flex;
  flex-wrap: wrap;
  gap: 8px 18px;
  padding: 12px 16px 14px;
  border-top: 1px solid var(--dlv-line);
  font-family: var(--dlv-font-mono);
  font-size: 11px;
  letter-spacing: 0.06em;
  text-transform: uppercase;
  color: var(--dlv-muted);
}

.dh-legend span {
  display: inline-flex;
  align-items: center;
  gap: 8px;
}

.dh-key {
  display: inline-block;
  width: 22px;
  height: 5px;
  border-radius: 3px;
}

.dh-key-1 { background: var(--dlv-green); }
.dh-key-2 { background: var(--dlv-route-2); }
.dh-key-bus {
  height: 2px;
  background: repeating-linear-gradient(90deg, var(--dlv-muted) 0 6px, transparent 6px 10px);
}
.dh-key-parcel {
  width: 10px;
  height: 10px;
  border-radius: 2px;
  background: var(--dlv-amber);
  box-shadow: 0 0 0 1.5px #121513 inset;
}

.dh-station-key {
  display: none;
}

/* --------------------------------------------------------------- sections */
.dh-section {
  padding: 88px 0 0;
}

.dh-section-last {
  padding-bottom: 96px;
}

.dh-section-head {
  margin-bottom: 36px;
}

.dh-h2 {
  margin: 0;
  max-width: 24ch;
  font-family: var(--dlv-font-display);
  font-weight: 800;
  font-stretch: 112%;
  font-size: clamp(26px, 3.4vw, 40px);
  line-height: 1.1;
  letter-spacing: -0.01em;
  color: var(--dlv-text);
}

/* ------------------------------------------------------------------ stops */
.dh-stops {
  --gap: 24px;
  list-style: none;
  margin: 0;
  padding: 0;
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 40px var(--gap);
}

.dh-stop {
  position: relative;
  padding-top: 62px;
}

/* the line that connects the stops of one row */
.dh-stop::before {
  content: '';
  position: absolute;
  top: 19px;
  left: 0;
  right: calc(-1 * var(--gap));
  height: 6px;
  border-radius: 3px;
  background: var(--dlv-green);
}

.dh-stop:nth-child(3n)::before {
  right: 0;
  background: linear-gradient(90deg, var(--dlv-green) 60%, transparent);
}

.dh-stop:nth-child(3n + 1)::after {
  content: '';
  position: absolute;
  top: 19px;
  left: -24px;
  width: 24px;
  height: 6px;
  background: linear-gradient(90deg, transparent, var(--dlv-green));
}

.dh-stop-link {
  display: flex;
  flex-direction: column;
  height: 100%;
  padding: 22px 22px 20px;
  border: 1px solid var(--dlv-line);
  border-radius: 10px;
  background: var(--dlv-surface);
  color: inherit;
  text-decoration: none;
  transition: border-color 0.2s, transform 0.2s, background-color 0.2s;
}

.dh-stop-link:hover {
  border-color: var(--dlv-green);
  transform: translateY(-2px);
}

.dh-stop-link:focus-visible {
  outline: 2px solid var(--dlv-green);
  outline-offset: 3px;
}

.dh-stop-marker {
  position: absolute;
  top: 0;
  left: 0;
  z-index: 1;
  display: grid;
  place-items: center;
  width: 44px;
  height: 44px;
  border-radius: 50%;
  background: var(--dlv-station-fill);
  border: 4px solid var(--dlv-station-ring);
  box-sizing: border-box;
  font-family: var(--dlv-font-mono);
  font-weight: 700;
  font-size: 13px;
  color: var(--dlv-text);
  transition: background-color 0.2s, color 0.2s, border-color 0.2s;
}

.dh-stop:hover .dh-stop-marker,
.dh-stop:focus-within .dh-stop-marker {
  background: var(--dlv-green);
  border-color: var(--dlv-green);
  color: var(--dlv-on-green);
}

.dh-stop-code {
  font-size: 11px;
  font-weight: 600;
  letter-spacing: 0.16em;
  text-transform: uppercase;
  color: var(--dlv-faint);
}

.dh-stop-title {
  margin-top: 8px;
  font-family: var(--dlv-font-display);
  font-weight: 750;
  font-size: 19px;
  line-height: 1.25;
  color: var(--dlv-text);
}

.dh-stop-details {
  margin-top: 10px;
  flex: 1;
  font-size: 14.5px;
  line-height: 1.6;
  color: var(--dlv-muted);
}

.dh-stop-more {
  margin-top: 18px;
  font-size: 12px;
  font-weight: 600;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  color: var(--dlv-green);
}

/* --------------------------------------------------------------- manifest */
.dh-manifest {
  margin: 0;
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  border-top: 2px solid var(--dlv-text);
}

.dh-manifest-row {
  display: grid;
  grid-template-columns: 170px minmax(0, 1fr);
  gap: 16px;
  padding: 16px 4px;
  border-bottom: 1px dashed var(--dlv-line-strong);
}

.dh-manifest-row:nth-child(odd) {
  padding-right: 28px;
}

.dh-manifest-row:nth-child(even) {
  padding-left: 28px;
  border-left: 1px solid var(--dlv-line);
}

.dh-manifest dt {
  display: flex;
  align-items: baseline;
  gap: 10px;
  font-family: var(--dlv-font-mono);
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  color: var(--dlv-text);
}

.dh-manifest-n {
  color: var(--dlv-green);
}

.dh-manifest dd {
  margin: 0;
  font-size: 15px;
  line-height: 1.55;
  color: var(--dlv-muted);
}

/* ------------------------------------------------------ proof of delivery */
.dh-pod {
  display: grid;
  grid-template-columns: 340px minmax(0, 1fr);
  gap: 40px;
  align-items: start;
}

/* the shipping label is paper in both themes */
.dh-label {
  --paper: #f7f6f0;
  --ink: #121513;
  --ink-2: #4a524c;
  --ink-green: #0b7a4b;
  position: sticky;
  top: calc(var(--vp-nav-height) + 24px);
  color: var(--ink);
  background: var(--paper);
  border: 2px solid var(--ink);
  border-radius: 6px;
  box-shadow: var(--dlv-shadow);
  transform: rotate(-1.2deg);
}

.dh-label-head {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 10px 14px;
  background: var(--ink);
  color: var(--paper);
}

.dh-label-brand {
  font-family: var(--dlv-font-display);
  font-weight: 900;
  font-stretch: 125%;
  text-transform: uppercase;
  letter-spacing: 0.06em;
  font-size: 18px;
}

.dh-label-svc {
  font-family: var(--dlv-font-mono);
  font-size: 10.5px;
  font-weight: 600;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  padding: 3px 7px;
  border: 1px solid currentColor;
  border-radius: 3px;
}

.dh-label-body {
  padding: 0 14px 16px;
}

.dh-label-fields {
  display: grid;
  grid-template-columns: 1fr 1fr;
  border-bottom: 2px solid var(--ink);
}

.dh-label-fields > div {
  padding: 10px 0;
  font-size: 14px;
  font-weight: 600;
}

.dh-label-fields > div:nth-child(odd) {
  border-right: 1px solid var(--ink);
  padding-right: 10px;
}

.dh-label-fields > div:nth-child(even) {
  padding-left: 12px;
}

.dh-label-fields > div:nth-child(-n + 2) {
  border-bottom: 1px solid var(--ink);
}

.dh-label-fields span,
.dh-label-k {
  display: block;
  font-family: var(--dlv-font-mono);
  font-size: 10px;
  font-weight: 600;
  letter-spacing: 0.16em;
  text-transform: uppercase;
  color: var(--ink-2);
  margin-bottom: 2px;
}

.dh-label-fields > div:nth-child(4) {
  font-family: var(--dlv-font-mono);
  font-size: 11px;
  letter-spacing: 0;
  white-space: nowrap;
}

.dh-label-track {
  padding: 14px 0 12px;
  border-bottom: 2px solid var(--ink);
}

.dh-label-no {
  display: block;
  font-family: var(--dlv-font-mono);
  font-weight: 700;
  font-size: 22px;
  letter-spacing: 0.04em;
}

.dh-barcode {
  display: block;
  width: 100%;
  height: 54px;
  margin-top: 10px;
  fill: var(--ink);
}

.dh-timeline {
  list-style: none;
  margin: 14px 0 0;
  padding: 0;
}

.dh-tl {
  position: relative;
  display: grid;
  grid-template-columns: 26px minmax(0, 1fr) auto;
  grid-template-rows: auto auto;
  column-gap: 12px;
  padding-bottom: 14px;
}

.dh-tl:last-child {
  padding-bottom: 0;
}

/* connector between steps */
.dh-tl:not(:last-child)::before {
  content: '';
  position: absolute;
  left: 12px;
  top: 26px;
  bottom: 0;
  width: 2px;
  background: var(--ink-green);
}

.dh-tl-current::before,
.dh-tl-next::before {
  background: repeating-linear-gradient(var(--ink-2) 0 4px, transparent 4px 7px) !important;
}

.dh-tl-icon {
  grid-row: 1 / 3;
  display: grid;
  place-items: center;
  width: 26px;
  height: 26px;
  border-radius: 50%;
  box-sizing: border-box;
}

.dh-tl-icon svg {
  width: 16px;
  height: 16px;
}

.dh-tl-done .dh-tl-icon {
  background: var(--ink-green);
}

.dh-tl-done .dh-tl-icon path {
  fill: none;
  stroke: #fff;
  stroke-width: 2.2;
  stroke-linecap: round;
  stroke-linejoin: round;
}

.dh-tl-current .dh-tl-icon {
  background: var(--dlv-amber);
  border: 2px solid var(--ink);
}

.dh-tl-current .dh-tl-icon svg {
  fill: var(--ink);
}

.dh-tl-next .dh-tl-icon {
  border: 2px dashed var(--ink-2);
  background: var(--paper);
}

.dh-tl-name {
  font-weight: 600;
  font-size: 14.5px;
  line-height: 26px;
}

.dh-tl-current .dh-tl-name {
  font-weight: 800;
}

.dh-tl-next .dh-tl-name {
  color: var(--ink-2);
}

.dh-tl-time {
  grid-column: 2;
  font-family: var(--dlv-font-mono);
  font-size: 11.5px;
  color: var(--ink-2);
}

.dh-tl-state {
  grid-column: 3;
  grid-row: 1;
  align-self: center;
  font-family: var(--dlv-font-mono);
  font-size: 10px;
  font-weight: 700;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  padding: 3px 7px;
  border-radius: 3px;
  color: var(--ink-2);
  border: 1px solid transparent;
}

.dh-tl-done .dh-tl-state {
  color: var(--ink-green);
}

.dh-tl-current .dh-tl-state {
  background: var(--dlv-amber);
  color: var(--ink);
  border-color: var(--ink);
}

.dh-tl-next .dh-tl-state {
  border: 1px dashed var(--ink-2);
}

/* app window */
.dh-window {
  margin: 0;
  border: 1px solid var(--dlv-line-strong);
  border-radius: 12px;
  overflow: hidden;
  background: var(--dlv-surface);
  box-shadow: var(--dlv-shadow);
}

.dh-window-bar {
  display: flex;
  align-items: center;
  gap: 14px;
  padding: 10px 14px;
  border-bottom: 1px solid var(--dlv-line);
  background: var(--dlv-surface-2);
}

.dh-window-lamps {
  display: inline-flex;
  gap: 7px;
  flex: none;
}

.dh-window-lamps i {
  width: 11px;
  height: 11px;
  border-radius: 50%;
  background: var(--dlv-line-strong);
}

.dh-window-lamps i:last-child {
  background: var(--dlv-green);
}

.dh-window-url {
  flex: 1;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  padding: 5px 12px;
  border-radius: 6px;
  background: var(--dlv-bg);
  border: 1px solid var(--dlv-line);
  font-family: var(--dlv-font-mono);
  font-size: 12px;
  color: var(--dlv-muted);
}

.dh-window img {
  display: block;
  width: 100%;
  height: auto;
}

.dh-window-caption {
  padding: 12px 16px;
  border-top: 1px solid var(--dlv-line);
  font-family: var(--dlv-font-mono);
  font-size: 11.5px;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--dlv-muted);
}

/* ------------------------------------------------------------- animation */
@keyframes dh-lane {
  to { stroke-dashoffset: -21; }
}

@keyframes dh-halo {
  0% { transform: scale(1); opacity: 0.45; }
  100% { transform: scale(2.1); opacity: 0; }
}

@keyframes dh-blink {
  50% { opacity: 0.25; }
}

@media (prefers-reduced-motion: reduce) {
  .dh-map-bus,
  .dh-parcel-halo,
  .dh-chip-icon {
    animation: none;
  }

  .dh-btn,
  .dh-btn-arrow,
  .dh-stop-link {
    transition: none;
  }

  .dh-stop-link:hover {
    transform: none;
  }
}

/* ------------------------------------------------------------- responsive */
@media (max-width: 1000px) {
  .dh-hero-grid {
    grid-template-columns: minmax(0, 1fr);
    gap: 44px;
  }

  .dh-text {
    max-width: 34ch;
  }

  .dh-stops {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .dh-stop:nth-child(3n)::before {
    right: calc(-1 * var(--gap));
    background: var(--dlv-green);
  }

  .dh-stop:nth-child(3n + 1)::after {
    display: none;
  }

  .dh-stop:nth-child(2n)::before {
    right: 0;
    background: linear-gradient(90deg, var(--dlv-green) 60%, transparent);
  }

  .dh-stop:nth-child(2n + 1)::after {
    content: '';
    display: block;
    position: absolute;
    top: 19px;
    left: -24px;
    width: 24px;
    height: 6px;
    background: linear-gradient(90deg, transparent, var(--dlv-green));
  }

  .dh-pod {
    grid-template-columns: minmax(0, 1fr);
  }

  .dh-label {
    position: relative;
    top: 0;
    max-width: 380px;
  }
}

@media (max-width: 720px) {
  .dh-manifest {
    grid-template-columns: minmax(0, 1fr);
  }

  .dh-manifest-row:nth-child(n) {
    padding: 14px 2px;
    border-left: 0;
  }
}

@media (max-width: 640px) {
  .dh-wrap {
    padding: 0 16px;
  }

  .dh-hero {
    padding: 36px 0 52px;
  }

  .dh-tagline {
    font-size: 15.5px;
  }

  .dh-actions {
    flex-direction: column;
    align-items: stretch;
  }

  .dh-btn {
    justify-content: space-between;
  }

  .dh-board-bar {
    padding: 10px 12px;
    font-size: 10px;
    letter-spacing: 0.1em;
  }

  /* on a phone the map shows station codes only, with a key underneath */
  .dh-map-name {
    display: none;
  }

  .dh-map-label {
    font-size: 19px;
  }

  .dh-map-tag {
    font-size: 13px;
  }

  .dh-station-key {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 4px 12px;
    margin: 0;
    padding: 0 16px 14px;
    list-style: none;
    font-size: 12.5px;
    color: var(--dlv-muted);
  }

  .dh-station-key b {
    display: inline-block;
    min-width: 34px;
    margin-right: 6px;
    font-family: var(--dlv-font-mono);
    color: var(--dlv-text);
  }

  .dh-legend {
    padding: 12px 16px 10px;
    font-size: 10.5px;
  }

  .dh-section {
    padding-top: 64px;
  }

  .dh-section-last {
    padding-bottom: 64px;
  }

  /* stops become a vertical line */
  .dh-stops {
    grid-template-columns: minmax(0, 1fr);
    gap: 0;
  }

  .dh-stop {
    padding: 0 0 20px 58px;
  }

  .dh-stop:last-child {
    padding-bottom: 0;
  }

  .dh-stop:nth-child(n)::before {
    top: 22px;
    bottom: 0;
    left: 19px;
    right: auto;
    width: 6px;
    height: auto;
    background: var(--dlv-green);
  }

  .dh-stop:last-child::before {
    display: none;
  }

  .dh-stop:nth-child(n)::after {
    display: none;
  }

  .dh-stop-link {
    padding: 18px 18px 16px;
  }

  .dh-manifest-row {
    grid-template-columns: minmax(0, 1fr);
    gap: 4px;
  }

  .dh-label {
    transform: none;
    max-width: none;
  }

  .dh-window-url {
    font-size: 11px;
  }
}
</style>
