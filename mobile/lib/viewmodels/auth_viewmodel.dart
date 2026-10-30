// Summary: Manages authentication state, sign-in, sign-out, and the current user profile.
// lib/viewmodels/auth_viewmodel.dart

import 'package:flutter/foundation.dart';
import '../models/profile.dart';
import '../services/auth_service.dart';

/// Where the user is in the sign-in process.
/// [unknown] until [AuthViewModel.bootstrap] finishes at app startup.

enum AuthState {
  unknown,
  authenticated,
  unauthenticated,
}
/// Holds sign-in state for the whole app and tells screens to rebuild
/// (via [ChangeNotifier]) when it changes. Used by the router, login,
/// register, forgot-password, profile and ticket-details screens, so
/// avoid renaming its public members.

class AuthViewModel extends ChangeNotifier {
  final AuthService _service;

  AuthViewModel([AuthService? service]) : _service = service ?? AuthService();

  AuthState _state = AuthState.unknown;
  Profile? _user;
  String? _error;
  bool _isLoading = false;
  bool _isUpdating = false;
  String? _updateError;
  bool _isRegistering = false;
  String? _registerError;
  bool _isSendingReset = false;
  String? _resetError;
  bool _resetSent = false;
  bool _registrationEmailSent = false;
  bool _registrationNeedsConfirmation = false;
  String? _pendingConfirmationEmail;

    // --- Getters ---
  // Role helpers (isAdmin, isTechnician, isClient) read from the signed-in
  // profile and are false when nobody is signed in.
  
  AuthState get state => _state;
  Profile? get user => _user;
  String? get error => _error;
  bool get isLoading => _isLoading;
  bool get isAuthenticated => _state == AuthState.authenticated;
  bool get isAdmin => _user?.isAdmin ?? false;
  bool get isTechnician => _user?.isTechnician ?? false;
  bool get isClient => _user?.isClient ?? false;
  bool get isUpdating => _isUpdating;
  String? get updateError => _updateError;
  bool get isRegistering => _isRegistering;
  String? get registerError => _registerError;
  bool get isSendingReset => _isSendingReset;
  String? get resetError => _resetError;
  bool get resetSent => _resetSent;
  bool get registrationEmailSent => _registrationEmailSent;
  bool get registrationNeedsConfirmation => _registrationNeedsConfirmation;
  String? get pendingConfirmationEmail => _pendingConfirmationEmail;

  // --- Helpers ---

  /// The Flutter app is CLIENT-ONLY. Technician and Admin accounts
  /// must use the ASP.NET web portal instead.
  static bool _isBlockedRole(String role) {
    final r = role.toLowerCase();
    return r == 'admin' || r == 'technician';
  }
  /// Message shown when a blocked role tries to sign in.
  
  static String _blockedMessage(String role) {
    return role.toLowerCase() == 'admin'
        ? 'Admin accounts can only access the web portal. Please use the browser version.'
        : 'Technician accounts can only access the web portal. Please use the browser version.';
  }

  // --- Auth actions ---

  /// Called once at app startup.
  /// Tries to fetch the current user using any stored cookie.
  Future<void> bootstrap() async {
    try {
      final user = await _service.getCurrentUser();
      // BLOCK non-client roles from using the mobile app.
      if (_isBlockedRole(user.role)) {
        await _service.logout();
        _user = null;
        _state = AuthState.unauthenticated;
        notifyListeners();
        return;
      }
      _user = user;
      _state = AuthState.authenticated;
    } catch (_) {
      _user = null;
      _state = AuthState.unauthenticated;
    }
    notifyListeners();
  }

   /// Returns true on success. Blocked roles are signed out again and
  /// [error] explains why. Any failure sets [error] and returns false.
  
  Future<bool> login({
    required String email,
    required String password,
    bool rememberMe = false,
  }) async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      final user = await _service.login(
        email: email,
        password: password,
        rememberMe: rememberMe,
      );

      // BLOCK Technician and Admin from logging in via mobile.
      if (_isBlockedRole(user.role)) {
        await _service.logout();
        _error = _blockedMessage(user.role);
        _user = null;
        _state = AuthState.unauthenticated;
        _isLoading = false;
        notifyListeners();
        return false;
      }

      _user = user;
      _state = AuthState.authenticated;
      _isLoading = false;
      notifyListeners();
      return true;
    } catch (e) {
      _error = e.toString();
      _user = null;
      _state = AuthState.unauthenticated;
      _isLoading = false;
      notifyListeners();
      return false;
    }
  }

  /// Signs out on the server, then clears the local user.
  
  Future<void> logout() async {
    await _service.logout();
    _user = null;
    _state = AuthState.unauthenticated;
    notifyListeners();
  }

   /// Always returns false: registration does not sign the user in.
  /// Check [registerError] for failure, or [registrationEmailSent] and
  /// [registrationNeedsConfirmation] for success.
  
  Future<bool> register({
    required String email,
    required String password,
    required String fullName,
    String? phone,
  }) async {
    _isRegistering = true;
    _registerError = null;
    _registrationEmailSent = false;
    _registrationNeedsConfirmation = false;
    _pendingConfirmationEmail = null;
    notifyListeners();

    try {
      final (registeredEmail, requiresConfirmation, error) =
          await _service.register(
        email: email,
        password: password,
        fullName: fullName,
        phone: phone,
      );

      _isRegistering = false;

      if (error != null) {
        _registerError = error;
        notifyListeners();
        return false;
      }

      _registrationEmailSent = true;
      _registrationNeedsConfirmation = requiresConfirmation;
      _pendingConfirmationEmail = registeredEmail;
      notifyListeners();
      return false; // Does not log in automatically
    } catch (e) {
      _registerError = e.toString();
      _isRegistering = false;
      notifyListeners();
      return false;
    }
  }

  Future<bool> forgotPassword({required String email}) async {
    _isSendingReset = true;
    _resetError = null;
    _resetSent = false;
    notifyListeners();

    try {
      await _service.forgotPassword(email: email);
      _isSendingReset = false;
      _resetSent = true;
      notifyListeners();
      return true;
    } catch (e) {
      _resetError = e.toString();
      _isSendingReset = false;
      notifyListeners();
      return false;
    }
  }

 /// Clears registration errors and the confirmation flags.
 
  void clearRegisterError() {
    _registerError = null;
    _registrationEmailSent = false;
    _registrationNeedsConfirmation = false;
    _pendingConfirmationEmail = null;
    notifyListeners();
  }


  /// Clears the forgot-password error and sent flag.

  void clearResetState() {
    _resetError = null;
    _resetSent = false;
    notifyListeners();
  }

  /// Clears the login error.

  void clearError() {
    _error = null;
    notifyListeners();
  }

    /// Saves profile changes. Returns true on success and replaces the cached
  /// user; otherwise sets [updateError].
  
  Future<bool> updateProfile({
  
    String? fullName,
    String? phone,
    String? position,
  }) async {
    _isUpdating = true;
    _updateError = null;
    notifyListeners();

    try {
      final updated = await _service.updateProfile(
        fullName: fullName,
        phone: phone,
        position: position,
      );
      _user = updated;
      _isUpdating = false;
      notifyListeners();
      return true;
    } catch (e) {
      _updateError = e.toString();
      _isUpdating = false;
      notifyListeners();
      return false;
    }
  }

  void clearUpdateError() {
    _updateError = null;
    notifyListeners();
  }
}