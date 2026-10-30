// lib/views/catalog/catalog_view.dart

import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../core/design/app_colors.dart';
import '../../core/design/app_radius.dart';
import '../../core/design/app_spacing.dart';
import '../../core/design/widgets/app_header.dart';
import '../../core/design/widgets/app_top_bar.dart';
import '../../models/service_catalog_item.dart';
import '../../viewmodels/service_catalog_viewmodel.dart';

/// Customer-facing service catalogue: browse the services we repair,
/// filtered by device type and problem category.
///
/// The view creates its own [ServiceCatalogViewModel], so it can be added
/// to the router without changing main.dart.
class CatalogView extends StatelessWidget {
  const CatalogView({super.key});

  @override
  Widget build(BuildContext context) {
    return ChangeNotifierProvider(
      create: (_) => ServiceCatalogViewModel()..load(),
      child: const _CatalogBody(),
    );
  }
}

class _CatalogBody extends StatefulWidget {
  const _CatalogBody();

  @override
  State<_CatalogBody> createState() => _CatalogBodyState();
}

class _CatalogBodyState extends State<_CatalogBody> {
  final _searchController = TextEditingController();

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final vm = context.watch<ServiceCatalogViewModel>();

    return Scaffold(
      appBar: AppTopBar(
        title: 'Service Catalogue',
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            tooltip: 'Refresh',
            onPressed: vm.isLoading ? null : () => vm.refresh(),
          ),
        ],
      ),
      body: SafeArea(child: _buildBody(vm)),
    );
  }

  Widget _buildBody(ServiceCatalogViewModel vm) {
    if (vm.isLoading && vm.allItems.isEmpty) {
      return const Center(
        child: CircularProgressIndicator(
          valueColor: AlwaysStoppedAnimation<Color>(AppColors.primary),
        ),
      );
    }

    if (vm.error != null && vm.allItems.isEmpty) {
      return _ErrorState(message: vm.error!, onRetry: () => vm.refresh());
    }

    final items = vm.filteredItems;

    return RefreshIndicator(
      onRefresh: () => vm.refresh(),
      color: AppColors.primary,
      child: CustomScrollView(
        physics: const AlwaysScrollableScrollPhysics(),
        slivers: [
          const SliverPadding(
            padding: EdgeInsets.fromLTRB(
              AppSpacing.md,
              AppSpacing.md,
              AppSpacing.md,
              0,
            ),
            sliver: SliverToBoxAdapter(
              child: AppHeader(
                badge: 'Our Services',
                badgeIcon: Icons.build_circle_outlined,
                title: 'Service Catalogue',
                description:
                    'Find the device and problem you need help with.',
              ),
            ),
          ),
          SliverToBoxAdapter(
            child: Padding(
              padding: const EdgeInsets.only(top: AppSpacing.md),
              child: _Filters(controller: _searchController, vm: vm),
            ),
          ),
          if (items.isEmpty)
            SliverFillRemaining(
              hasScrollBody: false,
              child: _EmptyState(
                filtered: vm.hasActiveFilters,
                onClear: () {
                  _searchController.clear();
                  vm.clearFilters();
                },
              ),
            )
          else
            SliverList.separated(
              itemCount: items.length,
              separatorBuilder: (_, _) => const Divider(
                height: 1,
                indent: AppSpacing.md,
                endIndent: AppSpacing.md,
                color: AppColors.border,
              ),
              itemBuilder: (context, i) => _ServiceTile(item: items[i]),
            ),
          const SliverPadding(padding: EdgeInsets.only(bottom: AppSpacing.xl)),
        ],
      ),
    );
  }
}

// ────────────────────────────────────────────────────────
// Search + filters
// ────────────────────────────────────────────────────────
class _Filters extends StatelessWidget {
  final TextEditingController controller;
  final ServiceCatalogViewModel vm;

  const _Filters({required this.controller, required this.vm});

