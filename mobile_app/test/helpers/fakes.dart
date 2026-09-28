import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/models/page_result.dart';
import 'package:ata_app/core/storage/token_storage.dart';
import 'package:ata_app/features/account/domain/entities/notification_preferences.dart';
import 'package:ata_app/features/account/domain/entities/profile.dart';
import 'package:ata_app/features/account/domain/repositories/account_repository.dart';
import 'package:ata_app/features/auth/domain/entities/auth_session.dart';
import 'package:ata_app/features/auth/domain/entities/otp_request.dart';
import 'package:ata_app/features/auth/domain/entities/user.dart';
import 'package:ata_app/features/auth/domain/entities/user_role.dart';
import 'package:ata_app/features/auth/domain/repositories/auth_repository.dart';
import 'package:ata_app/features/catalog/domain/entities/city.dart';
import 'package:ata_app/features/catalog/domain/entities/document_type.dart';
import 'package:ata_app/features/catalog/domain/entities/ride_category.dart';
import 'package:ata_app/features/catalog/domain/repositories/catalog_repository.dart';
import 'package:ata_app/features/notifications/domain/entities/notification_item.dart';
import 'package:ata_app/features/notifications/domain/repositories/notifications_repository.dart';
import 'package:ata_app/features/passenger_home/domain/repositories/passenger_repository.dart';
import 'package:ata_app/features/rides/domain/entities/trip_summary.dart';
import 'package:ata_app/features/rides/domain/repositories/rides_repository.dart';
import 'package:ata_app/features/wallet/domain/entities/wallet_summary.dart';
import 'package:ata_app/features/wallet/domain/entities/wallet_transaction.dart';
import 'package:ata_app/features/wallet/domain/repositories/wallet_repository.dart';
import 'package:fpdart/fpdart.dart';

/// In-memory token storage for tests.
class InMemoryTokenStorage implements TokenStorage {
  StoredTokens? tokens;

  @override
  Future<void> clear() async => tokens = null;

  @override
  Future<StoredTokens?> read() async => tokens;

  @override
  Future<void> save(StoredTokens value) async => tokens = value;
}

const User testUser = User(
  id: 'u1',
  phoneNumber: '+966512345678',
  fullName: 'عبدالله محمد',
  language: 'ar',
  gender: 'male',
  roles: <String>['passenger'],
);

const OtpRequest testOtpRequest = OtpRequest(
  requestId: 'req-1',
  phoneNumber: '+966512345678',
  expiresInSeconds: 300,
  resendAfterSeconds: 60,
  devCode: '1234',
);

const AuthSession testSession = AuthSession(
  accessToken: 'access',
  refreshToken: 'refresh',
  isNewUser: false,
  user: testUser,
  activeRole: UserRole.passenger,
);

const List<RideCategory> testCategories = <RideCategory>[
  RideCategory(
    id: 'c1',
    code: 'economy',
    name: 'اقتصادي',
    description: 'سيارة مريحة',
    icon: 'car',
    seats: 4,
    maxStops: 2,
    sortOrder: 1,
    estimate: RideEstimate(etaMinutes: 2, price: 38),
  ),
  RideCategory(
    id: 'c2',
    code: 'family',
    name: 'عائلي',
    description: 'حتى 6 ركاب',
    icon: 'car',
    seats: 6,
    maxStops: 1,
    sortOrder: 2,
    estimate: RideEstimate(etaMinutes: 4, price: 54),
  ),
];

/// Auth repository that never talks to the network.
class FakeAuthRepository implements AuthRepository {
  FakeAuthRepository({this.restored});

  AuthSession? restored;

  @override
  Future<Either<Failure, User>> completeRiderProfile({
    required String fullName,
  }) async => Right<Failure, User>(
    testUser.copyWith(fullName: fullName, termsAcceptedAt: DateTime.now()),
  );

  @override
  Future<Either<Failure, Unit>> logout() async =>
      const Right<Failure, Unit>(unit);

  @override
  Future<Either<Failure, OtpRequest>> requestOtp({
    required String phoneNumber,
    required UserRole role,
    required String language,
  }) async => const Right<Failure, OtpRequest>(testOtpRequest);

