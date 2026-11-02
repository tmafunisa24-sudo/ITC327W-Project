// lib/models/service_catalog_item.dart

class ServiceCatalogItem {
  final int id;
  final String deviceType;
  final String problemCategory;
  final String problemType;
  final bool isActive;

  const ServiceCatalogItem({
    required this.id,
    required this.deviceType,
    required this.problemCategory,
    required this.problemType,
    this.isActive = true,
  });

  factory ServiceCatalogItem.fromJson(Map<String, dynamic> json) {
    return ServiceCatalogItem(
      id: json['id'] as int,
      deviceType: json['device_type'] as String? ?? '',
      problemCategory: json['problem_category'] as String? ?? '',
      problemType: json['problem_type'] as String? ?? '',
      isActive: json['is_active'] as bool? ?? true,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'device_type': deviceType,
      'problem_category': problemCategory,
      'problem_type': problemType,
      'is_active': isActive,
    };
  }

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
  bool operator ==(Object other) =>
      identical(this, other) ||
      other is ServiceCatalogItem &&
          other.id == id &&
          other.deviceType == deviceType &&
          other.problemCategory == problemCategory &&
          other.problemType == problemType &&
          other.isActive == isActive;

  @override
  int get hashCode =>
      Object.hash(id, deviceType, problemCategory, problemType, isActive);
}