// lib/views/notifications/notifications_view.dart

import 'package:flutter/material.dart' hide Notification;
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../core/design/app_colors.dart';
import '../../core/design/app_radius.dart';
import '../../core/design/app_spacing.dart';
import '../../core/design/widgets/app_header.dart';
import '../../core/design/widgets/app_top_bar.dart';
import '../../models/notification.dart';
import '../../viewmodels/notification_viewmodel.dart';

class NotificationsView extends StatefulWidget {
  const NotificationsView({super.key});

  @override
  State<NotificationsView> createState() => _NotificationsViewState();
}

class _NotificationsViewState extends State<NotificationsView> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<NotificationViewModel>().loadAll();
    });
  }

  @override
  Widget build(BuildContext context) {
    final vm = context.watch<NotificationViewModel>();
    final unread = vm.unreadCount;

    return Scaffold(
      appBar: AppTopBar(
        title: 'Notifications',
        actions: [
          if (unread > 0)
            IconButton(
              icon: const Icon(Icons.done_all),
              tooltip: 'Mark all as read',
              onPressed: () async {
                await vm.markAllAsRead();
                if (!context.mounted) return;
                ScaffoldMessenger.of(context).showSnackBar(
                  const SnackBar(
                    content: Text('All notifications marked as read.'),
                  ),
                );
              },
            ),
        ],
      ),
      body: SafeArea(child: _buildBody(vm)),
    );
  }

  Widget _buildBody(NotificationViewModel vm) {
    // ── Loading (first load) ──
    if (vm.isLoading && vm.all.isEmpty) {
      return const Center(
        child: CircularProgressIndicator(
          valueColor: AlwaysStoppedAnimation<Color>(AppColors.primary),
        ),
      );
    }

    // ── Error (empty state) ──
    if (vm.error != null && vm.all.isEmpty) {
      return _ErrorState(
        message: vm.error!,
        onRetry: () => vm.loadAll(),
      );
    }

    return RefreshIndicator(
      onRefresh: () => vm.loadAll(),
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
                badge: 'Inbox',
                badgeIcon: Icons.notifications_outlined,
                title: 'Notifications',
                description:
                    'Ticket updates, assignments, and system alerts.',
              ),
            ),
          ),

          // ── Spacer ──
          const SliverPadding(padding: EdgeInsets.only(top: AppSpacing.md)),

          // ── Empty or list ──
          if (vm.all.isEmpty)
            const SliverFillRemaining(
              hasScrollBody: false,
              child: _EmptyState(),
            )
          else
            SliverList.separated(
              itemCount: vm.all.length,
              separatorBuilder: (_, _) => const Divider(
                height: 1,
                indent: AppSpacing.md,
                endIndent: AppSpacing.md,
                color: AppColors.border,
              ),
              itemBuilder: (context, i) => _NotificationTile(
                notification: vm.all[i],
                onMarkRead: () => vm.markAsRead(vm.all[i].id),
              ),
            ),

          // ── Bottom spacer ──
          const SliverPadding(
            padding: EdgeInsets.only(bottom: AppSpacing.xl),
          ),
        ],
      ),
    );
  }
}

// ═══════════════════════════════════════════════════════════
// Notification tile
// ═══════════════════════════════════════════════════════════
class _NotificationTile extends StatelessWidget {
  final Notification notification;
  final VoidCallback onMarkRead;

  const _NotificationTile({
    required this.notification,
    required this.onMarkRead,
  });

