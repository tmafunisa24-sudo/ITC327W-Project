// lib/viewmodels/create_ticket_viewmodel.dart

import 'dart:io';
import 'package:flutter/foundation.dart';

import '../models/service_catalog_item.dart';
import '../services/service_catalog_service.dart';
import '../services/ticket_service.dart';

class CreateTicketViewModel extends ChangeNotifier {
  final ServiceCatalogService _catalogService;
  final TicketService _ticketService;

  CreateTicketViewModel({
    ServiceCatalogService? catalogService,
    TicketService? ticketService,
  })  : _catalogService = catalogService ?? ServiceCatalogService(),
        _ticketService = ticketService ?? TicketService();

  // ── Catalog data ──
  List<ServiceCatalogItem> _allItems = [];
  bool _isLoadingCatalog = false;
  String? _catalogError;

  // ── Form state ──
  String? _selectedDeviceType;
  String? _selectedProblemCategory;
  String? _selectedProblemType;
  ServiceCatalogItem? _selectedService;
  String _title = '';
  String _description = '';
  String _priority = 'Medium';
  final List<File> _images = [];

  // ── Submit state ──
  bool _isSubmitting = false;
  String? _submitError;
  bool _submitted = false;

  // ── Getters: catalog ──
  List<ServiceCatalogItem> get allItems => _allItems;
  bool get isLoadingCatalog => _isLoadingCatalog;
  String? get catalogError => _catalogError;

  // ── Getters: derived dropdown options ──
  /// Unique device types, sorted.
  List<String> get deviceTypes {
    final set = _allItems.map((e) => e.deviceType).toSet().toList()..sort();
    return set;
  }

  /// Problem categories available for the selected device.
  List<String> get problemCategories {
    if (_selectedDeviceType == null) return const [];
    final set = _allItems
        .where((e) =>
            e.deviceType.toLowerCase() == _selectedDeviceType!.toLowerCase())
        .map((e) => e.problemCategory)
        .toSet()
        .toList()
      ..sort();
    return set;
  }

  /// Problem types available for the selected device + category.
  List<String> get problemTypes {
    if (_selectedDeviceType == null || _selectedProblemCategory == null) {
      return const [];
    }
    return _allItems
        .where((e) =>
            e.deviceType.toLowerCase() ==
                _selectedDeviceType!.toLowerCase() &&
            e.problemCategory.toLowerCase() ==
                _selectedProblemCategory!.toLowerCase())
        .map((e) => e.problemType)
        .toList();
  }

  // ── Getters: form values ──
  String? get selectedDeviceType => _selectedDeviceType;
  String? get selectedProblemCategory => _selectedProblemCategory;
  String? get selectedProblemType => _selectedProblemType;
  ServiceCatalogItem? get selectedService => _selectedService;
  String get title => _title;
  String get description => _description;
  String get priority => _priority;
  List<File> get images => List.unmodifiable(_images);

  // ── Getters: submit state ──
  bool get isSubmitting => _isSubmitting;
  String? get submitError => _submitError;
  bool get submitted => _submitted;

  // ── Derived: is form valid? ──
  bool get canSubmit {
    return _selectedService != null &&
        _description.trim().isNotEmpty &&
        !_isSubmitting;
  }

  // ────────────────────────────────────────────────────────
  // Actions
  // ────────────────────────────────────────────────────────

  Future<void> loadCatalog() async {
    _isLoadingCatalog = true;
    _catalogError = null;
    notifyListeners();

    try {
      _allItems = await _catalogService.getActive();
      _catalogError = null;
    } catch (e) {
      _catalogError = e.toString();
      _allItems = [];
    } finally {
      _isLoadingCatalog = false;
      notifyListeners();
    }
  }

  void setDeviceType(String? value) {
    _selectedDeviceType = value;
    // Reset dependent dropdowns when parent changes
    _selectedProblemCategory = null;
    _selectedProblemType = null;
    _selectedService = null;
    _title = '';
    notifyListeners();
  }

  void setProblemCategory(String? value) {
    _selectedProblemCategory = value;
    _selectedProblemType = null;
    _selectedService = null;
    _title = '';
    notifyListeners();
  }

  void setProblemType(String? value) {
    _selectedProblemType = value;

    // Auto-find the matching service catalog item
    _selectedService = _allItems.firstWhereOrNull((e) =>
        e.deviceType.toLowerCase() ==
            (_selectedDeviceType ?? '').toLowerCase() &&
        e.problemCategory.toLowerCase() ==
            (_selectedProblemCategory ?? '').toLowerCase() &&
        e.problemType.toLowerCase() == (value ?? '').toLowerCase());

    // Auto-generate title if empty
    if (_selectedService != null &&
        _selectedDeviceType != null &&
        value != null) {
      _title = '$_selectedDeviceType repair: $value';
    }

    notifyListeners();
  }

  void setTitle(String value) {
    _title = value;
    notifyListeners();
  }

  void setDescription(String value) {
    _description = value;
    notifyListeners();
  }

  void setPriority(String value) {
    _priority = value;
    notifyListeners();
  }

  void addImage(File file) {
    if (_images.length >= 5) return; // max 5
    _images.add(file);
    notifyListeners();
  }

  void removeImageAt(int index) {
    if (index < 0 || index >= _images.length) return;
    _images.removeAt(index);
    notifyListeners();
  }

  void reset() {
    _selectedDeviceType = null;
    _selectedProblemCategory = null;
    _selectedProblemType = null;
    _selectedService = null;
    _title = '';
    _description = '';
    _priority = 'Medium';
    _images.clear();
    _isSubmitting = false;
    _submitError = null;
    _submitted = false;
    notifyListeners();
  }

  Future<bool> submit() async {
    if (!canSubmit) return false;

    _isSubmitting = true;
    _submitError = null;
    _submitted = false;
    notifyListeners();

    try {
      await _ticketService.createTicket(
        title: _title,
        description: _description,
        deviceType: _selectedDeviceType!,
        problemCategory: _selectedProblemCategory!,
        problemType: _selectedProblemType!,
        serviceCatalogId: _selectedService!.id,
        priority: _priority,
        category: _selectedProblemCategory!,
        images: _images,
      );

      _submitted = true;
      _isSubmitting = false;
      notifyListeners();
      return true;
    } catch (e) {
      _submitError = e.toString();
      _isSubmitting = false;
      notifyListeners();
      return false;
    }
  }
}

// Small helper — avoids importing collection package
extension _ListFirstWhereOrNull<T> on Iterable<T> {
  T? firstWhereOrNull(bool Function(T) test) {
    for (final element in this) {
      if (test(element)) return element;
    }
    return null;
  }
}