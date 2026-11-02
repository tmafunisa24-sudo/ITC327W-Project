// lib/views/tickets/ticket_list_view.dart

import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../core/design/app_colors.dart';
import '../../core/design/app_radius.dart';
import '../../core/design/app_spacing.dart';
import '../../core/design/widgets/app_badge.dart';
import '../../core/design/widgets/app_header.dart';
import '../../core/design/widgets/app_top_bar.dart';
import '../../models/ticket.dart';
import '../../viewmodels/ticket_viewmodel.dart';

class TicketListView extends StatefulWidget {
  const TicketListView({super.key});

  @override
  State<TicketListView> createState() => _TicketListViewState();
}

class _TicketListViewState extends State<TicketListView> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<TicketViewModel>().load();
    });
  }

  @override
  Widget build(BuildContext context) {
    final vm = context.watch<TicketViewModel>();

    return Scaffold(
      appBar: AppTopBar(
        title: 'Tickets',
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            tooltip: 'Refresh',
            onPressed: vm.isLoading ? null : () => vm.refresh(),
          ),
        ],
      ),
      body: SafeArea(child: _buildBody(vm)),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => context.push('/tickets/new'),
        icon: const Icon(Icons.add),
        label: const Text('New Ticket'),
        backgroundColor: AppColors.primary,
        foregroundColor: Colors.white,
      ),
    );
  }

  Widget _buildBody(TicketViewModel vm) {
    if (vm.isLoading && vm.tickets.isEmpty) {
      return const Center(
        child: CircularProgressIndicator(
          valueColor: AlwaysStoppedAnimation<Color>(AppColors.primary),
        ),
      );
    }

    if (vm.error != null && vm.tickets.isEmpty) {
      return _ErrorState(message: vm.error!, onRetry: () => vm.refresh());
    }

    return RefreshIndicator(
      onRefresh: () => vm.refresh(),
      color: AppColors.primary,
      child: CustomScrollView(
        physics: const AlwaysScrollableScrollPhysics(),
        slivers: [
          // ── Header ──
          const SliverPadding(
            padding: EdgeInsets.fromLTRB(
              AppSpacing.md,
              AppSpacing.md,
              AppSpacing.md,
              0,
            ),
            sliver: SliverToBoxAdapter(
              child: AppHeader(
                badge: 'ServiceHub IT',
                badgeIcon: Icons.confirmation_number_outlined,
                title: 'Tickets',
                description: 'Manage and track support requests.',
              ),
            ),
          ),

          // ── Stats ──
          SliverToBoxAdapter(
            child: Padding(
              padding: const EdgeInsets.only(top: AppSpacing.md),
              child: _StatsRow(vm: vm),
            ),
          ),

          // ── Filter chips ──
          SliverToBoxAdapter(
            child: Padding(
              padding: const EdgeInsets.symmetric(vertical: AppSpacing.xs),
              child: _FilterChips(vm: vm),
            ),
          ),

          // ── Ticket list ──
          if (vm.tickets.isEmpty)
            const SliverFillRemaining(
              hasScrollBody: false,
              child: _EmptyState(),
            )
          else
            SliverList.separated(
              itemCount: vm.tickets.length,
              separatorBuilder: (_, _) => const Divider(
                height: 1,
                indent: AppSpacing.md,
                endIndent: AppSpacing.md,
                color: AppColors.border,
              ),
              itemBuilder: (context, i) =>
                  _TicketTile(ticket: vm.tickets[i]),
            ),

          const SliverPadding(
            padding: EdgeInsets.only(bottom: AppSpacing.xl),
          ),
        ],
      ),
    );
  }
}

// ────────────────────────────────────────────────────────
// Stats row
// ────────────────────────────────────────────────────────
class _StatsRow extends StatelessWidget {
  final TicketViewModel vm;
  const _StatsRow({required this.vm});

  @override
  Widget build(BuildContext context) {
    return Container(
      color: AppColors.surface,
      padding: const EdgeInsets.symmetric(
        horizontal: AppSpacing.sm,
        vertical: AppSpacing.sm,
      ),
      child: Row(
        children: [
          _StatCard(
            label: 'Total',
            value: vm.totalCount.toString(),
            color: AppColors.primary,
          ),
          _StatCard(
            label: 'Active',
            value: vm.activeCount.toString(),
            color: AppColors.warning,
          ),
          _StatCard(
            label: 'Resolved',
            value: vm.resolvedCount.toString(),
            color: AppColors.success,
          ),
          _StatCard(
            label: 'Unassigned',
            value: vm.unassignedCount.toString(),
            color: AppColors.textSubtle,
          ),
        ],
      ),
    );
  }
}

class _StatCard extends StatelessWidget {
  final String label;
  final String value;
  final Color color;

  const _StatCard({
    required this.label,
    required this.value,
    required this.color,
  });

  @override
  Widget build(BuildContext context) {
    return Expanded(
      child: Column(
        children: [
          Text(
            value,
            style: TextStyle(
              fontSize: 22,
              fontWeight: FontWeight.w800,
              color: color,
              height: 1,
            ),
          ),
          const SizedBox(height: 4),
          Text(
            label,
            style: const TextStyle(
              fontSize: 11,
              color: AppColors.textSubtle,
              fontWeight: FontWeight.w700,
              letterSpacing: 0.4,
            ),
          ),
        ],
      ),
    );
  }
}

// ────────────────────────────────────────────────────────
// Filter chips
// ────────────────────────────────────────────────────────
class _FilterChips extends StatelessWidget {
  final TicketViewModel vm;
  const _FilterChips({required this.vm});

