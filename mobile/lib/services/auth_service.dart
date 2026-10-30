
import '../api/auth_api.dart';
import '../models/profile.dart';

/// Thin layer between [AuthViewModel] and [AuthApi].
/// Holds no state; it forwards each call to the API so the API can be
/// swapped for a fake one in tests (see the optional constructor argument).
/// 

class AuthService {
  final AuthApi _api;
  AuthService([AuthApi? api]) : _api = api ?? AuthApi();

  Future<Profile> login({
    required String email,
    required String password,
    bool rememberMe = false,
  }) 
    /// Signs in with [email] and [password] and returns the user's [Profile].
  /// [rememberMe] asks the server for a longer-lived session.
 
  {
    return _api.login(
      email: email,
      password: password,
      rememberMe: rememberMe,
    );
  }
   /// Creates a new account. Returns (email, requiresConfirmation, error).
  /// A non-null error means registration failed.
  Future<(String?, bool, String?)> register({
    required String email,
    required String password,
    required String fullName,
    String? phone,
  }) {
    return _api.register(
      email: email,
      password: password,
      fullName: fullName,
      phone: phone,
    );
  }
/// Asks the server to email a password-reset link to [email].
  Future<void> forgotPassword({required String email}) {
    return _api.forgotPassword(email: email);
  }

 /// Updates the signed-in user's details; null fields are left unchanged.
 
  Future<Profile> updateProfile({
    String? fullName,
    String? phone,
    String? position,
  }){
    return _api.updateProfile(
      fullName: fullName,
      phone: phone,
      position: position
    );
  }

/// Fetches the profile of the user in the current session (stored cookie).

  Future<Profile> getCurrentUser() => _api.me();

  Future<void> logout() => _api.logout();

  
}