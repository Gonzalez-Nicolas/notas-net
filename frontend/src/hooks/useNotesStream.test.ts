import { act, renderHook, waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { streamOf } from '../lib/testUtils'
import { useNotesStream } from './useNotesStream'

const URL_OK = 'https://youtu.be/dQw4w9WgXcQ'

function mockFetch(body: ReadableStream<Uint8Array>, status = 200) {
  const fn = vi.fn().mockResolvedValue(new Response(body, { status }))
  vi.stubGlobal('fetch', fn)
  return fn
}

afterEach(() => vi.unstubAllGlobals())

describe('useNotesStream', () => {
  it('accumulates chunks and ends in done', async () => {
    mockFetch(
      streamOf(
        'event: status\ndata: {"message":"Generando notas…"}\n\n',
        'event: chunk\ndata: {"text":"# Hola"}\n\n',
        'event: chunk\ndata: {"text":" mundo"}\n\n',
        'event: done\ndata: {}\n\n',
      ),
    )
    const { result } = renderHook(() => useNotesStream())

    act(() => result.current.start(URL_OK))

    await waitFor(() => expect(result.current.state).toBe('done'))
    expect(result.current.markdown).toBe('# Hola mundo')
  })

  it('maps an error event to error state with its code', async () => {
    mockFetch(streamOf('event: error\ndata: {"code":"no_captions"}\n\n'))
    const { result } = renderHook(() => useNotesStream())

    act(() => result.current.start(URL_OK))

    await waitFor(() => expect(result.current.state).toBe('error'))
    expect(result.current.errorCode).toBe('no_captions')
  })

  it('maps an HTTP 429 to rate_limited', async () => {
    mockFetch(streamOf(''), 429)
    const { result } = renderHook(() => useNotesStream())

    act(() => result.current.start(URL_OK))

    await waitFor(() => expect(result.current.state).toBe('error'))
    expect(result.current.errorCode).toBe('rate_limited')
  })

  it('cancel aborts the request and returns to idle', async () => {
    let signal: AbortSignal | undefined
    vi.stubGlobal(
      'fetch',
      vi.fn((_url: string, init: RequestInit) => {
        signal = init.signal as AbortSignal
        return new Promise((_res, reject) =>
          signal!.addEventListener('abort', () => reject(new DOMException('Aborted', 'AbortError'))),
        )
      }),
    )
    const { result } = renderHook(() => useNotesStream())

    act(() => result.current.start(URL_OK))
    await waitFor(() => expect(result.current.state).toBe('loading'))
    act(() => result.current.cancel())

    await waitFor(() => expect(result.current.state).toBe('idle'))
    expect(signal?.aborted).toBe(true)
  })
})
