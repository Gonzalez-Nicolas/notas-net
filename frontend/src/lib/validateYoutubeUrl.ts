const HOSTS = new Set(['youtube.com', 'www.youtube.com', 'm.youtube.com'])
const VIDEO_ID = /^[A-Za-z0-9_-]{11}$/

export function validateYoutubeUrl(input: string): boolean {
  return extractVideoId(input) !== null
}

export function extractVideoId(input: string): string | null {
  let url: URL
  try {
    url = new URL(input.trim())
  } catch {
    return null
  }
  if (url.protocol !== 'https:' && url.protocol !== 'http:') return null

  const host = url.hostname.toLowerCase()
  const segments = url.pathname.split('/').filter(Boolean)
  let candidate: string | null = null

  if (host === 'youtu.be') candidate = segments[0] ?? null
  else if (HOSTS.has(host)) {
    if (segments.length === 2 && segments[0] === 'shorts') candidate = segments[1]
    else if (segments.length === 1 && segments[0] === 'watch') candidate = url.searchParams.get('v')
  }

  return candidate && VIDEO_ID.test(candidate) ? candidate : null
}
