// lib/views/profile/profile_view.dart

import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../core/design/app_colors.dart';
import '../../core/design/app_radius.dart';
import '../../core/design/app_spacing.dart';
import '../../core/design/widgets/app_button.dart';
import '../../core/design/widgets/app_card.dart';
import '../../core/design/widgets/app_header.dart';
import '../../core/design/widgets/app_text_field.dart';
import '../../core/design/widgets/app_top_bar.dart';
import '../../viewmodels/auth_viewmodel.dart';

class ProfileView extends StatelessWidget {
  const ProfileView({super.key});

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthViewModel>();
    final user = auth.user;

    return Scaffold(
      appBar: const AppTopBar(title: 'Profile'),
      body: SafeArea(
        child: user == null
            ? const Center(
                child: CircularProgressIndicator(
                  valueColor:
                      AlwaysStoppedAnimation<Color>(AppColors.primary),
                ),
              )
            : SingleChildScrollView(
                padding: const EdgeInsets.all(AppSpacing.md),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const AppHeader(
                      badge: 'Account',
                      badgeIcon: Icons.person_outline,
                      title: 'Profile',
                      description:
                          'Your account details, role, and preferences.',
                    ),
                    const SizedBox(height: AppSpacing.md),

                    _ProfileIdentityCard(
                      fullName: user.fullName,
                      email: user.email,
                      role: user.role,
                    ),
                    const SizedBox(height: AppSpacing.md),

                    AppSectionCard(
                      title: 'Personal Information',
                      child: Column(
                        children: [
                          _DetailRow(
                            label: 'Email',
                            value: user.email,
                            icon: Icons.email_outlined,
                          ),
                          _DetailRow(
                            label: 'Phone',
                            value: (user.phone ?? '').isEmpty
                                ? '—'
                                : user.phone!,
                            icon: Icons.phone_outlined,
                            isPlaceholder:
                                (user.phone ?? '').isEmpty,
                          ),
                          _DetailRow(
                            label: 'Position',
                            value: (user.position ?? '').isEmpty
                                ? '—'
                                : user.position!,
                            icon: Icons.work_outline,
                            isPlaceholder:
                                (user.position ?? '').isEmpty,
                          ),
                          _DetailRow(
                            label: 'Department',
                            value: (user.departmentId ?? '').isEmpty
                                ? '—'
                                : user.departmentId!,
                            icon: Icons.business_outlined,
                            isPlaceholder:
                                (user.departmentId ?? '').isEmpty,
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: AppSpacing.md),

                    AppSectionCard(
                      title: 'Account',
                      child: Column(
                        children: [
                          _DetailRow(
                            label: 'Role',
                            value: user.role,
                            icon: Icons.shield_outlined,
                          ),
                          _DetailRow(
                            label: 'Status',
                            value: user.status,
                            icon: Icons.check_circle_outline,
                            valueColor:
                                user.status.toLowerCase() == 'active'
                                    ? AppColors.success
                                    : AppColors.warning,
                          ),
                          _DetailRow(
                            label: 'Verified',
                            value: user.emailVerified ? 'Yes' : 'No',
                            icon: Icons.verified_outlined,
                            valueColor: user.emailVerified
                                ? AppColors.success
                                : AppColors.textSubtle,
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: AppSpacing.xl),

                    AppButton(
                      label: 'Edit Profile',
                      icon: Icons.edit_outlined,
                      onPressed: () => _showEditSheet(context),
                    ),
                    const SizedBox(height: AppSpacing.sm),

                    SizedBox(
                      height: 52,
                      width: double.infinity,
                      child: OutlinedButton.icon(
                        onPressed: () async {
                          final confirmed = await _confirmLogout(context);
                          if (confirmed && context.mounted) {
                            await context.read<AuthViewModel>().logout();
                          }
                        },
                        icon: const Icon(
                          Icons.logout,
                          color: AppColors.danger,
                        ),
                        label: const Text(
                          'Sign Out',
                          style: TextStyle(color: AppColors.danger),
                        ),
                        style: OutlinedButton.styleFrom(
                          side: const BorderSide(color: AppColors.danger),
                          shape: const RoundedRectangleBorder(
                            borderRadius: AppRadius.borderPill,
                          ),
                        ),
                      ),
                    ),
                    const SizedBox(height: AppSpacing.xl),
                  ],
                ),
              ),
      ),
    );
  }

  static Future<bool> _confirmLogout(BuildContext context) async {
    final result = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
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
    return result ?? false;
  }

  static void _showEditSheet(BuildContext context) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (_) => const _EditProfileSheet(),
    );
  }
}

// ────────────────────────────────────────────────────────
// Profile identity card — pale hero panel with avatar tile
// ────────────────────────────────────────────────────────
class _ProfileIdentityCard extends StatelessWidget {
  final String fullName;
  final String email;
  final String role;

