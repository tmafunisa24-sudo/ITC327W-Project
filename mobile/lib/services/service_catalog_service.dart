// ITC327W-Project - Mobile Service Catalogue Module
// File: lib/services/service_catalog_service.dart
// Feature Branch: feature/keletso-tladi-departments-maintenance
// Student: Keletso Tladi
// Student Number: 221010874
// Date: 2026-10-30
// Module: Service Catalogue - Service Layer

// lib/services/service_catalog_service.dart

import '../api/service_catalog_api.dart';
import '../models/service_catalog_item.dart';

/// Service layer for the catalogue.
/// View models talk to this class; it talks to ServiceCatalogApi.
/// This keeps screens free of HTTP details and makes testing easier.
class ServiceCatalogService {
  // The API used for network calls (replaceable in tests).
  final ServiceCatalogApi _api;
  /// Uses the supplied API, or a default ServiceCatalogApi when none is given.
  ServiceCatalogService([ServiceCatalogApi? api])
      : _api = api ?? ServiceCatalogApi();

  /// Returns all active catalogue items.
  /// Throws ApiException if the request fails.
  Future<List<ServiceCatalogItem>> getActive() => _api.getActive();
}