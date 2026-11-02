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
    if (list is! List) {
      throw const FormatException(
        'Invalid notification bundle: expected a notifications array.',
      );
    }

    return NotificationBundle(
      unreadCount: json['unreadCount'] as int? ?? 0,
      notifications: list.asMap().entries.map((entry) {
        if (entry.value is! Map) {
          throw FormatException(
            'Invalid notification at index ${entry.key}: expected a JSON object.',
          );
        }
        return Notification.fromJson(
          Map<String, dynamic>.from(entry.value as Map),
        );
      }).toList(),
    );
  }
}
