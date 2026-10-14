type Props = { message: string; onCancel: () => void }

export function StatusIndicator({ message, onCancel }: Props) {
  return (
    <div className="flex items-center justify-between gap-3 rounded-xl bg-indigo-50 px-4 py-3 text-indigo-900 dark:bg-indigo-950 dark:text-indigo-100">
      <p role="status" className="flex items-center gap-2">
        <span className="h-2 w-2 animate-pulse rounded-full bg-indigo-500" aria-hidden />
        {message}
      </p>
      <button type="button" onClick={onCancel} className="text-sm underline underline-offset-2">
        Cancelar
      </button>
    </div>
  )
}
