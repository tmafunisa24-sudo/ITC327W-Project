import 'package:flutter/foundation.dart';

// lib/core/config/env.dart

class Env {
  Env._();

  /// Backend base URL.
  ///
  /// ┌──────────────────────┬────────────────────────────┐
  /// │ Platform             │ Base URL                   │
  /// ├──────────────────────┼────────────────────────────┤
  /// │ Android emulator     │ http://10.0.2.2:5000       │
  /// │ iOS simulator        │ http://localhost:5000      │
  /// │ Physical device      │ http://[LAN-IP]:5000       │
  /// │ Flutter web (dev)    │ http://localhost:5000      │
  /// └──────────────────────┴────────────────────────────┘
  ///
  /// Override for a physical device with the PC's LAN IP, or for a custom
  /// backend address:
  ///   flutter run --dart-define=API_BASE_URL=http://192.168.1.5:5000
  static final String apiBaseUrl = const String.fromEnvironment(
    'API_BASE_URL',
  ).isNotEmpty
      ? const String.fromEnvironment('API_BASE_URL')
      : !kIsWeb && defaultTargetPlatform == TargetPlatform.android
          ? 'http://10.0.2.2:5000'
          : 'http://localhost:5000';

  static const Duration apiTimeout = Duration(seconds: 30);
}
