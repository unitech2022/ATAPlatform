import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_profile.dart';
import 'package:ata_app/features/corporate/domain/usecases/get_corporate_membership.dart';
import 'package:ata_app/features/corporate/presentation/cubit/corporate_membership_state.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// App-wide: the rider's company membership (`GET /passenger/corporate`),
/// loaded after sign-in and reloaded after an invitation is accepted or a
/// `corporate.*` push arrives. A failed reload keeps the last profile.
class CorporateMembershipCubit extends Cubit<CorporateMembershipState> {
  CorporateMembershipCubit({required this._getMembership})
    : super(const CorporateMembershipState());

  final GetCorporateMembership _getMembership;
  int _generation = 0;

  /// Binds to a signed-in rider and loads (no-op when already bound).
  Future<void> start() async {
    if (state.active) return;
    emit(const CorporateMembershipState(active: true));
    await _load();
  }

  /// Signed out, or not a rider: forget everything.
  void stop() {
    if (!state.active) return;
    _generation++;
    emit(const CorporateMembershipState());
  }

  /// Reloads the profile (invitation accepted, push, page opened).
  Future<void> refresh() async {
    if (!state.active) return;
    await _load();
  }

  Future<void> _load() async {
    final int generation = ++_generation;
    emit(state.copyWith(status: CorporateLoadStatus.loading));
    final result = await _getMembership(const NoParams());
    if (isClosed || generation != _generation) return;
    emit(
      result.fold(
        (failure) => state.copyWith(
          status: CorporateLoadStatus.failure,
          failure: failure,
        ),
        (CorporateProfile? profile) => CorporateMembershipState(
          active: true,
          status: CorporateLoadStatus.ready,
          profile: profile,
        ),
      ),
    );
  }
}
