// lib/api/notification_api.dart

import 'package:dio/dio.dart';
import '../core/network/api_client.dart';
import '../core/network/api_exception.dart';
import '../models/notification.dart';
import '../models/dtos/notification_bundle.dart';

class NotificationApi {
  final Dio _dio;
  NotificationApi([Dio? dio]) : _dio = dio ?? ApiClient.dio;

  /// GET /api/notifications
  Future<List<Notification>> getAll() async {
    final response = await _dio.get('/api/notifications');
    final code = response.statusCode ?? 0;

    if (code == 200) {
      final data = response.data;
      if (data is List) {
        return data
            .whereType<Map<String, dynamic>>()
            .map(Notification.fromJson)
            .toList();
      }
      return const [];
    }

    throw ApiException(
      _extractError(response) ?? 'Unable to load notifications.',
      statusCode: code,
    );
  }

  /// GET /api/notifications/recent?limit=6
  Future<NotificationBundle> getRecent({int limit = 6}) async {
    final response = await _dio.get(
      '/api/notifications/recent',
      queryParameters: {'limit': limit},
    );
    final code = response.statusCode ?? 0;

    if (code == 200) {
      final data = response.data;
      if (data is Map<String, dynamic>) {
        return NotificationBundle.fromJson(data);
      }
      throw ApiException('Invalid response.', statusCode: code);
    }

    throw ApiException(
      _extractError(response) ?? 'Unable to load notifications.',
      statusCode: code,
    );
  }

  /// GET /api/notifications/unread-count
  Future<int> getUnreadCount() async {
    final response = await _dio.get('/api/notifications/unread-count');
    final code = response.statusCode ?? 0;

    if (code == 200) {
      final data = response.data;
      if (data is Map<String, dynamic>) {
        return data['count'] as int? ?? 0;
      }
      return 0;
    }

    throw ApiException(
      _extractError(response) ?? 'Unable to load notification count.',
      statusCode: code,
    );
  }

  /// POST /api/notifications/{id}/read
  Future<void> markAsRead(String id) async {
    final response = await _dio.post('/api/notifications/$id/read');
    final code = response.statusCode ?? 0;

    if (code == 204 || code == 200) return;

    throw ApiException(
      _extractError(response) ?? 'Unable to mark notification as read.',
      statusCode: code,
    );
  }

  /// POST /api/notifications/read-all
  Future<void> markAllAsRead() async {
    final response = await _dio.post('/api/notifications/read-all');
    final code = response.statusCode ?? 0;

    if (code == 204 || code == 200) return;

    throw ApiException(
      _extractError(response) ?? 'Unable to mark notifications as read.',
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