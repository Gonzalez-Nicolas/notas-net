import { useCallback, useEffect, useRef, useState } from 'react'
import { parseSse } from '../lib/parseSse'

export type StreamState = 'idle' | 'loading' | 'done' | 'error'

const API_URL = import.meta.env.VITE_API_URL ?? ''

export function useNotesStream() {
  const [state, setState] = useState<StreamState>('idle')
  const [status, setStatus] = useState('')
  const [markdown, setMarkdown] = useState('')
  const [errorCode, setErrorCode] = useState<string | undefined>()
  const abortRef = useRef<AbortController | null>(null)

  const cancel = useCallback(() => {
    abortRef.current?.abort()
    abortRef.current = null
    setState('idle')
    setStatus('')
  }, [])

  const reset = useCallback(() => {
    cancel()
    setMarkdown('')
    setErrorCode(undefined)
  }, [cancel])

  const start = useCallback((url: string) => {
    abortRef.current?.abort()
    const controller = new AbortController()
    abortRef.current = controller
    setState('loading')
    setStatus('Iniciando…')
    setMarkdown('')
    setErrorCode(undefined)

    const fail = (code: string) => {
      setErrorCode(code)
      setState('error')
    }

    void (async () => {
      try {
        const response = await fetch(`${API_URL}/api/notes`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ url }),
          signal: controller.signal,
        })
        if (response.status === 429) return fail('rate_limited')
        if (!response.ok || !response.body) return fail('network')

        let finished = false
        for await (const e of parseSse(response.body)) {
          if (e.event === 'status') setStatus(e.data.message)
          else if (e.event === 'chunk') setMarkdown((m) => m + e.data.text)
          else if (e.event === 'error') {
            finished = true
            return fail(e.data.code)
          } else if (e.event === 'done') {
            finished = true
            setState('done')
          }
        }
        if (!finished) fail('network')
      } catch {
        if (controller.signal.aborted) return
        fail('network')
      }
    })()
  }, [])

  useEffect(() => () => abortRef.current?.abort(), [])

  return { state, status, markdown, errorCode, start, cancel, reset }
}
