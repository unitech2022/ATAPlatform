import 'dart:convert';
import 'dart:typed_data';

import 'package:ata_app/core/di/data_module.dart';
import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/di/use_case_module.dart';
import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/network/api_client.dart';
import 'package:ata_app/core/push/noop_push_service.dart';
import 'package:ata_app/core/storage/preferences_storage.dart';
import 'package:ata_app/features/corporate/data/datasources/corporate_remote_data_source.dart';
import 'package:ata_app/features/corporate/data/repositories/corporate_repository_impl.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_invitation.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_profile.dart';
import 'package:ata_app/features/corporate/domain/usecases/accept_corporate_invitation.dart';
import 'package:ata_app/features/corporate/domain/usecases/check_corporate_eligibility.dart';
import 'package:ata_app/features/corporate/domain/usecases/decline_corporate_invitation.dart';
import 'package:ata_app/features/corporate/domain/usecases/get_corporate_invitations.dart';
import 'package:ata_app/features/corporate/domain/usecases/get_corporate_membership.dart';
import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:fpdart/fpdart.dart';
import 'package:shared_preferences/shared_preferences.dart';

import '../../helpers/fakes.dart';
import 'corporate_models_test.dart' show profileJson;

/// Answers every request with the next scripted response and records it.
class _Adapter implements HttpClientAdapter {
  _Adapter(this.responses);

  final List<ResponseBody> responses;
  final List<RequestOptions> requests = <RequestOptions>[];

  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) async {
    requests.add(options);
    return responses[requests.length - 1];
  }

  @override
  void close({bool force = false}) {}
}

ResponseBody _json(Object? body, int status) => ResponseBody.fromString(
  jsonEncode(body),
  status,
  headers: <String, List<String>>{
    Headers.contentTypeHeader: <String>[Headers.jsonContentType],
  },
);

void main() {
  late PreferencesStorage prefs;

  setUp(() async {
    SharedPreferences.setMockInitialValues(<String, Object>{});
    prefs = PreferencesStorage(await SharedPreferences.getInstance());
  });
  tearDown(getIt.reset);

  CorporateRepositoryImpl repo(_Adapter adapter) {
    final ApiClient api = ApiClient(
      baseUrl: 'http://localhost/api/v1',
      tokens: InMemoryTokenStorage(),
      prefs: prefs,
      onSessionExpired: () {},
    );
    api.dio.httpClientAdapter = adapter;
    return CorporateRepositoryImpl(CorporateRemoteDataSource(api));
  }

  test('GET /passenger/corporate parses the profile', () async {
    final _Adapter adapter = _Adapter(<ResponseBody>[_json(profileJson, 200)]);
    final Either<Failure, CorporateProfile?> result = await repo(
      adapter,
    ).getProfile();
    expect(adapter.requests.single.method, 'GET');
    expect(adapter.requests.single.path, '/passenger/corporate');
    result.fold(
      (Failure f) => fail(f.code),
      (CorporateProfile? p) => expect(p!.membership.companyName, 'شركة المثال'),
    );
  });

  test('a null body is "not a member"', () async {
    final Either<Failure, CorporateProfile?> result = await repo(
      _Adapter(<ResponseBody>[_json(null, 200)]),
    ).getProfile();
    result.fold(
      (Failure f) => fail('expected "not a member", got ${f.code}'),
      (CorporateProfile? profile) => expect(profile, isNull),
    );
  });

  test('403 corporate_not_member is also "not a member"', () async {
    final Either<Failure, CorporateProfile?> result = await repo(
      _Adapter(<ResponseBody>[
        _json(<String, dynamic>{
          'error': <String, dynamic>{
            'code': 'corporate_not_member',
            'message': 'x',
          },
        }, 403),
      ]),
    ).getProfile();
    result.fold(
      (Failure f) => fail('expected "not a member", got ${f.code}'),
      (CorporateProfile? profile) => expect(profile, isNull),
    );
  });

  test('other errors stay failures', () async {
    final Either<Failure, CorporateProfile?> result = await repo(
      _Adapter(<ResponseBody>[
        _json(<String, dynamic>{
          'error': <String, dynamic>{'code': 'server_error', 'message': 'x'},
        }, 500),
      ]),
    ).getProfile();
    expect(result.isLeft(), isTrue);
  });

  test(
    'invitations: list, accept and decline hit the documented paths',
    () async {
      final _Adapter adapter = _Adapter(<ResponseBody>[
        _json(<dynamic>[
          <String, dynamic>{'id': 'i1', 'companyName': 'X', 'role': 'employee'},
        ], 200),
        _json(<String, dynamic>{}, 200),
        _json(<String, dynamic>{}, 200),
      ]);
      final CorporateRepositoryImpl repository = repo(adapter);
      final List<CorporateInvitation> list = (await repository.getInvitations())
          .getOrElse((_) => <CorporateInvitation>[]);
      expect(list.single.id, 'i1');
      expect((await repository.acceptInvitation('i1')).isRight(), isTrue);
      expect((await repository.declineInvitation('i2')).isRight(), isTrue);
      expect(
        adapter.requests.map((RequestOptions r) => '${r.method} ${r.path}'),
        <String>[
          'GET /passenger/corporate/invitations',
          'POST /passenger/corporate/invitations/i1/accept',
          'POST /passenger/corporate/invitations/i2/decline',
        ],
      );
    },
  );

  test(
    'accepting an expired invitation fails with invitation_expired',
    () async {
      final Either<Failure, Unit> result = await repo(
        _Adapter(<ResponseBody>[
          _json(<String, dynamic>{
            'error': <String, dynamic>{
              'code': 'invitation_expired',
              'message': 'x',
            },
          }, 410),
        ]),
      ).acceptInvitation('i1');
      result.fold(
        (Failure f) => expect(f.code, 'invitation_expired'),
        (_) => fail('expected a failure'),
      );
    },
  );

  test('the production wiring resolves every F19 use case', () async {
    await getIt.reset();
    registerCore(
      prefs: prefs,
      tokens: InMemoryTokenStorage(),
      baseUrl: 'http://localhost:5000/api/v1',
      push: const NoopPushService(),
    );
    registerData(hubUrl: 'http://localhost:5000/hubs/trips');
    registerUseCases();
    expect(getIt<GetCorporateMembership>(), isNotNull);
    expect(getIt<GetCorporateInvitations>(), isNotNull);
    expect(getIt<AcceptCorporateInvitation>(), isNotNull);
    expect(getIt<DeclineCorporateInvitation>(), isNotNull);
    expect(getIt<CheckCorporateEligibility>(), isNotNull);
  });
}
