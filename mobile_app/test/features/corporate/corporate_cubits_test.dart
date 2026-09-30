import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_invitation.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_policy_summary.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_profile.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_quote_check.dart';
import 'package:ata_app/features/corporate/domain/entities/eligibility_draft.dart';
import 'package:ata_app/features/corporate/domain/entities/policy_violation.dart';
import 'package:ata_app/features/corporate/domain/usecases/accept_corporate_invitation.dart';
import 'package:ata_app/features/corporate/domain/usecases/check_corporate_eligibility.dart';
import 'package:ata_app/features/corporate/domain/usecases/decline_corporate_invitation.dart';
import 'package:ata_app/features/corporate/domain/usecases/get_corporate_invitations.dart';
import 'package:ata_app/features/corporate/domain/usecases/get_corporate_membership.dart';
import 'package:ata_app/features/corporate/presentation/cubit/corporate_invitations_cubit.dart';
import 'package:ata_app/features/corporate/presentation/cubit/corporate_invitations_state.dart';
import 'package:ata_app/features/corporate/presentation/cubit/corporate_membership_cubit.dart';
import 'package:ata_app/features/corporate/presentation/cubit/corporate_membership_state.dart';
import 'package:ata_app/features/corporate/presentation/cubit/corporate_payment_cubit.dart';
import 'package:ata_app/features/corporate/presentation/cubit/corporate_payment_state.dart';
import 'package:bloc_test/bloc_test.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:fpdart/fpdart.dart';
import 'package:mocktail/mocktail.dart';

import '../../helpers/corporate_fakes.dart';

class _MockGetMembership extends Mock implements GetCorporateMembership {}

class _MockGetInvitations extends Mock implements GetCorporateInvitations {}

class _MockAccept extends Mock implements AcceptCorporateInvitation {}

class _MockDecline extends Mock implements DeclineCorporateInvitation {}

const ServerFailure _expired = ServerFailure(
  code: 'invitation_expired',
  message: '',
  statusCode: 410,
);

