import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/catalog/domain/entities/ride_category.dart';
import 'package:ata_app/features/catalog/domain/usecases/get_ride_categories.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// The ride categories of the catalog, so the policy card can show the
/// allowed categories by name instead of by code. A failure leaves the
/// list empty (codes are shown).
class CorporateCatalogCubit extends Cubit<List<RideCategory>> {
  CorporateCatalogCubit({required this._getCategories})
    : super(const <RideCategory>[]);

  final GetRideCategories _getCategories;

  Future<void> load() async {
    final result = await _getCategories(const NoParams());
    if (isClosed) return;
    result.fold((_) {}, emit);
  }
}
