// Summary: Models a department and provides its JSON serialization and parsing.
// ITC327W-Project - Mobile Service Catalogue Module
// File: lib/models/department.dart
// Feature Branch: feature/keletso-tladi-departments-maintenance
// Student: Keletso Tladi
// Student Number: 221010874
// Date: 2026-10-30
// Module: Departments - Department Model
// lib/models/department.dart
main
/// A company department as returned by the API.
/// Only name is required; every other field may be missing.
class Department {
  /// Department id (null for a department that has not been saved yet).
  final String? id;
  /// Display name of the department.
  final String name;
  /// Optional short description.
  final String? description;
  /// Number of employees in the department, if supplied by the API.
  final int? employeeCount;
  /// Number of assets assigned to the department, if supplied by the API.
  final int? assetCount;
  /// When the department was created.
  final DateTime? createdAt;
  /// When the department was last changed.
  final DateTime? updatedAt;

  /// Creates a department.
  const Department({
    this.id,
    required this.name,
    this.description,
    this.employeeCount,
    this.assetCount,
    this.createdAt,
    this.updatedAt,
  });

  /// Builds a department from the API JSON (snake_case keys).
  /// Dates are parsed from ISO-8601 strings when present.
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

  /// Converts the department to JSON. The id is left out when it is null.
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

  /// Returns a copy with the given fields replaced.
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
  // Two departments are equal when every field matches.
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
  // Kept consistent with operator == above.
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