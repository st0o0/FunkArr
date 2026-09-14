<script setup lang="ts">
import { data } from './catalog.data'

function imdbLink(id: string) {
  return `https://www.imdb.com/title/${id}/`
}

function tmdbLink(id: string, type: string) {
  return `https://www.themoviedb.org/${type === 'movie' ? 'movie' : 'tv'}/${id}`
}
</script>

# Community Rulesets Catalog

**{{ data.shows.length + data.movies.length }}** community rulesets: **{{ data.shows.length }}** shows, **{{ data.movies.length }}** movies.

## Shows

| Name | IMDB | TMDB | Rules |
|------|------|------|------:|
<tr v-for="s in data.shows" :key="s.name">
<td>{{ s.name }}</td>
<td><a v-if="s.imdbId" :href="imdbLink(s.imdbId)" target="_blank">{{ s.imdbId }}</a><span v-else>-</span></td>
<td><a v-if="s.tmdbId" :href="tmdbLink(s.tmdbId, s.type)" target="_blank">{{ s.tmdbId }}</a><span v-else>-</span></td>
<td style="text-align: right">{{ s.ruleCount }}</td>
</tr>

## Movies

| Name | IMDB | TMDB | Rules |
|------|------|------|------:|
<tr v-for="m in data.movies" :key="m.name">
<td>{{ m.name }}</td>
<td><a v-if="m.imdbId" :href="imdbLink(m.imdbId)" target="_blank">{{ m.imdbId }}</a><span v-else>-</span></td>
<td><a v-if="m.tmdbId" :href="tmdbLink(m.tmdbId, m.type)" target="_blank">{{ m.tmdbId }}</a><span v-else>-</span></td>
<td style="text-align: right">{{ m.ruleCount }}</td>
</tr>
