import { describe, expect, it } from 'vitest'
import { validateYoutubeUrl } from './validateYoutubeUrl'

describe('validateYoutubeUrl', () => {
  it.each([
    'https://www.youtube.com/watch?v=dQw4w9WgXcQ',
    'https://youtu.be/dQw4w9WgXcQ?si=abc',
    'https://www.youtube.com/shorts/dQw4w9WgXcQ',
    'https://m.youtube.com/watch?v=dQw4w9WgXcQ&t=30s&list=PL1',
  ])('accepts %s', (url) => {
    expect(validateYoutubeUrl(url)).toBe(true)
  })

  it.each(['', 'hola', 'https://vimeo.com/123', 'https://www.youtube.com/watch?v=corto'])(
    'rejects %j',
    (url) => {
      expect(validateYoutubeUrl(url)).toBe(false)
    },
  )
})