  static const _statuses = [
    '',
    'Open',
    'Assigned',
    'In Progress',
    'Resolved',
    'Closed',
  ];

  @override
  Widget build(BuildContext context) {
    return Container(
      color: AppColors.surface,
      height: 52,
      child: ListView.builder(
        scrollDirection: Axis.horizontal,
        padding: const EdgeInsets.symmetric(
          horizontal: AppSpacing.sm,
          vertical: AppSpacing.xs,
        ),
        itemCount: _statuses.length,
        itemBuilder: (context, i) {
          final status = _statuses[i];
          final isSelected = (vm.statusFilter ?? '') == status;
          return Padding(
            padding: const EdgeInsets.only(right: AppSpacing.xs),
            child: ChoiceChip(
              label: Text(status.isEmpty ? 'All' : status),
              selected: isSelected,
              onSelected: (_) {
                vm.setStatusFilter(status.isEmpty ? null : status);
              },
              selectedColor: AppColors.primary,
              backgroundColor: AppColors.surface,
              labelStyle: TextStyle(
                color: isSelected ? Colors.white : AppColors.textPrimary,
                fontWeight: FontWeight.w600,
                fontSize: 12,
              ),
              side: const BorderSide(color: AppColors.border),
              shape: RoundedRectangleBorder(
                borderRadius: AppRadius.borderPill,
              ),
            ),
          );
        },
      ),
    );
  }
}

// ────────────────────────────────────────────────────────
// Ticket tile
// ────────────────────────────────────────────────────────
class _TicketTile extends StatelessWidget {
  final Ticket ticket;
  const _TicketTile({required this.ticket});

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: () {
        final id = ticket.id;
        if (id == null || id.isEmpty) {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('This ticket has no id')),
          );
          return;
        }
        context.push('/tickets/$id');
      },
      child: Padding(
        padding: const EdgeInsets.symmetric(
          horizontal: AppSpacing.md,
          vertical: AppSpacing.sm,
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // ── Title + priority ──
            Row(
              children: [
                Expanded(
                  child: Text(
                    ticket.title,
                    style: const TextStyle(
                      fontSize: 15,
                      fontWeight: FontWeight.w700,
                      color: AppColors.textPrimary,
                    ),
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                  ),
                ),
                const SizedBox(width: AppSpacing.xs),
                AppBadge.forPriority(ticket.priority),
              ],
            ),
            const SizedBox(height: AppSpacing.xs),

            // ── Requester / category ──
            if (ticket.requester.isNotEmpty ||
                ticket.category.isNotEmpty) ...[
              Row(
                children: [
                  const Icon(
                    Icons.person_outline,
                    size: 14,
                    color: AppColors.textSubtle,
                  ),
                  const SizedBox(width: 4),
                  Expanded(
                    child: Text(
                      ticket.requester.isNotEmpty
                          ? ticket.requester
                          : ticket.category,
                      style: const TextStyle(
                        fontSize: 12,
                        color: AppColors.textSubtle,
                      ),
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: AppSpacing.xs),
            ],

            // ── Status + assigned + date ──
            Wrap(
              spacing: 8,
              runSpacing: 6,
              crossAxisAlignment: WrapCrossAlignment.center,
              children: [
                AppBadge.forStatus(ticket.status),
                if ((ticket.assignedTechnician ?? '').isNotEmpty)
                  _InfoChip(
                    icon: Icons.engineering_outlined,
                    label: ticket.assignedTechnician!,
                  ),
                if (ticket.category.isNotEmpty)
                  _InfoChip(
                    icon: Icons.category_outlined,
                    label: ticket.category,
                  ),
                _InfoChip(
                  icon: Icons.calendar_today_outlined,
                  label: _formatDate(ticket.createdAt),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }

  static String _formatDate(DateTime? dt) {
    if (dt == null) return '—';
    final local = dt.toLocal();
    const months = [
      'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
      'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec',
    ];
    return '${local.day} ${months[local.month - 1]} ${local.year}';
  }
}

class _InfoChip extends StatelessWidget {
  final IconData icon;
  final String label;
  const _InfoChip({required this.icon, required this.label});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
      decoration: BoxDecoration(
        color: AppColors.surfaceSoft,
        borderRadius: BorderRadius.circular(AppRadius.xs),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 12, color: AppColors.textSubtle),
          const SizedBox(width: 4),
          Text(
            label,
            style: const TextStyle(
              fontSize: 11,
              color: AppColors.textSubtle,
              fontWeight: FontWeight.w600,
            ),
          ),
        ],
      ),
    );
  }
}

// ────────────────────────────────────────────────────────
// Error / Empty states
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
                'Unable to load tickets',
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
  const _EmptyState();

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.all(AppSpacing.xxl),
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(
            Icons.inbox_outlined,
            size: 72,
            color: AppColors.primary.withValues(alpha: 0.4),
          ),
          const SizedBox(height: AppSpacing.md),
          const Text(
            'No tickets found',
            style: TextStyle(
              fontSize: 18,
              fontWeight: FontWeight.w700,
              color: AppColors.textPrimary,
            ),
          ),
          const SizedBox(height: AppSpacing.xs),
          const Text(
            'There are no support tickets to display right now.',
            textAlign: TextAlign.center,
            style: TextStyle(
              color: AppColors.textSubtle,
              fontSize: 14,
            ),
          ),
        ],
      ),
    );
  }
}