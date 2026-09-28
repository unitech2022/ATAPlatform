/// The role a session acts as. A phone number may hold both roles.
enum UserRole {
  passenger('passenger'),
  driver('driver');

  const UserRole(this.apiValue);

  /// Value used by the API (`role` field) and persisted locally.
  final String apiValue;

  static UserRole? tryParse(String? value) {
    for (final UserRole role in values) {
      if (role.apiValue == value) return role;
    }
    return null;
  }
}
