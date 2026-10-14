import { describe, expect, it } from 'vitest'
import { parseSse } from './parseSse'
import { streamOf } from './testUtils'

async function collect(stream: ReadableStream<Uint8Array>) {
  const out: { event: string; data: unknown }[] = []
  for await (const e of parseSse(stream)) out.push(e)
  return out
}

describe('parseSse', () => {
  it('parses complete events', async () => {
    const events = await collect(
      streamOf('event: status\ndata: {"message":"hola"}\n\nevent: done\ndata: {}\n\n'),
    )
    expect(events).toEqual([
      { event: 'status', data: { message: 'hola' } },
      { event: 'done', data: {} },
    ])
  })

  it('reassembles an event split across reads', async () => {
    const events = await collect(streamOf('event: chunk\nda', 'ta: {"text":"ab', 'c"}\n\n'))
    expect(events).toEqual([{ event: 'chunk', data: { text: 'abc' } }])
  })
})
