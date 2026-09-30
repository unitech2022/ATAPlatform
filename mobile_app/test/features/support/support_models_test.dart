import 'package:ata_app/core/models/page_result.dart';
import 'package:ata_app/features/notifications/domain/entities/notification_types.dart';
import 'package:ata_app/features/notifications/domain/usecases/parse_deep_link.dart';
import 'package:ata_app/features/safety/data/models/safety_case_models.dart';
import 'package:ata_app/features/support/data/models/help_models.dart';
import 'package:ata_app/features/support/data/models/ticket_models.dart';
import 'package:ata_app/features/support/domain/entities/attachments.dart';
import 'package:ata_app/features/support/domain/entities/fare_dispute.dart';
import 'package:ata_app/features/support/domain/entities/help.dart';
import 'package:ata_app/features/support/domain/entities/ticket.dart';
import 'package:ata_app/features/support/domain/entities/ticket_enums.dart';
import 'package:ata_app/features/support/domain/entities/ticket_requests.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('help models', () {
    test('a category and an article summary', () {
      final HelpCategory c = HelpModels.category(<String, dynamic>{
        'id': 'c1',
        'code': 'trips',
        'name': 'الرحلات',
        'icon': 'car',
        'articlesCount': 4,
      });
      expect(c.code, 'trips');
      expect(c.articlesCount, 4);

      final HelpArticleSummary s = HelpModels.summary(<String, dynamic>{
        'id': 'a1',
        'slug': 'schedule-a-ride',
        'title': 'كيف أجدول رحلة؟',
        'excerpt': 'يمكنك…',
        'categoryId': 'c1',
        'updatedAt': '2026-09-01T10:00:00Z',
      });
      expect(s.slug, 'schedule-a-ride');
      expect(s.categoryId, 'c1');
      expect(s.updatedAt, DateTime.utc(2026, 9, 1, 10));
    });

    test('an article with its category, tags and related articles', () {
      final HelpArticle a = HelpModels.article(<String, dynamic>{
        'id': 'a1',
        'slug': 's',
        'title': 'T',
        'body': '## H',
        'category': <String, dynamic>{'id': 'c1', 'name': 'الرحلات'},
        'tags': <Object>['جدولة', 7],
        'related': <Map<String, dynamic>>[
          <String, dynamic>{'slug': 'r', 'title': 'R'},
        ],
      });
      expect(a.body, '## H');
      expect(a.categoryName, 'الرحلات');
      expect(a.tags, <String>['جدولة', '7']);
      expect(a.related.single.slug, 'r');
    });

    test('missing fields fall back to empty values', () {
      final HelpArticle a = HelpModels.article(<String, dynamic>{'id': 'x'});
      expect(a.body, isEmpty);
      expect(a.related, isEmpty);
      expect(a.categoryName, isNull);
    });
  });

  group('tolerant paging', () {
    Map<String, dynamic> row(String id) => <String, dynamic>{
      'id': id,
      'ticketNumber': 'ST-$id',
      'subject': 's',
    };

    test('a bare array is one page', () {
      final PageResult<TicketSummary> p = PageResult.fromAny(<Object>[
        row('1'),
        row('2'),
        'junk',
      ], TicketModels.summary);
      expect(p.items.map((TicketSummary t) => t.id), <String>['1', '2']);
      expect(p.hasMore, isFalse);
    });

    test('the page envelope keeps its paging fields', () {
      final PageResult<TicketSummary> p = PageResult.fromAny(<String, dynamic>{
        'items': <Object>[row('1')],
        'page': 1,
        'pageSize': 1,
        'total': 3,
      }, TicketModels.summary);
      expect(p.items, hasLength(1));
      expect(p.hasMore, isTrue);
    });

    test('anything else is an empty page', () {
      expect(
        PageResult<TicketSummary>.fromAny(null, TicketModels.summary).items,
        isEmpty,
      );
      expect(
        PageResult<TicketSummary>.fromAny('x', TicketModels.summary).items,
        isEmpty,
      );
    });
  });

  group('ticket models', () {
    test('a summary with the unread counter', () {
      final TicketSummary t = TicketModels.summary(<String, dynamic>{
        'id': 't1',
        'ticketNumber': 'ST-20260930-00001',
        'type': 'payment_issue',
        'subject': 'مبلغ أعلى',
        'status': 'pending_user',
        'tripNumber': 'T-1',
        'lastMessageAt': '2026-09-30T09:00:00Z',
        'unread': 2,
        'createdAt': '2026-09-30T08:00:00Z',
      });
      expect(t.type, TicketType.paymentIssue);
      expect(t.status, TicketStatus.pendingUser);
      expect(t.unread, 2);
      expect(t.hasUnread, isTrue);
      expect(t.copyWith(unread: 0).hasUnread, isFalse);
    });

    test('unknown enum values are tolerated', () {
      final TicketSummary t = TicketModels.summary(<String, dynamic>{
        'id': 't',
        'type': 'weird',
        'status': 'weird',
      });
      expect(t.type, TicketType.other);
      expect(t.status, TicketStatus.open);
    });

    test('a detail sorts its messages and maps attachments and dispute', () {
      final TicketDetail d = TicketModels.detail(<String, dynamic>{
        'id': 't1',
        'ticketNumber': 'ST-1',
        'type': 'payment_issue',
        'subject': 's',
        'status': 'resolved',
        'priority': 'high',
        'trip': <String, dynamic>{
          'id': 'trip1',
          'tripNumber': 'T-9',
          'completedAt': '2026-09-28T10:00:00Z',
        },
        'messages': <Map<String, dynamic>>[
          <String, dynamic>{
            'id': 'm2',
            'authorRole': 'agent',
            'authorName': 'فريق دعم ATA',
            'body': 'ثانياً',
            'createdAt': '2026-09-30T09:00:00Z',
            'attachments': <Map<String, dynamic>>[
              <String, dynamic>{
                'fileId': 'f1',
                'fileName': 'a.png',
                'contentType': 'image/png',
              },
              <String, dynamic>{
                'fileId': 'f2',
                'fileName': 'a.pdf',
                'contentType': 'application/pdf',
              },
            ],
          },
          <String, dynamic>{
            'id': 'm1',
            'authorRole': 'user',
            'body': 'أولاً',
            'createdAt': '2026-09-30T08:00:00Z',
          },
        ],
        'dispute': <String, dynamic>{
          'reason': 'overcharged',
          'chargedAmount': 38.5,
          'requestedRefundAmount': 12,
          'status': 'partially_approved',
          'resolution': 'refund_partial',
          'approvedRefundAmount': 8,
        },
        'canReply': true,
        'canRate': true,
        'csatScore': null,
        'createdAt': '2026-09-30T08:00:00Z',
        'resolvedAt': '2026-09-30T09:30:00Z',
      });
      expect(d.messages.map((TicketMessage m) => m.id), <String>['m1', 'm2']);
      expect(d.messages.first.isMine, isTrue);
      expect(d.messages.last.isAgent, isTrue);
      expect(d.messages.last.attachments.first.isImage, isTrue);
      expect(d.messages.last.attachments.last.isImage, isFalse);
      expect(d.trip?.tripNumber, 'T-9');
      expect(d.dispute?.status, DisputeStatus.partiallyApproved);
      expect(d.dispute?.approvedRefundAmount, 8);
      expect(d.dispute?.requestedRefundAmount, 12);
      expect(d.ratable, isTrue);
      expect(d.replyEnabled, isTrue);
    });

    test(
      'canReply defaults from the status and a rated ticket is not ratable',
      () {
        final TicketDetail closed = TicketModels.detail(<String, dynamic>{
          'id': 't',
          'status': 'closed',
          'canRate': true,
        });
        expect(closed.canReply, isFalse);
        expect(closed.replyEnabled, isFalse);
        expect(closed.ratable, isTrue);

        final TicketDetail rated = TicketModels.detail(<String, dynamic>{
          'id': 't',
          'status': 'resolved',
          'canRate': true,
          'csatScore': 4,
        });
        expect(rated.ratable, isFalse);

        final TicketDetail open = TicketModels.detail(<String, dynamic>{
          'id': 't',
          'status': 'open',
          'canRate': true,
        });
        expect(open.ratable, isFalse);
      },
    );

    test('request bodies follow the API', () {
      expect(
        TicketModels.createBody(
          const NewTicketRequest(
            type: TicketType.paymentIssue,
            subject: ' مبلغ ',
            message: ' تفاصيل ',
            tripId: 'trip1',
            fileIds: <String>['f1'],
            dispute: DisputeDraft(
              reason: DisputeReason.overcharged,
              requestedRefundAmount: 12,
            ),
          ),
        ),
        <String, dynamic>{
          'type': 'payment_issue',
          'tripId': 'trip1',
          'subject': 'مبلغ',
          'message': 'تفاصيل',
          'fileIds': <String>['f1'],
          'dispute': <String, dynamic>{
            'reason': 'overcharged',
            'requestedRefundAmount': 12.0,
          },
        },
      );
      expect(
        TicketModels.createBody(
          const NewTicketRequest(
            type: TicketType.account,
            subject: 's',
            message: 'm',
          ),
        ),
        <String, dynamic>{'type': 'account', 'subject': 's', 'message': 'm'},
      );
      expect(
        TicketModels.replyBody(
          const TicketReplyRequest(ticketId: 't', body: ' hi '),
        ),
        <String, dynamic>{'body': 'hi'},
      );
      expect(
        TicketModels.ratingBody(
          const TicketRatingRequest(ticketId: 't', score: 5, comment: ' '),
        ),
        <String, dynamic>{'score': 5},
      );
      expect(
        TicketModels.ratingBody(
          const TicketRatingRequest(ticketId: 't', score: 3, comment: 'ok'),
        ),
        <String, dynamic>{'score': 3, 'comment': 'ok'},
      );
    });

    test('the upload result and the trip requirement per type', () {
      final UploadedAttachment u = TicketModels.uploaded(<String, dynamic>{
        'fileId': 'f1',
        'fileName': 'a.jpg',
        'contentType': 'image/jpeg',
        'sizeBytes': 2048,
      });
      expect(u.fileId, 'f1');
      expect(u.sizeBytes, 2048);
      expect(TicketType.tripIssue.needsTrip, isTrue);
      expect(TicketType.paymentIssue.needsTrip, isTrue);
      expect(TicketType.lostItem.needsTrip, isTrue);
      expect(TicketType.safety.needsTrip, isFalse);
      expect(TicketType.account.needsTrip, isFalse);
      expect(TicketType.other.needsTrip, isFalse);
    });

    test('attachment content types come from the extension', () {
      expect(AttachmentRules.contentTypeOf('a.JPG'), 'image/jpeg');
      expect(AttachmentRules.contentTypeOf('a.png'), 'image/png');
      expect(AttachmentRules.contentTypeOf('a.pdf'), 'application/pdf');
      expect(AttachmentRules.contentTypeOf('a.exe'), isNull);
      expect(AttachmentRules.contentTypeOf('noext'), isNull);
    });
  });

  group('links from F12 and notifications', () {
    test('a safety case carries its support ticket', () {
      expect(
        SafetyCaseModel.fromJson(<String, dynamic>{
          'id': 'c1',
          'caseNumber': 'SC-1',
          'supportTicketId': 'st1',
        }).supportTicketId,
        'st1',
      );
      expect(
        SafetyCaseModel.fromJson(<String, dynamic>{
          'id': 'c1',
          'caseNumber': 'SC-1',
        }).supportTicketId,
        isNull,
      );
    });

    test('ata://support links open the ticket for each role', () {
      const ParseDeepLink parse = ParseDeepLink();
      expect(
        parse(
          const DeepLinkParams(
            link: 'ata://support/tickets/k1',
            isDriver: false,
          ),
        )?.route,
        '/support/tickets/k1',
      );
      expect(
        parse(
          const DeepLinkParams(
            link: 'ata://support/tickets/k1',
            isDriver: true,
          ),
        )?.route,
        '/driver/support/tickets/k1',
      );
      expect(
        parse(
          const DeepLinkParams(link: 'ata://support', isDriver: true),
        )?.route,
        '/driver/support',
      );
      expect(
        parse(
          const DeepLinkParams(link: 'ata://support', isDriver: false),
        )?.route,
        '/support',
      );
    });

    test('older support rows derive their link from the ticket id', () {
      expect(
        NotificationTypes.fallbackLink('support.reply', <String, dynamic>{
          'ticketId': 't9',
        }),
        'ata://support/tickets/t9',
      );
      expect(
        NotificationTypes.fallbackLink('support.status', <String, dynamic>{
          'ticketId': 't9',
        }),
        'ata://support/tickets/t9',
      );
      expect(
        NotificationTypes.fallbackLink('lost_item.update', <String, dynamic>{
          'ticketId': 't9',
        }),
        'ata://support/tickets/t9',
      );
      expect(NotificationTypes.categoryOf('support.reply'), 'support');
    });
  });
}
