import guide from '@/features/docs/technical-guide.md?raw'
import MarkdownDocument from '@/shared/components/MarkdownDocument'

export default function DocsPage() {
  return (
    <MarkdownDocument
      markdown={guide}
      description="How the assistant works: knowledge, retrieval, the prompt and the model's reply."
    />
  )
}
