// lib/api/service_catalog_api.dart

import 'package:dio/dio.dart';
import '../core/network/api_client.dart';
import '../core/network/api_exception.dart';
import '../models/service_catalog_item.dart';

class ServiceCatalogApi {
  final Dio _dio;
  ServiceCatalogApi([Dio? dio]) : _dio = dio ?? ApiClient.dio;

  /// GET /api/service-catalog
  ///
  /// Returns all active service catalog items.
  /// Throws [ApiException] on failure.
  Future<List<ServiceCatalogItem>> getActive() async {
    final response = await _dio.get('/api/service-catalog');
    final code = response.statusCode ?? 0;

    if (code == 200) {
      final data = response.data;
      if (data is List) {
        return data.asMap().entries.map((entry) {
          if (entry.value is! Map) {
            throw ApiException(
              'Invalid service catalog item at index ${entry.key}: expected a JSON object.',
              statusCode: code,
            );
          }
          try {
            return ServiceCatalogItem.fromJson(
              Map<String, dynamic>.from(entry.value as Map),
            );
          } catch (error) {
            throw ApiException(
              'Unable to parse service catalog item at index ${entry.key}: $error',
              statusCode: code,
            );
          }
        }).toList();
      }
      throw ApiException(
        'Invalid service catalog response: expected a JSON array.',
        statusCode: code,
      );
    }

    throw ApiException(
      _extractError(response) ?? 'Unable to load the service catalog.',
      statusCode: code,
    );
  }

  String? _extractError(Response response) {
    final data = response.data;
    if (data is Map<String, dynamic>) {
      if (data['error'] is String) return data['error'] as String;
      if (data['detail'] is String) return data['detail'] as String;
    }
    return null;
  }
}
