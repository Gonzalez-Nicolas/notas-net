import { useState } from 'react'
import { ErrorBanner } from './components/ErrorBanner'
import { NotesViewer } from './components/NotesViewer'
import { StatusIndicator } from './components/StatusIndicator'
import { UrlForm } from './components/UrlForm'
import { EXAMPLES } from './examples'
import { useNotesStream } from './hooks/useNotesStream'
import { extractVideoId } from './lib/validateYoutubeUrl'

export default function App() {
  const { state, status, markdown, errorCode, start, cancel, reset } = useNotesStream()
  const [videoId, setVideoId] = useState<string | null>(null)

  const generate = (url: string) => {
    setVideoId(extractVideoId(url))
    start(url)
  }

  const copy = () => void navigator.clipboard?.writeText(markdown)

  const download = () => {
    const blob = new Blob([markdown], { type: 'text/markdown;charset=utf-8' })
    const link = document.createElement('a')
    link.href = URL.createObjectURL(blob)
    link.download = `notas-${videoId ?? 'video'}.md`
    link.click()
    URL.revokeObjectURL(link.href)
  }

  const showForm = state === 'idle' || state === 'error'

  return (
    <div className="min-h-screen bg-slate-50 text-slate-900 dark:bg-slate-950 dark:text-slate-100">
      <main className="mx-auto max-w-3xl space-y-6 px-4 py-10 sm:py-16">
        <header className="space-y-2 text-center">
          <h1 className="text-3xl font-bold tracking-tight sm:text-4xl">Notas Net</h1>
          <p className="text-slate-600 dark:text-slate-400">
            Pegá el link de un video de YouTube y recibí notas de estudio en Markdown.
          </p>
        </header>

        {showForm && <UrlForm onSubmit={generate} examples={EXAMPLES} />}
        {state === 'error' && <ErrorBanner code={errorCode} />}
        {state === 'loading' && <StatusIndicator message={status} onCancel={cancel} />}

        {videoId && (state === 'loading' || state === 'done') && (
          <img
            src={`https://img.youtube.com/vi/${videoId}/hqdefault.jpg`}
            alt="Miniatura del video"
            className="mx-auto w-full max-w-sm rounded-xl shadow"
          />
        )}

        {markdown && (
          <NotesViewer
            markdown={markdown}
            onCopy={state === 'done' ? copy : undefined}
            onDownload={state === 'done' ? download : undefined}
          />
        )}

        {state === 'done' && (
          <div className="text-center">
            <button
              type="button"
              onClick={reset}
              className="rounded-xl bg-indigo-600 px-6 py-3 font-medium text-white hover:bg-indigo-500"
            >
              Generar otras
            </button>
          </div>
        )}
      </main>
    </div>
  )
}
