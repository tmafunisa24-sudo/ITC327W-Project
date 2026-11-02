// lib/views/home/client_home_view.dart

import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../core/design/app_colors.dart';
import '../../core/design/app_radius.dart';
import '../../core/design/app_spacing.dart';
import '../../core/design/widgets/app_header.dart';
import '../../core/design/widgets/app_top_bar.dart';
import '../../models/dtos/dashboard_response.dart';
import '../../viewmodels/auth_viewmodel.dart';
import '../../viewmodels/dashboard_viewmodel.dart';
import 'widgets/dashboard_widgets.dart';

class ClientHomeView extends StatefulWidget{
  const ClientHomeView ({super.key});

  @override
  State<ClientHomeView> createState() => _ClientHomeViewState();
}

class _ClientHomeViewState extends State<ClientHomeView> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      final vm = context.read<DashboardViewModel>();
      if (vm.data == null && !vm.isLoading) {
        vm.load();
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthViewModel>();
    final vm = context.watch<DashboardViewModel>();
    final data = vm.data;

    return Scaffold(
      appBar: AppTopBar(
        title: 'My Dashboard',
        showBackButton: false,
        
      ),
      body: SafeArea(
        child: RefreshIndicator(
          onRefresh: () => vm.refresh(),
          color: AppColors.primary,
          child: _buildBody(context, auth, vm, data),
        ),
      ),
    );
  }

  Widget _buildBody(
    BuildContext context,
    AuthViewModel auth,
    DashboardViewModel vm,
    DashboardResponse? data,
  ) {
    if (vm.isLoading && data == null) {
      return const Center(
        child: CircularProgressIndicator(
          valueColor: AlwaysStoppedAnimation<Color>(AppColors.primary),
        ),
      );
    }

    if (vm.error != null && data == null) {
      return ErrorState(message: vm.error!, onRetry: () => vm.load());
    }

    if (data == null) {
      return const Center(child: CircularProgressIndicator());
    }

    final m = data.metrics;

    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.all(AppSpacing.md),
      children: [
        AppHeader(
          badge: 'Client',
          badgeIcon: Icons.person_outline,
          title: 'Welcome back, ${_firstName(auth.user?.fullName)}',
          description:
              'Click computer and accessories repair support, all in one place.',
        ),
        const SizedBox(height: AppSpacing.xl),

        // ── Primary CTA ──
        SizedBox(
          height: 52,
          width: double.infinity,
          child: ElevatedButton.icon(
            onPressed: () => context.push('/tickets/new'),
            icon: const Icon(Icons.add),
            label: const Text('Submit New Repair Request'),
          ),
        ),
        const SizedBox(height: AppSpacing.xl),

        // ── My Tickets ──
        const SectionTitle('My Tickets'),
        const SizedBox(height: AppSpacing.xs),
        Row(
          children: [
            Expanded(
              child: MetricCard(
                label: 'Total',
                value: m.myTickets,
                icon: Icons.confirmation_number_outlined,
                color: AppColors.primary,
              ),
            ),
            const SizedBox(width: AppSpacing.xs),
            Expanded(
              child: MetricCard(
                label: 'Pending',
                value: m.pendingTickets,
                icon: Icons.timelapse_outlined,
                color: AppColors.warning,
              ),
            ),
          ],
        ),
        const SizedBox(height: AppSpacing.xs),
        Row(
          children: [
            Expanded(
              child: MetricCard(
                label: 'Resolved',
                value: m.resolvedTickets,
                icon: Icons.check_circle_outline,
                color: AppColors.success,
              ),
            ),
            const SizedBox(width: AppSpacing.xs),
            Expanded(
              child: MetricCard(
                label: 'My Assets',
                value: m.assignedAssets,
                icon: Icons.inventory_2_outlined,
                color: AppColors.purple,
              ),
            ),
          ],
        ),
        const SizedBox(height: AppSpacing.xl),

        // ── Recent Requests ──
        SectionTitle(
          'Recent Requests',
          trailing: TextButton(
            onPressed: () => context.push('/tickets'),
            child: const Text('View All'),
          ),
        ),
        const SizedBox(height: AppSpacing.xs),
        RecentTicketsList(tickets: data.recentTickets),
        const SizedBox(height: AppSpacing.xl),

        // ── Quick links ──
        const SectionTitle('Quick Access'),
        const SizedBox(height: AppSpacing.xs),
        _QuickLinkButton(
          icon: Icons.person_outline,
          label: 'Profile',
          onTap: () => context.push('/profile'),
        ),
        const SizedBox(height: AppSpacing.xl),
      ],
    );
  }

  static String _firstName(String? fullName) {
    if (fullName == null || fullName.isEmpty) return 'there';
    return fullName.trim().split(RegExp(r'\s+')).first;
  }
}

// ────────────────────────────────────────────────────────
// Quick-link pill button (outlined primary)
// ────────────────────────────────────────────────────────
class _QuickLinkButton extends StatelessWidget {
  final IconData icon;
  final String label;
  final VoidCallback onTap;

  const _QuickLinkButton({
    required this.icon,
    required this.label,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      height: 48,
      width: double.infinity,
      child: OutlinedButton.icon(
        onPressed: onTap,
        icon: Icon(icon, size: 18),
        label: Text(label),
        style: OutlinedButton.styleFrom(
          foregroundColor: AppColors.primary,
          side: const BorderSide(color: AppColors.primary),
          shape: const RoundedRectangleBorder(
            borderRadius: AppRadius.borderPill,
          ),
          padding: const EdgeInsets.symmetric(horizontal: AppSpacing.md),
        ),
      ),
    );
  }
}