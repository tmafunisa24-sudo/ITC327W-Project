// lib/models/asset.dart

class Asset {
  final String? id;
  final String assetTag;
  final String assetName;
  final String brand;
  final String model;
  final String serialNumber;
  final String category;
  final DateTime? purchaseDate;
  final DateTime? warrantyExpiry;
  final String department;
  final String assignedEmployee;
  final String location;
  final String status;
  final String? imageUrl;
  final String? warrantyUrl;
  final String? qrCodeUrl;

  const Asset({
    this.id,
    required this.assetTag,
    required this.assetName,
    required this.brand,
    required this.model,
    required this.serialNumber,
    required this.category,
    this.purchaseDate,
    this.warrantyExpiry,
    required this.department,
    required this.assignedEmployee,
    required this.location,
    this.status = 'Available',
    this.imageUrl,
    this.warrantyUrl,
    this.qrCodeUrl,
  });

  factory Asset.fromJson(Map<String, dynamic> json) {
    return Asset(
      id: json['id'] as String?,
      assetTag: json['asset_tag'] as String? ?? '',
      assetName: json['asset_name'] as String? ?? '',
      brand: json['brand'] as String? ?? '',
      model: json['model'] as String? ?? '',
      serialNumber: json['serial_number'] as String? ?? '',
      category: json['category'] as String? ?? '',
      purchaseDate: json['purchase_date'] != null
          ? DateTime.parse(json['purchase_date'] as String)
          : null,
      warrantyExpiry: json['warranty_expiry'] != null
          ? DateTime.parse(json['warranty_expiry'] as String)
          : null,
      department: json['department'] as String? ?? '',
      assignedEmployee: json['assigned_employee'] as String? ?? '',
      location: json['location'] as String? ?? '',
      status: json['status'] as String? ?? 'Available',
      imageUrl: json['image_url'] as String?,
      warrantyUrl: json['warranty_url'] as String?,
      qrCodeUrl: json['qr_code_url'] as String?,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      if (id != null) 'id': id,
      'asset_tag': assetTag,
      'asset_name': assetName,
      'brand': brand,
      'model': model,
      'serial_number': serialNumber,
      'category': category,
      'purchase_date': purchaseDate?.toIso8601String(),
      'warranty_expiry': warrantyExpiry?.toIso8601String(),
      'department': department,
      'assigned_employee': assignedEmployee,
      'location': location,
      'status': status,
      'image_url': imageUrl,
      'warranty_url': warrantyUrl,
      'qr_code_url': qrCodeUrl,
    };
  }

  Asset copyWith({
    String? id,
    String? assetTag,
    String? assetName,
    String? brand,
    String? model,
    String? serialNumber,
    String? category,
    DateTime? purchaseDate,
    DateTime? warrantyExpiry,
    String? department,
    String? assignedEmployee,
    String? location,
    String? status,
    String? imageUrl,
    String? warrantyUrl,
    String? qrCodeUrl,
  }) {
    return Asset(
      id: id ?? this.id,
      assetTag: assetTag ?? this.assetTag,
      assetName: assetName ?? this.assetName,
      brand: brand ?? this.brand,
      model: model ?? this.model,
      serialNumber: serialNumber ?? this.serialNumber,
      category: category ?? this.category,
      purchaseDate: purchaseDate ?? this.purchaseDate,
      warrantyExpiry: warrantyExpiry ?? this.warrantyExpiry,
      department: department ?? this.department,
      assignedEmployee: assignedEmployee ?? this.assignedEmployee,
      location: location ?? this.location,
      status: status ?? this.status,
      imageUrl: imageUrl ?? this.imageUrl,
      warrantyUrl: warrantyUrl ?? this.warrantyUrl,
      qrCodeUrl: qrCodeUrl ?? this.qrCodeUrl,
    );
  }

  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      other is Asset &&
          other.id == id &&
          other.assetTag == assetTag &&
          other.assetName == assetName &&
          other.brand == brand &&
          other.model == model &&
          other.serialNumber == serialNumber &&
          other.category == category &&
          other.purchaseDate == purchaseDate &&
          other.warrantyExpiry == warrantyExpiry &&
          other.department == department &&
          other.assignedEmployee == assignedEmployee &&
          other.location == location &&
          other.status == status &&
          other.imageUrl == imageUrl &&
          other.warrantyUrl == warrantyUrl &&
          other.qrCodeUrl == qrCodeUrl;

  @override
  int get hashCode => Object.hash(
        id,
        assetTag,
        assetName,
        brand,
        model,
        serialNumber,
        category,
        purchaseDate,
        warrantyExpiry,
        department,
        assignedEmployee,
        location,
        status,
        imageUrl,
        warrantyUrl,
        qrCodeUrl,
      );
}