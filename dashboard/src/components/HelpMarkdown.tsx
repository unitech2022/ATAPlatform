import ReactMarkdown from 'react-markdown'
import rehypeSanitize from 'rehype-sanitize'

/**
 * Help-article Markdown → sanitized HTML: same approach as the website (`react-markdown` + `rehype-sanitize`, no raw HTML,
 * links restricted by the default schema; external links open in a new tab without referrer/opener access).
 */
export function HelpMarkdown({ children, dir }: { children: string; dir?: 'rtl' | 'ltr' }) {
  return (
    <div className="prose-ata" dir={dir}>
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
