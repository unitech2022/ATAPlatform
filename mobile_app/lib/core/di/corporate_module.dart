import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/features/corporate/data/datasources/corporate_remote_data_source.dart';
import 'package:ata_app/features/corporate/data/repositories/corporate_repository_impl.dart';
import 'package:ata_app/features/corporate/domain/repositories/corporate_repository.dart';
import 'package:ata_app/features/corporate/domain/usecases/accept_corporate_invitation.dart';
import 'package:ata_app/features/corporate/domain/usecases/check_corporate_eligibility.dart';
import 'package:ata_app/features/corporate/domain/usecases/decline_corporate_invitation.dart';
import 'package:ata_app/features/corporate/domain/usecases/get_corporate_invitations.dart';
import 'package:ata_app/features/corporate/domain/usecases/get_corporate_membership.dart';

/// F19 data layer: the employee side of corporate accounts.
void registerCorporateData() {
  getIt.registerLazySingleton<CorporateRepository>(
    () => CorporateRepositoryImpl(CorporateRemoteDataSource(getIt())),
  );
}

/// F19 use cases: membership, invitations and the eligibility check.
void registerCorporateUseCases() {
  getIt
    ..registerLazySingleton<GetCorporateMembership>(
      () => GetCorporateMembership(getIt()),
    )
    ..registerLazySingleton<GetCorporateInvitations>(
      () => GetCorporateInvitations(getIt()),
    )
    ..registerLazySingleton<AcceptCorporateInvitation>(
      () => AcceptCorporateInvitation(getIt()),
    )
    ..registerLazySingleton<DeclineCorporateInvitation>(
      () => DeclineCorporateInvitation(getIt()),
    )
    ..registerLazySingleton<CheckCorporateEligibility>(
      () => const CheckCorporateEligibility(),
    );
}
