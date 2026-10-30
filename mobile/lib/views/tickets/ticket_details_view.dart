// lib/views/tickets/ticket_details_view.dart

import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../core/design/app_colors.dart';
import '../../core/design/app_radius.dart';
import '../../core/design/app_spacing.dart';
import '../../core/design/widgets/app_badge.dart';
import '../../core/design/widgets/app_card.dart';
import '../../core/design/widgets/app_top_bar.dart';
import '../../models/ticket.dart';
import '../../viewmodels/auth_viewmodel.dart';
import '../../viewmodels/ticket_viewmodel.dart';

class TicketDetailsView extends StatefulWidget {
  final String ticketId;
  const TicketDetailsView({super.key, required this.ticketId});

  @override
  State<TicketDetailsView> createState() => _TicketDetailsViewState();
}

class _TicketDetailsViewState extends State<TicketDetailsView> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<TicketViewModel>().loadTicket(widget.ticketId);
    });
  }

  Future<void> _showUpdateSheet() async {
    final vm = context.read<TicketViewModel>();
    final ticket = vm.currentTicket;
    if (ticket == null) return;

    await showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (ctx) => _UpdateTicketSheet(ticket: ticket, viewModel: vm),
    );
  }

  @override
  Widget build(BuildContext context) {
    final vm = context.watch<TicketViewModel>();

    return Scaffold(
      appBar: AppTopBar(
        title: 'Ticket Details',
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            tooltip: 'Reload',
            onPressed: vm.isLoadingDetail
                ? null
                : () => vm.loadTicket(widget.ticketId),
          ),
        ],
      ),
      body: SafeArea(child: _buildBody(vm)),
    );
  }

  Widget _buildBody(TicketViewModel vm) {
    final isAdmin = context.watch<AuthViewModel>().isAdmin;
    final isClient = context.watch<AuthViewModel>().isClient;

    if (vm.isLoadingDetail && vm.currentTicket == null) {
      return const Center(
        child: CircularProgressIndicator(
          valueColor: AlwaysStoppedAnimation<Color>(AppColors.primary),
        ),
      );
    }

    if (vm.detailError != null && vm.currentTicket == null) {
      return _ErrorState(
        message: vm.detailError!,
        onRetry: () => vm.loadTicket(widget.ticketId),
      );
    }

    final ticket = vm.currentTicket;
    if (ticket == null) {
      return const Center(child: CircularProgressIndicator());
    }

    return RefreshIndicator(
      onRefresh: () => vm.loadTicket(widget.ticketId),
      color: AppColors.primary,
      child: SingleChildScrollView(
        physics: const AlwaysScrollableScrollPhysics(),
        padding: const EdgeInsets.all(AppSpacing.md),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            _TicketHeaderCard(ticket: ticket),
            const SizedBox(height: AppSpacing.md),

            if (ticket.description.isNotEmpty) ...[
              AppSectionCard(
                title: 'Description',
                child: Text(
                  ticket.description,
                  style: const TextStyle(
                    fontSize: 14,
                    color: AppColors.textPrimary,
                    height: 1.5,
                  ),
                ),
              ),
              const SizedBox(height: AppSpacing.md),
            ],

            AppSectionCard(
              title: 'Details',
              child: Column(
                children: [
                  _DetailRow(label: 'Requester', value: ticket.requester),
                  if (ticket.category.isNotEmpty)
                    _DetailRow(label: 'Category', value: ticket.category),
                  if ((ticket.deviceType ?? '').isNotEmpty)
                    _DetailRow(label: 'Device', value: ticket.deviceType!),
                  if ((ticket.problemCategory ?? '').isNotEmpty)
                    _DetailRow(
                      label: 'Problem Category',
                      value: ticket.problemCategory!,
                    ),
                  if ((ticket.problemType ?? '').isNotEmpty)
                    _DetailRow(
                      label: 'Problem Type',
                      value: ticket.problemType!,
                    ),
                  if ((ticket.ticketNumber ?? '').isNotEmpty)
                    _DetailRow(label: 'Ticket #', value: ticket.ticketNumber!),
                ],
              ),
            ),
            const SizedBox(height: AppSpacing.md),

            AppSectionCard(
              title: 'Assignment',
              child: Column(
                children: [
                  _DetailRow(
                    label: 'Technician',
                    value: (ticket.assignedTechnician ?? '').isEmpty
                        ? 'Unassigned'
                        : ticket.assignedTechnician!,
                    valueColor: (ticket.assignedTechnician ?? '').isEmpty
                        ? AppColors.textSubtle
                        : AppColors.textPrimary,
                  ),
                  _DetailRow(
                    label: 'Priority',
                    value: ticket.priority,
                    valueColor: _priorityColor(ticket.priority),
                  ),
                  _DetailRow(
                    label: 'Status',
                    value: ticket.status,
                    valueColor: _statusColor(ticket.status),
                  ),
                ],
              ),
            ),
            const SizedBox(height: AppSpacing.md),

            AppSectionCard(
              title: 'Timeline',
              child: Column(
                children: [
                  if (ticket.createdAt != null)
                    _DetailRow(
                      label: 'Created',
                      value: _formatDateTime(ticket.createdAt!),
                    ),
                  if (ticket.updatedAt != null)
                    _DetailRow(
                      label: 'Last Updated',
                      value: _formatDateTime(ticket.updatedAt!),
                    ),
                  if (ticket.dueDate != null)
                    _DetailRow(
                      label: 'Due Date',
                      value: _formatDateTime(ticket.dueDate!),
                      valueColor: AppColors.warning,
                    ),
                  if (ticket.closedAt != null)
                    _DetailRow(
                      label: 'Closed',
                      value: _formatDateTime(ticket.closedAt!),
                      valueColor: AppColors.success,
                    ),
                ],
              ),
            ),
            const SizedBox(height: AppSpacing.md),

            AppSectionCard(
              title: 'Payment',
              child: Column(
                children: [
                  _DetailRow(
                    label: 'Method',
                    value: (ticket.paymentMethod ?? '').isEmpty
                        ? 'Not selected'
                        : ticket.paymentMethod!,
                    valueColor: (ticket.paymentMethod ?? '').isEmpty
                        ? AppColors.textSubtle
                        : AppColors.textPrimary,
                  ),
                  _DetailRow(
                    label: 'Status',
                    value: ticket.paymentStatus,
                    valueColor: _paymentStatusColor(ticket.paymentStatus),
                  ),
                  if (isClient &&
                      (ticket.paymentMethod ?? '').isEmpty &&
                      ticket.status.toLowerCase() != 'closed') ...[
                    const SizedBox(height: AppSpacing.sm),
                    SizedBox(
                      width: double.infinity,
                      child: ElevatedButton.icon(
                        onPressed: vm.isUpdating
                            ? null
                            : () => _showPaymentMethodSheet(context, ticket),
                        icon: const Icon(Icons.payment_outlined),
                        label: const Text('Select Payment Method'),
                      ),
                    ),
                  ],
                  if (isAdmin &&
                      ['card', 'cash'].contains(
                          (ticket.paymentMethod ?? '').toLowerCase()) &&
                      ticket.paymentStatus.toLowerCase() == 'pending') ...[
                    const SizedBox(height: AppSpacing.sm),
                    SizedBox(
                      width: double.infinity,
                      child: ElevatedButton.icon(
                        onPressed: vm.isUpdating
                            ? null
                            : () => _confirmPayment(context, ticket),
                        icon: const Icon(Icons.check_circle_outline),
                        label: const Text('Confirm Payment'),
                        style: ElevatedButton.styleFrom(
                          backgroundColor: AppColors.success,
                        ),
                      ),
                    ),
                  ],
                ],
              ),
            ),

            if ((ticket.screenshotUrl ?? '').isNotEmpty ||
                (ticket.imageUrl ?? '').isNotEmpty) ...[
              const SizedBox(height: AppSpacing.md),
              AppSectionCard(
                title: 'Screenshot',
                child: _ScreenshotPreview(
                  url: (ticket.screenshotUrl ?? '').isNotEmpty
                      ? ticket.screenshotUrl!
                      : ticket.imageUrl!,
                ),
              ),
            ],

            if ((ticket.comments ?? '').isNotEmpty) ...[
              const SizedBox(height: AppSpacing.md),
              AppSectionCard(
                title: 'Comments',
                child: Container(
                  width: double.infinity,
                  padding: const EdgeInsets.all(AppSpacing.sm),
                  decoration: BoxDecoration(
                    color: AppColors.surfaceSoft,
                    borderRadius: BorderRadius.circular(AppRadius.sm),
                  ),
                  child: Text(
                    ticket.comments!,
                    style: const TextStyle(
                      fontSize: 14,
                      color: AppColors.textPrimary,
                      height: 1.5,
                    ),
                  ),
                ),
              ),
            ],

            const SizedBox(height: AppSpacing.xl),

            if (isAdmin || context.watch<AuthViewModel>().isTechnician)
              SizedBox(
                height: 52,
                child: ElevatedButton.icon(
                  onPressed: _showUpdateSheet,
                  icon: const Icon(Icons.edit_outlined),
                  label: const Text('Update Ticket'),
                ),
              ),

            if (isClient && ticket.status == 'Resolved') ...[
              const SizedBox(height: AppSpacing.sm),
              Container(
                padding: const EdgeInsets.all(AppSpacing.sm),
                decoration: BoxDecoration(
                  color: AppColors.successAlpha(0.06),
                  borderRadius: BorderRadius.circular(AppRadius.md),
                  border: Border.all(color: AppColors.successAlpha(0.2)),
                ),
                child: const Row(
                  children: [
                    Icon(
                      Icons.check_circle_outline,
                      color: AppColors.success,
                      size: 20,
                    ),
                    SizedBox(width: 10),
                    Expanded(
                      child: Text(
                        'This ticket is resolved. Closing is coming in the next update.',
                        style: TextStyle(
                          fontSize: 12,
                          color: AppColors.success,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ],

            const SizedBox(height: AppSpacing.xl),
          ],
        ),
      ),
    );
  }

  static Color _paymentStatusColor(String status) {
    switch (status.toLowerCase()) {
      case 'paid':
        return AppColors.success;
      case 'pending':
        return AppColors.warning;
      default:
        return AppColors.textSubtle;
    }
  }

  Future<void> _showPaymentMethodSheet(
    BuildContext context,
    Ticket ticket,
  ) async {
    final id = ticket.id;
    if (id == null || id.isEmpty) return;

    final selected = await showModalBottomSheet<String>(
      context: context,
      backgroundColor: Colors.transparent,
      builder: (ctx) =>
          _PaymentMethodSheet(currentMethod: ticket.paymentMethod),
    );

    if (selected == null || !context.mounted) return;

    final vm = context.read<TicketViewModel>();
    final ok = await vm.setPaymentMethod(ticketId: id, paymentMethod: selected);

    if (!context.mounted) return;

    if (ok) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('Payment method set to $selected.'),
          backgroundColor: AppColors.success,
        ),
      );
    } else {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(vm.updateError ?? 'Unable to save the payment method.'),
          backgroundColor: AppColors.danger,
        ),
      );
    }
  }

  Future<void> _confirmPayment(BuildContext context, Ticket ticket) async {
    final id = ticket.id;
    if (id == null || id.isEmpty) return;

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Confirm payment?'),
        content: Text('Mark this ${ticket.paymentMethod} payment as Paid?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(ctx).pop(false),
            child: const Text('Cancel'),
          ),
          TextButton(
            onPressed: () => Navigator.of(ctx).pop(true),
            style: TextButton.styleFrom(foregroundColor: AppColors.success),
            child: const Text('Confirm'),
          ),
        ],
      ),
    );

    if (confirmed != true || !context.mounted) return;

    final vm = context.read<TicketViewModel>();
    final ok = await vm.confirmPayment(id);

    if (!context.mounted) return;

    if (ok) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Payment confirmed.'),
          backgroundColor: AppColors.success,
        ),
      );
    } else {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(vm.updateError ?? 'Unable to confirm the payment.'),
          backgroundColor: AppColors.danger,
        ),
      );
    }
  }

  static Color _priorityColor(String p) {
    switch (p.toLowerCase()) {
      case 'critical':
        return AppColors.priorityCriticalFg;
      case 'high':
        return AppColors.danger;
      case 'low':
        return AppColors.success;
      default:
        return AppColors.priorityMediumFg;
    }
  }

  static Color _statusColor(String s) {
    switch (s.toLowerCase()) {
      case 'open':
        return AppColors.primary;
      case 'assigned':
        return AppColors.purple;
      case 'in progress':
        return AppColors.statusInProgressFg;
      case 'resolved':
        return AppColors.success;
      case 'closed':
        return AppColors.statusClosedFg;
      default:
        return AppColors.textSubtle;
    }
  }

  static String _formatDateTime(DateTime dt) {
    final l = dt.toLocal();
    const months = [
      'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
      'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec',
    ];
    final hh = l.hour.toString().padLeft(2, '0');
    final mm = l.minute.toString().padLeft(2, '0');
    return '${l.day} ${months[l.month - 1]} ${l.year} · $hh:$mm';
  }
}

