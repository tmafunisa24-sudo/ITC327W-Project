// lib/viewmodels/service_catalog_viewmodel.dart

import 'package:flutter/foundation.dart';

import '../models/service_catalog_item.dart';
import '../services/service_catalog_service.dart';

/// Drives the customer-facing service catalogue screen.
///
/// Loads the active catalogue once, then lets the customer narrow it by
/// device type, problem category and a free-text search. Filtering is done
/// locally, so no extra network calls are made.
class ServiceCatalogViewModel extends ChangeNotifier {
  final ServiceCatalogService _service;
  ServiceCatalogViewModel([ServiceCatalogService? service])
      : _service = service ?? ServiceCatalogService();

  // ── State ──
  List<ServiceCatalogItem> _allItems = [];
  bool _isLoading = false;
  String? _error;
  String? _deviceType;
  String? _problemCategory;
  String _searchTerm = '';

  // ── Getters ──
  List<ServiceCatalogItem> get allItems => _allItems;
  bool get isLoading => _isLoading;
  String? get error => _error;
  String? get selectedDeviceType => _deviceType;
  String? get selectedProblemCategory => _problemCategory;
  String get searchTerm => _searchTerm;

  /// Distinct device types in the catalogue, sorted A–Z.
  List<String> get deviceTypes => _distinct(_allItems.map((i) => i.deviceType));

  /// Distinct problem categories for the selected device type
  /// (or for every device type when none is selected), sorted A–Z.
  List<String> get problemCategories {
    final scoped = _deviceType == null
        ? _allItems
        : _allItems.where((i) => i.deviceType == _deviceType);
    return _distinct(scoped.map((i) => i.problemCategory));
  }

  /// Items matching the current device type, category and search term.
  List<ServiceCatalogItem> get filteredItems {
    final q = _searchTerm.trim().toLowerCase();
    return _allItems.where((i) {
      if (_deviceType != null && i.deviceType != _deviceType) return false;
      if (_problemCategory != null && i.problemCategory != _problemCategory) {
        return false;
      }
      if (q.isEmpty) return true;
      return i.deviceType.toLowerCase().contains(q) ||
          i.problemCategory.toLowerCase().contains(q) ||
          i.problemType.toLowerCase().contains(q);
    }).toList();
  }

  bool get hasActiveFilters =>
      _deviceType != null ||
      _problemCategory != null ||
      _searchTerm.trim().isNotEmpty;

  // ── Actions ──

  /// Loads the catalogue. Safe to call again to refresh.
  Future<void> load() async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      // Only active items are shown to customers.
      final items = await _service.getActive();
      _allItems = items.where((i) => i.isActive).toList();
      _error = null;

      // Drop filters that no longer exist after a refresh.
      if (_deviceType != null && !deviceTypes.contains(_deviceType)) {
        _deviceType = null;
      }
      if (_problemCategory != null &&
          !problemCategories.contains(_problemCategory)) {
        _problemCategory = null;
      }
    } catch (e) {
      _error = e.toString();
      _allItems = [];
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<void> refresh() => load();

  void setDeviceType(String? value) {
    if (_deviceType == value) return;
    _deviceType = value;
    // The category list depends on the device type, so reset it.
    _problemCategory = null;
    notifyListeners();
  }

  void setProblemCategory(String? value) {
    if (_problemCategory == value) return;
    _problemCategory = value;
    notifyListeners();
  }

  void setSearchTerm(String value) {
    if (_searchTerm == value) return;
    _searchTerm = value;
    notifyListeners();
  }

  void clearFilters() {
    _deviceType = null;
    _problemCategory = null;
    _searchTerm = '';
    notifyListeners();
  }

  // ── Helpers ──
  static List<String> _distinct(Iterable<String> values) {
    final set = values.where((v) => v.trim().isNotEmpty).toSet().toList();
    set.sort((a, b) => a.toLowerCase().compareTo(b.toLowerCase()));
    return set;
  }
}
