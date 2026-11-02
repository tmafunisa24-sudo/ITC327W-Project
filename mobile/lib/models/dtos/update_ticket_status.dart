// lib/models/dtos/update_ticket_status.dart

class UpdateTicketStatus {
  final String status;
  final String? assignedTechnicianId;
  final bool assignmentSubmitted;
  final String? comment;
  final DateTime? dueDate;

  const UpdateTicketStatus({
    required this.status,
    this.assignedTechnicianId,
    this.assignmentSubmitted = false,
    this.comment,
    this.dueDate,
  });

  Map<String, dynamic> toJson() {
    return {
      'status': status,
      if (assignedTechnicianId != null && assignedTechnicianId!.trim().isNotEmpty)
        'assignedTechnicianId': assignedTechnicianId!.trim(),
      'assignmentSubmitted': assignmentSubmitted,
      if (comment != null && comment!.trim().isNotEmpty)
        'comment': comment!.trim(),
      if (dueDate != null) 'dueDate': dueDate!.toIso8601String(),
    };
  }
}
