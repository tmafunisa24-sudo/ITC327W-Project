// lib/views/home/shell_scaffold.dart

import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../core/design/app_colors.dart';
import '../../core/design/app_radius.dart';

/// Shell hosting the bottom navigation bar for authenticated screens.
///
/// The Flutter app is CLIENT-ONLY. Technician and Admin use the
/// ASP.NET web portal instead, so no role branching is needed here.
class ShellScaffold extends StatelessWidget {
  final StatefulNavigationShell navigationShell;

  const ShellScaffold({super.key, required this.navigationShell});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: navigationShell,
      bottomNavigationBar: NavigationBar(
        selectedIndex: navigationShell.currentIndex,
        onDestinationSelected: (index) {
          navigationShell.goBranch(
            index,
            initialLocation: index == navigationShell.currentIndex,
          );
        },
        backgroundColor: AppColors.surface,
        indicatorColor: AppColors.primary.withValues(alpha: 0.12),
        surfaceTintColor: Colors.transparent,
        elevation: 3,
        shadowColor: Colors.black.withValues(alpha: 0.08),
        labelBehavior: NavigationDestinationLabelBehavior.alwaysShow,
        destinations: _tabs
            .map(
              (t) => NavigationDestination(
                icon: Icon(t.icon, color: AppColors.textSubtle),
                selectedIcon: Icon(t.icon, color: AppColors.primary),
                label: t.label,
              ),
            )
            .toList(),
      ),
    );
  }

  static const List<_Tab> _tabs = [
    _Tab(icon: Icons.home_outlined, label: 'Home'),
    _Tab(icon: Icons.confirmation_number_outlined, label: 'Requests'),
    _Tab(icon: Icons.add_circle_outline, label: 'New'),
    _Tab(icon: Icons.notifications_outlined, label: 'Notifications'),
  ];
}

class _Tab {
  final IconData icon;
  final String label;

  const _Tab({required this.icon, required this.label});
}