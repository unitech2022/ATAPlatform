import 'package:ata_app/features/support/domain/markdown/markdown_nodes.dart';

/// A small, safe Markdown reader for help articles: headings, paragraphs,
/// bullet / numbered lists, quotes, code blocks, rules, **bold**, *italic*,
/// `code` and `[links](https://…)`.
///
/// Raw HTML is never interpreted: `<script>` / `<style>` blocks are dropped
/// and every other tag is stripped, images are reduced to their alt text and
/// only `http`, `https`, `mailto` and `tel` links stay clickable.
abstract final class MarkdownParser {
  static final RegExp _scriptBlocks = RegExp(
    r'<(script|style|iframe|object|embed)\b[^>]*>[\s\S]*?</\1\s*>',
    caseSensitive: false,
  );
  static final RegExp _tags = RegExp(r'</?[a-zA-Z!][^>]*>');
  static final RegExp _heading = RegExp(r'^(#{1,6})\s+(.*?)\s*#*\s*$');
  static final RegExp _bullet = RegExp(r'^(\s*)[-*+]\s+(.*)$');
  static final RegExp _numbered = RegExp(r'^(\s*)(\d{1,3})[.)]\s+(.*)$');
  static final RegExp _rule = RegExp(r'^\s*([-*_])(\s*\1){2,}\s*$');
  static final RegExp _inline = RegExp(
    r'(!?)\[([^\]]*)\]\(([^)\s]*)(?:\s+"[^"]*")?\)'
    r'|\*\*(.+?)\*\*|__(.+?)__|`([^`]+)`|\*([^*\s][^*]*?)\*'
    r'|(?<![\w])_([^_\s][^_]*?)_(?![\w])',
  );
  static const Set<String> _safeSchemes = <String>{
    'http',
    'https',
    'mailto',
    'tel',
  };

  static List<MdBlock> parse(String source) {
    final List<String> lines = _sanitize(source).split('\n');
    final List<MdBlock> blocks = <MdBlock>[];
    final List<String> paragraph = <String>[];
    void flush() {
      if (paragraph.isEmpty) return;
      blocks.add(MdParagraph(inlines(paragraph.join(' '))));
      paragraph.clear();
    }

    for (int i = 0; i < lines.length; i++) {
      final String line = lines[i];
      final String trimmed = line.trim();
      if (trimmed.startsWith('```')) {
        flush();
        final List<String> code = <String>[];
        i++;
        while (i < lines.length && !lines[i].trim().startsWith('```')) {
          code.add(lines[i]);
          i++;
        }
        blocks.add(MdCode(code.join('\n')));
      } else if (trimmed.isEmpty) {
        flush();
      } else if (_rule.hasMatch(line)) {
        flush();
        blocks.add(const MdRule());
      } else if (_heading.firstMatch(trimmed) case final RegExpMatch m?) {
        flush();
        blocks.add(
          MdHeading(m.group(1)!.length.clamp(1, 3), inlines(m.group(2)!)),
        );
      } else if (trimmed.startsWith('>')) {
        flush();
        blocks.add(
          MdQuote(inlines(trimmed.replaceFirst(RegExp(r'^>+\s?'), ''))),
        );
      } else if (_bullet.firstMatch(line) case final RegExpMatch m?) {
        flush();
        blocks.add(
          MdListItem(inlines(m.group(2)!), depth: _depth(m.group(1)!)),
        );
      } else if (_numbered.firstMatch(line) case final RegExpMatch m?) {
        flush();
        blocks.add(
          MdListItem(
            inlines(m.group(3)!),
            number: int.parse(m.group(2)!),
            depth: _depth(m.group(1)!),
          ),
        );
      } else {
        paragraph.add(trimmed);
      }
    }
    flush();
    return blocks;
  }

  /// Inline spans of [text] (bold, italic, code, links).
  static List<MdSpan> inlines(
    String text, {
    bool bold = false,
    bool italic = false,
  }) {
    final List<MdSpan> spans = <MdSpan>[];
    int cursor = 0;
    for (final RegExpMatch m in _inline.allMatches(text)) {
      if (m.start > cursor) {
        spans.add(
          MdSpan(text.substring(cursor, m.start), bold: bold, italic: italic),
        );
      }
      cursor = m.end;
      if (m.group(2) != null) {
        _link(spans, m, bold: bold, italic: italic);
      } else if (m.group(4) ?? m.group(5) case final String inner?) {
        spans.addAll(inlines(inner, bold: true, italic: italic));
      } else if (m.group(6) case final String code?) {
        spans.add(MdSpan(code, code: true));
      } else if (m.group(7) ?? m.group(8) case final String inner?) {
        spans.addAll(inlines(inner, bold: bold, italic: true));
      }
    }
    if (cursor < text.length) {
      spans.add(MdSpan(text.substring(cursor), bold: bold, italic: italic));
    }
    return spans;
  }

  static void _link(
    List<MdSpan> spans,
    RegExpMatch m, {
    required bool bold,
    required bool italic,
  }) {
    final bool image = m.group(1) == '!';
    final String label = m.group(2)!;
    final String? url = image ? null : _safeUrl(m.group(3)!);
    final String shown = label.isEmpty && url != null ? url : label;
    if (shown.isEmpty) return;
    spans.add(MdSpan(shown, bold: bold, italic: italic, url: url));
  }

  static String? _safeUrl(String raw) {
    final Uri? uri = Uri.tryParse(raw.trim());
    if (uri == null || !_safeSchemes.contains(uri.scheme.toLowerCase())) {
      return null;
    }
    if (uri.hasScheme &&
        (uri.scheme == 'http' || uri.scheme == 'https') &&
        uri.host.isEmpty) {
      return null;
    }
    return uri.toString();
  }

  static int _depth(String indent) =>
      (indent.replaceAll('\t', '    ').length ~/ 2).clamp(0, 3);

  static String _sanitize(String source) => source
      .replaceAll('\r\n', '\n')
      .replaceAll(_scriptBlocks, '')
      .replaceAll(_tags, '');
}
