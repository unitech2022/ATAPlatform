/// Driver transitions exposed as `POST /driver/trips/{id}/<step>`.
enum TripStep {
  enRoute('en-route'),
  arrived('arrived'),
  start('start'),
  complete('complete');

  const TripStep(this.apiPath);

  final String apiPath;
}

/// Which side of the trip the app is acting as.
enum TripActor { passenger, driver }
