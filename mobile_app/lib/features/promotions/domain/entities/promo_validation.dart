import 'package:ata_app/features/promotions/domain/entities/promotion.dart';
import 'package:equatable/equatable.dart';

/// Body of `POST /passenger/promotions/validate`.
class PromoValidationParams extends Equatable {
  const PromoValidationParams({
    required this.code,
    this.quoteId,
    this.rideCategoryId,
    this.paymentMethod,
    this.bookingType,
  });

  final String code;

  /// With a quote the API computes the discount of that price.
  final String? quoteId;
  final String? rideCategoryId;
  final String? paymentMethod;
  final String? bookingType;

  PromoValidationParams withCode(String code) => PromoValidationParams(
    code: code,
    quoteId: quoteId,
    rideCategoryId: rideCategoryId,
    paymentMethod: paymentMethod,
    bookingType: bookingType,
  );

  @override
  List<Object?> get props => <Object?>[
    code,
    quoteId,
    rideCategoryId,
    paymentMethod,
    bookingType,
  ];
}

/// `200 { valid, promotion, discountAmount, totalBefore, totalAfter }`.
class PromoValidation extends Equatable {
  const PromoValidation({
    required this.code,
    this.name = '',
    this.type = PromotionType.unknown,
    this.value = 0,
    this.maxDiscount,
    this.isStackable = false,
    this.discountAmount,
    this.totalBefore,
    this.totalAfter,
  });

  final String code;
  final String name;
  final PromotionType type;
  final double value;
  final double? maxDiscount;
  final bool isStackable;

  /// Only when validated against a `quoteId`.
  final double? discountAmount;
  final double? totalBefore;
  final double? totalAfter;

  @override
  List<Object?> get props => <Object?>[
    code,
    name,
    type,
    value,
    maxDiscount,
    isStackable,
    discountAmount,
    totalBefore,
    totalAfter,
  ];
}
