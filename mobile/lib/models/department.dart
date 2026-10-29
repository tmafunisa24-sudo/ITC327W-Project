// lib/models/department.dart

class Department {
  final String? id;
  final String name;
  final String? description;
  final int? employeeCount;
  final int? assetCount;
  final DateTime? createdAt;
  final DateTime? updatedAt;

  const Department({
    this.id,
    required this.name,
    this.description,
    this.employeeCount,
    this.assetCount,
    this.createdAt,
    this.updatedAt,
  });

  factory Department.fromJson(Map<String, dynamic> json) {
    return Department(
      id: json['id'] as String?,
      // Try both keys, prefer "name", fall back to "department_name"
      name: (json['name'] ?? json['department_name']) as String? ?? '',
      description: json['description'] as String?,
      employeeCount: json['employee_count'] as int?,
      assetCount: json['asset_count'] as int?,
      createdAt: json['created_at'] != null
          ? DateTime.parse(json['created_at'] as String)
          : null,
      updatedAt: json['updated_at'] != null
          ? DateTime.parse(json['updated_at'] as String)
          : null,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      if (id != null) 'id': id,
      'name': name,
      'description': description,
      'employee_count': employeeCount,
      'asset_count': assetCount,
      'created_at': createdAt?.toIso8601String(),
      'updated_at': updatedAt?.toIso8601String(),
    };
  }

  Department copyWith({
    String? id,
    String? name,
    String? description,
    int? employeeCount,
    int? assetCount,
    DateTime? createdAt,
    DateTime? updatedAt,
  }) {
    return Department(
      id: id ?? this.id,
      name: name ?? this.name,
      description: description ?? this.description,
      employeeCount: employeeCount ?? this.employeeCount,
      assetCount: assetCount ?? this.assetCount,
      createdAt: createdAt ?? this.createdAt,
      updatedAt: updatedAt ?? this.updatedAt,
    );
  }

  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      other is Department &&
          other.id == id &&
          other.name == name &&
          other.description == description &&
          other.employeeCount == employeeCount &&
          other.assetCount == assetCount &&
          other.createdAt == createdAt &&
          other.updatedAt == updatedAt;

  @override
  int get hashCode => Object.hash(
        id,
        name,
        description,
        employeeCount,
        assetCount,
        createdAt,
        updatedAt,
      );
}