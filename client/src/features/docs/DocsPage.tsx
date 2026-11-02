import { useSearchParams } from 'react-router-dom'
import aiAndRag from '@/features/docs/ai-and-rag.md?raw'
import installation from '@/features/docs/how-to-run-and-use.md?raw'
import workflow from '@/features/docs/workflow.md?raw'
import MarkdownDocument from '@/shared/components/MarkdownDocument'
import PageHeader from '@/shared/components/PageHeader'
import Tabs from '@/shared/components/Tabs'

const tabs = [
  { id: 'installation', label: 'Installation' },
  { id: 'ai-rag', label: 'AI and RAG' },
  { id: 'workflow', label: 'Workflow' },
]

const documents: Record<string, string> = { installation, 'ai-rag': aiAndRag, workflow }

/** The developer docs, open to everyone: how to run the app, the ideas behind its AI, and how the app works step by step. */
export default function DocsPage() {
  // The tab is in the address (?tab=workflow), so a link can open any of them.
  const [params, setParams] = useSearchParams()
  const requested = params.get('tab') ?? ''
  const tab = requested in documents ? requested : 'installation'

  return (
    <main className="mx-auto max-w-6xl px-4 py-6 sm:px-6 sm:py-10">
      <PageHeader
        title="Dev Docs"
        description="How to run the app, the ideas behind its AI and RAG, and how the app works step by step."
      />
      <div className="mt-6 overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
        <Tabs tabs={tabs} active={tab} onChange={(id) => setParams(id === 'installation' ? {} : { tab: id })} />
      </div>
      <div className="mt-6">
        <MarkdownDocument key={tab} markdown={documents[tab]} />
      </div>
    </main>
  )
}
