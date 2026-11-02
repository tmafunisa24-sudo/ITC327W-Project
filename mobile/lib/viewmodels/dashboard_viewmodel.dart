// lib/viewmodels/dashboard_viewmodel.dart

import 'package:flutter/foundation.dart';
import '../models/dtos/dashboard_response.dart';
import '../services/dashboard_service.dart';

class DashboardViewModel extends ChangeNotifier {
  final DashboardService _service;
  DashboardViewModel([DashboardService? service])
      : _service = service ?? DashboardService();

  DashboardResponse? _data;
  bool _isLoading = false;
  String? _error;

  DashboardResponse? get data => _data;
  bool get isLoading => _isLoading;
  String? get error => _error;

  Future<void> load() async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      _data = await _service.getDashboard();
      _error = null;
    } catch (e) {
      _error = e.toString();
      _data = null;
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<void> refresh() => load();

  void clear() {
    _data = null;
    _error = null;
    _isLoading = false;
    notifyListeners();
  }
}