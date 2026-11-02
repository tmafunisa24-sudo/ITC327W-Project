// lib/services/dashboard_service.dart

import '../api/dashboard_api.dart';
import '../models/dtos/dashboard_response.dart';

class DashboardService {
  final DashboardApi _api;
  DashboardService([DashboardApi? api]) : _api = api ?? DashboardApi();

  Future<DashboardResponse> getDashboard() => _api.getDashboard();
}