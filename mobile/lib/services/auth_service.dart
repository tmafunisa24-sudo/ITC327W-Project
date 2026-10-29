// lib/services/auth_service.dart

import '../api/auth_api.dart';
import '../models/profile.dart';

class AuthService {
  final AuthApi _api;
  AuthService([AuthApi? api]) : _api = api ?? AuthApi();

  Future<Profile> login({
    required String email,
    required String password,
    bool rememberMe = false,
  }) {
    return _api.login(
      email: email,
      password: password,
      rememberMe: rememberMe,
    );
  }
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

  Future<void> forgotPassword({required String email}) {
    return _api.forgotPassword(email: email);
  }

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

  Future<Profile> getCurrentUser() => _api.me();

  Future<void> logout() => _api.logout();

  
}