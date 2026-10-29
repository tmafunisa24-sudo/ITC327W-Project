// lib/views/assets/asset_details_view.dart

import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../core/design/app_colors.dart';
import '../../core/design/app_radius.dart';
import '../../core/design/app_spacing.dart';
import '../../core/design/widgets/app_badge.dart';
import '../../core/design/widgets/app_card.dart';
import '../../core/design/widgets/app_top_bar.dart';
import '../../models/asset.dart';
import '../../viewmodels/asset_viewmodel.dart';

class AssetDetailsView extends StatefulWidget {
  final String assetId;
  const AssetDetailsView({super.key, required this.assetId});

  @override
  State<AssetDetailsView> createState() => _AssetDetailsViewState();
}

class _AssetDetailsViewState extends State<AssetDetailsView> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<AssetViewModel>().loadAsset(widget.assetId);
    });
  }

  @override
  Widget build(BuildContext context) {
    final vm = context.watch<AssetViewModel>();

    return Scaffold(
      appBar: AppTopBar(
        title: 'Asset Details',
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            tooltip: 'Reload',
            onPressed: vm.isLoadingDetail
                ? null
                : () => vm.loadAsset(widget.assetId),
          ),
        ],
      ),
      body: SafeArea(child: _buildBody(vm)),
    );
  }

  Widget _buildBody(AssetViewModel vm) {
    if (vm.isLoadingDetail && vm.currentAsset == null) {
      return const Center(
        child: CircularProgressIndicator(
          valueColor: AlwaysStoppedAnimation<Color>(AppColors.primary),
        ),
      );
    }

    if (vm.detailError != null && vm.currentAsset == null) {
      return _ErrorState(
        message: vm.detailError!,
        onRetry: () => vm.loadAsset(widget.assetId),
      );
    }

    final asset = vm.currentAsset;
    if (asset == null) {
      return const Center(child: CircularProgressIndicator());
    }

    return RefreshIndicator(
      onRefresh: () => vm.loadAsset(widget.assetId),
      color: AppColors.primary,
      child: SingleChildScrollView(
        physics: const AlwaysScrollableScrollPhysics(),
        padding: const EdgeInsets.all(AppSpacing.md),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            _AssetHeaderCard(asset: asset),
            const SizedBox(height: AppSpacing.md),

            if (asset.imageUrl != null && asset.imageUrl!.isNotEmpty) ...[
              AppSectionCard(
                title: 'Image',
                child: ClipRRect(
                  borderRadius: BorderRadius.circular(AppRadius.sm),
                  child: Image.network(
                    asset.imageUrl!,
                    fit: BoxFit.cover,
                    loadingBuilder: (_, child, p) => p == null
                        ? child
                        : Container(
                            height: 180,
                            color: AppColors.surfaceSoft,
                            child: const Center(
                              child: CircularProgressIndicator(
                                valueColor: AlwaysStoppedAnimation<Color>(
                                  AppColors.primary,
                                ),
                              ),
                            ),
                          ),
                    errorBuilder: (_, _, _) => Container(
                      height: 120,
                      color: AppColors.surfaceSoft,
                      child: const Center(
                        child: Column(
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            Icon(
                              Icons.broken_image_outlined,
                              size: 32,
                              color: AppColors.textSubtle,
                            ),
                            SizedBox(height: 8),
                            Text(
                              'Image could not be loaded',
                              style: TextStyle(
                                color: AppColors.textSubtle,
                                fontSize: 13,
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                  ),
                ),
              ),
              const SizedBox(height: AppSpacing.md),
            ],

            AppSectionCard(
              title: 'Identification',
              child: Column(
                children: [
                  _DetailRow(label: 'Asset Tag', value: asset.assetTag),
                  _DetailRow(
                    label: 'Serial Number',
                    value: asset.serialNumber,
                  ),
                  _DetailRow(
                    label: 'Brand',
                    value: asset.brand,
                    isPlaceholder: asset.brand.isEmpty,
                  ),
                  _DetailRow(
                    label: 'Model',
                    value: asset.model,
                    isPlaceholder: asset.model.isEmpty,
                  ),
                  _DetailRow(
                    label: 'Category',
                    value: asset.category,
                    isPlaceholder: asset.category.isEmpty,
                  ),
                ],
              ),
            ),
            const SizedBox(height: AppSpacing.md),

            AppSectionCard(
              title: 'Assignment',
              child: Column(
                children: [
                  _DetailRow(
                    label: 'Status',
                    value: asset.status,
                    valueColor: _statusColor(asset.status),
                  ),
                  _DetailRow(
                    label: 'Assigned To',
                    value: asset.assignedEmployee.isEmpty
                        ? 'Unassigned'
                        : asset.assignedEmployee,
                    valueColor: asset.assignedEmployee.isEmpty
                        ? AppColors.textPlaceholder
                        : AppColors.textPrimary,
                    isPlaceholder: asset.assignedEmployee.isEmpty,
                  ),
                  _DetailRow(
                    label: 'Department',
                    value: asset.department.isEmpty
                        ? '—'
                        : asset.department,
                    isPlaceholder: asset.department.isEmpty,
                  ),
                  _DetailRow(
                    label: 'Location',
                    value: asset.location.isEmpty ? '—' : asset.location,
                    isPlaceholder: asset.location.isEmpty,
                  ),
                ],
              ),
            ),
            const SizedBox(height: AppSpacing.md),

            if (asset.purchaseDate != null ||
                asset.warrantyExpiry != null) ...[
              AppSectionCard(
                title: 'Timeline',
                child: Column(
                  children: [
                    if (asset.purchaseDate != null)
                      _DetailRow(
                        label: 'Purchased',
                        value: _formatDate(asset.purchaseDate!),
                      ),
                    if (asset.warrantyExpiry != null)
                      _DetailRow(
                        label: 'Warranty Expires',
                        value: _formatDate(asset.warrantyExpiry!),
                        valueColor: AppColors.warning,
                      ),
                  ],
                ),
              ),
              const SizedBox(height: AppSpacing.md),
            ],

            const SizedBox(height: AppSpacing.xl),
          ],
        ),
      ),
    );
  }

  static Color _statusColor(String status) {
    switch (status.toLowerCase()) {
      case 'available':
        return AppColors.success;
      case 'assigned':
        return AppColors.purple;
      case 'maintenance':
        return AppColors.warning;
      default:
        return AppColors.textSubtle;
    }
  }

  static String _formatDate(DateTime dt) {
    final local = dt.toLocal();
    const months = [
      'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
      'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec',
    ];
    return '${local.day} ${months[local.month - 1]} ${local.year}';
  }
}

// ────────────────────────────────────────────────────────
// Asset header card — pale hero panel with title + status badge
// ────────────────────────────────────────────────────────
class _AssetHeaderCard extends StatelessWidget {
  final Asset asset;
  const _AssetHeaderCard({required this.asset});

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: double.infinity,
      child: ClipRRect(
        borderRadius: AppRadius.borderCard,
        child: Stack(
          children: [
            Positioned.fill(
              child: DecoratedBox(
                decoration: BoxDecoration(
                  gradient: AppColors.heroPanel,
                  borderRadius: AppRadius.borderCard,
                  border: Border.all(color: AppColors.border),
                ),
              ),
            ),
            Positioned(
              right: -40,
              bottom: -40,
              width: 160,
              height: 160,
              child: DecoratedBox(
                decoration: BoxDecoration(
                  gradient: AppColors.heroGlow,
                  shape: BoxShape.circle,
                ),
              ),
            ),
            Padding(
              padding: const EdgeInsets.all(AppSpacing.lg),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text(
                    'ASSET DETAILS',
                    style: TextStyle(
                      fontSize: 11,
                      fontWeight: FontWeight.w800,
                      color: AppColors.primary,
                      letterSpacing: 1.2,
                    ),
                  ),
                  const SizedBox(height: AppSpacing.sm),
                  Text(
                    asset.assetName,
                    style: const TextStyle(
                      fontSize: 20,
                      fontWeight: FontWeight.w800,
                      color: AppColors.textPrimary,
                      height: 1.25,
                    ),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    asset.assetTag,
                    style: const TextStyle(
                      fontSize: 13,
                      color: AppColors.textSubtle,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                  const SizedBox(height: AppSpacing.sm),
                  Wrap(
                    spacing: 8,
                    runSpacing: 8,
                    children: [
                      AppBadge(
                        label: asset.status,
                        backgroundColor:
                            _statusColor(asset.status).withValues(alpha: 0.14),
                        foregroundColor: _statusColor(asset.status),
                        showDot: true,
                      ),
                      if (asset.category.isNotEmpty)
                        AppBadge(
                          label: asset.category,
                          variant: AppBadgeVariant.secondary,
                        ),
                    ],
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  static Color _statusColor(String status) {
    switch (status.toLowerCase()) {
      case 'available':
        return AppColors.success;
      case 'assigned':
        return AppColors.purple;
      case 'maintenance':
        return AppColors.warning;
      default:
        return AppColors.textSubtle;
    }
  }
}

// ────────────────────────────────────────────────────────
// Detail row with uppercase label and larger value
// ────────────────────────────────────────────────────────
class _DetailRow extends StatelessWidget {
  final String label;
  final String value;
  final Color? valueColor;
  final bool isPlaceholder;

  const _DetailRow({
    required this.label,
    required this.value,
    this.valueColor,
    this.isPlaceholder = false,
  });

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 8),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 130,
            child: Text(
              label.toUpperCase(),
              style: const TextStyle(
                fontSize: 11,
                color: AppColors.textSubtle,
                fontWeight: FontWeight.w700,
                letterSpacing: 0.6,
              ),
            ),
          ),
          Expanded(
            child: Text(
              value,
              style: TextStyle(
                fontSize: 14,
                color: isPlaceholder
                    ? AppColors.textPlaceholder
                    : (valueColor ?? AppColors.textPrimary),
                fontWeight: FontWeight.w600,
                fontStyle:
                    isPlaceholder ? FontStyle.italic : FontStyle.normal,
              ),
            ),
          ),
        ],
      ),
    );
  }
}

// ────────────────────────────────────────────────────────
// Error state
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
                'Unable to load asset',
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