// ────────────────────────────────────────────────────────
// Ticket header — pale hero card with title + status/priority badges
// ────────────────────────────────────────────────────────
class _TicketHeaderCard extends StatelessWidget {
  final Ticket ticket;
  const _TicketHeaderCard({required this.ticket});

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: double.infinity,
      child: ClipRRect(
        borderRadius: AppRadius.borderCard,
        child: Stack(
          children: [
            Positioned.fill(
              child: DecoratedBox(
                decoration: BoxDecoration(
                  gradient: AppColors.heroPanel,
                  borderRadius: AppRadius.borderCard,
                  border: Border.all(color: AppColors.border),
                ),
              ),
            ),
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
            Padding(
              padding: const EdgeInsets.all(AppSpacing.lg),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text(
                    'TICKET DETAILS',
                    style: TextStyle(
                      fontSize: 11,
                      fontWeight: FontWeight.w800,
                      color: AppColors.primary,
                      letterSpacing: 1.2,
                    ),
                  ),
                  const SizedBox(height: AppSpacing.sm),
                  Text(
                    ticket.title,
                    style: const TextStyle(
                      fontSize: 20,
                      fontWeight: FontWeight.w800,
                      color: AppColors.textPrimary,
                      height: 1.25,
                    ),
                  ),
                  const SizedBox(height: AppSpacing.sm),
                  Wrap(
                    spacing: 8,
                    runSpacing: 8,
                    children: [
                      AppBadge.forStatus(ticket.status),
                      AppBadge.forPriority(ticket.priority),
                    ],
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

// ────────────────────────────────────────────────────────
// Payment method sheet
// ────────────────────────────────────────────────────────
class _PaymentMethodSheet extends StatefulWidget {
  final String? currentMethod;
  const _PaymentMethodSheet({this.currentMethod});

  @override
  State<_PaymentMethodSheet> createState() => _PaymentMethodSheetState();
}

class _PaymentMethodSheetState extends State<_PaymentMethodSheet> {
  late String _selected;

  @override
  void initState() {
    super.initState();
    final current = widget.currentMethod?.toLowerCase() ?? '';
    _selected = current == 'cash' ? 'Cash' : 'Card';
  }

  @override
  Widget build(BuildContext context) {
    return Material(
      color: AppColors.surface,
      borderRadius: BorderRadius.vertical(
        top: Radius.circular(AppRadius.xl),
      ),
      child: SafeArea(
        top: false,
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Container(
              width: 40,
              height: 4,
              margin: const EdgeInsets.only(bottom: AppSpacing.sm),
              decoration: BoxDecoration(
                color: AppColors.border,
                borderRadius: BorderRadius.circular(2),
              ),
            ),
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: AppSpacing.lg),
              child: Row(
                children: [
                  const Expanded(
                    child: Text(
                      'Select Payment Method',
                      style: TextStyle(
                        fontSize: 18,
                        fontWeight: FontWeight.w800,
                        color: AppColors.textPrimary,
                      ),
                    ),
                  ),
                  IconButton(
                    icon: const Icon(Icons.close),
                    onPressed: () => Navigator.of(context).pop(),
                  ),
                ],
              ),
            ),
            const Divider(height: 1),
            RadioListTile<String>(
              value: 'Card',
              // ignore: deprecated_member_use
              groupValue: _selected,
              // ignore: deprecated_member_use
              onChanged: (value) =>
                  setState(() => _selected = value ?? 'Card'),
              title: const Text(
                'Card',
                style: TextStyle(fontWeight: FontWeight.w600),
              ),
              subtitle: const Text('Pay by card'),
              activeColor: AppColors.primary,
            ),
            RadioListTile<String>(
              value: 'Cash',
              // ignore: deprecated_member_use
              groupValue: _selected,
              // ignore: deprecated_member_use
              onChanged: (value) =>
                  setState(() => _selected = value ?? 'Cash'),
              title: const Text(
                'Cash',
                style: TextStyle(fontWeight: FontWeight.w600),
              ),
              subtitle: const Text('Pay in person with cash'),
              activeColor: AppColors.primary,
            ),
            const SizedBox(height: AppSpacing.sm),
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: AppSpacing.lg),
              child: SizedBox(
                width: double.infinity,
                height: 52,
                child: ElevatedButton(
                  onPressed: () => Navigator.of(context).pop(_selected),
                  child: const Text('Save Payment Method'),
                ),
              ),
            ),
            const SizedBox(height: AppSpacing.xs),
          ],
        ),
      ),
    );
  }
}

