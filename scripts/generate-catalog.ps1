$rulesetsDir = Join-Path $PSScriptRoot "..\data\community\rulesets"
$outputFile = Join-Path $PSScriptRoot "..\data\community\CATALOG.md"

$shows = @()
$movies = @()

foreach ($file in Get-ChildItem "$rulesetsDir\*.json" | Sort-Object Name) {
    $json = Get-Content $file.FullName -Raw | ConvertFrom-Json
    $name = if ($json.media -and $json.media.name) { $json.media.name } else { $json.topic }
    $type = if ($json.media -and $json.media.type) { $json.media.type } else { "show" }
    $imdb = if ($json.media -and $json.media.imdbId) { "[$($json.media.imdbId)](https://www.imdb.com/title/$($json.media.imdbId)/)" } else { "-" }
    $tmdb = if ($json.media -and $json.media.tmdbId) { "[$($json.media.tmdbId)](https://www.themoviedb.org/$(if ($type -eq 'movie') { 'movie' } else { 'tv' })/$($json.media.tmdbId))" } else { "-" }
    $ruleCount = if ($json.rules) { $json.rules.Count } else { 0 }

    $entry = [PSCustomObject]@{
        Name = $name
        IMDB = $imdb
        TMDB = $tmdb
        Rules = $ruleCount
    }

    if ($type -eq "movie") { $movies += $entry } else { $shows += $entry }
}

$shows = $shows | Sort-Object Name
$movies = $movies | Sort-Object Name

$totalCount = $shows.Count + $movies.Count

$lines = @()
$lines += "# Community Rulesets Catalog"
$lines += ""
$lines += "**$totalCount** community rulesets: **$($shows.Count)** shows, **$($movies.Count)** movies."
$lines += ""
$lines += "## Shows"
$lines += ""
$lines += "| Name | IMDB | TMDB | Rules |"
$lines += "|------|------|------|------:|"
foreach ($s in $shows) {
    $lines += "| $($s.Name) | $($s.IMDB) | $($s.TMDB) | $($s.Rules) |"
}
$lines += ""
$lines += "## Movies"
$lines += ""
$lines += "| Name | IMDB | TMDB | Rules |"
$lines += "|------|------|------|------:|"
foreach ($m in $movies) {
    $lines += "| $($m.Name) | $($m.IMDB) | $($m.TMDB) | $($m.Rules) |"
}
$lines += ""

$lines -join "`n" | Set-Content -Path $outputFile -NoNewline -Encoding UTF8
Write-Host "Generated $outputFile with $totalCount rulesets ($($shows.Count) shows, $($movies.Count) movies)"
