// lib/core/design/widgets/app_button.dart

import 'package:flutter/material.dart';

import '../app_colors.dart';
import '../app_radius.dart';
import '../app_shadows.dart';
import '../app_spacing.dart';
import '../app_text_styles.dart';

/// The variants of button used across the app.
/// Matching the actual buttons in the web app's CSS.
enum AppButtonVariant {
  /// Gradient blue pill. The primary CTA everywhere.
  primary,

  /// Grey gradient pill. "Cancel", "Change Status".
  secondary,

  /// Soft blue background, blue text, blue border, pill.
  outlined,

  /// Gradient red pill. Destructive actions.
  danger,

  /// Ghost / text-only. "View All", "Cancel" links.
  ghost,

  /// Ticket action buttons — solid blue, 8px radius, 36px height.
  /// `.btn-assign` in tickets.css.
  ticketAssign,

  /// Ticket action buttons — solid grey, 8px radius, 36px height.
  /// `.btn-status` in tickets.css.
  ticketStatus,

  /// Ticket action buttons — solid red, 8px radius, 36px height.
  /// `.btn-delete` in tickets.css.
  ticketDelete,

  /// Ticket action buttons — soft blue background, blue text, 8px radius, 36px height.
  /// `.btn-view` in tickets.css.
  ticketView,

  /// Medium-sized primary pill with 12px radius. `.btn-create-ticket`.
  createTicket,
}

/// The size of the button.
enum AppButtonSize { small, medium, large }

class AppButton extends StatelessWidget {
  final String label;
  final VoidCallback? onPressed;
  final IconData? icon;
  final AppButtonVariant variant;
  final AppButtonSize size;
  final bool isLoading;
  final bool fullWidth;

  const AppButton({
    super.key,
    required this.label,
    required this.onPressed,
    this.icon,
    this.variant = AppButtonVariant.primary,
    this.size = AppButtonSize.medium,
    this.isLoading = false,
    this.fullWidth = true,
  });

  @override
  Widget build(BuildContext context) {
    final disabled = onPressed == null || isLoading;

    final content = _buildContent(disabled);

    final button = _buildButton(disabled, content);

    if (!fullWidth) {
      return button;
    }
    return SizedBox(width: double.infinity, height: _height, child: button);
  }

  double get _height {
    if (_isTicket) return 36;
    switch (size) {
      case AppButtonSize.small:
        return 36;
      case AppButtonSize.medium:
        return 52;
      case AppButtonSize.large:
        return 56;
    }
  }

  bool get _isTicket =>
      variant == AppButtonVariant.ticketAssign ||
      variant == AppButtonVariant.ticketStatus ||
      variant == AppButtonVariant.ticketDelete ||
      variant == AppButtonVariant.ticketView;

  double get _radius {
    if (_isTicket) return AppRadius.sm;
    if (variant == AppButtonVariant.createTicket) return AppRadius.md;
    return AppRadius.pill;
  }

  Widget _buildContent(bool disabled) {
    if (isLoading) {
      return const SizedBox(
        width: 22,
        height: 22,
        child: CircularProgressIndicator(
          strokeWidth: 2.5,
          valueColor: AlwaysStoppedAnimation<Color>(Colors.white),
        ),
      );
    }

    final textStyle = _isTicket
        ? AppTextStyles.buttonSmall
        : AppTextStyles.button;

    if (icon != null) {
      return Row(
        mainAxisSize: MainAxisSize.min,
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(icon, size: _isTicket ? 16 : 20),
          SizedBox(width: _isTicket ? 6 : 8),
          Text(label, style: textStyle),
        ],
      );
    }

