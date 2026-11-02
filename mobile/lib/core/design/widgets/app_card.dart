// lib/core/design/widgets/app_card.dart

import 'package:flutter/material.dart';

import '../app_colors.dart';
import '../app_radius.dart';
import '../app_shadows.dart';
import '../app_spacing.dart';
import '../app_text_styles.dart';

/// A styled card — the standard white rounded panel used across the app.
///
/// Matches `.card` from `site.css`:
/// - 22px radius
/// - 1px blue-tinted border
/// - Spec's card shadow
/// - 22.4px padding
class AppCard extends StatelessWidget {
  final Widget child;
  final EdgeInsetsGeometry? padding;
  final bool elevated;
  final VoidCallback? onTap;
  final Color? background;
  final Color? borderColor;
  final BorderRadius? borderRadius;

  const AppCard({
    super.key,
    required this.child,
    this.padding,
    this.elevated = false,
    this.onTap,
    this.background,
    this.borderColor,
    this.borderRadius,
  });

  @override
  Widget build(BuildContext context) {
    final content = Container(
      padding: padding ?? const EdgeInsets.all(AppSpacing.lg),
      decoration: BoxDecoration(
        color: background ?? AppColors.surface,
        borderRadius: borderRadius ?? AppRadius.borderCard,
        border: Border.all(color: borderColor ?? AppColors.border),
        boxShadow: elevated ? AppShadows.card : AppShadows.ticketCard,
      ),
      child: child,
    );

    if (onTap == null) return content;

    return Material(
      color: Colors.transparent,
      child: InkWell(
        onTap: onTap,
        borderRadius: borderRadius ?? AppRadius.borderCard,
        child: content,
      ),
    );
  }
}

/// A card with a small-caps section header at the top.
/// Used for detail views, forms, and groupings.
class AppSectionCard extends StatelessWidget {
  final String title;
  final Widget child;
  final EdgeInsetsGeometry? padding;
  final Widget? trailing;

  const AppSectionCard({
    super.key,
    required this.title,
    required this.child,
    this.padding,
    this.trailing,
  });

  @override
  Widget build(BuildContext context) {
    return AppCard(
      padding: padding ?? const EdgeInsets.all(AppSpacing.md),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Expanded(
                child: Text(
                  title.toUpperCase(),
                  style: AppTextStyles.sectionLabel,
                ),
              ),
              ?trailing,
            ],
          ),
          const SizedBox(height: AppSpacing.sm),
          child,
        ],
      ),
    );
  }
}