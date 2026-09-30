import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/features/support/domain/markdown/markdown_nodes.dart';
import 'package:ata_app/features/support/domain/markdown/markdown_parser.dart';
import 'package:flutter/material.dart';
import 'package:url_launcher/url_launcher.dart';

/// Renders an article's Markdown with the design tokens. The source goes
/// through [MarkdownParser] first, so scripts, HTML and unsafe links never
/// reach the screen; everything is drawn as plain [Text].
class MarkdownView extends StatelessWidget {
  const MarkdownView({super.key, required this.source});

  final String source;

  static const double _bulletWidth = 24;
  static const double _indent = 16;
  static const double _quoteBar = 3;

  @override
  Widget build(BuildContext context) {
    final List<MdBlock> blocks = MarkdownParser.parse(source);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        for (final MdBlock block in blocks)
          Padding(
            padding: const EdgeInsets.only(bottom: AtaSpacing.sm),
            child: _block(block),
          ),
      ],
    );
  }

  Widget _block(MdBlock block) => switch (block) {
    MdHeading() => _text(block.spans, switch (block.level) {
      1 => AtaText.headline,
      2 => AtaText.section,
      _ => AtaText.bodyStrong,
    }),
    MdParagraph() => _text(block.spans, AtaText.body),
    MdListItem() => Padding(
      padding: EdgeInsetsDirectional.only(start: block.depth * _indent),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: <Widget>[
          SizedBox(
            width: _bulletWidth,
            child: Text(
              block.number == null ? '•' : '${block.number}.',
              style: AtaText.body,
            ),
          ),
          Expanded(child: _text(block.spans, AtaText.body)),
        ],
      ),
    ),
    MdQuote() => Container(
      padding: const EdgeInsetsDirectional.only(start: AtaSpacing.sm),
      decoration: const BoxDecoration(
        border: BorderDirectional(
          start: BorderSide(color: AtaColors.brand, width: _quoteBar),
        ),
      ),
      child: _text(block.spans, AtaText.bodyMuted),
    ),
    MdCode() => Container(
      padding: const EdgeInsets.all(AtaSpacing.sm),
      decoration: const BoxDecoration(
        color: AtaColors.cloud,
        borderRadius: AtaRadii.smallRadius,
      ),
      child: Text(
        block.text,
        style: AtaText.small.copyWith(fontFamily: 'monospace'),
        textDirection: TextDirection.ltr,
      ),
    ),
    MdRule() => const Divider(),
  };

  Widget _text(List<MdSpan> spans, TextStyle base) => Text.rich(
    TextSpan(
      style: base,
      children: <InlineSpan>[for (final MdSpan s in spans) _span(s, base)],
    ),
  );

  InlineSpan _span(MdSpan span, TextStyle base) {
    final TextStyle style = TextStyle(
      fontWeight: span.bold ? FontWeight.w700 : null,
      fontStyle: span.italic ? FontStyle.italic : null,
      fontFamily: span.code ? 'monospace' : null,
      backgroundColor: span.code ? AtaColors.cloud : null,
    );
    final String? url = span.url;
    if (url == null) return TextSpan(text: span.text, style: style);
    return WidgetSpan(
      alignment: PlaceholderAlignment.baseline,
      baseline: TextBaseline.alphabetic,
      child: GestureDetector(
        onTap: () =>
            launchUrl(Uri.parse(url), mode: LaunchMode.externalApplication),
        child: Text(
          span.text,
          style: base
              .merge(style)
              .copyWith(
                color: AtaColors.brand,
                decoration: TextDecoration.underline,
              ),
        ),
      ),
    );
  }
}