// ────────────────────────────────────────────────────────
// Detail row
// ────────────────────────────────────────────────────────
class _DetailRow extends StatelessWidget {
  final String label;
  final String value;
  final Color? valueColor;

  const _DetailRow({
    required this.label,
    required this.value,
    this.valueColor,
  });

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 5),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 130,
            child: Text(
              label,
              style: const TextStyle(
                fontSize: 13,
                color: AppColors.textSubtle,
              ),
            ),
          ),
          Expanded(
            child: Text(
              value,
              style: TextStyle(
                fontSize: 13,
                color: valueColor ?? AppColors.textPrimary,
                fontWeight: FontWeight.w600,
              ),
            ),
          ),
        ],
      ),
    );
  }
}

// ────────────────────────────────────────────────────────
// Screenshot preview (THIS WAS THE CUTOFF — completing it)
// ────────────────────────────────────────────────────────
class _ScreenshotPreview extends StatelessWidget {
  final String url;
  const _ScreenshotPreview({required this.url});

  @override
  Widget build(BuildContext context) {
    return ClipRRect(
      borderRadius: BorderRadius.circular(AppRadius.sm),
      child: Container(
        width: double.infinity,
        constraints: const BoxConstraints(maxHeight: 320),
        color: AppColors.surfaceSoft,
        child: Image.network(
          url,
          fit: BoxFit.contain,
          loadingBuilder: (context, child, progress) {
            if (progress == null) return child;
            return const SizedBox(
              height: 200,
              child: Center(
                child: CircularProgressIndicator(
                  valueColor:
                      AlwaysStoppedAnimation<Color>(AppColors.primary),
                ),
              ),
            );
          },
          errorBuilder: (context, error, stackTrace) {
            return Container(
              height: 200,
              alignment: Alignment.center,
              child: const Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Icon(
                    Icons.broken_image_outlined,
                    size: 48,
                    color: AppColors.textSubtle,
                  ),
                  SizedBox(height: 8),
                  Text(
                    'Unable to load image',
                    style: TextStyle(
                      fontSize: 13,
                      color: AppColors.textSubtle,
                    ),
                  ),
                ],
              ),
            );
          },
        ),
      ),
    );
  }
}

