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
  /// Override at build time:
  ///   flutter run --dart-define=API_BASE_URL=http://192.168.1.5:5000
  static const String apiBaseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'http://10.0.2.2:5000',
  );

  static const Duration apiTimeout = Duration(seconds: 30);
}