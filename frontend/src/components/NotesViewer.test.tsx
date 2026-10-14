import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { NotesViewer } from './NotesViewer'

describe('NotesViewer', () => {
  it('renders markdown headings and GFM tables', () => {
    render(<NotesViewer markdown={'# Título\n\n| a | b |\n|---|---|\n| 1 | 2 |'} />)

    expect(screen.getByRole('heading', { level: 1, name: 'Título' })).toBeInTheDocument()
    expect(screen.getByRole('table')).toBeInTheDocument()
  })

  it('shows copy and download actions only when handlers are provided', async () => {
    const onCopy = vi.fn()
    const { rerender } = render(<NotesViewer markdown="x" />)
    expect(screen.queryByRole('button', { name: 'Copiar Markdown' })).toBeNull()

    rerender(<NotesViewer markdown="x" onCopy={onCopy} onDownload={vi.fn()} />)
    await userEvent.click(screen.getByRole('button', { name: 'Copiar Markdown' }))

    expect(onCopy).toHaveBeenCalled()
    expect(screen.getByRole('button', { name: 'Descargar .md' })).toBeInTheDocument()
  })
})