void main() {
  setUpAll(() => registerFallbackValue(const NoParams()));

  group('CorporateMembershipCubit', () {
    late _MockGetMembership getMembership;

    setUp(() => getMembership = _MockGetMembership());

    CorporateMembershipCubit build() =>
        CorporateMembershipCubit(getMembership: getMembership);

    blocTest<CorporateMembershipCubit, CorporateMembershipState>(
      'start loads the profile of an active employee',
      build: build,
      setUp: () => when(() => getMembership(any())).thenAnswer(
        (_) async =>
            const Right<Failure, CorporateProfile?>(testCorporateProfile),
      ),
      act: (CorporateMembershipCubit c) => c.start(),
      expect: () => <CorporateMembershipState>[
        const CorporateMembershipState(active: true),
        const CorporateMembershipState(
          active: true,
          status: CorporateLoadStatus.loading,
        ),
        const CorporateMembershipState(
          active: true,
          status: CorporateLoadStatus.ready,
          profile: testCorporateProfile,
        ),
      ],
      verify: (CorporateMembershipCubit c) {
        expect(c.state.isMember, isTrue);
        expect(c.state.isActive, isTrue);
      },
    );

    blocTest<CorporateMembershipCubit, CorporateMembershipState>(
      'a rider without a company is not a member',
      build: build,
      setUp: () => when(
        () => getMembership(any()),
      ).thenAnswer((_) async => const Right<Failure, CorporateProfile?>(null)),
      act: (CorporateMembershipCubit c) => c.start(),
      verify: (CorporateMembershipCubit c) {
        expect(c.state.status, CorporateLoadStatus.ready);
        expect(c.state.isMember, isFalse);
        expect(c.state.isActive, isFalse);
      },
    );

    test('start twice loads once; stop forgets everything', () async {
      when(() => getMembership(any())).thenAnswer(
        (_) async =>
            const Right<Failure, CorporateProfile?>(testCorporateProfile),
      );
      final CorporateMembershipCubit cubit = build();
      await cubit.start();
      await cubit.start();
      verify(() => getMembership(any())).called(1);
      cubit.stop();
      expect(cubit.state, const CorporateMembershipState());
      // A refresh after stop is ignored.
      await cubit.refresh();
      verifyNever(() => getMembership(any()));
      await cubit.close();
    });

    test('a failed refresh keeps the last profile', () async {
      when(() => getMembership(any())).thenAnswer(
        (_) async =>
            const Right<Failure, CorporateProfile?>(testCorporateProfile),
      );
      final CorporateMembershipCubit cubit = build();
      await cubit.start();
      when(() => getMembership(any())).thenAnswer(
        (_) async => const Left<Failure, CorporateProfile?>(
          NetworkFailure(message: 'x'),
        ),
      );
      await cubit.refresh();
      expect(cubit.state.status, CorporateLoadStatus.failure);
      expect(cubit.state.profile, testCorporateProfile);
      expect(cubit.state.failure, isA<NetworkFailure>());
      await cubit.close();
    });

    test('a refresh that finds no membership clears the profile', () async {
      when(() => getMembership(any())).thenAnswer(
        (_) async =>
            const Right<Failure, CorporateProfile?>(testCorporateProfile),
      );
      final CorporateMembershipCubit cubit = build();
      await cubit.start();
      when(
        () => getMembership(any()),
      ).thenAnswer((_) async => const Right<Failure, CorporateProfile?>(null));
      await cubit.refresh();
      expect(cubit.state.isMember, isFalse);
      await cubit.close();
    });
  });

  group('CorporateInvitationsCubit', () {
    late _MockGetInvitations getInvitations;
    late _MockAccept accept;
    late _MockDecline decline;

    setUp(() {
      getInvitations = _MockGetInvitations();
      accept = _MockAccept();
      decline = _MockDecline();
      when(() => getInvitations(any())).thenAnswer(
        (_) async => const Right<Failure, List<CorporateInvitation>>(
          <CorporateInvitation>[testInvitation],
        ),
      );
    });

    CorporateInvitationsCubit build() => CorporateInvitationsCubit(
      getInvitations: getInvitations,
      accept: accept,
      decline: decline,
    );

    test('load lists the pending invitations', () async {
      final CorporateInvitationsCubit cubit = build();
      await cubit.load();
      expect(cubit.state.status, CorporateLoadStatus.ready);
      expect(cubit.state.invitations, <CorporateInvitation>[testInvitation]);
      expect(cubit.state.hasInvitations, isTrue);
      await cubit.close();
    });

    test('a failed load exposes the failure', () async {
      when(() => getInvitations(any())).thenAnswer(
        (_) async => const Left<Failure, List<CorporateInvitation>>(
          NetworkFailure(message: 'x'),
        ),
      );
      final CorporateInvitationsCubit cubit = build();
      await cubit.load();
      expect(cubit.state.status, CorporateLoadStatus.failure);
      expect(cubit.state.failure, isNotNull);
      await cubit.close();
    });

    blocTest<CorporateInvitationsCubit, CorporateInvitationsState>(
      'accept removes the invitation and reports the company',
      build: build,
      seed: () => const CorporateInvitationsState(
        status: CorporateLoadStatus.ready,
        invitations: <CorporateInvitation>[testInvitation],
      ),
      setUp: () => when(
        () => accept('inv1'),
      ).thenAnswer((_) async => const Right<Failure, Unit>(unit)),
      act: (CorporateInvitationsCubit c) => c.accept('inv1'),
      expect: () => <CorporateInvitationsState>[
        const CorporateInvitationsState(
          status: CorporateLoadStatus.ready,
          invitations: <CorporateInvitation>[testInvitation],
          busyId: 'inv1',
        ),
        const CorporateInvitationsState(
          status: CorporateLoadStatus.ready,
          outcome: InvitationOutcome.accepted,
          outcomeCompany: 'شركة المثال',
        ),
      ],
    );

    test('decline removes the invitation; outcomeShown resets', () async {
      when(
        () => decline('inv1'),
      ).thenAnswer((_) async => const Right<Failure, Unit>(unit));
      final CorporateInvitationsCubit cubit = build();
      await cubit.load();
      await cubit.decline('inv1');
      expect(cubit.state.invitations, isEmpty);
      expect(cubit.state.outcome, InvitationOutcome.declined);
      expect(cubit.state.isBusy, isFalse);
      cubit.outcomeShown();
      expect(cubit.state.outcome, InvitationOutcome.none);
      await cubit.close();
    });

    test('an expired invitation is dropped with its error', () async {
      when(
        () => accept('inv1'),
      ).thenAnswer((_) async => const Left<Failure, Unit>(_expired));
      final CorporateInvitationsCubit cubit = build();
      await cubit.load();
      await cubit.accept('inv1');
      expect(cubit.state.invitations, isEmpty);
      expect(cubit.state.actionFailure!.code, 'invitation_expired');
      expect(cubit.state.outcome, InvitationOutcome.none);
      await cubit.close();
    });

    test('member elsewhere keeps the invitation and shows the error', () async {
      when(() => accept('inv1')).thenAnswer(
        (_) async => const Left<Failure, Unit>(
          ServerFailure(code: 'corporate_member_elsewhere', message: ''),
        ),
      );
      final CorporateInvitationsCubit cubit = build();
      await cubit.load();
      await cubit.accept('inv1');
      expect(cubit.state.invitations, hasLength(1));
      expect(cubit.state.actionFailure!.code, 'corporate_member_elsewhere');
      expect(cubit.state.busyId, isNull);
      await cubit.close();
    });

    test('one action at a time', () async {
      when(() => accept('inv1')).thenAnswer((_) async {
        await Future<void>.delayed(const Duration(milliseconds: 10));
        return const Right<Failure, Unit>(unit);
      });
      final CorporateInvitationsCubit cubit = build();
      await cubit.load();
      final Future<void> first = cubit.accept('inv1');
      await cubit.decline('inv1');
      await first;
      verify(() => accept('inv1')).called(1);
      verifyNever(() => decline(any()));
      await cubit.close();
    });

    test('reset forgets everything', () async {
      final CorporateInvitationsCubit cubit = build();
      await cubit.load();
      cubit.reset();
      expect(cubit.state, const CorporateInvitationsState());
      await cubit.close();
    });
  });

  group('CorporatePaymentCubit', () {
    final DateTime noon = DateTime.utc(2026, 10, 4, 9);

    CorporatePaymentCubit build() => CorporatePaymentCubit(
      checkEligibility: CheckCorporateEligibility(() => noon),
    );

    EligibilityDraft draft({
      String category = 'economy',
      double? fare,
      CorporateQuoteCheck? server,
    }) => EligibilityDraft(
      profile: testCorporateProfile,
      categoryCode: category,
      estimatedFare: fare,
      serverCheck: server,
    );

    test('not shown until selected; shown for an active member', () {
      final CorporatePaymentCubit cubit = build()
        ..sync(draft(), selected: false);
      expect(cubit.state.available, isTrue);
      expect(cubit.state.showForm, isFalse);
      cubit.sync(draft(), selected: true);
      expect(cubit.state.showForm, isTrue);
      expect(cubit.close, returnsNormally);
    });

    test('the option is not available without an active membership', () {
      final CorporatePaymentCubit cubit = build()
        ..sync(const EligibilityDraft(profile: null), selected: true);
      expect(cubit.state.available, isFalse);
      expect(cubit.state.showForm, isFalse);
      expect(cubit.state.ready, isFalse);
    });

    test('the purpose is required by the policy before the request', () {
      final CorporatePaymentCubit cubit = build()
        ..sync(draft(), selected: true);
      expect(cubit.state.purposeRequired, isTrue);
      expect(cubit.state.purposeMissing, isTrue);
      expect(cubit.state.ready, isFalse);
      expect(cubit.state.booking.ready, isFalse);

      cubit.purposeChanged('   ');
      expect(cubit.state.purposeMissing, isTrue);

      cubit.purposeChanged('  اجتماع عميل ');
      expect(cubit.state.purposeMissing, isFalse);
      expect(cubit.state.ready, isTrue);
      expect(cubit.state.booking.tripPurpose, 'اجتماع عميل');
      expect(cubit.state.booking.ready, isTrue);
    });

    test('a required cost center must be picked before the request', () {
      final CorporateProfile requiring = CorporateProfile(
        membership: testCorporateProfile.membership,
        policy: const CorporatePolicySummary(requireCostCenter: true),
        costCenters: testCorporateProfile.costCenters,
      );
      final CorporatePaymentCubit cubit = build()
        ..sync(EligibilityDraft(profile: requiring), selected: true);
      expect(cubit.state.purposeRequired, isFalse);
      expect(cubit.state.costCenterRequired, isTrue);
      expect(cubit.state.costCenterMissing, isTrue);
      expect(cubit.state.ready, isFalse);
      cubit.selectCostCenter('cc2');
      expect(cubit.state.costCenterMissing, isFalse);
      expect(cubit.state.ready, isTrue);
    });

    test('the cost center is chosen, toggled and dropped when not offered', () {
      final CorporatePaymentCubit cubit = build()
        ..sync(draft(), selected: true)
        ..selectCostCenter('cc1');
      expect(cubit.state.costCenterId, 'cc1');
      expect(cubit.state.booking.costCenterId, 'cc1');
      cubit.selectCostCenter('cc1');
      expect(cubit.state.costCenterId, isNull);
      cubit.selectCostCenter('cc2');
      expect(cubit.state.costCenterId, 'cc2');
      // The profile reloads without that cost center.
      cubit.sync(
        EligibilityDraft(
          profile: CorporateProfile(
            membership: testCorporateProfile.membership,
            policy: testCorporateProfile.policy,
          ),
        ),
        selected: true,
      );
      expect(cubit.state.costCenterId, isNull);
    });

    test('violations from the quote block the request and stay explained', () {
      final CorporatePaymentCubit cubit = build()
        ..purposeChanged('x')
        ..sync(
          draft(
            server: const CorporateQuoteCheck(
              allowed: false,
              violations: <PolicyViolation>[
                PolicyViolation(
                  rule: PolicyRule.zone,
                  allowed: <String>['الشمال'],
                ),
              ],
              remainingBudget: 79.5,
            ),
          ),
          selected: true,
        );
      expect(cubit.state.eligibility.eligible, isFalse);
      expect(cubit.state.eligibility.remainingBudget, 79.5);
      expect(cubit.state.usable, isFalse);
      expect(cubit.state.ready, isFalse);
    });

    test('a category the policy does not allow disables the option', () {
      final CorporateProfile restricted = CorporateProfile(
        membership: testCorporateProfile.membership,
        policy: const CorporatePolicySummary(
          allowedRideCategoryCodes: <String>['economy'],
        ),
      );
      final CorporatePaymentCubit cubit = build()
        ..sync(
          EligibilityDraft(profile: restricted, categoryCode: 'premium'),
          selected: false,
        );
      expect(cubit.state.available, isTrue);
      expect(cubit.state.usable, isFalse);
      expect(
        cubit.state.eligibility.violations.single.rule,
        PolicyRule.category,
      );
      cubit.sync(
        EligibilityDraft(profile: restricted, categoryCode: 'economy'),
        selected: false,
      );
      expect(cubit.state.usable, isTrue);
    });

    blocTest<CorporatePaymentCubit, CorporatePaymentState>(
      'the booking changes with the typed purpose only',
      build: build,
      seed: () => const CorporatePaymentState(),
      act: (CorporatePaymentCubit c) {
        c.sync(draft(), selected: true);
        c.purposeChanged('زيارة');
      },
      verify: (CorporatePaymentCubit c) {
        expect(c.state.booking.tripPurpose, 'زيارة');
        expect(c.state.booking.quoteKey, 'true|');
      },
    );
  });
}