  @override
  Widget build(BuildContext context) {
    final isUnread = !notification.isRead;

    return Material(
      color: isUnread
          ? AppColors.primary.withValues(alpha: 0.06)
          : AppColors.surface,
      child: InkWell(
        onTap: () {
          if (isUnread) onMarkRead();
          final ticketId = _extractTicketId(notification.link);
          if (ticketId != null && ticketId.isNotEmpty) {
            context.push('/tickets/$ticketId');
          }
        },
        child: Padding(
          padding: const EdgeInsets.symmetric(
            horizontal: AppSpacing.md,
            vertical: AppSpacing.sm,
          ),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // ── Icon tile ──
              Container(
                width: 42,
                height: 42,
                decoration: BoxDecoration(
                  color: _iconColor(notification.type).withValues(alpha: 0.15),
                  borderRadius: BorderRadius.circular(AppRadius.sm),
                ),
                child: Icon(
                  _iconFor(notification.type),
                  color: _iconColor(notification.type),
                  size: 20,
                ),
              ),
              const SizedBox(width: AppSpacing.sm),

              // ── Content ──
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        Expanded(
                          child: Text(
                            notification.title,
                            style: TextStyle(
                              fontSize: 14,
                              fontWeight:
                                  isUnread ? FontWeight.w800 : FontWeight.w600,
                              color: AppColors.textPrimary,
                            ),
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                          ),
                        ),
                        if (isUnread)
                          Container(
                            width: 8,
                            height: 8,
                            decoration: const BoxDecoration(
                              color: AppColors.primary,
                              shape: BoxShape.circle,
                            ),
                          ),
                      ],
                    ),
                    const SizedBox(height: 4),
                    Text(
                      notification.message,
                      style: const TextStyle(
                        fontSize: 13,
                        color: AppColors.textSubtle,
                        height: 1.4,
                      ),
                      maxLines: 3,
                      overflow: TextOverflow.ellipsis,
                    ),
                    const SizedBox(height: 6),
                    Text(
                      _formatRelative(notification.createdAt),
                      style: const TextStyle(
                        fontSize: 11,
                        color: AppColors.textSubtle,
                        fontWeight: FontWeight.w500,
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  static String? _extractTicketId(String? link) {
    if (link == null || link.isEmpty) return null;
    final parts = link.split('/').where((s) => s.isNotEmpty).toList();
    if (parts.isNotEmpty && parts.last.length >= 30) {
      return parts.last.split('?').first;
    }
    final idParam = Uri.tryParse(link)?.queryParameters['id'];
    return idParam;
  }

  static IconData _iconFor(String? type) {
    switch ((type ?? '').toLowerCase()) {
      case 'ticket_assigned':
        return Icons.assignment_ind_outlined;
      case 'ticket_status':
        return Icons.sync;
      case 'ticket_resolved':
        return Icons.check_circle_outline;
      case 'ticket_comment':
        return Icons.chat_bubble_outline;
      case 'new_ticket':
        return Icons.add_circle_outline;
      case 'user_registration':
        return Icons.person_add_alt;
      case 'asset_assigned':
        return Icons.inventory_2_outlined;
      case 'maintenance':
        return Icons.build_outlined;
      case 'system':
        return Icons.info_outline;
      default:
        return Icons.notifications_none;
    }
  }

  static Color _iconColor(String? type) {
    switch ((type ?? '').toLowerCase()) {
      case 'ticket_assigned':
        return AppColors.purple;
      case 'ticket_status':
        return AppColors.primary;
      case 'ticket_resolved':
        return AppColors.success;
      case 'ticket_comment':
        return const Color(0xFF0891B2);
      case 'new_ticket':
        return AppColors.primary;
      case 'user_registration':
        return AppColors.purple;
      case 'asset_assigned':
        return AppColors.warning;
      case 'maintenance':
        return AppColors.textSubtle;
      case 'system':
        return AppColors.primary;
      default:
        return AppColors.textSubtle;
    }
  }

  static String _formatRelative(DateTime? dt) {
    if (dt == null) return '';
    final now = DateTime.now();
    final diff = now.difference(dt.toLocal());

    if (diff.inSeconds < 60) return 'Just now';
    if (diff.inMinutes < 60) {
      return '${diff.inMinutes} minute${diff.inMinutes == 1 ? '' : 's'} ago';
    }
    if (diff.inHours < 24) {
      return '${diff.inHours} hour${diff.inHours == 1 ? '' : 's'} ago';
    }
    if (diff.inDays < 7) {
      return '${diff.inDays} day${diff.inDays == 1 ? '' : 's'} ago';
    }
    const months = [
      'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
      'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec',
    ];
    final local = dt.toLocal();
    return '${local.day} ${months[local.month - 1]} ${local.year}';
  }
}

// ═══════════════════════════════════════════════════════════
// Empty state
// ═══════════════════════════════════════════════════════════
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
            Icons.notifications_off_outlined,
            size: 72,
            color: AppColors.primary.withValues(alpha: 0.4),
          ),
          const SizedBox(height: AppSpacing.md),
          const Text(
            'No notifications yet',
            style: TextStyle(
              fontSize: 18,
              fontWeight: FontWeight.w700,
              color: AppColors.textPrimary,
            ),
          ),
          const SizedBox(height: AppSpacing.xs),
          const Text(
            'You are all caught up. Check back later for ticket updates.',
            textAlign: TextAlign.center,
            style: TextStyle(color: AppColors.textSubtle),
          ),
        ],
      ),
    );
  }
}

// ═══════════════════════════════════════════════════════════
// Error state
// ═══════════════════════════════════════════════════════════
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
                'Unable to load notifications',
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
                style: const TextStyle(color: AppColors.textSubtle),
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