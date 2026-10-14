import { errorMessage } from '../lib/errors'

export function ErrorBanner({ code }: { code?: string }) {
  return (
    <div
      role="alert"
      className="rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-red-800 dark:border-red-900 dark:bg-red-950 dark:text-red-200"
    >
      {errorMessage(code)}
    </div>
  )
}
