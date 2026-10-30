// lib/models/dtos/update_ticket_status.dart

class UpdateTicketStatus {
  final String status;
  final String? assignedTechnician;
  final String? comment;
  final DateTime? dueDate;

  const UpdateTicketStatus({
    required this.status,
    this.assignedTechnician,
    this.comment,
    this.dueDate,
  });

  Map<String, dynamic> toJson() {
    return {
      'status': status,
      if (assignedTechnician != null && assignedTechnician!.trim().isNotEmpty)
        'assignedTechnician': assignedTechnician!.trim(),
      if (comment != null && comment!.trim().isNotEmpty)
        'comment': comment!.trim(),
      if (dueDate != null) 'dueDate': dueDate!.toIso8601String(),
    };
  }
}