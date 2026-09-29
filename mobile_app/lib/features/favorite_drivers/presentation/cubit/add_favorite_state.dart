import 'package:ata_app/core/errors/failures.dart';
import 'package:equatable/equatable.dart';

/// Progress of adding the driver of one completed trip to the favourites.
enum AddFavoriteStatus { idle, adding, added, alreadyFavorite, failed }

/// State of [AddFavoriteCubit].
class AddFavoriteState extends Equatable {
  const AddFavoriteState({this.status = AddFavoriteStatus.idle, this.failure});

  final AddFavoriteStatus status;
  final Failure? failure;

  bool get isAdding => status == AddFavoriteStatus.adding;
  bool get isFavorite =>
      status == AddFavoriteStatus.added ||
      status == AddFavoriteStatus.alreadyFavorite;

  @override
  List<Object?> get props => <Object?>[status, failure];
}