// ────────────────────────────────────────────────────────
// Error state
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
                'Unable to load ticket',
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

// ────────────────────────────────────────────────────────
// Update ticket sheet
// ────────────────────────────────────────────────────────
class _UpdateTicketSheet extends StatefulWidget {
  final Ticket ticket;
  final TicketViewModel viewModel;

  const _UpdateTicketSheet({
    required this.ticket,
    required this.viewModel,
  });

  @override
  State<_UpdateTicketSheet> createState() => _UpdateTicketSheetState();
}

class _UpdateTicketSheetState extends State<_UpdateTicketSheet> {
  String? _status;
  String? _priority;
  String? _technician;
  final _commentController = TextEditingController();

  static const _statuses = [
    'Open',
    'Assigned',
    'In Progress',
    'Resolved',
    'Closed',
  ];
  static const _priorities = ['Low', 'Medium', 'High', 'Critical'];

  @override
  void initState() {
    super.initState();
    _status = _statuses.firstWhere(
      (s) => s.toLowerCase() == widget.ticket.status.toLowerCase(),
      orElse: () => _statuses.first,
    );
    _priority = _priorities.firstWhere(
      (p) => p.toLowerCase() == widget.ticket.priority.toLowerCase(),
      orElse: () => 'Medium',
    );
    _technician = widget.ticket.assignedTechnician;
  }

