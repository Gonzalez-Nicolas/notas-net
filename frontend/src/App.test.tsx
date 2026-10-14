import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import App from './App'
import { streamOf } from './lib/testUtils'

const URL_OK = 'https://youtu.be/dQw4w9WgXcQ'

async function submit() {
  await userEvent.type(screen.getByLabelText('Link de YouTube'), URL_OK)
  await userEvent.click(screen.getByRole('button', { name: 'Generar notas' }))
}

afterEach(() => vi.unstubAllGlobals())

describe('App', () => {
  it('shows the status and a cancel button while loading', async () => {
    vi.stubGlobal('fetch', vi.fn(() => new Promise(() => {})))
    render(<App />)

    await submit()

    expect(await screen.findByRole('button', { name: 'Cancelar' })).toBeInTheDocument()
    expect(screen.getByRole('status')).toBeInTheDocument()
  })

  it('shows a friendly message on error', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(new Response(streamOf('event: error\ndata: {"code":"no_captions"}\n\n'))),
    )
    render(<App />)

    await submit()

    expect(await screen.findByRole('alert')).toHaveTextContent('no tiene subtítulos')
  })

  it('shows actions and a way to start over when done', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(
        new Response(
          streamOf('event: chunk\ndata: {"text":"# Notas"}\n\n', 'event: done\ndata: {}\n\n'),
        ),
      ),
    )
    render(<App />)

    await submit()

    expect(await screen.findByRole('button', { name: 'Copiar Markdown' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Descargar .md' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Generar otras' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Notas' })).toBeInTheDocument()
  })
})
