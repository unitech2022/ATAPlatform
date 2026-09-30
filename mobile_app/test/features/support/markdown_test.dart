import 'package:ata_app/features/support/domain/markdown/markdown_nodes.dart';
import 'package:ata_app/features/support/domain/markdown/markdown_parser.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('MarkdownParser blocks', () {
    test('headings, paragraphs and lists', () {
      final List<MdBlock> blocks = MarkdownParser.parse(
        '# Title\n\nsome\ntext\n\n- one\n- two\n\n1. first\n2. second\n',
      );
      expect(blocks[0], const MdHeading(1, <MdSpan>[MdSpan('Title')]));
      expect(blocks[1], const MdParagraph(<MdSpan>[MdSpan('some text')]));
      expect(blocks[2], const MdListItem(<MdSpan>[MdSpan('one')]));
      expect(blocks[3], const MdListItem(<MdSpan>[MdSpan('two')]));
      expect(blocks[4], const MdListItem(<MdSpan>[MdSpan('first')], number: 1));
      expect(
        blocks[5],
        const MdListItem(<MdSpan>[MdSpan('second')], number: 2),
      );
    });

    test('quotes, rules and fenced code', () {
      final List<MdBlock> blocks = MarkdownParser.parse(
        '> note\n\n---\n\n```\nline 1\nline 2\n```\n',
      );
      expect(blocks[0], const MdQuote(<MdSpan>[MdSpan('note')]));
      expect(blocks[1], const MdRule());
      expect(blocks[2], const MdCode('line 1\nline 2'));
    });

    test('deep headings are drawn at level 3 and Arabic text is kept', () {
      final List<MdBlock> blocks = MarkdownParser.parse(
        '###### كيف أجدول رحلة؟',
      );
      expect((blocks.single as MdHeading).level, 3);
      expect((blocks.single as MdHeading).spans.single.text, 'كيف أجدول رحلة؟');
    });
  });

  group('MarkdownParser inlines', () {
    test('bold, italic and code', () {
      final List<MdSpan> spans = MarkdownParser.inlines(
        'a **bold** and *soft* and `code`',
      );
      expect(spans, const <MdSpan>[
        MdSpan('a '),
        MdSpan('bold', bold: true),
        MdSpan(' and '),
        MdSpan('soft', italic: true),
        MdSpan(' and '),
        MdSpan('code', code: true),
      ]);
    });

    test('safe links keep their url', () {
      final List<MdSpan> spans = MarkdownParser.inlines(
        'see [ATA](https://ata.sa/help) or [mail](mailto:a@b.sa)',
      );
      expect(spans[1].url, 'https://ata.sa/help');
      expect(spans[1].text, 'ATA');
      expect(spans[3].url, 'mailto:a@b.sa');
    });

    test('unsafe links become plain text and images only their alt', () {
      final List<MdSpan> spans = MarkdownParser.inlines(
        '[x](javascript:alert(1)) ![logo](https://x/y.png) [d](data:text/html;base64,AAA)',
      );
      expect(spans.every((MdSpan s) => s.url == null), isTrue);
      expect(spans.map((MdSpan s) => s.text).join(), contains('logo'));
      expect(spans.map((MdSpan s) => s.text).join(), isNot(contains('y.png')));
    });
  });

  group('MarkdownParser sanitizing', () {
    test('scripts and their content disappear', () {
      final List<MdBlock> blocks = MarkdownParser.parse(
        'before\n\n<script>alert("x")</script>\n\nafter <b>bold</b> <img src=x onerror=alert(1)>',
      );
      final String all = blocks
          .whereType<MdParagraph>()
          .expand((MdParagraph p) => p.spans)
          .map((MdSpan s) => s.text)
          .join('|');
      expect(all, isNot(contains('alert')));
      expect(all, isNot(contains('<')));
      expect(all, contains('before'));
      expect(all, contains('after'));
      expect(all, contains('bold'));
    });

    test('style and iframe blocks are dropped too', () {
      final List<MdBlock> blocks = MarkdownParser.parse(
        '<style>body{display:none}</style>\n<iframe src="https://evil"></iframe>\n\nok',
      );
      expect(blocks, const <MdBlock>[
        MdParagraph(<MdSpan>[MdSpan('ok')]),
      ]);
    });

    test('a less-than sign in normal text survives', () {
      final List<MdBlock> blocks = MarkdownParser.parse('5 < 7 and 9 > 2');
      expect(
        (blocks.single as MdParagraph).spans.single.text,
        '5 < 7 and 9 > 2',
      );
    });
  });
}
