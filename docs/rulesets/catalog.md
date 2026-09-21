<script setup lang="ts">
import { data } from './catalog.data'

function imdbLink(id: string) {
  return `https://www.imdb.com/title/${id}/`
}

function tmdbLink(id: string, type: string) {
  return `https://www.themoviedb.org/${type === 'movie' ? 'movie' : 'tv'}/${id}`
}
</script>

# Community-Regelwerk-Katalog

**{{ data.shows.length + data.movies.length }}** Community-Regelwerke: **{{ data.shows.length }}** Serien, **{{ data.movies.length }}** Filme.

## Serien

<table>
<thead>
<tr>
<th>Serie</th>
<th>IMDB ID</th>
<th>TMDB ID</th>
<th style="text-align: right">Anzahl Regeln</th>
<th style="text-align: right">Konfidenz</th>
</tr>
</thead>
<tbody>
<tr v-for="s in data.shows" :key="s.name">
<td>{{ s.name }}</td>
<td><a v-if="s.imdbId" :href="imdbLink(s.imdbId)" target="_blank">{{ s.imdbId }}</a><span v-else>-</span></td>
<td><a v-if="s.tmdbId" :href="tmdbLink(s.tmdbId, s.type)" target="_blank">{{ s.tmdbId }}</a><span v-else>-</span></td>
<td style="text-align: right">{{ s.ruleCount }}</td>
<td style="text-align: right">{{ Math.round(s.confidence * 100) }}%</td>
</tr>
</tbody>
</table>

## Filme

<table>
<thead>
<tr>
<th>Film</th>
<th>IMDB ID</th>
<th>TMDB ID</th>
<th style="text-align: right">Anzahl Regeln</th>
<th style="text-align: right">Konfidenz</th>
</tr>
</thead>
<tbody>
<tr v-for="m in data.movies" :key="m.name">
<td>{{ m.name }}</td>
<td><a v-if="m.imdbId" :href="imdbLink(m.imdbId)" target="_blank">{{ m.imdbId }}</a><span v-else>-</span></td>
<td><a v-if="m.tmdbId" :href="tmdbLink(m.tmdbId, m.type)" target="_blank">{{ m.tmdbId }}</a><span v-else>-</span></td>
<td style="text-align: right">{{ m.ruleCount }}</td>
<td style="text-align: right">{{ Math.round(m.confidence * 100) }}%</td>
</tr>
</tbody>
</table>
