import { useState, type FormEvent } from 'react'
import { validateYoutubeUrl } from '../lib/validateYoutubeUrl'

type Props = {
  onSubmit: (url: string) => void
  examples: { label: string; url: string }[]
  disabled?: boolean
}

export function UrlForm({ onSubmit, examples, disabled }: Props) {
  const [value, setValue] = useState('')
  const [invalid, setInvalid] = useState(false)

  const submit = (e: FormEvent) => {
    e.preventDefault()
    const url = value.trim()
    if (!validateYoutubeUrl(url)) return setInvalid(true)
    setInvalid(false)
    onSubmit(url)
  }

  return (
    <div className="space-y-4">
      <form onSubmit={submit} className="flex flex-col gap-3 sm:flex-row" noValidate>
        <input
          aria-label="Link de YouTube"
          type="url"
          value={value}
          disabled={disabled}
          onChange={(e) => setValue(e.target.value)}
          placeholder="https://www.youtube.com/watch?v=…"
          className="min-w-0 flex-1 rounded-xl border border-slate-300 bg-white px-4 py-3 text-base shadow-sm outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-500/30 dark:border-slate-700 dark:bg-slate-900"
        />
        <button
          type="submit"
          disabled={disabled}
          className="rounded-xl bg-indigo-600 px-6 py-3 font-medium text-white shadow-sm transition hover:bg-indigo-500 disabled:opacity-50"
        >
          Generar notas
        </button>
      </form>
      {invalid && (
        <p role="alert" className="text-sm text-red-600 dark:text-red-400">
          Ingresá un link de YouTube válido.
        </p>
      )}
      <div className="flex flex-wrap items-center gap-2 text-sm">
        <span className="text-slate-500">Probá con:</span>
        {examples.map((ex) => (
          <button
            key={ex.url}
            type="button"
            disabled={disabled}
            onClick={() => onSubmit(ex.url)}
            className="rounded-full border border-slate-300 px-3 py-1 text-slate-700 transition hover:border-indigo-500 hover:text-indigo-600 disabled:opacity-50 dark:border-slate-700 dark:text-slate-300"
          >
            {ex.label}
          </button>
        ))}
      </div>
    </div>
  )
}
