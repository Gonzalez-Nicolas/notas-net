const MESSAGES: Record<string, string> = {
  invalid_url: 'Ese link no parece ser un video de YouTube válido.',
  no_captions: 'Este video no tiene subtítulos disponibles, así que no puedo generar notas.',
  video_unavailable: 'No pude acceder al video. Puede ser privado o no existir.',
  rate_limited: 'Se alcanzó el límite de uso. Probá de nuevo en unos minutos.',
  llm_failure: 'Falló la generación de notas. Probá de nuevo en unos segundos.',
}

export function errorMessage(code: string | undefined): string {
  return (code && MESSAGES[code]) || 'Ocurrió un error inesperado. Probá de nuevo.'
}
