import Markdown from 'react-markdown'
import remarkGfm from 'remark-gfm'

type Props = {
  markdown: string
  onCopy?: () => void
  onDownload?: () => void
}

const buttonClass =
  'rounded-lg border border-slate-300 px-3 py-1.5 text-sm transition hover:border-indigo-500 hover:text-indigo-600 dark:border-slate-700'

export function NotesViewer({ markdown, onCopy, onDownload }: Props) {
  return (
    <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm sm:p-8 dark:border-slate-800 dark:bg-slate-900">
      {(onCopy || onDownload) && (
        <div className="mb-4 flex flex-wrap gap-2">
          {onCopy && (
            <button type="button" onClick={onCopy} className={buttonClass}>
              Copiar Markdown
            </button>
          )}
          {onDownload && (
            <button type="button" onClick={onDownload} className={buttonClass}>
              Descargar .md
            </button>
          )}
        </div>
      )}
      <article className="prose prose-slate max-w-none dark:prose-invert">
        <Markdown remarkPlugins={[remarkGfm]}>{markdown}</Markdown>
      </article>
    </section>
  )
}
