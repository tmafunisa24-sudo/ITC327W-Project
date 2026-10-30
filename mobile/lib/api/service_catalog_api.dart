// ITC327W-Project - Mobile Service Catalogue Module
// File: lib/api/service_catalog_api.dart
// Feature Branch: feature/keletso-tladi-departments-maintenance
// Student: Keletso Tladi
// Student Number: 221010874
// Date: 2026-10-30
// Module: Service Catalogue - API Layer

// lib/api/service_catalog_api.dart

import 'package:dio/dio.dart';
import '../core/network/api_client.dart';
import '../core/network/api_exception.dart';
import '../models/service_catalog_item.dart';

/// Talks to the backend service catalogue endpoint using Dio.
class ServiceCatalogApi {
  // Shared Dio client (cookies and base URL are configured in ApiClient).
  final Dio _dio;
  /// Uses the supplied Dio, or the shared ApiClient.dio when none is given.
  ServiceCatalogApi([Dio? dio]) : _dio = dio ?? ApiClient.dio;

  /// GET /api/service-catalog
  ///
  /// Returns all active service catalog items.
  /// Throws [ApiException] on failure.
  Future<List<ServiceCatalogItem>> getActive() async {
    // Request the catalogue from the server.
    final response = await _dio.get('/api/service-catalog');
    // A missing status code is treated as 0, which falls through to the error.
    final code = response.statusCode ?? 0;

    // Success: read the list of items.
    if (code == 200) {
      final data = response.data;
      // Keep only JSON objects, then convert each one to a ServiceCatalogItem.
      if (data is List) {
        return data
            .whereType<Map<String, dynamic>>()
            .map(ServiceCatalogItem.fromJson)
            .toList();
      }
      // The server answered 200 but not with a list: show an empty catalogue.
      return const [];
    }

    // Any other status code is reported to the caller as an ApiException.
    throw ApiException(
      _extractError(response) ?? 'Unable to load the service catalog.',
      statusCode: code,
    );
  }

  /// Looks for an error message in the response body.
  /// Checks the 'error' key first, then 'detail'. Returns null if neither is a string.
  String? _extractError(Response response) {
    final data = response.data;
    if (data is Map<String, dynamic>) {
      if (data['error'] is String) return data['error'] as String;
      if (data['detail'] is String) return data['detail'] as String;
    }
    return null;
  }
}