// lib/core/design/widgets/app_top_bar.dart

import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../app_colors.dart';
import '../app_radius.dart';
import '../app_spacing.dart';
import '../../../viewmodels/auth_viewmodel.dart';
import '../../../widgets/notification_bell.dart';

/// The standard top bar for every authenticated screen.
///
/// Layout:
///   [← back (if any)]   [Title]         [🔔] [👤] [🚪]
///
/// - Back arrow only shows when the route can pop.
/// - Bell shows an unread count via [NotificationBell].
/// - Profile pushes `/profile`.
/// - Logout shows a confirm dialog and calls `AuthViewModel.logout()`.
///
/// Pass `showBell`, `showProfile`, `showLogout` = false to hide any icon
/// for the rare case where a screen wants to customise. Default: all on.
class AppTopBar extends StatelessWidget implements PreferredSizeWidget {
  final String title;
  final List<Widget>? actions;
  final bool showBackButton;
  final bool showBell;
  final bool showProfile;
  final bool showLogout;
  final Widget? leading;

  const AppTopBar({
    super.key,
    required this.title,
    this.actions,
    this.showBackButton = true,
    this.showBell = true,
    this.showProfile = true,
    this.showLogout = true,
    this.leading,
  });

  @override
  Size get preferredSize => const Size.fromHeight(kToolbarHeight);

  @override
  Widget build(BuildContext context) {
    return AppBar(
      title: Text(
        title,
        style: const TextStyle(
          fontSize: 18,
          fontWeight: FontWeight.w800,
          color: AppColors.textPrimary,
          letterSpacing: -0.3,
        ),
      ),
      backgroundColor: AppColors.surface,
      foregroundColor: AppColors.textPrimary,
      elevation: 0,
      scrolledUnderElevation: 1,
      shadowColor: AppColors.border,
      centerTitle: false,
      leading: leading ??
          (showBackButton && Navigator.of(context).canPop()
              ? IconButton(
                  icon: const Icon(Icons.arrow_back),
                  onPressed: () => Navigator.of(context).pop(),
                )
              : null),
      actions: [
        ...(actions ?? const <Widget>[]),
        if (showBell) const NotificationBell(),
        if (showProfile)
          IconButton(
            icon: const Icon(Icons.person_outline),
            tooltip: 'Profile',
            onPressed: () => context.push('/profile'),
          ),
        if (showLogout)
          IconButton(
            icon: const Icon(Icons.logout),
            tooltip: 'Sign out',
            onPressed: () => _confirmLogout(context),
          ),
        const SizedBox(width: AppSpacing.xs),
      ],
    );
  }

  static Future<void> _confirmLogout(BuildContext context) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        shape: const RoundedRectangleBorder(borderRadius: AppRadius.borderLg),
        title: const Text('Sign out?'),
        content: const Text('Are you sure you want to sign out?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(ctx).pop(false),
            child: const Text('Cancel'),
          ),
          TextButton(
            onPressed: () => Navigator.of(ctx).pop(true),
            style: TextButton.styleFrom(foregroundColor: AppColors.danger),
            child: const Text('Sign Out'),
          ),
        ],
      ),
    );

    if (confirmed == true && context.mounted) {
      await context.read<AuthViewModel>().logout();
    }
  }
}