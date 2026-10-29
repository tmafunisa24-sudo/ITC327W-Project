// lib/services/notification_service.dart

import '../api/notification_api.dart';
import '../models/notification.dart';
import '../models/dtos/notification_bundle.dart';

class NotificationService {
  final NotificationApi _api;
  NotificationService([NotificationApi? api]) : _api = api ?? NotificationApi();

  Future<List<Notification>> getAll() => _api.getAll();

  Future<NotificationBundle> getRecent({int limit = 6}) =>
      _api.getRecent(limit: limit);

  Future<int> getUnreadCount() => _api.getUnreadCount();

  Future<void> markAsRead(String id) => _api.markAsRead(id);

  Future<void> markAllAsRead() => _api.markAllAsRead();
}