// lib/models/ticket_attachment.dart

class TicketAttachment {
  final String id;
  final String ticketId;
  final String fileUrl;
  final String fileName;
  final String fileType;
  final int fileSize;
  final String uploadedBy;
  final DateTime createdAt;

  const TicketAttachment({
    required this.id,
    required this.ticketId,
    required this.fileUrl,
    required this.fileName,
    required this.fileType,
    required this.fileSize,
    required this.uploadedBy,
    required this.createdAt,
  });

  factory TicketAttachment.fromJson(Map<String, dynamic> json) {
    return TicketAttachment(
      id: json['id'] as String,
      ticketId: json['ticket_id'] as String,
      fileUrl: json['file_url'] as String? ?? '',
      fileName: json['file_name'] as String? ?? '',
      fileType: json['file_type'] as String? ?? '',
      fileSize: json['file_size'] as int? ?? 0,
      uploadedBy: json['uploaded_by'] as String,
      createdAt: json['created_at'] != null
          ? DateTime.parse(json['created_at'] as String)
          : DateTime.now().toUtc(),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'ticket_id': ticketId,
      'file_url': fileUrl,
      'file_name': fileName,
      'file_type': fileType,
      'file_size': fileSize,
      'uploaded_by': uploadedBy,
      'created_at': createdAt.toIso8601String(),
    };
  }

  TicketAttachment copyWith({
    String? id,
    String? ticketId,
    String? fileUrl,
    String? fileName,
    String? fileType,
    int? fileSize,
    String? uploadedBy,
    DateTime? createdAt,
  }) {
    return TicketAttachment(
      id: id ?? this.id,
      ticketId: ticketId ?? this.ticketId,
      fileUrl: fileUrl ?? this.fileUrl,
      fileName: fileName ?? this.fileName,
      fileType: fileType ?? this.fileType,
      fileSize: fileSize ?? this.fileSize,
      uploadedBy: uploadedBy ?? this.uploadedBy,
      createdAt: createdAt ?? this.createdAt,
    );
  }

  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      other is TicketAttachment &&
          other.id == id &&
          other.ticketId == ticketId &&
          other.fileUrl == fileUrl &&
          other.fileName == fileName &&
          other.fileType == fileType &&
          other.fileSize == fileSize &&
          other.uploadedBy == uploadedBy &&
          other.createdAt == createdAt;

  @override
  int get hashCode => Object.hash(
        id,
        ticketId,
        fileUrl,
        fileName,
        fileType,
        fileSize,
        uploadedBy,
        createdAt,
      );
}