// lib/viewmodels/asset_viewmodel.dart

import 'package:flutter/foundation.dart';
import '../models/asset.dart';
import '../services/asset_service.dart';

class AssetViewModel extends ChangeNotifier {
  final AssetService _service;
  AssetViewModel([AssetService? service])
      : _service = service ?? AssetService();

  // ── List state ──
  List<Asset> _assets = [];
  bool _isLoading = false;
  String? _error;
  String? _searchTerm;
  String? _categoryFilter;
  String? _statusFilter;

  // ── Detail state ──
  Asset? _currentAsset;
  bool _isLoadingDetail = false;
  String? _detailError;

  // ── Getters: list ──
  List<Asset> get assets => _assets;
  bool get isLoading => _isLoading;
  String? get error => _error;
  String? get searchTerm => _searchTerm;
  String? get categoryFilter => _categoryFilter;
  String? get statusFilter => _statusFilter;

  // ── Getters: detail ──
  Asset? get currentAsset => _currentAsset;
  bool get isLoadingDetail => _isLoadingDetail;
  String? get detailError => _detailError;

  // ── Derived stats ──
  int get totalCount => _assets.length;
  int get availableCount => _assets
      .where((a) => a.status.toLowerCase() == 'available')
      .length;
  int get assignedCount =>
      _assets.where((a) => a.status.toLowerCase() == 'assigned').length;
  int get maintenanceCount => _assets
      .where((a) => a.status.toLowerCase() == 'maintenance')
      .length;

  // ── Unique categories (for filter dropdown) ──
  List<String> get categories {
    final set = _assets
        .map((a) => a.category)
        .where((c) => c.trim().isNotEmpty)
        .toSet()
        .toList()
      ..sort();
    return set;
  }

  // ── Actions ──

  Future<void> load() async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      _assets = await _service.getAssets(
        searchTerm: _searchTerm,
        category: _categoryFilter,
        status: _statusFilter,
      );
      _error = null;
    } catch (e) {
      _error = e.toString();
      _assets = [];
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<void> refresh() => load();

  void setSearchTerm(String? term) {
    _searchTerm = (term == null || term.trim().isEmpty) ? null : term.trim();
    load();
  }

  void setCategoryFilter(String? category) {
    _categoryFilter = (category == null || category.isEmpty) ? null : category;
    load();
  }

  void setStatusFilter(String? status) {
    _statusFilter = (status == null || status.isEmpty) ? null : status;
    load();
  }

  void clearFilters() {
    _searchTerm = null;
    _categoryFilter = null;
    _statusFilter = null;
    load();
  }

  Future<void> loadAsset(String id) async {
    _isLoadingDetail = true;
    _detailError = null;
    _currentAsset = null;
    notifyListeners();

    try {
      _currentAsset = await _service.getAsset(id);
      _detailError = null;
    } catch (e) {
      _detailError = e.toString();
      _currentAsset = null;
    } finally {
      _isLoadingDetail = false;
      notifyListeners();
    }
  }

  void clearCurrentAsset() {
    _currentAsset = null;
    _detailError = null;
    _isLoadingDetail = false;
    notifyListeners();
  }
}