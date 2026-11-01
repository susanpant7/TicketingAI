import type { Setting } from '@/features/settings/settingsApi'

const value = (settings: Setting[], key: string) => settings.find((s) => s.key === key)?.value ?? ''

/** A box above the chat settings: when to use a model on our own machine and when to use an external API. */
export default function ChatModelGuide({ settings }: { settings: Setting[] }) {
  const provider = value(settings, 'chat.provider')
  const model = value(settings, 'chat.model')

  return (
    <div className="mb-4 rounded-2xl border border-indigo-200 bg-indigo-50/60 p-6">
      <h3 className="text-base font-semibold text-slate-900">Which chat model should we use?</h3>
      {provider && (
        <p className="mt-1 text-sm text-slate-600">
          Now: provider <code className="rounded bg-white px-1.5 py-0.5 font-medium">{provider}</code>, model{' '}
          <code className="rounded bg-white px-1.5 py-0.5 font-medium">{model}</code>
        </p>
      )}

      <div className="mt-4 grid gap-4 md:grid-cols-2">
        <section className="rounded-xl bg-white p-4 ring-1 ring-slate-200">
          <h4 className="text-sm font-semibold text-slate-900">On our own machine</h4>
          <p className="text-xs text-slate-500">
            Chat provider: <code>ollama</code>
          </p>
          <p className="mt-3 text-sm font-medium text-slate-700">Use it when</p>
          <ul className="mt-1 list-disc space-y-1 pl-5 text-sm text-slate-600">
            <li>the questions and the knowledge must stay on our own server</li>
            <li>we have no API account, or the free limits are not enough</li>
            <li>the machine has 8 GB of memory or more</li>
          </ul>
          <p className="mt-3 text-sm font-medium text-slate-700">Chat model</p>
          <ul className="mt-1 list-disc space-y-1 pl-5 text-sm text-slate-600">
            <li>
              <code>qwen2.5:7b</code>: the default, and the one the prompt was tested on
            </li>
            <li>
              <code>qwen2.5:3b</code>: faster and smaller, but the answers must be tested again
            </li>
          </ul>
          <p className="mt-3 text-sm font-medium text-slate-700">Chat API base URL</p>
          <ul className="mt-1 list-disc space-y-1 pl-5 text-sm text-slate-600">
            <li>
              <code>http://localhost:11434</code> when the app runs on our machine
            </li>
            <li>
              <code>http://ollama:11434</code> with Docker Compose
            </li>
          </ul>
          <p className="mt-3 text-sm text-slate-600">
            Needs Ollama running with the model pulled. Replies take 15 to 40 seconds on a CPU without a GPU.
          </p>
        </section>

        <section className="rounded-xl bg-white p-4 ring-1 ring-slate-200">
          <h4 className="text-sm font-semibold text-slate-900">An external API</h4>
          <p className="text-xs text-slate-500">
            Chat provider: <code>api</code>
          </p>
          <p className="mt-3 text-sm font-medium text-slate-700">Use it when</p>
          <ul className="mt-1 list-disc space-y-1 pl-5 text-sm text-slate-600">
            <li>we want fast replies (a few seconds)</li>
            <li>the server is small or has no GPU, so it cannot run a 7B model</li>
            <li>the data is not sensitive, because the questions and knowledge are sent to the service</li>
          </ul>
          <p className="mt-3 text-sm font-medium text-slate-700">Chat API base URL</p>
          <ul className="mt-1 list-disc space-y-1 pl-5 text-sm text-slate-600">
            <li>
              Gemini: <code className="break-all">https://generativelanguage.googleapis.com/v1beta/openai</code>
            </li>
            <li>
              Cloudflare Workers AI: <code className="break-all">https://api.cloudflare.com/client/v4/accounts/&lt;ACCOUNT_ID&gt;/ai/v1</code>
            </li>
          </ul>
          <p className="mt-3 text-sm font-medium text-slate-700">Chat model</p>
          <ul className="mt-1 list-disc space-y-1 pl-5 text-sm text-slate-600">
            <li>
              Gemini: <code>gemini-3.8-flash</code>
            </li>
            <li>
              Cloudflare Workers AI: <code>@cf/meta/llama-3.1-8b-instruct</code>
            </li>
          </ul>
          <p className="mt-2 text-xs text-slate-500">
            Model names change. A retired name gives a 404 "model not found", so check the current list in the service's
            dashboard (Google AI Studio for Gemini). The old Gemini name <code>gemini-2.5-flash</code> no longer works for new
            accounts.
          </p>
          <p className="mt-3 text-sm text-slate-600">
            Needs the key in the server's <code>.env</code> file as <code>ChatApi__ApiKey</code>. Free plans have limits.
          </p>
        </section>
      </div>

      <p className="mt-4 rounded-lg bg-amber-50 px-3 py-2 text-sm text-amber-800 ring-1 ring-inset ring-amber-200">
        Change <strong>Chat provider</strong>, <strong>Chat API base URL</strong> and <strong>Chat model</strong> together. The
        address and the model name must belong to the chosen provider: with <code>api</code>, a name like <code>qwen2.5:7b</code> gives a
        "404 model not found" error, and with <code>ollama</code> a Gemini name fails the same way. Embeddings always use
        Ollama (see the Ollama address for embeddings below), so the uploaded knowledge stays valid.
      </p>
    </div>
  )
}
