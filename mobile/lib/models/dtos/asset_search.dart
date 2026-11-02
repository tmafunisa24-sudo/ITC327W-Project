// lib/models/dtos/asset_search.dart

class AssetSearch {
  final String? searchTerm;
  final String? category;
  final String? status;
  final int page;
  final int pageSize;

  const AssetSearch({
    this.searchTerm,
    this.category,
    this.status,
    this.page = 1,
    this.pageSize = 10,
  });

  // Converts to query params for the API call.
  // Returns a Map<String, dynamic> ready to hand to Dio.
  Map<String, dynamic> toQueryParams() {
    return {
      if (searchTerm != null && searchTerm!.isNotEmpty)
        'searchTerm': searchTerm,
      if (category != null && category!.isNotEmpty) 'category': category,
      if (status != null && status!.isNotEmpty) 'status': status,
      'page': page,
      'pageSize': pageSize,
    };
  }

  AssetSearch copyWith({
    String? searchTerm,
    String? category,
    String? status,
    int? page,
    int? pageSize,
    bool clearSearchTerm = false,
    bool clearCategory = false,
    bool clearStatus = false,
  }) {
    return AssetSearch(
      searchTerm: clearSearchTerm ? null : (searchTerm ?? this.searchTerm),
      category: clearCategory ? null : (category ?? this.category),
      status: clearStatus ? null : (status ?? this.status),
      page: page ?? this.page,
      pageSize: pageSize ?? this.pageSize,
    );
  }
}