  @override
  Future<Either<Failure, AuthSession?>> restoreSession() async =>
      Right<Failure, AuthSession?>(restored);

  @override
  Future<Either<Failure, AuthSession>> verifyOtp({
    required String requestId,
    required String phoneNumber,
    required String code,
    required UserRole role,
  }) async =>
      Right<Failure, AuthSession>(testSession.copyWith(activeRole: role));
}

class FakeAccountRepository implements AccountRepository {
  String locale = 'ar';

  @override
  Future<Either<Failure, Unit>> deleteAccount() async =>
      const Right<Failure, Unit>(unit);

  @override
  Future<Either<Failure, NotificationPreferences>>
  getNotificationPreferences() async =>
      const Right<Failure, NotificationPreferences>(NotificationPreferences());

  @override
  Future<Either<Failure, Profile>> getProfile() async =>
      const Right<Failure, Profile>(Profile(user: testUser));

  @override
  String getSavedLocale() => locale;

  @override
  Future<Either<Failure, Unit>> saveLocale(String languageCode) async {
    locale = languageCode;
    return const Right<Failure, Unit>(unit);
  }

  @override
  Future<Either<Failure, User>> updateLanguage(String languageCode) async =>
      Right<Failure, User>(testUser.copyWith(language: languageCode));

  @override
  Future<Either<Failure, NotificationPreferences>>
  updateNotificationPreferences(NotificationPreferences preferences) async =>
      Right<Failure, NotificationPreferences>(preferences);
}

class FakeCatalogRepository implements CatalogRepository {
  @override
  Future<Either<Failure, List<City>>> getCities() async =>
      const Right<Failure, List<City>>(<City>[]);

  @override
  Future<Either<Failure, List<DocumentType>>> getDocumentTypes() async =>
      const Right<Failure, List<DocumentType>>(<DocumentType>[]);

  @override
  Future<Either<Failure, List<RideCategory>>> getRideCategories() async =>
      const Right<Failure, List<RideCategory>>(testCategories);
}

class FakeNotificationsRepository implements NotificationsRepository {
  @override
  Future<Either<Failure, NotificationsPage>> getNotifications({
    int page = 1,
  }) async => const Right<Failure, NotificationsPage>(
    NotificationsPage(items: <NotificationItem>[], unreadCount: 0),
  );

  @override
  Future<Either<Failure, Unit>> markRead(List<String>? ids) async =>
      const Right<Failure, Unit>(unit);
}

class FakeRidesRepository implements RidesRepository {
  @override
  Future<Either<Failure, PageResult<TripSummary>>> getTrips({
    String status = 'all',
    int page = 1,
  }) async => const Right<Failure, PageResult<TripSummary>>(
    PageResult<TripSummary>.empty(),
  );
}

class FakeWalletRepository implements WalletRepository {
  double balance = 125;

  @override
  Future<Either<Failure, PageResult<WalletTransaction>>> getTransactions({
    int page = 1,
  }) async => const Right<Failure, PageResult<WalletTransaction>>(
    PageResult<WalletTransaction>.empty(),
  );

  @override
  Future<Either<Failure, WalletSummary>> getWallet() async =>
      Right<Failure, WalletSummary>(
        WalletSummary(
          id: 'w1',
          kind: 'passenger',
          currency: 'SAR',
          balance: balance,
          paymentMethods: const <PaymentMethod>[
            PaymentMethod(type: 'wallet', label: 'محفظة ATA', isDefault: true),
            PaymentMethod(type: 'cash', label: 'الدفع نقداً'),
          ],
        ),
      );

  @override
  Future<Either<Failure, TopUpResult>> topUp({required double amount}) async {
    balance += amount;
    return Right<Failure, TopUpResult>(
      TopUpResult(transactionId: 't1', balance: balance),
    );
  }
}

class FakePassengerRepository implements PassengerRepository {
  bool? lastPreferFemale;

  @override
  Future<Either<Failure, Unit>> updatePreferences({
    bool? preferFemaleDriver,
    String? defaultPaymentMethod,
  }) async {
    lastPreferFemale = preferFemaleDriver;
    return const Right<Failure, Unit>(unit);
  }
}
