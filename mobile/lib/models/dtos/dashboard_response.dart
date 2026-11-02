// lib/models/dtos/dashboard_response.dart

import 'dashboard_metrics.dart';

class DashboardResponse {
  final String role;
  final DashboardMetrics metrics;
  final List<RecentTicket> recentTickets;
  final TopTechnician? topTechnician;

  const DashboardResponse({
    required this.role,
    required this.metrics,
    required this.recentTickets,
    this.topTechnician,
  });

  factory DashboardResponse.fromJson(Map<String, dynamic> json) {
    return DashboardResponse(
      role: json['role'] as String? ?? 'Client',
      metrics: DashboardMetrics.fromJson(
        (json['metrics'] as Map<String, dynamic>?) ?? const {},
      ),
      recentTickets: (json['recentTickets'] as List?)
              ?.whereType<Map<String, dynamic>>()
              .map(RecentTicket.fromJson)
              .toList() ??
          const [],
      topTechnician: json['topTechnician'] is Map<String, dynamic>
          ? TopTechnician.fromJson(json['topTechnician'] as Map<String, dynamic>)
          : null,
    );
  }
}

class RecentTicket {
  final String? id;
  final String title;
  final String status;
  final String priority;
  final String? assignedTechnician;
  final DateTime? updatedAt;

  const RecentTicket({
    this.id,
    required this.title,
    required this.status,
    required this.priority,
    this.assignedTechnician,
    this.updatedAt,
  });

  factory RecentTicket.fromJson(Map<String, dynamic> json) {
    return RecentTicket(
      id: json['id'] as String?,
      title: json['title'] as String? ?? '',
      status: json['status'] as String? ?? 'Open',
      priority: json['priority'] as String? ?? 'Medium',
      assignedTechnician: json['assignedTechnician'] as String?,
      updatedAt: json['updatedAt'] != null
          ? DateTime.tryParse(json['updatedAt'] as String)
          : null,
    );
  }
}

class TopTechnician {
  final String name;
  final int resolvedCount;

  const TopTechnician({required this.name, required this.resolvedCount});

  factory TopTechnician.fromJson(Map<String, dynamic> json) {
    return TopTechnician(
      name: json['name'] as String? ?? '',
      resolvedCount: (json['resolvedCount'] as num?)?.toInt() ?? 0,
    );
  }
}