    return Text(label, style: textStyle);
  }

  Widget _buildButton(bool disabled, Widget content) {
    switch (variant) {
      case AppButtonVariant.primary:
        return _GradientButton(
          gradient: AppColors.primaryButton,
          shadow: AppShadows.primaryButton,
          disabled: disabled,
          onPressed: onPressed,
          radius: _radius,
          content: content,
        );

      case AppButtonVariant.secondary:
        return _GradientButton(
          gradient: AppColors.secondaryButton,
          shadow: AppShadows.none,
          disabled: disabled,
          onPressed: onPressed,
          radius: _radius,
          content: content,
        );

      case AppButtonVariant.danger:
        return _GradientButton(
          gradient: AppColors.dangerButton,
          shadow: AppShadows.none,
          disabled: disabled,
          onPressed: onPressed,
          radius: _radius,
          content: content,
        );

      case AppButtonVariant.outlined:
        return OutlinedButton(
          onPressed: disabled ? null : onPressed,
          style: OutlinedButton.styleFrom(
            foregroundColor: AppColors.primary,
            backgroundColor: AppColors.primaryAlpha(0.06),
            side: const BorderSide(color: AppColors.primary),
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(_radius),
            ),
            padding: const EdgeInsets.symmetric(
              horizontal: AppSpacing.lg,
              vertical: AppSpacing.sm,
            ),
          ),
          child: content,
        );

      case AppButtonVariant.ghost:
        return TextButton(
          onPressed: disabled ? null : onPressed,
          style: TextButton.styleFrom(
            foregroundColor: AppColors.primary,
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(_radius),
            ),
            padding: const EdgeInsets.symmetric(
              horizontal: AppSpacing.lg,
              vertical: AppSpacing.sm,
            ),
          ),
          child: content,
        );

      case AppButtonVariant.ticketAssign:
        return _GradientButton(
          gradient: AppColors.brand,
          shadow: AppShadows.none,
          disabled: disabled,
          onPressed: onPressed,
          radius: _radius,
          content: content,
        );

      case AppButtonVariant.ticketStatus:
        return _GradientButton(
          gradient: AppColors.secondaryButton,
          shadow: AppShadows.none,
          disabled: disabled,
          onPressed: onPressed,
          radius: _radius,
          content: content,
        );

      case AppButtonVariant.ticketDelete:
        return _GradientButton(
          gradient: AppColors.dangerButton,
          shadow: AppShadows.none,
          disabled: disabled,
          onPressed: onPressed,
          radius: _radius,
          content: content,
        );

      case AppButtonVariant.ticketView:
        return ElevatedButton(
          onPressed: disabled ? null : onPressed,
          style: ElevatedButton.styleFrom(
            backgroundColor: AppColors.primarySoft,
            foregroundColor: AppColors.primary,
            elevation: 0,
            side: BorderSide(color: AppColors.primaryAlpha(0.12)),
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(_radius),
            ),
            padding: const EdgeInsets.symmetric(
              horizontal: AppSpacing.sm,
              vertical: AppSpacing.xs,
            ),
          ),
          child: content,
        );

      case AppButtonVariant.createTicket:
        return _GradientButton(
          gradient: AppColors.primaryButton,
          shadow: AppShadows.primaryButton,
          disabled: disabled,
          onPressed: onPressed,
          radius: _radius,
          content: content,
        );
    }
  }
}

/// Internal widget for gradient-filled buttons.
/// Uses an InkWell inside a decorated Container so we can control the
/// background gradient, shadow, and radius exactly.
class _GradientButton extends StatelessWidget {
  final LinearGradient gradient;
  final List<BoxShadow> shadow;
  final bool disabled;
  final VoidCallback? onPressed;
  final double radius;
  final Widget content;

  const _GradientButton({
    required this.gradient,
    required this.shadow,
    required this.disabled,
    required this.onPressed,
    required this.radius,
    required this.content,
  });

  @override
  Widget build(BuildContext context) {
    return Material(
      color: Colors.transparent,
      child: Ink(
        decoration: BoxDecoration(
          gradient: disabled ? null : gradient,
          color: disabled ? AppColors.primaryAlpha(0.4) : null,
          borderRadius: BorderRadius.circular(radius),
          boxShadow: disabled ? AppShadows.none : shadow,
        ),
        child: InkWell(
          onTap: disabled ? null : onPressed,
          borderRadius: BorderRadius.circular(radius),
          child: Container(
            alignment: Alignment.center,
            padding: const EdgeInsets.symmetric(horizontal: AppSpacing.lg),
            child: content,
          ),
        ),
      ),
    );
  }
}