import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/favorite_drivers/domain/entities/favorite_params.dart';
import 'package:ata_app/features/favorite_drivers/domain/usecases/add_favorite_driver.dart';
import 'package:ata_app/features/favorite_drivers/presentation/cubit/add_favorite_state.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// "أضف إلى المفضلة" of a completed trip (receipt, end-of-trip view, rides
/// history): `POST /passenger/favorite-drivers { tripId }`.
/// `409 favorite_exists` counts as already favourite.
class AddFavoriteCubit extends Cubit<AddFavoriteState> {
  AddFavoriteCubit({required this.tripId, required this._addFavorite})
    : super(const AddFavoriteState());

  static const String exists = 'favorite_exists';

  final String tripId;
  final AddFavoriteDriver _addFavorite;

  Future<void> add() async {
    if (state.isAdding || state.isFavorite) return;
    emit(const AddFavoriteState(status: AddFavoriteStatus.adding));
    final result = await _addFavorite(AddFavoriteParams(tripId: tripId));
    if (isClosed) return;
    emit(
      result.fold(
        (Failure failure) => failure.code == exists
            ? const AddFavoriteState(status: AddFavoriteStatus.alreadyFavorite)
            : AddFavoriteState(
                status: AddFavoriteStatus.failed,
                failure: failure,
              ),
        (_) => const AddFavoriteState(status: AddFavoriteStatus.added),
      ),
    );
  }
}
