import type { ReactNode } from 'react'
import Markdown from 'react-markdown'
import remarkGfm from 'remark-gfm'
import { Link } from 'react-router-dom'
import PageHeader from '@/shared/components/PageHeader'

// Same ids as GitHub gives headings, so the links inside a document ("#8-workflow-chat-rag") work.
const slugify = (text: string) =>
  text
    .toLowerCase()
    .replace(/[^\w\s-]/g, '')
    .trim()
    .replace(/\s/g, '-')

// The text of a heading: its children can be strings or elements such as inline code.
function textOf(node: ReactNode): string {
  if (typeof node === 'string' || typeof node === 'number') return String(node)
  if (Array.isArray(node)) return node.map(textOf).join('')
  if (node && typeof node === 'object' && 'props' in node) return textOf((node.props as { children?: ReactNode }).children)
  return ''
}

// The page title is the first "# " line. The side menu lists the "## " lines, skipping the ones inside code blocks.
function parseDocument(markdown: string) {
  let inCode = false
  let title = ''
  const sections: { id: string; title: string }[] = []

  for (const line of markdown.split('\n')) {
    if (line.startsWith('```')) inCode = !inCode
    if (inCode) continue
    if (line.startsWith('# ') && !title) title = line.slice(2).trim()
    if (line.startsWith('## ')) sections.push({ id: slugify(line.slice(3)), title: line.slice(3).trim() })
  }

  return { title, sections, body: markdown.replace(/^# .*\n/, '') }
}

interface MarkdownDocumentProps {
  /** The Markdown text. Its first "# " line is the page title and its "## " lines are the side menu. */
  markdown: string
  description: string
}

/** A page that shows a Markdown document, with a side menu of its sections. */
export default function MarkdownDocument({ markdown, description }: MarkdownDocumentProps) {
  const { title, sections, body } = parseDocument(markdown)

  return (
    <main className="mx-auto max-w-6xl px-6 py-10">
      <PageHeader title={title} description={description} />

      <div className="mt-8 grid gap-8 lg:grid-cols-[14rem_minmax(0,1fr)]">
        <nav aria-label="Sections" className="hidden lg:block">
          <ul className="sticky top-24 space-y-1 border-l border-slate-200 text-sm">
            {sections.map((section) => (
              <li key={section.id}>
                <a
                  href={`#${section.id}`}
                  className="-ml-px block border-l border-transparent py-1 pl-4 text-slate-600 hover:border-indigo-600 hover:text-indigo-700"
                >
                  {section.title}
                </a>
              </li>
            ))}
          </ul>
        </nav>

        <article className="min-w-0 rounded-2xl border border-slate-200 bg-white p-6 shadow-sm sm:p-10">
          <div className="prose prose-slate max-w-none prose-headings:scroll-mt-24 prose-img:rounded-xl prose-img:border prose-img:border-slate-200 prose-img:shadow-sm prose-pre:bg-slate-900 prose-pre:text-slate-100 prose-code:before:content-none prose-code:after:content-none [&_:not(pre)>code]:rounded [&_:not(pre)>code]:bg-slate-100 [&_:not(pre)>code]:px-1 [&_:not(pre)>code]:py-0.5 [&_:not(pre)>code]:font-medium">
            <Markdown
              remarkPlugins={[remarkGfm]}
              components={{
                // Links to other sites open in a new tab, links to other pages of the app use the router,
                // and links inside the page (#section) stay as they are.
                a: ({ href, children }) =>
                  href?.startsWith('http') ? (
                    <a href={href} target="_blank" rel="noopener noreferrer">
                      {children}
                    </a>
                  ) : href?.startsWith('/') ? (
                    <Link to={href}>{children}</Link>
                  ) : (
                    <a href={href}>{children}</a>
                  ),
                // A click opens a screenshot at full size in a new tab.
                img: ({ src, alt }) => (
                  <a href={src} target="_blank" rel="noopener noreferrer">
                    <img src={src} alt={alt} loading="lazy" />
                  </a>
                ),
                h2: ({ children }) => <h2 id={slugify(textOf(children))}>{children}</h2>,
                h3: ({ children }) => <h3 id={slugify(textOf(children))}>{children}</h3>,
                table: ({ children }) => (
                  <div className="overflow-x-auto">
                    <table>{children}</table>
                  </div>
                ),
              }}
            >
              {body}
            </Markdown>
          </div>
        </article>
      </div>
    </main>
  )
}
