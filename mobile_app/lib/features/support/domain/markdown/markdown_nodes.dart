import 'package:equatable/equatable.dart';

/// A run of inline text with its style. [url] is only ever a sanitized
/// `http` / `https` / `mailto` / `tel` link.
class MdSpan extends Equatable {
  const MdSpan(
    this.text, {
    this.bold = false,
    this.italic = false,
    this.code = false,
    this.url,
  });

  final String text;
  final bool bold;
  final bool italic;
  final bool code;
  final String? url;

  bool get isLink => url != null;

  @override
  List<Object?> get props => <Object?>[text, bold, italic, code, url];
}

/// A block of a help article.
sealed class MdBlock extends Equatable {
  const MdBlock();

  @override
  List<Object?> get props => const <Object?>[];
}

final class MdHeading extends MdBlock {
  const MdHeading(this.level, this.spans);

  /// 1 to 3 (deeper levels are drawn as 3).
  final int level;
  final List<MdSpan> spans;

  @override
  List<Object?> get props => <Object?>[level, spans];
}

final class MdParagraph extends MdBlock {
  const MdParagraph(this.spans);

  final List<MdSpan> spans;

  @override
  List<Object?> get props => <Object?>[spans];
}

/// A list item; [number] is set for ordered lists, [depth] is the nesting.
final class MdListItem extends MdBlock {
  const MdListItem(this.spans, {this.number, this.depth = 0});

  final List<MdSpan> spans;
  final int? number;
  final int depth;

  @override
  List<Object?> get props => <Object?>[spans, number, depth];
}

final class MdQuote extends MdBlock {
  const MdQuote(this.spans);

  final List<MdSpan> spans;

  @override
  List<Object?> get props => <Object?>[spans];
}

final class MdCode extends MdBlock {
  const MdCode(this.text);

  final String text;

  @override
  List<Object?> get props => <Object?>[text];
}

final class MdRule extends MdBlock {
  const MdRule();
}
