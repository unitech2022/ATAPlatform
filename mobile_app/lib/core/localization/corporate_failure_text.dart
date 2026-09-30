import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/localization/corporate_text.dart';
import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/utils/money.dart';
import 'package:ata_app/features/corporate/domain/entities/policy_violation.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';

/// F19 error texts (corporate accounts); `null` for other codes.
String? corporateFailureText(ServerFailure failure, AppLocalizations l10n) {
  switch (failure.code) {
    case ErrorCodes.corporateNotMember:
      return l10n.corporateNotMemberError;
    case ErrorCodes.corporateAccountInactive:
      return l10n.corporateAccountInactiveError;
    case ErrorCodes.corporateMemberElsewhere:
      return l10n.corporateMemberElsewhereError;
    case ErrorCodes.corporatePolicyViolation:
      return _policyViolation(failure, l10n);
    case ErrorCodes.corporateBudgetExceeded:
      final double? remaining = failure.numDetail(ErrorCodes.remaining);
      return remaining == null
          ? l10n.corporateBudgetExceededError
          : l10n.corporateBudgetExceededLeftError(Money.compact(remaining));
    case ErrorCodes.corporateCreditLimitExceeded:
      return l10n.corporateCreditLimitError;
    case ErrorCodes.invitationExpired:
      return l10n.invitationExpiredError;
  }
  return null;
}

/// The violated rules of `422 corporate_policy_violation`, one per line
/// with the options the company allows.
String _policyViolation(ServerFailure failure, AppLocalizations l10n) {
  final List<PolicyViolation> violations = PolicyViolation.listFrom(
    failure.details?[ErrorCodes.violations],
  );
  if (violations.isEmpty) return l10n.corporatePolicyViolationError;
  return violations
      .map((PolicyViolation v) {
        final String? allowed = CorporateText.allowedOptions(l10n, v);
        final String message = CorporateText.violation(l10n, v);
        return allowed == null ? message : '$message ($allowed)';
      })
      .join('\n');
}

/// `422 corporate_policy_violation` / `corporate_budget_exceeded` /
/// `corporate_credit_limit_exceeded`: the company policy refused the trip.
bool isCorporatePolicyRefusal(Failure failure) =>
    failure.code == ErrorCodes.corporatePolicyViolation ||
    failure.code == ErrorCodes.corporateBudgetExceeded ||
    failure.code == ErrorCodes.corporateCreditLimitExceeded;
