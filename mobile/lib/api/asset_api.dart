// lib/api/asset_api.dart

import 'package:dio/dio.dart';
import '../core/network/api_client.dart';
import '../core/network/api_exception.dart';
import '../models/asset.dart';

class AssetApi {
  final Dio _dio;
  AssetApi([Dio? dio]) : _dio = dio ?? ApiClient.dio;

  /// GET /api/assets?searchTerm=&category=&status=
  Future<List<Asset>> getAssets({
    String? searchTerm,
    String? category,
    String? status,
  }) async {
    final queryParams = <String, dynamic>{};
    if (searchTerm != null && searchTerm.trim().isNotEmpty) {
      queryParams['searchTerm'] = searchTerm.trim();
    }
    if (category != null && category.trim().isNotEmpty) {
      queryParams['category'] = category.trim();
    }
    if (status != null && status.trim().isNotEmpty) {
      queryParams['status'] = status.trim();
    }

    final response = await _dio.get(
      '/api/assets',
      queryParameters: queryParams.isEmpty ? null : queryParams,
    );

    final code = response.statusCode ?? 0;

    if (code == 200) {
      final data = response.data;
      if (data is List) {
        return data
            .whereType<Map<String, dynamic>>()
            .map(Asset.fromJson)
            .toList();
      }
      return const [];
    }

    throw ApiException(
      _extractError(response) ?? 'Unable to load assets.',
      statusCode: code,
    );
  }

  /// GET /api/assets/{id}
  Future<Asset> getById(String id) async {
    final response = await _dio.get('/api/assets/$id');
    final code = response.statusCode ?? 0;

    if (code == 200) {
      final data = response.data;
      if (data is Map<String, dynamic>) {
        return Asset.fromJson(data);
      }
      throw ApiException('Invalid asset response.', statusCode: code);
    }

    throw ApiException(
      _extractError(response) ?? 'Unable to load the asset.',
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