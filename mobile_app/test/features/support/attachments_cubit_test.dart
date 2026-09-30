import 'package:ata_app/features/support/domain/entities/attachments.dart';
import 'package:ata_app/features/support/domain/usecases/download_support_file.dart';
import 'package:ata_app/features/support/domain/usecases/pick_support_attachments.dart';
import 'package:ata_app/features/support/domain/usecases/upload_support_attachment.dart';
import 'package:ata_app/features/support/presentation/cubit/attachment_file_cubit.dart';
import 'package:ata_app/features/support/presentation/cubit/attachments_cubit.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/support_fakes.dart';

void main() {
  late FakeSupportRepository repo;
  late FakeAttachmentPicker picker;
  late AttachmentsCubit cubit;

  setUp(() {
    repo = FakeSupportRepository();
    picker = FakeAttachmentPicker();
    cubit = AttachmentsCubit(
      pick: PickSupportAttachments(picker),
      upload: UploadSupportAttachment(repo),
    );
  });
  tearDown(() => cubit.close());

  Future<void> settle() => Future<void>.delayed(Duration.zero);

  List<PickedAttachment> files(int n) => <PickedAttachment>[
    for (int i = 0; i < n; i++) pickedImage('p$i.jpg'),
  ];

  test('a picked file is uploaded and its file id kept', () async {
    picker.next = <PickedAttachment>[pickedImage('a.jpg')];
    await cubit.pick();
    expect(cubit.state.uploading, isTrue);
    await settle();
    expect(cubit.state.uploading, isFalse);
    expect(cubit.state.items.single.status, AttachmentStatus.uploaded);
    expect(cubit.state.fileIds, <String>['f1']);
    expect(repo.uploads.single.name, 'a.jpg');
  });

  test('asks the picker only for the room that is left', () async {
    picker.next = files(2);
    await cubit.pick();
    await settle();
    picker.next = <PickedAttachment>[];
    await cubit.pick();
    expect(picker.requestedCounts, <int>[5, 3]);
  });

  test('at most five attachments: the sixth is refused', () async {
    picker.next = files(5);
    await cubit.pick();
    await settle();
    expect(cubit.state.items, hasLength(5));
    expect(cubit.state.canAdd, isFalse);

    picker.next = <PickedAttachment>[pickedImage('extra.jpg')];
    await cubit.pick();
    expect(cubit.state.items, hasLength(5));
    expect(cubit.state.failure?.code, 'attachment_limit');
    expect(repo.uploads, hasLength(5));
    // The picker was not even opened.
    expect(picker.requestedCounts, <int>[5]);
  });

  test(
    'picking more files than there is room for keeps the first ones',
    () async {
      picker.next = files(2);
      await cubit.pick();
      await settle();
      picker.next = files(4);
      await cubit.pick();
      await settle();
      expect(cubit.state.items, hasLength(5));
      expect(cubit.state.failure?.code, 'attachment_limit');
      expect(cubit.state.fileIds, hasLength(5));
    },
  );

  test(
    'unsupported types and big files are refused, the rest is kept',
    () async {
      picker.next = <PickedAttachment>[
        const PickedAttachment(name: 'virus.exe', sizeBytes: 10, path: '/x'),
        const PickedAttachment(
          name: 'huge.pdf',
          sizeBytes: AttachmentRules.maxBytes + 1,
          path: '/y',
        ),
        pickedImage('ok.png'),
      ];
      await cubit.pick();
      await settle();
      expect(cubit.state.items.single.file.name, 'ok.png');
      expect(cubit.state.failure?.code, 'unsupported_file_type');
      expect(repo.uploads.single.name, 'ok.png');

      picker.next = <PickedAttachment>[
        const PickedAttachment(
          name: 'huge.pdf',
          sizeBytes: AttachmentRules.maxBytes + 1,
          path: '/y',
        ),
      ];
      await cubit.pick();
      expect(cubit.state.failure?.code, 'file_too_large');
    },
  );

  test('exactly 10 MB is allowed', () async {
    picker.next = <PickedAttachment>[
      const PickedAttachment(
        name: 'edge.pdf',
        sizeBytes: AttachmentRules.maxBytes,
        path: '/z',
      ),
    ];
    await cubit.pick();
    await settle();
    expect(cubit.state.fileIds, hasLength(1));
    expect(cubit.state.failure, isNull);
  });

  test('cancelling the chooser changes nothing', () async {
    await cubit.pick();
    expect(cubit.state.items, isEmpty);
    expect(cubit.state.failure, isNull);
  });

  test('a failed upload can be retried', () async {
    repo.uploadFailure = apiFailure('unsupported_file_type');
    picker.next = <PickedAttachment>[pickedImage('a.jpg')];
    await cubit.pick();
    await settle();
    expect(cubit.state.hasFailed, isTrue);
    expect(cubit.state.items.single.failure?.code, 'unsupported_file_type');
    expect(cubit.state.fileIds, isEmpty);

    repo.uploadFailure = null;
    final int id = cubit.state.items.single.localId;
    await cubit.retry(id);
    expect(cubit.state.hasFailed, isFalse);
    expect(cubit.state.fileIds, hasLength(1));
  });

  test('removing a file frees its slot and drops its id', () async {
    picker.next = files(2);
    await cubit.pick();
    await settle();
    cubit.remove(cubit.state.items.first.localId);
    expect(cubit.state.items, hasLength(1));
    expect(cubit.state.fileIds, <String>['f2']);
    expect(cubit.state.canAdd, isTrue);
  });

  test('an upload that finishes after removal is ignored', () async {
    picker.next = <PickedAttachment>[pickedImage('a.jpg')];
    await cubit.pick();
    cubit.remove(cubit.state.items.single.localId);
    await settle();
    expect(cubit.state.items, isEmpty);
  });

  test('clear forgets everything after a message was sent', () async {
    picker.next = files(2);
    await cubit.pick();
    await settle();
    cubit.clear();
    expect(cubit.state.items, isEmpty);
    expect(cubit.state.fileIds, isEmpty);
  });

  group('AttachmentFileCubit', () {
    AttachmentFileCubit file() =>
        AttachmentFileCubit(download: DownloadSupportFile(repo), fileId: 'f1');

    test('fetches the bytes once', () async {
      final AttachmentFileCubit c = file();
      await c.load();
      expect(c.state.bytes, isNotNull);
      expect(c.state.loading, isFalse);
      await c.load();
      await c.close();
    });

    test('a failure is kept and can be retried', () async {
      repo.fileFailure = apiFailure('http_403');
      final AttachmentFileCubit c = file();
      await c.load();
      expect(c.state.failure?.code, 'http_403');
      expect(c.state.bytes, isNull);
      repo.fileFailure = null;
      await c.load();
      expect(c.state.bytes, isNotNull);
      await c.close();
    });
  });
}
