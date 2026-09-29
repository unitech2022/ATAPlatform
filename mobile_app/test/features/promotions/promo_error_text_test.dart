import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/localization/rewards_failure_text.dart';
import 'package:ata_app/l10n/generated/app_localizations_ar.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('promo error texts', () {
    final AppLocalizationsAr l10n = AppLocalizationsAr();

    String text(String code, [Map<String, dynamic>? details]) => failureText(
      ServerFailure(code: code, message: 'server', details: details),
      l10n,
    );

    test('every promo code and reason has its own localized text', () {
      expect(text('promo_not_found'), 'كود الخصم غير صحيح');
      expect(text('promo_expired'), 'انتهت صلاحية كود الخصم');
      expect(text('promo_usage_limit_reached'), 'تم استنفاد كود الخصم');
      expect(
        text('promo_usage_limit_reached', <String, dynamic>{'scope': 'user'}),
        'استخدمت هذا الكود الحد الأقصى من المرات',
      );
      expect(text('promo_not_eligible'), 'كود الخصم لا ينطبق على هذه الرحلة');
      final Set<String> reasons = <String>{
        for (final String r in <String>[
          'first_trip_only',
          'new_users_only',
          'city',
          'category',
          'zone',
          'payment_method',
          'booking_type',
          'min_fare',
          'pricing_mode',
        ])
          text('promo_not_eligible', <String, dynamic>{'reason': r}),
      };
      expect(reasons, hasLength(9));
      expect(promoReasonText('unknown', l10n), l10n.promoNotEligibleError);
      expect(text('rating_window_closed'), 'انتهت مدة التقييم');
      expect(text('rating_exists'), 'تم تقييم هذه الرحلة مسبقاً');
      expect(
        text('incentive_opt_in_closed'),
        'الاشتراك في هذا الحافز غير متاح',
      );
    });
  });
}
