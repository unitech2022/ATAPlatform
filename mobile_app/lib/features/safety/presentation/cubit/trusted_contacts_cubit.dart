import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/core/utils/phone_number.dart';
import 'package:ata_app/features/safety/domain/entities/trusted_contact.dart';
import 'package:ata_app/features/safety/domain/usecases/add_trusted_contact.dart';
import 'package:ata_app/features/safety/domain/usecases/delete_trusted_contact.dart';
import 'package:ata_app/features/safety/domain/usecases/get_trusted_contacts.dart';
import 'package:ata_app/features/safety/domain/usecases/update_trusted_contact.dart';
import 'package:ata_app/features/safety/presentation/cubit/trusted_contacts_state.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:fpdart/fpdart.dart';

/// Trusted contacts (F12): list, add / edit form with phone validation,
/// delete, the per-contact auto-share toggle and the limit of 5.
class TrustedContactsCubit extends Cubit<TrustedContactsState> {
  TrustedContactsCubit({
    required this._getContacts,
    required this._addContact,
    required this._updateContact,
    required this._deleteContact,
  }) : super(const TrustedContactsState());

  final GetTrustedContacts _getContacts;
  final AddTrustedContact _addContact;
  final UpdateTrustedContact _updateContact;
  final DeleteTrustedContact _deleteContact;

  static const String _phoneField = 'phoneNumber';
  static const String _selfValue = 'self';

  Future<void> load() async {
    emit(state.copyWith(loading: true, clearFailure: true));
    final result = await _getContacts(const NoParams());
    if (isClosed) return;
    result.fold(
      (Failure f) => emit(state.copyWith(loading: false, failure: f)),
      (List<TrustedContact> contacts) =>
          emit(state.copyWith(loading: false, contacts: contacts)),
    );
  }

  void startAdd() {
    if (!state.canAdd) return;
    emit(state.copyWith(form: const ContactForm(), clearFailure: true));
  }

  void startEdit(TrustedContact contact) => emit(
    state.copyWith(
      form: ContactForm(editingId: contact.id, draft: contact.toDraft()),
      clearFailure: true,
    ),
  );

  void cancelForm() => emit(state.copyWith(clearForm: true));

  void nameChanged(String value) => _edit(
    (TrustedContactDraft d) => d.copyWith(name: value),
    ContactFieldError.nameRequired,
  );

  void phoneChanged(String value) => _edit(
    (TrustedContactDraft d) => d.copyWith(phoneNumber: value),
    ContactFieldError.phoneInvalid,
  );

  void relationshipChanged(String value) =>
      _edit((TrustedContactDraft d) => d.copyWith(relationship: value));

  void autoShareChanged(bool value) =>
      _edit((TrustedContactDraft d) => d.copyWith(autoShare: value));

  void notifyOnSosChanged(bool value) =>
      _edit((TrustedContactDraft d) => d.copyWith(notifyOnSos: value));

  Future<void> save() async {
    final ContactForm? form = state.form;
    if (form == null || form.saving) return;
    final String? phone = PhoneNumber.normalize(form.draft.phoneNumber);
    final Set<ContactFieldError> errors = <ContactFieldError>{
      if (form.draft.name.trim().isEmpty) ContactFieldError.nameRequired,
      if (phone == null) ContactFieldError.phoneInvalid,
    };
    if (errors.isNotEmpty) {
      emit(state.copyWith(form: form.copyWith(errors: errors)));
      return;
    }
    final TrustedContactDraft draft = form.draft.copyWith(phoneNumber: phone);
    emit(state.copyWith(form: form.copyWith(saving: true, clearFailure: true)));
    final String? id = form.editingId;
    final Either<Failure, TrustedContact> result = id == null
        ? await _addContact(draft)
        : await _updateContact(
            UpdateTrustedContactParams(id: id, draft: draft),
          );
    if (isClosed) return;
    result.fold(
      (Failure f) => emit(state.copyWith(form: _formFailure(form, f))),
      (TrustedContact saved) =>
          emit(state.copyWith(contacts: _upsert(saved), clearForm: true)),
    );
  }

  Future<void> delete(String id) async {
    if (state.busyId != null) return;
    emit(state.copyWith(busyId: id, clearFailure: true));
    final result = await _deleteContact(id);
    if (isClosed) return;
    result.fold(
      (Failure f) => emit(state.copyWith(clearBusy: true, failure: f)),
      (_) => emit(
        state.copyWith(
          clearBusy: true,
          contacts: state.contacts
              .where((TrustedContact c) => c.id != id)
              .toList(growable: false),
        ),
      ),
    );
  }

  /// Flips auto-share optimistically; reverts on failure.
  Future<void> toggleAutoShare(TrustedContact contact) async {
    if (state.busyId != null) return;
    final TrustedContactDraft draft = contact.toDraft().copyWith(
      autoShare: !contact.autoShare,
    );
    final List<TrustedContact> before = state.contacts;
    emit(
      state.copyWith(
        busyId: contact.id,
        clearFailure: true,
        contacts: _upsert(_withDraft(contact, draft)),
      ),
    );
    final result = await _updateContact(
      UpdateTrustedContactParams(id: contact.id, draft: draft),
    );
    if (isClosed) return;
    result.fold(
      (Failure f) =>
          emit(state.copyWith(clearBusy: true, contacts: before, failure: f)),
      (TrustedContact saved) =>
          emit(state.copyWith(clearBusy: true, contacts: _upsert(saved))),
    );
  }

  void _edit(
    TrustedContactDraft Function(TrustedContactDraft) change, [
    ContactFieldError? clears,
  ]) {
    final ContactForm? form = state.form;
    if (form == null) return;
    emit(
      state.copyWith(
        form: form.copyWith(
          draft: change(form.draft),
          errors: clears == null
              ? form.errors
              : (Set<ContactFieldError>.of(form.errors)
                  ..remove(clears)
                  ..remove(ContactFieldError.phoneSelf)),
          clearFailure: true,
        ),
      ),
    );
  }

  ContactForm _formFailure(ContactForm form, Failure f) {
    final bool self =
        f.code == ErrorCodes.validationFailed &&
        f.details?[_phoneField]?.toString() == _selfValue;
    if (self) {
      return form.copyWith(
        saving: false,
        errors: <ContactFieldError>{ContactFieldError.phoneSelf},
      );
    }
    if (f.code == ErrorCodes.phoneInvalid) {
      return form.copyWith(
        saving: false,
        errors: <ContactFieldError>{ContactFieldError.phoneInvalid},
      );
    }
    return form.copyWith(saving: false, failure: f);
  }

  List<TrustedContact> _upsert(TrustedContact contact) {
    final List<TrustedContact> list = List<TrustedContact>.of(state.contacts);
    final int index = list.indexWhere((TrustedContact c) => c.id == contact.id);
    if (index < 0) {
      list.add(contact);
    } else {
      list[index] = contact;
    }
    return List<TrustedContact>.unmodifiable(list);
  }

  static TrustedContact _withDraft(TrustedContact c, TrustedContactDraft d) =>
      TrustedContact(
        id: c.id,
        name: d.name,
        phoneNumber: d.phoneNumber,
        relationship: d.relationship,
        autoShare: d.autoShare,
        notifyOnSos: d.notifyOnSos,
        createdAt: c.createdAt,
      );
}
