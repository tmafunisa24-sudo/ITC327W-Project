// lib/models/ticket.dart

class Ticket {
  final String? id;
  final String? ticketNumber;
  final String? employeeId;
  final String? deviceType;
  final String? problemCategory;
  final String? problemType;
  final int? serviceCatalogId;
  final String title;
  final String description;
  final String requester;
  final String priority;
  final String status;
  final String? assignedTechnician;
  final String category;
  final String? paymentMethod;
  final String paymentStatus;
  final String? screenshotUrl;
  final String? imageUrl;
  final String? comments;
  final DateTime? dueDate;
  final DateTime? closedAt;
  final DateTime? createdAt;
  final DateTime? updatedAt;

  const Ticket({
    this.id,
    this.ticketNumber,
    this.employeeId,
    this.deviceType,
    this.problemCategory,
    this.problemType,
    this.serviceCatalogId,
    required this.title,
    required this.description,
    required this.requester,
    this.priority = 'Medium',
    this.status = 'Open',
    this.assignedTechnician,
    required this.category,
    this.paymentMethod,
    this.paymentStatus = 'Pending',
    this.screenshotUrl,
    this.imageUrl,
    this.comments,
    this.dueDate,
    this.closedAt,
    this.createdAt,
    this.updatedAt,
  });

  factory Ticket.fromJson(Map<String, dynamic> json) {
    return Ticket(
      id: json['id'] as String?,
      ticketNumber: json['ticket_number'] as String?,
      employeeId: json['employee_id'] as String?,
      deviceType: json['device_type'] as String?,
      problemCategory: json['problem_category'] as String?,
      problemType: json['problem_type'] as String?,
      serviceCatalogId: json['service_catalog_id'] as int?,
      title: json['title'] as String? ?? '',
      description: json['description'] as String? ?? '',
      requester: json['requester'] as String? ?? '',
      priority: json['priority'] as String? ?? 'Medium',
      status: json['status'] as String? ?? 'Open',
      assignedTechnician: json['assigned_technician'] as String?,
      category: json['category'] as String? ?? '',
      paymentMethod: json['payment_method'] as String?,
      paymentStatus: json['payment_status'] as String? ?? 'Pending',
      screenshotUrl: json['screenshot_url'] as String?,
      imageUrl: json['image_url'] as String?,
      comments: json['comments'] as String?,
      dueDate: json['due_date'] != null
          ? DateTime.parse(json['due_date'] as String)
          : null,
      closedAt: json['closed_at'] != null
          ? DateTime.parse(json['closed_at'] as String)
          : null,
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
      'ticket_number': ticketNumber,
      'employee_id': employeeId,
      'device_type': deviceType,
      'problem_category': problemCategory,
      'problem_type': problemType,
      'service_catalog_id': serviceCatalogId,
      'title': title,
      'description': description,
      'requester': requester,
      'priority': priority,
      'status': status,
      'assigned_technician': assignedTechnician,
      'category': category,
      'payment_method': paymentMethod,
      'payment_status': paymentStatus,
      'screenshot_url': screenshotUrl,
      'image_url': imageUrl,
      'comments': comments,
      'due_date': dueDate?.toIso8601String(),
      'closed_at': closedAt?.toIso8601String(),
      'created_at': createdAt?.toIso8601String(),
      'updated_at': updatedAt?.toIso8601String(),
    };
  }

  Ticket copyWith({
    String? id,
    String? ticketNumber,
    String? employeeId,
    String? deviceType,
    String? problemCategory,
    String? problemType,
    int? serviceCatalogId,
    String? title,
    String? description,
    String? requester,
    String? priority,
    String? status,
    String? assignedTechnician,
    String? category,
    String? paymentMethod,
    String? paymentStatus,
    String? screenshotUrl,
    String? imageUrl,
    String? comments,
    DateTime? dueDate,
    DateTime? closedAt,
    DateTime? createdAt,
    DateTime? updatedAt,
  }) {
    return Ticket(
      id: id ?? this.id,
      ticketNumber: ticketNumber ?? this.ticketNumber,
      employeeId: employeeId ?? this.employeeId,
      deviceType: deviceType ?? this.deviceType,
      problemCategory: problemCategory ?? this.problemCategory,
      problemType: problemType ?? this.problemType,
      serviceCatalogId: serviceCatalogId ?? this.serviceCatalogId,
      title: title ?? this.title,
      description: description ?? this.description,
      requester: requester ?? this.requester,
      priority: priority ?? this.priority,
      status: status ?? this.status,
      assignedTechnician: assignedTechnician ?? this.assignedTechnician,
      category: category ?? this.category,
      paymentMethod: paymentMethod ?? this.paymentMethod,
      paymentStatus: paymentStatus ?? this.paymentStatus,
      screenshotUrl: screenshotUrl ?? this.screenshotUrl,
      imageUrl: imageUrl ?? this.imageUrl,
      comments: comments ?? this.comments,
      dueDate: dueDate ?? this.dueDate,
      closedAt: closedAt ?? this.closedAt,
      createdAt: createdAt ?? this.createdAt,
      updatedAt: updatedAt ?? this.updatedAt,
    );
  }

  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      other is Ticket &&
          other.id == id &&
          other.ticketNumber == ticketNumber &&
          other.employeeId == employeeId &&
          other.deviceType == deviceType &&
          other.problemCategory == problemCategory &&
          other.problemType == problemType &&
          other.serviceCatalogId == serviceCatalogId &&
          other.title == title &&
          other.description == description &&
          other.requester == requester &&
          other.priority == priority &&
          other.status == status &&
          other.assignedTechnician == assignedTechnician &&
          other.category == category &&
          other.paymentMethod == paymentMethod &&
          other.paymentStatus == paymentStatus &&
          other.screenshotUrl == screenshotUrl &&
          other.imageUrl == imageUrl &&
          other.comments == comments &&
          other.dueDate == dueDate &&
          other.closedAt == closedAt &&
          other.createdAt == createdAt &&
          other.updatedAt == updatedAt;

  @override
  int get hashCode => Object.hashAll([
        id,
        ticketNumber,
        employeeId,
        deviceType,
        problemCategory,
        problemType,
        serviceCatalogId,
        title,
        description,
        requester,
        priority,
        status,
        assignedTechnician,
        category,
        paymentMethod,
        paymentStatus,
        screenshotUrl,
        imageUrl,
        comments,
        dueDate,
        closedAt,
        createdAt,
        updatedAt,
      ]);
}