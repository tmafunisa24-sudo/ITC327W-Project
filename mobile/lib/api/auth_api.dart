// lib/api/auth_api.dart

import 'package:dio/dio.dart';
import '../core/network/api_client.dart';
import '../core/network/api_exception.dart';
import '../models/profile.dart';

class AuthApi {
  final Dio _dio;
  AuthApi([Dio? dio]) : _dio = dio ?? ApiClient.dio;

  /// POST /api/auth/login
  ///
  /// On success, the server sets the ServiceHubITAuth cookie.
  /// The CookieManager interceptor stores it automatically.
  ///
  /// Returns the authenticated [Profile].
  /// Throws [ApiException] on failure.
  Future<Profile> login({
    required String email,
    required String password,
    bool rememberMe = false,
  }) async {
    final response = await _dio.post(
      '/api/auth/login',
      data: {
        'email': email,
        'password': password,
        'rememberMe': rememberMe,
      },
    );

    final status = response.statusCode ?? 0;

    if (status == 200) {
      final data = response.data as Map<String, dynamic>;
      final userJson = data['user'] as Map<String, dynamic>;
      return Profile.fromJson(userJson);
    }

    throw ApiException(
      _extractError(response) ?? 'Login failed.',
      statusCode: status,
    );
  }

  /// GET /api/auth/me
  ///
  /// Returns the current [Profile] if authenticated.
  /// Throws [ApiException] with statusCode 401 if not.
  Future<Profile> me() async {
    final response = await _dio.get('/api/auth/me');
    final status = response.statusCode ?? 0;

    if (status == 200) {
      return Profile.fromJson(response.data as Map<String, dynamic>);
    }

    throw ApiException(
      _extractError(response) ?? 'Not authenticated.',
      statusCode: status,
    );
  }

  /// POST /api/auth/logout
  ///
  /// Clears the server cookie and locally wipes the cookie jar.
  /// Never throws — logout should always succeed from the client's view.
  Future<void> logout() async {
    try {
      await _dio.post('/api/auth/logout');
    } catch (_) {
      // Ignore — we clear cookies locally anyway
    } finally {
      await ApiClient.clearCookies();
    }
  }

  /// PATCH /api/auth/me
  ///
  /// Updates the current user's own profile.
  /// Only name, phone, and position are editable.
  /// Throws [ApiException] on failure.
  Future<Profile> updateProfile({
    String? fullName,
    String? phone,
    String? position,
  }) async {
    final payload = <String, dynamic>{};
    if (fullName != null) payload['fullName'] = fullName;
    if (phone != null) payload['phone'] = phone;
    if (position != null) payload['position'] = position;

    final response = await _dio.patch(
      '/api/auth/me',
      data: payload,
    );

    final code = response.statusCode ?? 0;

    if (code == 200) {
      final data = response.data;
      if (data is Map<String, dynamic>) {
        return Profile.fromJson(data);
      }
      throw ApiException('Invalid response.', statusCode: code);
    }

    throw ApiException(
      _extractError(response) ?? 'Unable to update your profile.',
      statusCode: code,
    );
  }

  /// POST /api/auth/register
  ///
  /// Creates a new account. Returns a record:
  /// - `(email, requiresConfirmation, null)` on success
  /// - `(null, false, errorMessage)` on failure
  Future<(String?, bool, String?)> register({
    required String email,
    required String password,
    required String fullName,
    String? phone,
  }) async {
    final response = await _dio.post(
      '/api/auth/register',
      data: {
        'email': email,
        'password': password,
        'fullName': fullName,
        if (phone != null && phone.isNotEmpty) 'phone': phone,
      },
    );

    final status = response.statusCode ?? 0;

    if (status == 200) {
      final data = response.data as Map<String, dynamic>;
      final success = data['success'] as bool? ?? false;
      final email = data['email'] as String? ?? '';
      final requiresConfirmation =
          data['requiresConfirmation'] as bool? ?? false;

      if (success) {
        return (email, requiresConfirmation, null);
      }

      return (null, false, data['message'] as String? ?? 'Registration failed.');
    }

    throw ApiException(
      _extractError(response) ?? 'Registration failed.',
      statusCode: status,
    );
  }

  /// POST /api/auth/forgot-password
  ///
  /// Sends a password-reset email via Supabase.
  /// Always returns success to avoid leaking whether the email exists.
  Future<void> forgotPassword({required String email}) async {
    final response = await _dio.post(
      '/api/auth/forgot-password',
      data: {'email': email},
    );

    final status = response.statusCode ?? 0;

    if (status == 200) return;

    throw ApiException(
      _extractError(response) ?? 'Unable to send reset email.',
      statusCode: status,
    );
  }

  // ────────────────────────────────────────────────────────
  // Helpers
  // ────────────────────────────────────────────────────────

  String? _extractError(Response response) {
    final data = response.data;
    if (data is Map<String, dynamic>) {
      if (data['error'] is String) return data['error'] as String;
      if (data['detail'] is String) return data['detail'] as String;
      if (data['title'] is String) return data['title'] as String;
    }
    return null;
  }
}