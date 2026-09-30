import 'package:ata_app/features/catalog/domain/entities/ride_category.dart';
import 'package:ata_app/features/corporate/presentation/widgets/corporate_payment_section.dart';
import 'package:ata_app/features/passenger_home/presentation/cubit/home_cubit.dart';
import 'package:ata_app/features/passenger_home/presentation/cubit/home_state.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// The company-account block under the payment row (F19): it needs the
/// catalog to name the categories the policy allows.
class CorporateBlock extends StatelessWidget {
  const CorporateBlock({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocSelector<HomeCubit, HomeState, List<RideCategory>>(
      selector: (HomeState state) => state.categories,
      builder: (BuildContext context, List<RideCategory> categories) =>
          CorporatePaymentSection(categories: categories),
    );
  }
}