  const _ProfileIdentityCard({
    required this.fullName,
    required this.email,
    required this.role,
  });

  @override
  Widget build(BuildContext context) {
    final initials = _initials(fullName.isEmpty ? email : fullName);

    return ClipRRect(
      borderRadius: AppRadius.borderCard,
      child: Stack(
        children: [
          // Pale hero gradient background
          Positioned.fill(
            child: DecoratedBox(
              decoration: BoxDecoration(
                gradient: AppColors.heroPanel,
                borderRadius: AppRadius.borderCard,
                border: Border.all(color: AppColors.border),
              ),
            ),
          ),
          // Corner glow
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
          // Content
          Padding(
            padding: const EdgeInsets.all(AppSpacing.lg),
            child: Row(
              children: [
                // Avatar tile — rounded square, brand gradient
                Container(
                  width: 72,
                  height: 72,
                  decoration: BoxDecoration(
                    gradient: AppColors.brand,
                    borderRadius: BorderRadius.circular(AppRadius.lg),
                    boxShadow: [
                      BoxShadow(
                        color: AppColors.primaryAlpha(0.28),
                        blurRadius: 24,
                        offset: const Offset(0, 10),
                      ),
                    ],
                  ),
                  child: Center(
                    child: Text(
                      initials,
                      style: const TextStyle(
                        color: Colors.white,
                        fontSize: 26,
                        fontWeight: FontWeight.w800,
                        letterSpacing: -0.5,
                      ),
                    ),
                  ),
                ),
                const SizedBox(width: AppSpacing.md),
                // Name, email, role
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        fullName.isEmpty ? 'User' : fullName,
                        style: const TextStyle(
                          fontSize: 20,
                          fontWeight: FontWeight.w800,
                          color: AppColors.textPrimary,
                          letterSpacing: -0.3,
                          height: 1.2,
                        ),
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                      ),
                      const SizedBox(height: 4),
                      Text(
                        email,
                        style: const TextStyle(
                          fontSize: 13,
                          color: AppColors.textSubtle,
                        ),
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                      ),
                      const SizedBox(height: AppSpacing.xs),
                      Container(
                        padding: const EdgeInsets.symmetric(
                          horizontal: 10,
                          vertical: 4,
                        ),
                        decoration: BoxDecoration(
                          color: AppColors.primaryAlpha(0.10),
                          borderRadius: AppRadius.borderPill,
                        ),
                        child: Text(
                          role.toUpperCase(),
                          style: const TextStyle(
                            color: AppColors.primary,
                            fontSize: 11,
                            fontWeight: FontWeight.w800,
                            letterSpacing: 0.8,
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  static String _initials(String source) {
    final parts = source.trim().split(RegExp(r'\s+'));
    if (parts.isEmpty) return '?';
    if (parts.length == 1) {
      return parts.first.isNotEmpty ? parts.first[0].toUpperCase() : '?';
    }
    return '${parts.first[0]}${parts.last[0]}'.toUpperCase();
  }
}

// ────────────────────────────────────────────────────────
// Detail row with icon (label uppercase, value larger)
// ────────────────────────────────────────────────────────
class _DetailRow extends StatelessWidget {
  final String label;
  final String value;
  final IconData icon;
  final Color? valueColor;
  final bool isPlaceholder;

  const _DetailRow({
    required this.label,
    required this.value,
    required this.icon,
    this.valueColor,
    this.isPlaceholder = false,
  });

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 10),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.center,
        children: [
          Container(
            width: 32,
            height: 32,
            decoration: BoxDecoration(
              color: AppColors.surfaceSoft,
              borderRadius: BorderRadius.circular(AppRadius.xs),
            ),
            child: Icon(
              icon,
              size: 16,
              color: AppColors.textSubtle,
            ),
          ),
          const SizedBox(width: AppSpacing.sm),
          SizedBox(
            width: 100,
            child: Text(
              label.toUpperCase(),
              style: const TextStyle(
                fontSize: 11,
                color: AppColors.textSubtle,
                fontWeight: FontWeight.w700,
                letterSpacing: 0.6,
              ),
            ),
          ),
          Expanded(
            child: Text(
              value,
              style: TextStyle(
                fontSize: 14,
                color: isPlaceholder
                    ? AppColors.textPlaceholder
                    : (valueColor ?? AppColors.textPrimary),
                fontWeight: FontWeight.w600,
                fontStyle:
                    isPlaceholder ? FontStyle.italic : FontStyle.normal,
              ),
            ),
          ),
        ],
      ),
    );
  }
}

// ────────────────────────────────────────────────────────
// Edit profile bottom sheet
// ────────────────────────────────────────────────────────
class _EditProfileSheet extends StatefulWidget {
  const _EditProfileSheet();

  @override
  State<_EditProfileSheet> createState() => _EditProfileSheetState();
}

class _EditProfileSheetState extends State<_EditProfileSheet> {
  final _formKey = GlobalKey<FormState>();
  late final TextEditingController _fullNameController;
  late final TextEditingController _phoneController;
  late final TextEditingController _positionController;

  @override
  void initState() {
    super.initState();
    final user = context.read<AuthViewModel>().user;
    _fullNameController = TextEditingController(text: user?.fullName ?? '');
    _phoneController = TextEditingController(text: user?.phone ?? '');
    _positionController = TextEditingController(text: user?.position ?? '');
  }

  @override
  void dispose() {
    _fullNameController.dispose();
    _phoneController.dispose();
    _positionController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;

    final auth = context.read<AuthViewModel>();
    final ok = await auth.updateProfile(
      fullName: _fullNameController.text.trim(),
      phone: _phoneController.text.trim(),
      position: _positionController.text.trim(),
    );

    if (!mounted) return;

    if (ok) {
      Navigator.of(context).pop();
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Profile updated successfully.'),
          backgroundColor: AppColors.success,
        ),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthViewModel>();

    return DraggableScrollableSheet(
      initialChildSize: 0.7,
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
                margin: const EdgeInsets.symmetric(
                  vertical: AppSpacing.sm,
                ),
                decoration: BoxDecoration(
                  color: AppColors.border,
                  borderRadius: BorderRadius.circular(2),
                ),
              ),
              Padding(
                padding:
                    const EdgeInsets.symmetric(horizontal: AppSpacing.lg),
                child: Row(
                  children: [
                    const Expanded(
                      child: Text(
                        'Edit Profile',
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
                child: Form(
                  key: _formKey,
                  child: ListView(
                    controller: scrollController,
                    padding: const EdgeInsets.all(AppSpacing.lg),
                    children: [
                      AppTextField(
                        controller: _fullNameController,
                        label: 'Full Name',
                        prefixIcon: Icons.person_outline,
                        validator: (v) {
                          if (v == null || v.trim().isEmpty) {
                            return 'Name is required';
                          }
                          return null;
                        },
                      ),
                      const SizedBox(height: AppSpacing.md),
                      AppTextField(
                        controller: _phoneController,
                        label: 'Phone',
                        prefixIcon: Icons.phone_outlined,
                        keyboardType: TextInputType.phone,
                      ),
                      const SizedBox(height: AppSpacing.md),
                      AppTextField(
                        controller: _positionController,
                        label: 'Position',
                        prefixIcon: Icons.work_outline,
                      ),
                      const SizedBox(height: AppSpacing.xl),
                      if (auth.updateError != null) ...[
                        Container(
                          padding: const EdgeInsets.all(AppSpacing.sm),
                          decoration: BoxDecoration(
                            color: AppColors.dangerAlpha(0.08),
                            borderRadius:
                                BorderRadius.circular(AppRadius.md),
                            border: Border.all(
                              color: AppColors.dangerAlpha(0.3),
                            ),
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
                                  auth.updateError!,
                                  style: const TextStyle(
                                    color: AppColors.danger,
                                    fontSize: 13,
                                  ),
                                ),
                              ),
                            ],
                          ),
                        ),
                        const SizedBox(height: AppSpacing.md),
                      ],
                      AppButton(
                        label: auth.isUpdating
                            ? 'Saving...'
                            : 'Save Changes',
                        onPressed: auth.isUpdating ? null : _submit,
                        isLoading: auth.isUpdating,
                      ),
                    ],
                  ),
                ),
              ),
            ],
          ),
        );
      },
    );
  }
}