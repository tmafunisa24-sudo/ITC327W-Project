// lib/core/design/widgets/app_header.dart

import 'package:flutter/material.dart';

import '../app_colors.dart';
import '../app_radius.dart';
import '../app_spacing.dart';

/// A branded page header — matches `.ticket-header` and `.hero-panel`
/// from the ASP.NET app.
///
/// Renders as a **card** (not an app bar) with:
/// - Pale blue gradient background (`#FFFFFF → #EEF6FF`)
/// - Blue-tinted border
/// - Corner glow
/// - Optional badge, title, description, and trailing action
///
/// Use this at the top of a page's scrollable content, not as a fixed header.
class AppHeader extends StatelessWidget {
  /// Optional small pill at the top (e.g. "ADMINISTRATOR").
  final String? badge;

  /// Optional icon to show inside the badge.
  final IconData? badgeIcon;

  /// Main page title. Bold, 28.8px, weight 800.
  final String title;

  /// Optional muted description below the title.
  final String? description;

  /// Optional trailing widget (a button, a pill, etc.).
  final Widget? trailing;

  const AppHeader({
    super.key,
    this.badge,
    this.badgeIcon,
    required this.title,
    this.description,
    this.trailing,
  });

  @override
  Widget build(BuildContext context) {
    return ClipRRect(
      borderRadius: AppRadius.borderCard,
      child: Stack(
        children: [
          // ── Background layers ──
          Positioned.fill(
            child: DecoratedBox(
              decoration: BoxDecoration(
                gradient: AppColors.heroPanel,
                borderRadius: AppRadius.borderCard,
                border: Border.all(color: AppColors.border),
              ),
            ),
          ),

          // ── Corner glow ──
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

          // ── Content ──
          Padding(
            padding: const EdgeInsets.fromLTRB(
              AppSpacing.lg, // 20 left
              AppSpacing.xl, // 24 top
              AppSpacing.lg, // 20 right
              AppSpacing.xl, // 24 bottom
            ),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      if (badge != null) ...[
                        _HeaderBadge(
                          label: badge!,
                          icon: badgeIcon,
                        ),
                        const SizedBox(height: AppSpacing.md),
                      ],
                      Text(
                        title,
                        style: const TextStyle(
                          fontSize: 22,
                          fontWeight: FontWeight.w800,
                          color: AppColors.textPrimary,
                          letterSpacing: -0.4,
                          height: 1.2,
                        ),
                      ),
                      if (description != null) ...[
                        const SizedBox(height: AppSpacing.xs),
                        Text(
                          description!,
                          style: const TextStyle(
                            fontSize: 14,
                            color: AppColors.textSubtle,
                            height: 1.4,
                          ),
                        ),
                      ],
                    ],
                  ),
                ),
                if (trailing != null) ...[
                  const SizedBox(width: AppSpacing.md),
                  trailing!,
                ],
              ],
            ),
          ),
        ],
      ),
    );
  }
}

/// The badge/pill at the top of a header.
/// Matches `.ticket-header-badge` and `.hero-badge`.
class _HeaderBadge extends StatelessWidget {
  final String label;
  final IconData? icon;

  const _HeaderBadge({required this.label, this.icon});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(
        horizontal: 12,
        vertical: 6,
      ),
      decoration: BoxDecoration(
        color: AppColors.primaryAlpha(0.08),
        borderRadius: AppRadius.borderPill,
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (icon != null) ...[
            Icon(icon, size: 12, color: AppColors.primary),
            const SizedBox(width: 6),
          ],
          Text(
            label.toUpperCase(),
            style: const TextStyle(
              fontSize: 11,
              fontWeight: FontWeight.w800,
              color: AppColors.primary,
              letterSpacing: 1.2,
            ),
          ),
        ],
      ),
    );
  }
}