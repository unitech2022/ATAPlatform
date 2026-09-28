import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/trip_chat/domain/entities/trip_message.dart';
import 'package:ata_app/features/trip_chat/domain/usecases/request_masked_call.dart';
import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// State of [MaskedCallCubit].
class MaskedCallState extends Equatable {
  const MaskedCallState({
    this.loading = false,
    this.call,
    this.requestId = 0,
    this.failure,
  });

  final bool loading;

  /// Last answer of `POST …/call`.
  final MaskedCall? call;

  /// Bumped on every answer so the page acts once (dial / open chat).
  final int requestId;
  final Failure? failure;

  @override
  List<Object?> get props => <Object?>[loading, call, requestId, failure];
}

/// Masked call (F12.4): `proxy` → the page dials the proxy number;
/// `unavailable` (or an error) → the page opens the chat. The real number
/// of the other party is never used.
class MaskedCallCubit extends Cubit<MaskedCallState> {
  MaskedCallCubit({required this._requestCall})
    : super(const MaskedCallState());

  final RequestMaskedCall _requestCall;

  static const MaskedCall _unavailable = MaskedCall(mode: 'unavailable');

  Future<void> call(ChatTarget target) async {
    if (state.loading) return;
    emit(MaskedCallState(loading: true, requestId: state.requestId));
    final result = await _requestCall(target);
    if (isClosed) return;
    result.fold(
      (Failure f) => emit(
        MaskedCallState(
          call: _unavailable,
          requestId: state.requestId + 1,
          failure: f,
        ),
      ),
      (MaskedCall call) =>
          emit(MaskedCallState(call: call, requestId: state.requestId + 1)),
    );
  }
}
