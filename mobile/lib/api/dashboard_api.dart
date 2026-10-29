// lib/api/dashboard_api.dart

import 'package:dio/dio.dart';
import '../core/network/api_client.dart';
import '../core/network/api_exception.dart';
import '../models/dtos/dashboard_response.dart';

class DashboardApi {
  final Dio _dio;
  DashboardApi([Dio? dio]) : _dio = dio ?? ApiClient.dio;

  Future<DashboardResponse> getDashboard() async {
    final response = await _dio.get('/api/dashboard');
    final code = response.statusCode ?? 0;

    if (code == 200) {
      final data = response.data;
      if (data is Map<String, dynamic>) {
        return DashboardResponse.fromJson(data);
      }
      throw ApiException('Invalid dashboard response.', statusCode: code);
    }

    throw ApiException(
      _extractError(response) ?? 'Unable to load dashboard.',
      statusCode: code,
    );
  }

  String? _extractError(Response response) {
    final data = response.data;
    if (data is Map<String, dynamic>) {
      if (data['error'] is String) return data['error'] as String;
      if (data['detail'] is String) return data['detail'] as String;
    }
    return null;
  }
}
