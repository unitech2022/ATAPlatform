import 'package:ata_app/features/airport/domain/entities/airport_selection.dart';
import 'package:ata_app/features/trip/domain/entities/trip_airport.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';

/// Localized copy for airport choices and trip airport details.
abstract final class AirportText {
  /// `مطار الملك خالد · من المطار`.
  static String title(AppLocalizations l10n, AirportSelection s) =>
      '${s.airport.name} · ${direction(l10n, s.direction)}';

  static String direction(AppLocalizations l10n, AirportDirection d) =>
      d == AirportDirection.pickup
      ? l10n.airportDirectionPickup
      : l10n.airportDirectionDropoff;

  /// Zone / terminal and flight number of a choice, joined with ` · `.
  static String detail(AppLocalizations l10n, AirportSelection s) {
    final List<String> parts = <String>[
      if (s.isPickup && s.zone != null) s.zone!.name,
      if (!s.isPickup && s.terminalCode != null)
        l10n.airportTerminalLabel(s.terminalCode!),
      if (s.flightNumber != null) l10n.airportFlightLine(s.flightNumber!),
    ];
    return parts.isEmpty ? l10n.airportNoDetails : parts.join(' · ');
  }

  /// `التقاط من مطار RUH` / `توصيل إلى مطار RUH`.
  static String tripTitle(AppLocalizations l10n, TripAirport a) => a.isPickup
      ? l10n.tripAirportPickup(a.code)
      : l10n.tripAirportDropoff(a.code);

  /// Zone, terminal, flight and free waiting of a trip's airport details.
  static String tripDetail(AppLocalizations l10n, TripAirport a) {
    final List<String> parts = <String>[
      ?a.zoneName,
      if (a.terminalCode != null) l10n.airportTerminalLabel(a.terminalCode!),
      if (a.flightNumber != null) l10n.airportFlightLine(a.flightNumber!),
      if (a.freeWaitingMinutes != null)
        l10n.airportWaitingFree(a.freeWaitingMinutes!),
    ];
    return parts.join(' · ');
  }
}
