// lib/models/dtos/dashboard_metrics.dart

class DashboardMetrics {
  final Map<String, int> values;

  DashboardMetrics(this.values);

  factory DashboardMetrics.fromJson(Map<String, dynamic> json) {
    return DashboardMetrics(
      json.map((key, value) => MapEntry(key, (value as num?)?.toInt() ?? 0)),
    );
  }

  int operator [](String key) => values[key] ?? 0;

  int get totalUsers => this['totalUsers'];
  int get pendingApprovals => this['pendingApprovals'];
  int get clients => this['clients'];
  int get technicians => this['technicians'];
  int get administrators => this['administrators'];
  int get totalTickets => this['totalTickets'];
  int get openTickets => this['openTickets'];
  int get unresolvedTickets => this['unresolvedTickets'];
  int get resolvedTickets => this['resolvedTickets'];
  int get totalAssets => this['totalAssets'];
  int get availableAssets => this['availableAssets'];
  int get assignedAssets => this['assignedAssets'];
  int get maintenanceAssets => this['maintenanceAssets'];

  // Technician
  int get assignedTickets => this['assignedTickets'];
  int get completedTickets => this['completedTickets'];
  int get inProgressTickets => this['inProgressTickets'];
  int get pendingTickets => this['pendingTickets'];

  // Client
  int get myTickets => this['myTickets'];
}