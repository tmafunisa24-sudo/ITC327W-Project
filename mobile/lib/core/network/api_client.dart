// lib/core/network/api_client.dart

import 'package:cookie_jar/cookie_jar.dart';
import 'package:dio/dio.dart';
import 'package:dio_cookie_manager/dio_cookie_manager.dart';
import 'package:path_provider/path_provider.dart';

import '../config/env.dart';

class ApiClient {
  ApiClient._();

  static late final Dio dio;
  static late final CookieJar cookieJar;
  static bool _initialized = false;

  /// Call once at app startup.
  static Future<void> initialize() async {
    if (_initialized) return;

    final dir = await getApplicationDocumentsDirectory();
    cookieJar = PersistCookieJar(
      storage: FileStorage('${dir.path}/.cookies/'),
      ignoreExpires: false,
    );

    dio = Dio(
      BaseOptions(
        baseUrl: Env.apiBaseUrl,
        connectTimeout: Env.apiTimeout,
        receiveTimeout: Env.apiTimeout,
        headers: {'Accept': 'application/json'},
        // Don't throw on 4xx — we handle them explicitly
        validateStatus: (status) => status != null && status < 500,
      ),
    );

    dio.interceptors.add(CookieManager(cookieJar));

    _initialized = true;
  }

  static Future<void> clearCookies() async {
    await cookieJar.deleteAll();
  }

  static Future<bool> hasAuthCookie() async {
    final uri = Uri.parse(Env.apiBaseUrl);
    final cookies = await cookieJar.loadForRequest(uri);
    return cookies.any((c) => c.name == 'ServiceHubITAuth');
  }
}