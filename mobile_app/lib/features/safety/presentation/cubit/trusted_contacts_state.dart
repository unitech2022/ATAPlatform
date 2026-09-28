import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/safety/domain/entities/trusted_contact.dart';
import 'package:equatable/equatable.dart';

/// Field errors of the contact form.
enum ContactFieldError { nameRequired, phoneInvalid, phoneSelf }

/// The add / edit form (inline on the contacts page).
class ContactForm extends Equatable {
  const ContactForm({
    this.editingId,
    this.draft = const TrustedContactDraft(),
    this.errors = const <ContactFieldError>{},
    this.saving = false,
    this.failure,
  });

  /// `null` when adding.
  final String? editingId;
  final TrustedContactDraft draft;
  final Set<ContactFieldError> errors;
  final bool saving;
  final Failure? failure;

  bool get isEditing => editingId != null;

  ContactForm copyWith({
    TrustedContactDraft? draft,
    Set<ContactFieldError>? errors,
    bool? saving,
    Failure? failure,
    bool clearFailure = false,
  }) => ContactForm(
    editingId: editingId,
    draft: draft ?? this.draft,
    errors: errors ?? this.errors,
    saving: saving ?? this.saving,
    failure: clearFailure ? null : failure ?? this.failure,
  );

  @override
  List<Object?> get props => <Object?>[
    editingId,
    draft,
    errors,
    saving,
    failure,
  ];
}

/// State of `TrustedContactsCubit`.
class TrustedContactsState extends Equatable {
  const TrustedContactsState({
    this.loading = false,
    this.contacts = const <TrustedContact>[],
    this.form,
    this.busyId,
    this.failure,
  });

  final bool loading;
  final List<TrustedContact> contacts;
  final ContactForm? form;

  /// Contact being deleted / toggled.
  final String? busyId;
  final Failure? failure;

  /// At most [TrustedContact.maxPerUser] contacts.
  bool get canAdd => contacts.length < TrustedContact.maxPerUser;

  TrustedContactsState copyWith({
    bool? loading,
    List<TrustedContact>? contacts,
    ContactForm? form,
    String? busyId,
    Failure? failure,
    bool clearForm = false,
    bool clearBusy = false,
    bool clearFailure = false,
  }) => TrustedContactsState(
    loading: loading ?? this.loading,
    contacts: contacts ?? this.contacts,
    form: clearForm ? null : form ?? this.form,
    busyId: clearBusy ? null : busyId ?? this.busyId,
    failure: clearFailure ? null : failure ?? this.failure,
  );

  @override
  List<Object?> get props => <Object?>[
    loading,
    contacts,
    form,
    busyId,
    failure,
  ];
}
