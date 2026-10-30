// lib/models/dtos/notification_bundle.dart

import '../../models/notification.dart';

class NotificationBundle {
  final int unreadCount;
  final List<Notification> notifications;

  const NotificationBundle({
    required this.unreadCount,
    required this.notifications,
  });

  factory NotificationBundle.fromJson(Map<String, dynamic> json) {
    final list = json['notifications'];
    return NotificationBundle(
      unreadCount: json['unreadCount'] as int? ?? 0,
      notifications: list is List
          ? list
              .whereType<Map<String, dynamic>>()
              .map(Notification.fromJson)
              .toList()
          : const [],
    );
  }
}