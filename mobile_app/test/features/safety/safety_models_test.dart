import 'package:ata_app/features/safety/data/models/safety_case_models.dart';
import 'package:ata_app/features/safety/data/models/safety_models.dart';
import 'package:ata_app/features/safety/domain/entities/lost_item.dart';
import 'package:ata_app/features/safety/domain/entities/safety_alert.dart';
import 'package:ata_app/features/safety/domain/entities/sos.dart';
import 'package:ata_app/features/safety/domain/entities/trip_share.dart';
import 'package:ata_app/features/safety/domain/entities/trusted_contact.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('create-share response reads the "shares" array', () {
    final List<TripShare> shares = SafetyJson.list(<String, dynamic>{
      'shares': <Map<String, dynamic>>[
        <String, dynamic>{
          'id': 's1',
          'url': 'https://ata.sa/t/Xy',
          'channel': 'sms',
          'trustedContactId': 'c1',
          'expiresAt': null,
        },
      ],
    }, key: 'shares').map(TripShareModel.fromJson).toList();
    expect(shares.single.url, 'https://ata.sa/t/Xy');
    expect(shares.single.trustedContactId, 'c1');
    expect(shares.single.isRevoked, isFalse);
  });

  test('SOS result, contacts and drafts map the documented fields', () {
    final SosResult sos = SosModel.resultFromJson(<String, dynamic>{
      'caseId': 'k1',
      'caseNumber': 'SC-20260928-0007',
      'status': 'open',
      'emergencyNumber': '911',
      'contactsNotified': 2,
    });
    expect(sos.contactsNotified, 2);
    expect(
      TrustedContactModel.toJson(
        const TrustedContactDraft(
          name: ' سارة ',
          phoneNumber: '+966551234567',
          relationship: ' ',
        ),
      ),
      <String, dynamic>{
        'name': 'سارة',
        'phoneNumber': '+966551234567',
        'autoShare': false,
        'notifyOnSos': true,
      },
    );
  });

  test('safety.check push data yields the alert id from the deep link', () {
    final SafetyAlert? alert = SafetyAlertModel.fromPush(<String, dynamic>{
      'eventCode': 'safety.check',
      'deepLink': 'ata://safety/check/a42',
      'tripId': 't1',
      'alertType': 'route_deviation',
    });
    expect(alert?.id, 'a42');
    expect(alert?.type, 'route_deviation');
    expect(alert?.isPending, isTrue);
  });

  test('lost item report parses the category and status', () {
    final LostItemReport r = LostItemModel.fromJson(<String, dynamic>{
      'id': 'l1',
      'reportNumber': 'LI-20260928-0003',
      'tripNumber': 'T-1',
      'itemCategory': 'phone',
      'description': 'آيفون أسود',
      'status': 'open',
      'driverResponse': null,
    });
    expect(r.itemCategory, LostItemCategory.phone);
    expect(r.awaitsDriver, isTrue);
    expect(r.answered(found: true).status, 'found');
  });
}
