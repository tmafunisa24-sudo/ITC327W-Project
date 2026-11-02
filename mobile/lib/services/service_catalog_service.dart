// lib/services/service_catalog_service.dart

import '../api/service_catalog_api.dart';
import '../models/service_catalog_item.dart';

class ServiceCatalogService {
  final ServiceCatalogApi _api;
  ServiceCatalogService([ServiceCatalogApi? api])
      : _api = api ?? ServiceCatalogApi();

  Future<List<ServiceCatalogItem>> getActive() => _api.getActive();
}