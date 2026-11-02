// lib/models/dtos/ticket_prediction.dart

class TicketPrediction {
  final String priority;
  final int estimatedResolutionHours;
  final String recommendedTechnician;

  const TicketPrediction({
    required this.priority,
    required this.estimatedResolutionHours,
    required this.recommendedTechnician,
  });

  factory TicketPrediction.fromJson(Map<String, dynamic> json) {
    return TicketPrediction(
      priority: json['Priority'] as String? ??
          json['priority'] as String? ??
          '',
      estimatedResolutionHours:
          json['EstimatedResolutionHours'] as int? ??
              json['estimatedResolutionHours'] as int? ??
              0,
      recommendedTechnician:
          json['RecommendedTechnician'] as String? ??
              json['recommendedTechnician'] as String? ??
              '',
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'priority': priority,
      'estimatedResolutionHours': estimatedResolutionHours,
      'recommendedTechnician': recommendedTechnician,
    };
  }

  TicketPrediction copyWith({
    String? priority,
    int? estimatedResolutionHours,
    String? recommendedTechnician,
  }) {
    return TicketPrediction(
      priority: priority ?? this.priority,
      estimatedResolutionHours:
          estimatedResolutionHours ?? this.estimatedResolutionHours,
      recommendedTechnician:
          recommendedTechnician ?? this.recommendedTechnician,
    );
  }

  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      other is TicketPrediction &&
          other.priority == priority &&
          other.estimatedResolutionHours == estimatedResolutionHours &&
          other.recommendedTechnician == recommendedTechnician;

  @override
  int get hashCode =>
      Object.hash(priority, estimatedResolutionHours, recommendedTechnician);
}