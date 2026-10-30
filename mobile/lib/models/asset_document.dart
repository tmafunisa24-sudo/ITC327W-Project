// lib/models/asset_document.dart

class AssetDocument {
  final String? id;
  final String assetId;
  final String documentType;
  final String originalName;
  final String storagePath;
  final String storageUrl;
  final String? extractedText;
  final DateTime? createdAt;

  const AssetDocument({
    this.id,
    required this.assetId,
    required this.documentType,
    required this.originalName,
    required this.storagePath,
    required this.storageUrl,
    this.extractedText,
    this.createdAt,
  });

  factory AssetDocument.fromJson(Map<String, dynamic> json) {
    return AssetDocument(
      id: json['id'] as String?,
      assetId: json['asset_id'] as String? ?? '',
      documentType: json['document_type'] as String? ?? '',
      originalName: json['original_name'] as String? ?? '',
      storagePath: json['storage_path'] as String? ?? '',
      storageUrl: json['storage_url'] as String? ?? '',
      extractedText: json['extracted_text'] as String?,
      createdAt: json['created_at'] != null
          ? DateTime.parse(json['created_at'] as String)
          : null,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      if (id != null) 'id': id,
      'asset_id': assetId,
      'document_type': documentType,
      'original_name': originalName,
      'storage_path': storagePath,
      'storage_url': storageUrl,
      'extracted_text': extractedText,
      'created_at': createdAt?.toIso8601String(),
    };
  }

  AssetDocument copyWith({
    String? id,
    String? assetId,
    String? documentType,
    String? originalName,
    String? storagePath,
    String? storageUrl,
    String? extractedText,
    DateTime? createdAt,
  }) {
    return AssetDocument(
      id: id ?? this.id,
      assetId: assetId ?? this.assetId,
      documentType: documentType ?? this.documentType,
      originalName: originalName ?? this.originalName,
      storagePath: storagePath ?? this.storagePath,
      storageUrl: storageUrl ?? this.storageUrl,
      extractedText: extractedText ?? this.extractedText,
      createdAt: createdAt ?? this.createdAt,
    );
  }

  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      other is AssetDocument &&
          other.id == id &&
          other.assetId == assetId &&
          other.documentType == documentType &&
          other.originalName == originalName &&
          other.storagePath == storagePath &&
          other.storageUrl == storageUrl &&
          other.extractedText == extractedText &&
          other.createdAt == createdAt;

  @override
  int get hashCode => Object.hash(
        id,
        assetId,
        documentType,
        originalName,
        storagePath,
        storageUrl,
        extractedText,
        createdAt,
      );
}