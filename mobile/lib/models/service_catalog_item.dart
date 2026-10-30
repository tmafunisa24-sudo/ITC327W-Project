// Summary: Models an available service-catalog item and maps it to and from JSON.
// ITC327W-Project - Mobile Service Catalogue Module
// File: lib/models/service_catalog_item.dart
// Feature Branch: feature/keletso-tladi-departments-maintenance
// Student: Keletso Tladi
// Student Number: 221010874
// Date: 2026-10-30
// Module: Service Catalogue - Item Model
// lib/models/service_catalog_item.dart
main
/// One entry in the service catalogue: a device type, a problem category
/// and a specific problem type that a customer can request help with.
/// Mirrors the JSON returned by GET /api/service-catalog.
class ServiceCatalogItem {
  /// Unique catalogue id. Sent as serviceCatalogId when a ticket is created.
  final int id;
  /// The kind of device, e.g. Laptop or Phone.
  final String deviceType;
  /// The broad problem group for the device, e.g. Hardware or Software.
  final String problemCategory;
  /// The specific problem the customer wants fixed.
  final String problemType;
  /// Only active items are offered to customers.
  final bool isActive;

  /// Creates a catalogue item. isActive defaults to true.
  const ServiceCatalogItem({
    required this.id,
    required this.deviceType,
    required this.problemCategory,
    required this.problemType,
    this.isActive = true,
  });

  /// Builds an item from the API JSON (snake_case keys).
  /// Missing text values become empty strings; a missing is_active becomes true.
  factory ServiceCatalogItem.fromJson(Map<String, dynamic> json) {
    return ServiceCatalogItem(
      id: json['id'] as int,
      deviceType: json['device_type'] as String? ?? '',
      problemCategory: json['problem_category'] as String? ?? '',
      problemType: json['problem_type'] as String? ?? '',
      isActive: json['is_active'] as bool? ?? true,
    );
  }

  /// Converts the item back to snake_case JSON.
  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'device_type': deviceType,
      'problem_category': problemCategory,
      'problem_type': problemType,
      'is_active': isActive,
    };
  }

  /// Returns a copy with the given fields replaced.
  ServiceCatalogItem copyWith({
    int? id,
    String? deviceType,
    String? problemCategory,
    String? problemType,
    bool? isActive,
  }) {
    return ServiceCatalogItem(
      id: id ?? this.id,
      deviceType: deviceType ?? this.deviceType,
      problemCategory: problemCategory ?? this.problemCategory,
      problemType: problemType ?? this.problemType,
      isActive: isActive ?? this.isActive,
    );
  }

  @override
  // Two items are equal when every field matches.
  bool operator ==(Object other) =>
      identical(this, other) ||
      other is ServiceCatalogItem &&
          other.id == id &&
          other.deviceType == deviceType &&
          other.problemCategory == problemCategory &&
          other.problemType == problemType &&
          other.isActive == isActive;

  @override
  // Kept consistent with operator == above.
  int get hashCode =>
      Object.hash(id, deviceType, problemCategory, problemType, isActive);
}