  @override
  void dispose() {
    _commentController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    final id = widget.ticket.id;
    if (id == null || id.isEmpty) return;

    final ok = await widget.viewModel.updateStatus(
      ticketId: id,
      status: _status ?? widget.ticket.status,
      assignedTechnician: _technician,
      comment: _commentController.text.trim().isEmpty
          ? null
          : _commentController.text.trim(),
    );

    if (!mounted) return;

    if (ok) {
      Navigator.of(context).pop();
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Ticket updated.'),
          backgroundColor: AppColors.success,
        ),
      );
    } else {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            widget.viewModel.updateError ?? 'Unable to update ticket.',
          ),
          backgroundColor: AppColors.danger,
        ),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return DraggableScrollableSheet(
      initialChildSize: 0.75,
      minChildSize: 0.5,
      maxChildSize: 0.95,
      expand: false,
      builder: (_, scrollController) {
        return Container(
          decoration: const BoxDecoration(
            color: AppColors.surface,
            borderRadius: BorderRadius.vertical(
              top: Radius.circular(AppRadius.xl),
            ),
          ),
          child: Column(
            children: [
              Container(
                width: 40,
                height: 4,
                margin: const EdgeInsets.symmetric(vertical: AppSpacing.sm),
                decoration: BoxDecoration(
                  color: AppColors.border,
                  borderRadius: BorderRadius.circular(2),
                ),
              ),
              Padding(
                padding: const EdgeInsets.symmetric(horizontal: AppSpacing.lg),
                child: Row(
                  children: [
                    const Expanded(
                      child: Text(
                        'Update Ticket',
                        style: TextStyle(
                          fontSize: 18,
                          fontWeight: FontWeight.w800,
                          color: AppColors.textPrimary,
                        ),
                      ),
                    ),
                    IconButton(
                      icon: const Icon(Icons.close),
                      onPressed: () => Navigator.of(context).pop(),
                    ),
                  ],
                ),
              ),
              const Divider(height: 1),
              Expanded(
                child: ListView(
                  controller: scrollController,
                  padding: const EdgeInsets.all(AppSpacing.lg),
                  children: [
                    const Text(
                      'Status',
                      style: TextStyle(
                        fontSize: 13,
                        fontWeight: FontWeight.w600,
                        color: AppColors.textPrimary,
                      ),
                    ),
                    const SizedBox(height: 6),
                    DropdownButtonFormField<String>(
                      initialValue: _status,
                      isExpanded: true,
                      decoration: const InputDecoration(
                        prefixIcon: Icon(Icons.flag_outlined),
                      ),
                      items: _statuses
                          .map((s) =>
                              DropdownMenuItem(value: s, child: Text(s)))
                          .toList(),
                      onChanged: (v) => setState(() => _status = v),
                    ),
                    const SizedBox(height: AppSpacing.md),
                    const Text(
                      'Priority',
                      style: TextStyle(
                        fontSize: 13,
                        fontWeight: FontWeight.w600,
                        color: AppColors.textPrimary,
                      ),
                    ),
                    const SizedBox(height: 6),
                    DropdownButtonFormField<String>(
                      initialValue: _priority,
                      isExpanded: true,
                      decoration: const InputDecoration(
                        prefixIcon: Icon(Icons.priority_high),
                      ),
                      items: _priorities
                          .map((p) =>
                              DropdownMenuItem(value: p, child: Text(p)))
                          .toList(),
                      onChanged: (v) => setState(() => _priority = v),
                    ),
                    const SizedBox(height: AppSpacing.md),
                    const Text(
                      'Assigned Technician',
                      style: TextStyle(
                        fontSize: 13,
                        fontWeight: FontWeight.w600,
                        color: AppColors.textPrimary,
                      ),
                    ),
                    const SizedBox(height: 6),
                    TextFormField(
                      initialValue: _technician ?? '',
                      decoration: const InputDecoration(
                        prefixIcon: Icon(Icons.engineering_outlined),
                        hintText: 'Leave blank to unassign',
                      ),
                      onChanged: (v) =>
                          _technician = v.trim().isEmpty ? null : v.trim(),
                    ),
                    const SizedBox(height: AppSpacing.md),
                    const Text(
                      'Add Comment',
                      style: TextStyle(
                        fontSize: 13,
                        fontWeight: FontWeight.w600,
                        color: AppColors.textPrimary,
                      ),
                    ),
                    const SizedBox(height: 6),
                    TextFormField(
                      controller: _commentController,
                      maxLines: 4,
                      decoration: const InputDecoration(
                        hintText: 'Add an optional comment...',
                        alignLabelWithHint: true,
                      ),
                    ),
                    const SizedBox(height: AppSpacing.xl),
                    SizedBox(
                      height: 52,
                      child: ElevatedButton(
                        onPressed:
                            widget.viewModel.isUpdating ? null : _submit,
                        child: widget.viewModel.isUpdating
                            ? const SizedBox(
                                width: 22,
                                height: 22,
                                child: CircularProgressIndicator(
                                  strokeWidth: 2.5,
                                  valueColor: AlwaysStoppedAnimation<Color>(
                                    Colors.white,
                                  ),
                                ),
                              )
                            : const Text('Save Changes'),
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        );
      },
    );
  }
}