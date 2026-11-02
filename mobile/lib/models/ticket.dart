// lib/models/ticket.dart

class Ticket {
  final String? id;
  final String? ticketNumber;
  final String? employeeId;
  final String? deviceType;
  final String? problemCategory;
  final String? problemType;
  final int? serviceCatalogId;
  final String? technicianId;
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
    this.technicianId,
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
      id: _string(json, 'id'),
      ticketNumber: _string(json, 'ticket_number', 'ticketNumber'),
      employeeId: _string(json, 'employee_id', 'employeeId'),
      deviceType: _string(json, 'device_type', 'deviceType'),
      problemCategory: _string(json, 'problem_category', 'problemCategory'),
      problemType: _string(json, 'problem_type', 'problemType'),
      serviceCatalogId: _integer(
        json,
        'service_catalog_id',
        'serviceCatalogId',
      ),
      technicianId: _string(
        json,
        'technician_id',
        'technicianId',
        'assignedTechnicianId',
      ),
      title: _string(json, 'title') ?? '',
      description: _string(json, 'description') ?? '',
      requester: _string(json, 'requester') ?? '',
      priority: _string(json, 'priority') ?? 'Medium',
      status: _string(json, 'status') ?? 'Open',
      assignedTechnician: _string(
        json,
        'assigned_technician',
        'assignedTechnician',
      ),
      category: _string(json, 'category') ?? '',
      paymentMethod: _string(json, 'payment_method', 'paymentMethod'),
      paymentStatus:
          _string(json, 'payment_status', 'paymentStatus') ?? 'Pending',
      screenshotUrl: _string(json, 'screenshot_url', 'screenshotUrl'),
      imageUrl: _string(json, 'image_url', 'imageUrl'),
      comments: _string(json, 'comments'),
      dueDate: _date(json, 'due_date', 'dueDate'),
      closedAt: _date(json, 'closed_at', 'closedAt'),
      createdAt: _date(json, 'created_at', 'createdAt'),
      updatedAt: _date(json, 'updated_at', 'updatedAt'),
    );
  }

  static dynamic _value(Map<String, dynamic> json, List<String> keys) {
    for (final key in keys) {
      if (json.containsKey(key)) return json[key];
    }
    return null;
  }

  static String? _string(
    Map<String, dynamic> json,
    String key1, [
    String? key2,
    String? key3,
  ]) {
    final keys = [key1, ?key2, ?key3];
    final value = _value(json, keys);
    if (value == null) return null;
    if (value is String) return value;
    throw FormatException(
      'Expected "$key1" to be a string, got ${value.runtimeType}.',
    );
  }

  static int? _integer(Map<String, dynamic> json, String key1, String key2) {
    final value = _value(json, [key1, key2]);
    if (value == null) return null;
    if (value is int) return value;
    if (value is num) return value.toInt();
    throw FormatException(
      'Expected "$key1" to be a number, got ${value.runtimeType}.',
    );
  }

  static DateTime? _date(Map<String, dynamic> json, String key1, String key2) {
    final value = _value(json, [key1, key2]);
    if (value == null) return null;
    if (value is String) return DateTime.parse(value);
    throw FormatException(
      'Expected "$key1" to be a date string, got ${value.runtimeType}.',
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
      'technician_id': technicianId,
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
    String? technicianId,
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
      technicianId: technicianId ?? this.technicianId,
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
          other.technicianId == technicianId &&
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
    technicianId,
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