  @override
  Widget build(BuildContext context) {
    return Container(
      color: AppColors.surface,
      padding: const EdgeInsets.fromLTRB(
        AppSpacing.sm,
        AppSpacing.sm,
        AppSpacing.sm,
        AppSpacing.sm,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          TextField(
            controller: controller,
            onChanged: vm.setSearchTerm,
            textInputAction: TextInputAction.search,
            decoration: InputDecoration(
              hintText: 'Search services...',
              prefixIcon: const Icon(Icons.search, size: 20),
              suffixIcon: vm.searchTerm.isNotEmpty
                  ? IconButton(
                      icon: const Icon(Icons.clear, size: 18),
                      onPressed: () {
                        controller.clear();
                        vm.setSearchTerm('');
                      },
                    )
                  : null,
              isDense: true,
              contentPadding: const EdgeInsets.symmetric(
                horizontal: 12,
                vertical: 12,
              ),
              border: OutlineInputBorder(
                borderRadius: AppRadius.borderLg,
                borderSide: const BorderSide(color: AppColors.borderInput),
              ),
              enabledBorder: OutlineInputBorder(
                borderRadius: AppRadius.borderLg,
                borderSide: const BorderSide(color: AppColors.borderInput),
              ),
              focusedBorder: OutlineInputBorder(
                borderRadius: AppRadius.borderLg,
                borderSide:
                    const BorderSide(color: AppColors.primary, width: 2),
              ),
              filled: true,
              fillColor: AppColors.surface,
            ),
          ),
          const SizedBox(height: AppSpacing.sm),
          const _FilterLabel('Device type'),
          _ChipRow(
            options: vm.deviceTypes,
            selected: vm.selectedDeviceType,
            onSelected: vm.setDeviceType,
          ),
          if (vm.problemCategories.isNotEmpty) ...[
            const SizedBox(height: AppSpacing.xs),
            const _FilterLabel('Problem category'),
            _ChipRow(
              options: vm.problemCategories,
              selected: vm.selectedProblemCategory,
              onSelected: vm.setProblemCategory,
            ),
          ],
        ],
      ),
    );
  }
}

class _FilterLabel extends StatelessWidget {
  final String text;
  const _FilterLabel(this.text);

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 4),
      child: Text(
        text.toUpperCase(),
        style: const TextStyle(
          fontSize: 10,
          color: AppColors.textSubtle,
          fontWeight: FontWeight.w700,
          letterSpacing: 0.4,
        ),
      ),
    );
  }
}

/// A horizontally scrolling row of chips with an "All" chip first.
class _ChipRow extends StatelessWidget {
  final List<String> options;
  final String? selected;
  final ValueChanged<String?> onSelected;

  const _ChipRow({
    required this.options,
    required this.selected,
    required this.onSelected,
  });

  @override
  Widget build(BuildContext context) {
    final labels = ['', ...options];
    return SizedBox(
      height: 36,
      child: ListView.builder(
        scrollDirection: Axis.horizontal,
        itemCount: labels.length,
        itemBuilder: (context, i) {
          final label = labels[i];
          final isSelected = (selected ?? '') == label;
          return Padding(
            padding: const EdgeInsets.only(right: AppSpacing.xs),
            child: ChoiceChip(
              label: Text(label.isEmpty ? 'All' : label),
              selected: isSelected,
              onSelected: (_) => onSelected(label.isEmpty ? null : label),
              selectedColor: AppColors.primary,
              backgroundColor: AppColors.surface,
              labelStyle: TextStyle(
                color: isSelected ? Colors.white : AppColors.textPrimary,
                fontWeight: FontWeight.w600,
                fontSize: 12,
              ),
              side: const BorderSide(color: AppColors.border),
              shape: const RoundedRectangleBorder(
                borderRadius: AppRadius.borderPill,
              ),
              padding: const EdgeInsets.symmetric(horizontal: 8),
            ),
          );
        },
      ),
    );
  }
}

