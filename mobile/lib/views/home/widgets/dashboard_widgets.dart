// lib/views/home/widgets/dashboard_widgets.dart

import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../core/design/app_colors.dart';
import '../../../core/design/app_radius.dart';
import '../../../core/design/app_shadows.dart';
import '../../../core/design/app_spacing.dart';
import '../../../core/design/widgets/app_badge.dart';
import '../../../core/design/widgets/app_card.dart';
import '../../../models/dtos/dashboard_response.dart';

// ────────────────────────────────────────────────────────
// Section title — small caps header with optional trailing action
// ────────────────────────────────────────────────────────
class SectionTitle extends StatelessWidget {
  final String text;
  final Widget? trailing;

  const SectionTitle(this.text, {super.key, this.trailing});

  @override
  Widget build(BuildContext context) {
    if (trailing == null) {
      return Text(
        text.toUpperCase(),
        style: const TextStyle(
          fontSize: 11,
          fontWeight: FontWeight.w800,
          color: AppColors.textSubtle,
          letterSpacing: 1.0,
        ),
      );
    }

    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        Text(
          text.toUpperCase(),
          style: const TextStyle(
            fontSize: 11,
            fontWeight: FontWeight.w800,
            color: AppColors.textSubtle,
            letterSpacing: 1.0,
          ),
        ),
        trailing!,
      ],
    );
  }
}

// ────────────────────────────────────────────────────────
// Metric card — bordered tile with icon, value, label
// ────────────────────────────────────────────────────────
class MetricCard extends StatelessWidget {
  final String label;
  final int value;
  final IconData icon;
  final Color color;

  const MetricCard({
    super.key,
    required this.label,
    required this.value,
    required this.icon,
    required this.color,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(AppSpacing.md),
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: AppRadius.borderCard,
        border: Border.all(color: AppColors.border),
        boxShadow: AppShadows.ticketCard,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            width: 40,
            height: 40,
            decoration: BoxDecoration(
              color: color.withValues(alpha: 0.12),
              borderRadius: BorderRadius.circular(AppRadius.sm),
            ),
            child: Icon(icon, color: color, size: 20),
          ),
          const SizedBox(height: AppSpacing.sm),
          Text(
            '$value',
            style: TextStyle(
              fontSize: 26,
              fontWeight: FontWeight.w800,
              color: color,
              height: 1,
              letterSpacing: -0.5,
            ),
          ),
          const SizedBox(height: 4),
          Text(
            label.toUpperCase(),
            style: const TextStyle(
              fontSize: 10,
              color: AppColors.textSubtle,
              fontWeight: FontWeight.w700,
              letterSpacing: 0.6,
            ),
          ),
        ],
      ),
    );
  }
}

// ────────────────────────────────────────────────────────
// Top technician card — highlight tile with trophy + name + count
// ────────────────────────────────────────────────────────
class TopTechnicianCard extends StatelessWidget {
  final TopTechnician tech;
  const TopTechnicianCard({super.key, required this.tech});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(AppSpacing.md),
      decoration: BoxDecoration(
        gradient: AppColors.heroPanel,
        borderRadius: AppRadius.borderCard,
        border: Border.all(color: AppColors.border),
        boxShadow: AppShadows.ticketCard,
      ),
      child: Row(
        children: [
          Container(
            width: 52,
            height: 52,
            decoration: BoxDecoration(
              gradient: AppColors.brand,
              borderRadius: BorderRadius.circular(AppRadius.lg),
              boxShadow: [
                BoxShadow(
                  color: AppColors.primaryAlpha(0.28),
                  blurRadius: 20,
                  offset: const Offset(0, 8),
                ),
              ],
            ),
            child: const Icon(
              Icons.emoji_events_outlined,
              color: Colors.white,
              size: 26,
            ),
          ),
          const SizedBox(width: AppSpacing.sm),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text(
                  'TOP TECHNICIAN',
                  style: TextStyle(
                    fontSize: 10,
                    fontWeight: FontWeight.w800,
                    color: AppColors.textSubtle,
                    letterSpacing: 1.0,
                  ),
                ),
                const SizedBox(height: 4),
                Text(
                  tech.name,
                  style: const TextStyle(
                    fontSize: 15,
                    fontWeight: FontWeight.w800,
                    color: AppColors.textPrimary,
                  ),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
                const SizedBox(height: 2),
                Text(
                  '${tech.resolvedCount} tickets resolved',
                  style: const TextStyle(
                    fontSize: 12,
                    color: AppColors.textSubtle,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

// ────────────────────────────────────────────────────────
// Recent tickets list — bordered list with status + priority
// ────────────────────────────────────────────────────────
class RecentTicketsList extends StatelessWidget {
  final List<RecentTicket> tickets;
  const RecentTicketsList({super.key, required this.tickets});

  @override
  Widget build(BuildContext context) {
    if (tickets.isEmpty) {
      return AppCard(
        child: const Center(
          child: Padding(
            padding: EdgeInsets.symmetric(vertical: AppSpacing.md),
            child: Text(
              'No recent tickets',
              style: TextStyle(
                fontSize: 13,
                color: AppColors.textSubtle,
              ),
            ),
          ),
        ),
      );
    }

    return Material(
      color: AppColors.surface,
      borderRadius: AppRadius.borderCard,
      child: Container(
        decoration: BoxDecoration(
          borderRadius: AppRadius.borderCard,
          border: Border.all(color: AppColors.border),
          boxShadow: AppShadows.ticketCard,
        ),
        child: Column(
          children: tickets.asMap().entries.map((entry) {
            final i = entry.key;
            final t = entry.value;
            return Column(
              children: [
                if (i > 0)
                  const Divider(
                    height: 1,
                    indent: AppSpacing.md,
                    endIndent: AppSpacing.md,
                    color: AppColors.border,
                  ),
                InkWell(
                  onTap: t.id != null
                      ? () => context.push('/tickets/${t.id}')
                      : null,
                  child: Padding(
                    padding: const EdgeInsets.symmetric(
                      horizontal: AppSpacing.md,
                      vertical: AppSpacing.sm,
                    ),
                    child: Row(
                      children: [
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                t.title,
                                style: const TextStyle(
                                  fontSize: 14,
                                  fontWeight: FontWeight.w700,
                                  color: AppColors.textPrimary,
                                ),
                                maxLines: 1,
                                overflow: TextOverflow.ellipsis,
                              ),
                              const SizedBox(height: 6),
                              AppBadge.forStatus(t.status),
                            ],
                          ),
                        ),
                        const SizedBox(width: AppSpacing.sm),
                        AppBadge.forPriority(t.priority),
                      ],
                    ),
                  ),
                ),
              ],
            );
          }).toList(),
        ),
      ),
    );
  }
}

// ────────────────────────────────────────────────────────
// Error state
// ────────────────────────────────────────────────────────
class ErrorState extends StatelessWidget {
  final String message;
  final VoidCallback onRetry;

  const ErrorState({
    super.key,
    required this.message,
    required this.onRetry,
  });

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
                'Unable to load dashboard',
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