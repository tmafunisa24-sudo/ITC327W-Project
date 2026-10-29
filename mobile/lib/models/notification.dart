// lib/models/notification.dart

class Notification {
  final String id;
  final String userId;
  final String title;
  final String message;
  final String type;
  final String? link;
  final bool isRead;
  final DateTime? createdAt;

  const Notification({
    required this.id,
    required this.userId,
    required this.title,
    required this.message,
    this.type = 'general',
    this.link,
    this.isRead = false,
    this.createdAt,
  });

  factory Notification.fromJson(Map<String, dynamic> json) {
    return Notification(
      id: json['id'] as String,
      userId: json['user_id'] as String,
      title: json['title'] as String? ?? '',
      message: json['message'] as String? ?? '',
      type: json['type'] as String? ?? 'general',
      link: json['link'] as String?,
      isRead: json['is_read'] as bool? ?? false,
      createdAt: json['created_at'] != null
          ? DateTime.parse(json['created_at'] as String)
          : null,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'user_id': userId,
      'title': title,
      'message': message,
      'type': type,
      'link': link,
      'is_read': isRead,
      'created_at': createdAt?.toIso8601String(),
    };
  }

  Notification copyWith({
    String? id,
    String? userId,
    String? title,
    String? message,
    String? type,
    String? link,
    bool? isRead,
    DateTime? createdAt,
  }) {
    return Notification(
      id: id ?? this.id,
      userId: userId ?? this.userId,
      title: title ?? this.title,
      message: message ?? this.message,
      type: type ?? this.type,
      link: link ?? this.link,
      isRead: isRead ?? this.isRead,
      createdAt: createdAt ?? this.createdAt,
    );
  }

  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      other is Notification &&
          other.id == id &&
          other.userId == userId &&
          other.title == title &&
          other.message == message &&
          other.type == type &&
          other.link == link &&
          other.isRead == isRead &&
          other.createdAt == createdAt;

  @override
  int get hashCode => Object.hash(
        id,
        userId,
        title,
        message,
        type,
        link,
        isRead,
        createdAt,
      );
}