// ────────────────────────────────────────────────────────
// Service tile
// ────────────────────────────────────────────────────────
class _ServiceTile extends StatelessWidget {
  final ServiceCatalogItem item;
  const _ServiceTile({required this.item});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(
        horizontal: AppSpacing.md,
        vertical: AppSpacing.sm,
      ),
      child: Row(
        children: [
          Container(
            width: 44,
            height: 44,
            decoration: BoxDecoration(
              color: AppColors.primarySoft,
              borderRadius: BorderRadius.circular(AppRadius.sm),
            ),
            child: Icon(_iconFor(item.deviceType), color: AppColors.primary),
          ),
          const SizedBox(width: AppSpacing.sm),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  item.problemType,
                  style: const TextStyle(
                    fontSize: 15,
                    fontWeight: FontWeight.w700,
                    color: AppColors.textPrimary,
                  ),
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                ),
                const SizedBox(height: 6),
                Wrap(
                  spacing: 6,
                  runSpacing: 4,
                  children: [
                    if (item.deviceType.isNotEmpty)
                      _InfoChip(label: item.deviceType),
                    if (item.problemCategory.isNotEmpty)
                      _InfoChip(label: item.problemCategory),
                  ],
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  static IconData _iconFor(String deviceType) {
    final t = deviceType.toLowerCase();
    if (t.contains('laptop') || t.contains('notebook')) {
      return Icons.laptop_mac;
    }
    if (t.contains('desktop') || t.contains('pc') || t.contains('computer')) {
      return Icons.desktop_windows_outlined;
    }
    if (t.contains('phone') || t.contains('mobile')) {
      return Icons.smartphone;
    }
    if (t.contains('tablet')) return Icons.tablet_mac;
    if (t.contains('printer')) return Icons.print_outlined;
    if (t.contains('network') || t.contains('router')) return Icons.wifi;
    return Icons.build_outlined;
  }
}

class _InfoChip extends StatelessWidget {
  final String label;
  const _InfoChip({required this.label});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
      decoration: BoxDecoration(
        color: AppColors.surfaceSoft,
        borderRadius: BorderRadius.circular(AppRadius.xs),
      ),
      child: Text(
        label,
        style: const TextStyle(
          fontSize: 11,
          color: AppColors.textSubtle,
          fontWeight: FontWeight.w600,
        ),
      ),
    );
  }
}

// ────────────────────────────────────────────────────────
// Error + empty states
// ────────────────────────────────────────────────────────
class _ErrorState extends StatelessWidget {
  final String message;
  final VoidCallback onRetry;

  const _ErrorState({required this.message, required this.onRetry});

  @override
  Widget build(BuildContext context) {
    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      children: [
        Padding(
          padding: const EdgeInsets.all(AppSpacing.xxl),
          child: Column(
            children: [
              const SizedBox(height: AppSpacing.xxxl),
              Icon(
                Icons.error_outline,
                size: 64,
                color: AppColors.danger.withValues(alpha: 0.6),
              ),
              const SizedBox(height: AppSpacing.md),
              const Text(
                'Unable to load the catalogue',
                style: TextStyle(
                  fontSize: 18,
                  fontWeight: FontWeight.w700,
                  color: AppColors.textPrimary,
                ),
              ),
              const SizedBox(height: AppSpacing.xs),
              Text(
                message,
                textAlign: TextAlign.center,
                style: const TextStyle(
                  color: AppColors.textSubtle,
                  fontSize: 14,
                ),
              ),
              const SizedBox(height: AppSpacing.xl),
              ElevatedButton.icon(
                onPressed: onRetry,
                icon: const Icon(Icons.refresh),
                label: const Text('Try Again'),
              ),
            ],
          ),
        ),
      ],
    );
  }
}

class _EmptyState extends StatelessWidget {
  final bool filtered;
  final VoidCallback onClear;

  const _EmptyState({required this.filtered, required this.onClear});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.all(AppSpacing.xxl),
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(
            Icons.build_circle_outlined,
            size: 72,
            color: AppColors.primary.withValues(alpha: 0.4),
          ),
          const SizedBox(height: AppSpacing.md),
          Text(
            filtered ? 'No matching services' : 'No services available',
            style: const TextStyle(
              fontSize: 18,
              fontWeight: FontWeight.w700,
              color: AppColors.textPrimary,
            ),
          ),
          const SizedBox(height: AppSpacing.xs),
          Text(
            filtered
                ? 'Try a different search or clear the filters.'
                : 'There are no services to display right now.',
            textAlign: TextAlign.center,
            style: const TextStyle(color: AppColors.textSubtle, fontSize: 14),
          ),
          if (filtered) ...[
            const SizedBox(height: AppSpacing.md),
            TextButton(onPressed: onClear, child: const Text('Clear filters')),
          ],
        ],
      ),
    );
  }
}
