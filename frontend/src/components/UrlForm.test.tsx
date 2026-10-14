import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { UrlForm } from './UrlForm'

const examples = [{ label: 'Ejemplo uno', url: 'https://youtu.be/dQw4w9WgXcQ' }]

describe('UrlForm', () => {
  it('does not submit an invalid link and shows a message', async () => {
    const onSubmit = vi.fn()
    render(<UrlForm onSubmit={onSubmit} examples={examples} />)

    await userEvent.type(screen.getByLabelText('Link de YouTube'), 'hola')
    await userEvent.click(screen.getByRole('button', { name: 'Generar notas' }))

    expect(onSubmit).not.toHaveBeenCalled()
    expect(screen.getByRole('alert')).toHaveTextContent('link de YouTube válido')
  })

  it('submits a valid link', async () => {
    const onSubmit = vi.fn()
    render(<UrlForm onSubmit={onSubmit} examples={examples} />)

    await userEvent.type(screen.getByLabelText('Link de YouTube'), 'https://youtu.be/dQw4w9WgXcQ')
    await userEvent.click(screen.getByRole('button', { name: 'Generar notas' }))

    expect(onSubmit).toHaveBeenCalledWith('https://youtu.be/dQw4w9WgXcQ')
  })

  it('clicking an example submits its url', async () => {
    const onSubmit = vi.fn()
    render(<UrlForm onSubmit={onSubmit} examples={examples} />)

    await userEvent.click(screen.getByRole('button', { name: 'Ejemplo uno' }))

    expect(onSubmit).toHaveBeenCalledWith('https://youtu.be/dQw4w9WgXcQ')
  })
})
