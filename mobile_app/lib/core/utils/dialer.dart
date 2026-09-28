import 'package:flutter/material.dart';
import 'package:url_launcher/url_launcher.dart';

/// Opens the phone dialer on `tel:[number]`; shows [failedText] when no
/// app can handle it.
Future<void> dialNumber(
  BuildContext context,
  String number, {
  required String failedText,
}) async {
  final ScaffoldMessengerState messenger = ScaffoldMessenger.of(context);
  final Uri uri = Uri(scheme: 'tel', path: number.replaceAll(' ', ''));
  final bool ok = await launchUrl(uri);
  if (!ok) messenger.showSnackBar(SnackBar(content: Text(failedText)));
}
