
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

  List<ServiceCatalogItem> _allItems = [];
  bool _isLoadingCatalog = false;
  String? _catalogError;

  String? _selectedDeviceType;
  String? _selectedProblemCategory;
  String? _selectedProblemType;
  ServiceCatalogItem? _selectedService;

  String _title = '';
  String _description = '';
  String _priority = 'Medium';

  final List<File> _images = [];

  bool _isSubmitting = false;
  String? _submitError;
  bool _submitted = false;

  bool _disposed = false;

  // Catalog getters
  List<ServiceCatalogItem> get allItems => List.unmodifiable(_allItems);
  bool get isLoadingCatalog => _isLoadingCatalog;
  String? get catalogError => _catalogError;

  List<String> get deviceTypes {
    return _allItems
        .map((item) => item.deviceType)
        .toSet()
        .toList()
      ..sort();
  }

  List<String> get problemCategories {
    if (_selectedDeviceType == null) return const [];

    return _allItems
        .where((item) =>
            _same(item.deviceType, _selectedDeviceType!))
        .map((item) => item.problemCategory)
        .toSet()
        .toList()
      ..sort();
  }

  List<String> get problemTypes {
    if (_selectedDeviceType == null ||
        _selectedProblemCategory == null) {
      return const [];
    }

    return _allItems
        .where((item) =>
            _same(item.deviceType, _selectedDeviceType!) &&
            _same(item.problemCategory, _selectedProblemCategory!))
        .map((item) => item.problemType)
        .toSet()
        .toList()
      ..sort();
  }

  // Form getters
  String? get selectedDeviceType => _selectedDeviceType;
  String? get selectedProblemCategory => _selectedProblemCategory;
  String? get selectedProblemType => _selectedProblemType;
  ServiceCatalogItem? get selectedService => _selectedService;

  String get title => _title;
  String get description => _description;
  String get priority => _priority;
  List<File> get images => List.unmodifiable(_images);

  // Submission getters
  bool get isSubmitting => _isSubmitting;
  String? get submitError => _submitError;
  bool get submitted => _submitted;

  bool get canSubmit {
    return _selectedDeviceType != null &&
        _selectedProblemCategory != null &&
        _selectedProblemType != null &&
        _selectedService != null &&
        _title.trim().isNotEmpty &&
        _description.trim().isNotEmpty &&
        !_isSubmitting &&
        !_isLoadingCatalog;
  }

  static bool _same(String first, String second) {
    return first.trim().toLowerCase() ==
        second.trim().toLowerCase();
  }

  void _notify() {
    if (!_disposed) notifyListeners();
  }

  void _clearSubmissionState() {
    _submitError = null;
    _submitted = false;
  }

  Future<void> loadCatalog() async {
    if (_isLoadingCatalog) return;

    _isLoadingCatalog = true;
    _catalogError = null;
    _notify();

    try {
      final items = await _catalogService.getActive();

      if (_disposed) return;

      _allItems = items;

      // Clear selections that are no longer available.
      final deviceIsValid = _allItems.any(
        (item) => _same(item.deviceType, _selectedDeviceType ?? ''),
      );

      if (!deviceIsValid) {
        _clearSelections();
      } else {
        final categoryIsValid = _allItems.any(
          (item) =>
              _same(item.deviceType, _selectedDeviceType!) &&
              _same(item.problemCategory, _selectedProblemCategory ?? ''),
        );

        if (!categoryIsValid) {
          _selectedProblemCategory = null;
          _selectedProblemType = null;
          _selectedService = null;
          _title = '';
        } else {
          final matchingService = _findSelectedService();

          if (matchingService == null) {
            _selectedProblemType = null;
            _selectedService = null;
            _title = '';
          } else {
            _selectedService = matchingService;
          }
        }
      }

      _catalogError = null;
    } catch (error) {
      if (_disposed) return;

      _catalogError = error.toString();
      _allItems = [];
      _clearSelections();
    } finally {
      if (!_disposed) {
        _isLoadingCatalog = false;
        _notify();
      }
    }
  }

  ServiceCatalogItem? _findSelectedService() {
    if (_selectedDeviceType == null ||
        _selectedProblemCategory == null ||
        _selectedProblemType == null) {
      return null;
    }

    return _allItems.firstWhereOrNull(
      (item) =>
          _same(item.deviceType, _selectedDeviceType!) &&
          _same(item.problemCategory, _selectedProblemCategory!) &&
          _same(item.problemType, _selectedProblemType!),
    );
  }

  void _clearSelections() {
    _selectedDeviceType = null;
    _selectedProblemCategory = null;
    _selectedProblemType = null;
    _selectedService = null;
    _title = '';
  }

  void setDeviceType(String? value) {
    if (_isSubmitting) return;

    _selectedDeviceType = value;
    _selectedProblemCategory = null;
    _selectedProblemType = null;
    _selectedService = null;
    _title = '';

    _clearSubmissionState();
    _notify();
  }

  void setProblemCategory(String? value) {
    if (_isSubmitting) return;

    _selectedProblemCategory = value;
    _selectedProblemType = null;
    _selectedService = null;
    _title = '';

    _clearSubmissionState();
    _notify();
  }

  void setProblemType(String? value) {
    if (_isSubmitting) return;

    _selectedProblemType = value;
    _selectedService = _findSelectedService();

    if (_selectedService != null &&
        _selectedDeviceType != null &&
        value != null) {
      _title = '$_selectedDeviceType repair: $value';
    } else {
      _title = '';
    }

    _clearSubmissionState();
    _notify();
  }

  void setTitle(String value) {
    if (_isSubmitting) return;

    _title = value;
    _clearSubmissionState();
    _notify();
  }

  void setDescription(String value) {
    if (_isSubmitting) return;

    _description = value;
    _clearSubmissionState();
    _notify();
  }

  void setPriority(String value) {
    if (_isSubmitting) return;

    const allowedPriorities = [
      'Low',
      'Medium',
      'High',
      'Critical',
    ];

    if (!allowedPriorities.contains(value)) return;

    _priority = value;
    _clearSubmissionState();
    _notify();
  }

  void addImage(File file) {
    if (_isSubmitting || _images.length >= 5) return;

    _images.add(file);
    _clearSubmissionState();
    _notify();
  }

  void removeImageAt(int index) {
    if (_isSubmitting ||
        index < 0 ||
        index >= _images.length) {
      return;
    }

    _images.removeAt(index);
    _clearSubmissionState();
    _notify();
  }

  void reset() {
    if (_isSubmitting) return;

    _clearSelections();

    _description = '';
    _priority = 'Medium';

    _images.clear();

    _isSubmitting = false;
    _submitError = null;
    _submitted = false;

    _notify();
  }

  Future<bool> submit() async {
    if (!canSubmit) return false;

    _isSubmitting = true;
    _submitError = null;
    _submitted = false;
    _notify();

    try {
      await _ticketService.createTicket(
        title: _title.trim(),
        description: _description.trim(),
        deviceType: _selectedDeviceType!,
        problemCategory: _selectedProblemCategory!,
        problemType: _selectedProblemType!,
        serviceCatalogId: _selectedService!.id,
        priority: _priority,
        category: _selectedProblemCategory!,
        images: List<File>.unmodifiable(_images),
      );

      if (_disposed) return true;

      _submitted = true;
      return true;
    } catch (error) {
      if (_disposed) return false;

      _submitError = error.toString();
      return false;
    } finally {
      if (!_disposed) {
        _isSubmitting = false;
        _notify();
      }
    }
  }

  @override
  void dispose() {
    _disposed = true;
    super.dispose();
  }
}

extension _ListFirstWhereOrNull<T> on Iterable<T> {
  T? firstWhereOrNull(bool Function(T) test) {
    for (final element in this) {
      if (test(element)) return element;
    }
    return null;
  }
}
