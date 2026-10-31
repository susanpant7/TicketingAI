import guide from '@/features/docs/how-to-run-and-use.md?raw'
import MarkdownDocument from '@/shared/components/MarkdownDocument'

export default function GuidePage() {
  return <MarkdownDocument markdown={guide} description="How to run the app, set it up, and use it." />
}
