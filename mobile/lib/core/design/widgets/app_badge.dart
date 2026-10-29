// lib/core/design/widgets/app_badge.dart

import 'package:flutter/material.dart';

import '../app_colors.dart';
import '../app_radius.dart';

/// Badge variants — matching the actual badges in the app.
enum AppBadgeVariant {
  primary,
  success,
  warning,
  danger,
  purple,
  secondary,
  neutral,
}

/// A small pill-shaped label — statuses, priorities, counts, roles.
///
/// Matches `.badge`, `.status-badge`, `.priority-badge` from the CSS:
/// - 999px pill radius
/// - 12.48px uppercase-ish text, weight 700
/// - 0.38rem 0.7rem (6/11px) padding
/// - Optional leading dot (used by status badges)
///
/// Use [AppBadge.forStatus] and [AppBadge.forPriority] for the app's
/// status and priority color maps.
class AppBadge extends StatelessWidget {
  final String label;
  final AppBadgeVariant variant;
  final IconData? icon;
  final bool showDot;

  /// Explicit colors. If provided, override [variant].
  final Color? backgroundColor;
  final Color? foregroundColor;

  const AppBadge({
    super.key,
    required this.label,
    this.variant = AppBadgeVariant.neutral,
    this.icon,
    this.showDot = false,
    this.backgroundColor,
    this.foregroundColor,
  });

  /// Builds a status badge using [AppColors.forStatus].
  /// Falls back to "closed" colors for unknown statuses.
  factory AppBadge.forStatus(String status) {
    final (bg, fg) = AppColors.forStatus(status);
    return AppBadge(
      label: status,
      backgroundColor: bg,
      foregroundColor: fg,
      showDot: true,
    );
  }

  /// Builds a priority badge using [AppColors.forPriority].
  /// Falls back to "medium" colors for unknown priorities.
  factory AppBadge.forPriority(String priority) {
    final (bg, fg) = AppColors.forPriority(priority);
    return AppBadge(
      label: priority,
      backgroundColor: bg,
      foregroundColor: fg,
    );
  }

  @override
  Widget build(BuildContext context) {
    final (bg, fg) = _resolveColors();

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
      decoration: BoxDecoration(
        color: bg,
        borderRadius: AppRadius.borderPill,
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (showDot) ...[
            Container(
              width: 6,
              height: 6,
              decoration: BoxDecoration(
                color: fg,
                shape: BoxShape.circle,
              ),
            ),
            const SizedBox(width: 6),
          ],
          if (icon != null) ...[
            Icon(icon, size: 12, color: fg),
            const SizedBox(width: 4),
          ],
          Text(
            label,
            style: TextStyle(
              color: fg,
              fontSize: 11,
              fontWeight: FontWeight.w700,
              letterSpacing: 0.1,
            ),
          ),
        ],
      ),
    );
  }

  (Color bg, Color fg) _resolveColors() {
    if (backgroundColor != null && foregroundColor != null) {
      return (backgroundColor!, foregroundColor!);
    }

    switch (variant) {
      case AppBadgeVariant.primary:
        return (AppColors.primaryAlpha(0.12), AppColors.primary);
      case AppBadgeVariant.success:
        return (AppColors.successAlpha(0.14), AppColors.success);
      case AppBadgeVariant.warning:
        return (AppColors.warningAlpha(0.16), AppColors.warningDark);
      case AppBadgeVariant.danger:
        return (AppColors.dangerAlpha(0.14), AppColors.danger);
      case AppBadgeVariant.purple:
        return (AppColors.purpleAlpha(0.14), AppColors.purple);
      case AppBadgeVariant.secondary:
        return (AppColors.secondaryAlpha(0.14), AppColors.secondary);
      case AppBadgeVariant.neutral:
        return (
          AppColors.textSubtle.withValues(alpha: 0.15),
          AppColors.textMuted,
        );
    }
  }
}