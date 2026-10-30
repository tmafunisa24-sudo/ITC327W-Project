
// Summary: Provides the ticket creation form, including service selection and image attachments.
// lib/views/tickets/create_ticket_view.dart
main

import 'dart:io';

import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:image_picker/image_picker.dart';
import 'package:provider/provider.dart';

import '../../core/design/app_colors.dart';
import '../../core/design/app_radius.dart';
import '../../core/design/app_spacing.dart';
import '../../core/design/widgets/app_header.dart';
import '../../core/design/widgets/app_text_field.dart';
import '../../core/design/widgets/app_top_bar.dart';
import '../../viewmodels/create_ticket_viewmodel.dart';

class CreateTicketView extends StatefulWidget {
  const CreateTicketView({super.key});

  @override
  State<CreateTicketView> createState() => _CreateTicketViewState();
}

class _CreateTicketViewState extends State<CreateTicketView> {
  final _descriptionController = TextEditingController();

  @override
  void initState() {
    super.initState();

    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (!mounted) return;

      final vm = context.read<CreateTicketViewModel>();

      if (vm.allItems.isEmpty && !vm.isLoadingCatalog) {
        vm.loadCatalog();
      }
    });
  }

  @override
  void dispose() {
    _descriptionController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final vm = context.watch<CreateTicketViewModel>();

    // Keep the text controller synchronised with the viewmodel.
    if (_descriptionController.text != vm.description) {
      _descriptionController.value = TextEditingValue(
        text: vm.description,
        selection: TextSelection.collapsed(
          offset: vm.description.length,
        ),
      );
    }

    return Scaffold(
      appBar: const AppTopBar(title: 'New Repair Request'),
      body: SafeArea(
        child: vm.isLoadingCatalog
            ? const Center(
                child: CircularProgressIndicator(
                  valueColor:
                      AlwaysStoppedAnimation<Color>(AppColors.primary),
                ),
              )
            : vm.catalogError != null
                ? _ErrorState(
                    message: vm.catalogError!,
                    onRetry: () => vm.loadCatalog(),
                  )
                : _buildForm(vm),
      ),
    );
  }

  Widget _buildForm(CreateTicketViewModel vm) {
    return SingleChildScrollView(
      padding: const EdgeInsets.all(AppSpacing.md),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          const AppHeader(
            badge: 'Repair Request',
            badgeIcon: Icons.build_outlined,
            title: 'What needs attention?',
            description:
                'Tell us which device is having trouble and what\'s happening.',
          ),
          const SizedBox(height: AppSpacing.lg),

          _FormLabel(text: 'Device Type', required: true),
          DropdownButtonFormField<String>(
            initialValue: vm.selectedDeviceType,
            isExpanded: true,
            decoration: const InputDecoration(
              hintText: 'Select device',
              prefixIcon: Icon(Icons.devices_outlined),
            ),
            items: vm.deviceTypes
                .map((d) => DropdownMenuItem(value: d, child: Text(d)))
                .toList(),
            onChanged: vm.isSubmitting ? null : vm.setDeviceType,
          ),
          const SizedBox(height: AppSpacing.md),

          _FormLabel(text: 'Problem Category', required: true),
          DropdownButtonFormField<String>(
            initialValue: vm.selectedProblemCategory,
            isExpanded: true,
            decoration: const InputDecoration(
              hintText: 'Select problem category',
              prefixIcon: Icon(Icons.category_outlined),
            ),
            items: vm.problemCategories
                .map((c) => DropdownMenuItem(value: c, child: Text(c)))
                .toList(),
            onChanged: vm.selectedDeviceType == null || vm.isSubmitting
                ? null
                : vm.setProblemCategory,
          ),
          const SizedBox(height: AppSpacing.md),

          _FormLabel(text: 'Problem Type', required: true),
          DropdownButtonFormField<String>(
            initialValue: vm.selectedProblemType,
            isExpanded: true,
            decoration: const InputDecoration(
              hintText: 'Select specific problem',
              prefixIcon: Icon(Icons.build_outlined),
            ),
            items: vm.problemTypes
                .map((t) => DropdownMenuItem(value: t, child: Text(t)))
                .toList(),
            onChanged:
                vm.selectedProblemCategory == null || vm.isSubmitting
                    ? null
                    : vm.setProblemType,
          ),
          const SizedBox(height: AppSpacing.md),

          _FormLabel(text: 'Title', required: true),
          TextFormField(
            key: ValueKey(vm.title),
            initialValue: vm.title,
            enabled: !vm.isSubmitting,
            maxLength: 120,
            decoration: const InputDecoration(
              hintText: 'Enter a short repair request title',
              prefixIcon: Icon(Icons.title_outlined),
              counterText: '',
            ),
            onChanged: vm.setTitle,
          ),
          const SizedBox(height: AppSpacing.md),

          _FormLabel(text: 'Description', required: true),
          AppTextField(
            controller: _descriptionController,
            hint: 'Describe the problem, when it started, and what you have tried...',
            maxLines: 5,
            minLines: 5,
            onChanged: vm.setDescription,
          ),
          const SizedBox(height: AppSpacing.md),

          _FormLabel(text: 'Priority'),
          SegmentedButton<String>(
            segments: const [
              ButtonSegment(value: 'Low', label: Text('Low')),
              ButtonSegment(value: 'Medium', label: Text('Medium')),
              ButtonSegment(value: 'High', label: Text('High')),
              ButtonSegment(value: 'Critical', label: Text('Critical')),
            ],
            selected: {vm.priority},
            onSelectionChanged: vm.isSubmitting
                ? null
                : (s) => vm.setPriority(s.first),
          ),
          const SizedBox(height: AppSpacing.lg),

          _FormLabel(text: 'Screenshots (optional)'),
          Text(
            '${vm.images.length} of 5 attachments added',
            style: const TextStyle(
              color: AppColors.textSubtle,
              fontSize: 12,
            ),
          ),
          const SizedBox(height: AppSpacing.xs),

          if (vm.images.isEmpty)
            InkWell(
              onTap: vm.isSubmitting ? null : _pickImages,
              borderRadius: BorderRadius.circular(AppRadius.md),
              child: Container(
                padding: const EdgeInsets.all(AppSpacing.lg),
                decoration: BoxDecoration(
                  color: AppColors.surfaceSoft,
                  borderRadius: BorderRadius.circular(AppRadius.md),
                  border: Border.all(
                    color: AppColors.primaryAlpha(0.18),
                  ),
                ),
                child: const Row(
                  children: [
                    Icon(
                      Icons.add_photo_alternate_outlined,
                      color: AppColors.primary,
                      size: 24,
                    ),
                    SizedBox(width: AppSpacing.sm),
                    Expanded(
                      child: Text(
                        'Tap to add screenshots',
                        style: TextStyle(
                          fontSize: 13,
                          color: AppColors.textSubtle,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            )
          else
            Column(
              children: [
                Wrap(
                  spacing: AppSpacing.xs,
                  runSpacing: AppSpacing.xs,
                  children: List.generate(vm.images.length, (i) {
                    return Stack(
                      children: [
                        ClipRRect(
                          borderRadius:
                              BorderRadius.circular(AppRadius.sm),
                          child: Image.file(
                            vm.images[i],
                            width: 90,
                            height: 90,
                            fit: BoxFit.cover,
                            errorBuilder: (context, error, stackTrace) {
                              return Container(
                                width: 90,
                                height: 90,
                                color: AppColors.surfaceSoft,
                                child: const Icon(
                                  Icons.broken_image_outlined,
                                  color: AppColors.textSubtle,
                                ),
                              );
                            },
                          ),
                        ),
                        Positioned(
                          top: 4,
                          right: 4,
                          child: IconButton(
                            tooltip: 'Remove attachment',
                            constraints: const BoxConstraints.tightFor(
                              width: 30,
                              height: 30,
                            ),
                            padding: EdgeInsets.zero,
                            style: IconButton.styleFrom(
                              backgroundColor: AppColors.danger,
                              foregroundColor: Colors.white,
                            ),
                            onPressed: vm.isSubmitting
                                ? null
                                : () => vm.removeImageAt(i),
                            icon: const Icon(
                              Icons.close,
                              size: 16,
                            ),
                          ),
                        ),
                      ],
                    );
                  }),
                ),
                if (vm.images.length < 5)
                  TextButton.icon(
                    onPressed: vm.isSubmitting ? null : _pickImages,
                    icon: const Icon(Icons.add),
                    label: const Text('Add more'),
                  ),
              ],
            ),

          const SizedBox(height: AppSpacing.lg),

          if (vm.submitError != null) ...[
            Container(
              padding: const EdgeInsets.all(AppSpacing.sm),
              decoration: BoxDecoration(
                color: AppColors.dangerAlpha(0.08),
                borderRadius: BorderRadius.circular(AppRadius.md),
                border: Border.all(color: AppColors.dangerAlpha(0.3)),
              ),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Icon(
                    Icons.error_outline,
                    color: AppColors.danger,
                    size: 20,
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: Text(
                      vm.submitError!,
                      style: const TextStyle(
                        color: AppColors.danger,
                        fontSize: 14,
                      ),
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: AppSpacing.md),
          ],

          SizedBox(
            height: 52,
            child: ElevatedButton(
              onPressed: vm.canSubmit && !vm.isSubmitting
                  ? _submit
                  : null,
              child: vm.isSubmitting
                  ? const SizedBox(
                      width: 22,
                      height: 22,
                      child: CircularProgressIndicator(
                        strokeWidth: 2.5,
                        valueColor:
                            AlwaysStoppedAnimation<Color>(Colors.white),
                      ),
                    )
                  : const Text('Submit Repair Request'),
            ),
          ),
          const SizedBox(height: AppSpacing.sm),

          SizedBox(
            height: 52,
            child: OutlinedButton(
              onPressed: vm.isSubmitting
                  ? null
                  : () {
                      if (context.canPop()) {
                        context.pop();
                      } else {
                        context.go('/tickets');
                      }
                    },
              child: const Text('Cancel'),
            ),
          ),
          const SizedBox(height: AppSpacing.xl),
        ],
      ),
    );
  }

  Future<void> _pickImages() async {
    final vm = context.read<CreateTicketViewModel>();

    if (vm.isSubmitting) return;

    try {
      final picker = ImagePicker();
      final picked = await picker.pickMultiImage(imageQuality: 85);

      if (!mounted || picked.isEmpty) return;

      final remaining = 5 - vm.images.length;

      if (remaining <= 0) return;

      final accepted = picked.take(remaining).toList();

      for (final image in accepted) {
        vm.addImage(File(image.path));
      }

      if (picked.length > remaining && mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Only five screenshots can be attached.'),
          ),
        );
      }
    } catch (error) {
      if (!mounted) return;

      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('Unable to select images: $error'),
        ),
      );
    }
  }

  Future<void> _submit() async {
    final vm = context.read<CreateTicketViewModel>();
    final router = GoRouter.of(context);
    final messenger = ScaffoldMessenger.of(context);

    final ok = await vm.submit();

    if (!mounted) return;

    if (ok) {
      messenger.showSnackBar(
        const SnackBar(
          content: Text('Ticket created successfully.'),
          backgroundColor: AppColors.success,
        ),
      );

      if (router.canPop()) {
        router.pop();
      } else {
        router.go('/tickets');
      }
    }
  }
}

class _FormLabel extends StatelessWidget {
  final String text;
  final bool required;

  const _FormLabel({
    required this.text,
    this.required = false,
  });

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 6),
      child: Row(
        children: [
          Text(
            text,
            style: const TextStyle(
              fontSize: 13,
              fontWeight: FontWeight.w600,
              color: AppColors.textPrimary,
            ),
          ),
          if (required)
            const Text(
              ' *',
              style: TextStyle(
                color: AppColors.danger,
                fontWeight: FontWeight.w700,
              ),
            ),
        ],
      ),
    );
  }
}

class _ErrorState extends StatelessWidget {
  final String message;
  final VoidCallback onRetry;

  const _ErrorState({
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
                'Unable to load form data',
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
