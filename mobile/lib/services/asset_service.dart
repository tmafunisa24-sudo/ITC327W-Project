// lib/services/asset_service.dart

import '../api/asset_api.dart';
import '../models/asset.dart';

class AssetService {
  final AssetApi _api;
  AssetService([AssetApi? api]) : _api = api ?? AssetApi();

  Future<List<Asset>> getAssets({
    String? searchTerm,
    String? category,
    String? status,
  }) {
    return _api.getAssets(
      searchTerm: searchTerm,
      category: category,
      status: status,
    );
  }

  Future<Asset> getAsset(String id) => _api.getById(id);
}