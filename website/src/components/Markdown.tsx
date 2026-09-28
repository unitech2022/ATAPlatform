import ReactMarkdown from 'react-markdown'
import rehypeSanitize from 'rehype-sanitize'

/** Article body: Markdown → sanitized HTML (no raw HTML, safe links). */
export function Markdown({ children }: { children: string }) {
  return (
    <div className="prose-ata">
      <ReactMarkdown
        rehypePlugins={[rehypeSanitize]}
        components={{
          a: ({ href, children: label }) => {
            const external = Boolean(href && /^https?:\/\//i.test(href))
            return (
              <a href={href} target={external ? '_blank' : undefined} rel={external ? 'noopener noreferrer' : undefined}>
                {label}
              </a>
            )
          },
        }}
      >
        {children}
      </ReactMarkdown>
    </div>
  )
}
