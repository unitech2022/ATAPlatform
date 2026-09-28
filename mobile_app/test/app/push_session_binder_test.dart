import 'dart:async';

import 'package:ata_app/app/push/push_session_binder.dart';
import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/push/push_event.dart';
import 'package:ata_app/core/push/push_service.dart';
import 'package:ata_app/features/account/domain/usecases/register_push_device.dart';
import 'package:ata_app/features/auth/domain/entities/auth_session.dart';
import 'package:ata_app/features/auth/domain/entities/user_role.dart';
import 'package:ata_app/features/auth/presentation/cubit/session_state.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:fpdart/fpdart.dart';

import '../helpers/fakes.dart';

/// Records every provider call.
class FakePushService implements PushService {
  final List<String> calls = <String>[];
  final List<Map<String, String>> tags = <Map<String, String>>[];

  @override
  bool get isEnabled => true;

  @override
  String? get subscriptionId => 'sub-1';

  @override
  Stream<PushEvent> get opened => const Stream<PushEvent>.empty();

  @override
  Stream<PushEvent> get received => const Stream<PushEvent>.empty();

  @override
  Future<void> initialize() async => calls.add('initialize');

  @override
  Future<void> login(String userId) async => calls.add('login:$userId');

  @override
  Future<void> logout() async => calls.add('logout');

  @override
  Future<void> setTags(Map<String, String> value) async {
    calls.add('tags');
    tags.add(value);
  }

  @override
  Future<void> setLanguage(String languageCode) async =>
      calls.add('language:$languageCode');

  @override
  Future<bool> requestPermission() async {
    calls.add('permission');
    return true;
  }
}

class _RecordingAccountRepository extends FakeAccountRepository {
  final List<String?> tokens = <String?>[];

  @override
  Future<Either<Failure, Unit>> registerDevice({String? pushToken}) {
    tokens.add(pushToken);
    return super.registerDevice(pushToken: pushToken);
  }
}

void main() {
  late FakePushService push;
  late _RecordingAccountRepository account;
  late StreamController<SessionState> sessions;
  late StreamController<String> languages;
  late PushSessionBinder binder;

  const AuthSession driverSession = AuthSession(
    accessToken: 'a',
    refreshToken: 'r',
    isNewUser: false,
    user: testUser,
    activeRole: UserRole.driver,
  );

  setUp(() {
    push = FakePushService();
    account = _RecordingAccountRepository();
    sessions = StreamController<SessionState>();
    languages = StreamController<String>();
    binder = PushSessionBinder(
      push: push,
      registerDevice: RegisterPushDevice(account),
    );
  });

  tearDown(() async {
    await binder.close();
    await sessions.close();
    await languages.close();
  });

  Future<void> settle() async {
    await Future<void>.delayed(Duration.zero);
    await binder.settled;
  }

  void bind(SessionState initial) => binder.bind(
    session: initial,
    sessionChanges: sessions.stream,
    language: 'ar',
    languageChanges: languages.stream,
  );

  test('nothing happens before the session is known', () async {
    bind(const SessionState.unknown());
    await settle();
    expect(push.calls, isEmpty);
  });

  test('sign-in logs in, tags, sets language, prompts, registers', () async {
    bind(const SessionState.unauthenticated());
    sessions.add(const SessionState.authenticated(testSession));
    await settle();
    expect(push.calls, <String>[
      'login:u1',
      'tags',
      'language:ar',
      'permission',
    ]);
    expect(push.tags.single, <String, String>{
      'role': 'passenger',
      'lang': 'ar',
      'city': 'riyadh',
    });
    expect(account.tokens, <String?>['sub-1']);
  });

  test('a restored session is bound once, even if re-emitted', () async {
    bind(const SessionState.authenticated(testSession));
    sessions.add(const SessionState.authenticated(testSession));
    await settle();
    expect(push.calls.where((String c) => c.startsWith('login')).length, 1);
  });

  test('role and language changes update the tags', () async {
    bind(const SessionState.authenticated(testSession));
    await settle();
    push.calls.clear();
    push.tags.clear();
    sessions.add(const SessionState.authenticated(driverSession));
    languages.add('en');
    await settle();
    expect(push.tags, <Map<String, String>>[
      <String, String>{'role': 'driver'},
      <String, String>{'lang': 'en'},
    ]);
    expect(push.calls, contains('language:en'));
  });

  test('language changes while signed out are only remembered', () async {
    bind(const SessionState.unauthenticated());
    languages.add('en');
    await settle();
    expect(push.calls, isEmpty);
    sessions.add(const SessionState.authenticated(testSession));
    await settle();
    expect(push.tags.single['lang'], 'en');
  });

  test('sign-out logs out once', () async {
    bind(const SessionState.authenticated(testSession));
    sessions
      ..add(const SessionState.unauthenticated())
      ..add(const SessionState.unauthenticated());
    await settle();
    expect(push.calls.where((String c) => c == 'logout').length, 1);
  });

  test('signing out without a bound user does not call logout', () async {
    bind(const SessionState.unauthenticated());
    sessions.add(const SessionState.unauthenticated());
    await settle();
    expect(push.calls, isEmpty);
  });
}
