// lib/models/profile.dart

class Profile {
  final String id;
  final String? userId;
  final String fullName;
  final String email;
  final String? phone;
  final String role;
  final String? departmentId;
  final String? position;
  final String? profileImage;
  final bool emailVerified;
  final String status;
  final DateTime? lastLoginAt;
  final DateTime createdAt;
  final DateTime? updatedAt;

  const Profile({
    required this.id,
    this.userId,
    required this.fullName,
    required this.email,
    this.phone,
    this.role = 'Client',
    this.departmentId,
    this.position,
    this.profileImage,
    this.emailVerified = false,
    this.status = 'Active',
    this.lastLoginAt,
    required this.createdAt,
    this.updatedAt,
  });

  factory Profile.fromJson(Map<String, dynamic> json) {
    // Tolerant of both camelCase (ASP.NET API) and snake_case (Supabase)
    return Profile(
      id: (json['id'] ?? json['Id']) as String,
      userId: (json['userId'] ?? json['user_id']) as String?,
      fullName: (json['fullName'] ?? json['full_name']) as String? ?? '',
      email: (json['email'] ?? json['Email']) as String? ?? '',
      phone: json['phone'] as String?,
      role: (json['role'] ?? json['Role']) as String? ?? 'Client',
      departmentId: (json['departmentId'] ?? json['department_id']) as String?,
      position: json['position'] as String?,
      profileImage: (json['profileImage'] ?? json['profile_image']) as String?,
      emailVerified:
          (json['emailVerified'] ?? json['email_verified']) as bool? ?? false,
      status: json['status'] as String? ?? 'Active',
      lastLoginAt: _parseDate(json['lastLoginAt'] ?? json['last_login_at']),
      createdAt: _parseDate(json['createdAt'] ?? json['created_at']) ??
          DateTime.now().toUtc(),
      updatedAt: _parseDate(json['updatedAt'] ?? json['updated_at']),
    );
  }

  static DateTime? _parseDate(dynamic value) {
    if (value == null) return null;
    if (value is String) return DateTime.parse(value);
    return null;
  }

  Map<String, dynamic> toJson() => {
        'id': id,
        if (userId != null) 'userId': userId,
        'fullName': fullName,
        'email': email,
        'phone': phone,
        'role': role,
        if (departmentId != null) 'departmentId': departmentId,
        'position': position,
        'profileImage': profileImage,
        'emailVerified': emailVerified,
        'status': status,
        if (lastLoginAt != null) 'lastLoginAt': lastLoginAt?.toIso8601String(),
        'createdAt': createdAt.toIso8601String(),
        if (updatedAt != null) 'updatedAt': updatedAt?.toIso8601String(),
      };

  // Convenience role getters
  bool get isAdmin => role == 'Admin';
  bool get isTechnician => role == 'Technician';
  bool get isClient => role == 'Client';
  bool get isPending => role == 'Pending';

  Profile copyWith({
    String? id,
    String? userId,
    String? fullName,
    String? email,
    String? phone,
    String? role,
    String? departmentId,
    String? position,
    String? profileImage,
    bool? emailVerified,
    String? status,
    DateTime? lastLoginAt,
    DateTime? createdAt,
    DateTime? updatedAt,
  }) {
    return Profile(
      id: id ?? this.id,
      userId: userId ?? this.userId,
      fullName: fullName ?? this.fullName,
      email: email ?? this.email,
      phone: phone ?? this.phone,
      role: role ?? this.role,
      departmentId: departmentId ?? this.departmentId,
      position: position ?? this.position,
      profileImage: profileImage ?? this.profileImage,
      emailVerified: emailVerified ?? this.emailVerified,
      status: status ?? this.status,
      lastLoginAt: lastLoginAt ?? this.lastLoginAt,
      createdAt: createdAt ?? this.createdAt,
      updatedAt: updatedAt ?? this.updatedAt,
    );
  }

  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      other is Profile &&
          other.id == id &&
          other.userId == userId &&
          other.fullName == fullName &&
          other.email == email &&
          other.phone == phone &&
          other.role == role &&
          other.departmentId == departmentId &&
          other.position == position &&
          other.profileImage == profileImage &&
          other.emailVerified == emailVerified &&
          other.status == status &&
          other.lastLoginAt == lastLoginAt &&
          other.createdAt == createdAt &&
          other.updatedAt == updatedAt;

  @override
  int get hashCode => Object.hash(
        id,
        userId,
        fullName,
        email,
        phone,
        role,
        departmentId,
        position,
        profileImage,
        emailVerified,
        status,
        lastLoginAt,
        createdAt,
        updatedAt,
      );
}