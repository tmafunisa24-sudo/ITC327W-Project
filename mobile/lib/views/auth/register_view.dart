// lib/views/auth/register_view.dart


import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';
// =======================================================
import '../../core/design/app_colors.dart';
import '../../core/design/app_radius.dart';
import '../../core/design/app_shadows.dart';
import '../../core/design/app_spacing.dart';
import '../../core/design/widgets/app_text_field.dart';
import '../../viewmodels/auth_viewmodel.dart';
import 'widgets/auth_widgets.dart';
// ========================================================

class RegisterView extends StatefulWidget {
  const RegisterView({super.key});

  @override
  State<RegisterView> createState() => _RegisterViewState();
}

class _RegisterViewState extends State<RegisterView>
    with TickerProviderStateMixin {
  final _formKey = GlobalKey<FormState>();
  final _fullNameController = TextEditingController();
  final _emailController = TextEditingController();
  final _phoneController = TextEditingController();
  final _passwordController = TextEditingController();
  final _confirmPasswordController = TextEditingController();
  bool _obscurePassword = true;
  bool _obscureConfirm = true;

  // Entrance animations
  late AnimationController _heroController;
  late AnimationController _titleController;
  late AnimationController _subtitleController;
  late AnimationController _cardController;

  late Animation<double> _heroOpacity;
  late Animation<Offset> _heroOffset;
  late Animation<double> _titleOpacity;
  late Animation<Offset> _titleOffset;
  late Animation<double> _subtitleOpacity;
  late Animation<Offset> _subtitleOffset;
  late Animation<double> _cardOpacity;
  late Animation<Offset> _cardOffset;

  @override
  void initState() {
    super.initState();

    _heroController = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 800),
    );
    _heroOpacity = Tween<double>(begin: 0, end: 1).animate(
      CurvedAnimation(parent: _heroController, curve: Curves.easeOut),
    );
    _heroOffset = Tween<Offset>(
      begin: const Offset(0, -0.05),
      end: Offset.zero,
    ).animate(
      CurvedAnimation(parent: _heroController, curve: Curves.easeOut),
    );

    _titleController = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 900),
    );
    _titleOpacity = Tween<double>(begin: 0, end: 1).animate(
      CurvedAnimation(parent: _titleController, curve: Curves.easeOut),
    );
    _titleOffset = Tween<Offset>(
      begin: const Offset(0, 0.08),
      end: Offset.zero,
    ).animate(
      CurvedAnimation(parent: _titleController, curve: Curves.easeOut),
    );

    _subtitleController = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 800),
    );
    _subtitleOpacity = Tween<double>(begin: 0, end: 1).animate(
      CurvedAnimation(parent: _subtitleController, curve: Curves.easeOut),
    );
    _subtitleOffset = Tween<Offset>(
      begin: const Offset(0, 0.08),
      end: Offset.zero,
    ).animate(
      CurvedAnimation(parent: _subtitleController, curve: Curves.easeOut),
    );

    _cardController = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 850),
    );
    _cardOpacity = Tween<double>(begin: 0, end: 1).animate(
      CurvedAnimation(parent: _cardController, curve: Curves.easeOut),
    );
    _cardOffset = Tween<Offset>(
      begin: const Offset(0, 0.05),
      end: Offset.zero,
    ).animate(
      CurvedAnimation(parent: _cardController, curve: Curves.easeOut),
    );

    _heroController.forward();
    Future.delayed(const Duration(milliseconds: 100), () {
      if (mounted) _subtitleController.forward();
    });
    Future.delayed(const Duration(milliseconds: 200), () {
      if (mounted) _titleController.forward();
    });
    Future.delayed(const Duration(milliseconds: 250), () {
      if (mounted) _cardController.forward();
    });
  }

  @override
  void dispose() {
    _fullNameController.dispose();
    _emailController.dispose();
    _phoneController.dispose();
    _passwordController.dispose();
    _confirmPasswordController.dispose();
    _heroController.dispose();
    _titleController.dispose();
    _subtitleController.dispose();
    _cardController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    FocusScope.of(context).unfocus();

    final vm = context.read<AuthViewModel>();
    await vm.register(
      email: _emailController.text.trim(),
      password: _passwordController.text,
      fullName: _fullNameController.text.trim(),
      phone: _phoneController.text.trim().isEmpty
          ? null
          : _phoneController.text.trim(),
    );

    // No navigation here — the card will swap to the confirmation state
    // via the vm.registerEmailSent / vm.registerNeedsConfirmation flags.
  }

  @override
  Widget build(BuildContext context) {
    final vm = context.watch<AuthViewModel>();

    return Scaffold(
      backgroundColor: AppColors.loginBgTop,
      body: Stack(
        children: [
          // ── Layer 1: Navy gradient background ──
          Positioned.fill(
            child: DecoratedBox(
              decoration: BoxDecoration(
                gradient: AppColors.loginBackground,
              ),
            ),
          ),
          // ── Layer 2: Radial glow top-left ──
          Positioned(
            top: -100,
            left: -100,
            width: 500,
            height: 500,
            child: DecoratedBox(
              decoration: BoxDecoration(
                gradient: AppColors.loginGlowTopLeft,
                shape: BoxShape.circle,
              ),
            ),
          ),
          // ── Layer 3: Radial glow bottom-right ──
          Positioned(
            bottom: -150,
            right: -100,
            width: 600,
            height: 600,
            child: DecoratedBox(
              decoration: BoxDecoration(
                gradient: AppColors.loginGlowBottomRight,
                shape: BoxShape.circle,
              ),
            ),
          ),
          // ── Layer 4: Faint diagonal sheen ──
          Positioned.fill(
            child: IgnorePointer(
              child: DecoratedBox(
                decoration: BoxDecoration(
                  gradient: LinearGradient(
                    begin: const Alignment(-1, -1),
                    end: const Alignment(1, 1),
                    colors: [
                      Colors.white.withValues(alpha: 0.04),
                      Colors.white.withValues(alpha: 0.0),
                    ],
                  ),
                ),
              ),
            ),
          ),

          // ── Layer 5: Content ──
          SafeArea(
            child: Center(
              child: SingleChildScrollView(
                padding: const EdgeInsets.symmetric(
                  horizontal: AppSpacing.xl,
                  vertical: AppSpacing.xxl,
                ),
                child: ConstrainedBox(
                  constraints: const BoxConstraints(maxWidth: 540),
                  child: Form(
                    key: _formKey,
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        // ── Logo tile ──
                        FadeTransition(
                          opacity: _heroOpacity,
                          child: SlideTransition(
                            position: _heroOffset,
                            child: const Center(
                              child: GlassLogoTile(),
                            ),
                          ),
                        ),
                        const SizedBox(height: AppSpacing.xl),

                        // ── Subtitle pill ──
                        FadeTransition(
                          opacity: _subtitleOpacity,
                          child: SlideTransition(
                            position: _subtitleOffset,
                            child: const Center(
                              child: SubtitlePill(
                                text: 'Click computer and accessories',
                              ),
                            ),
                          ),
                        ),
                        const SizedBox(height: AppSpacing.xs),

                        // ── Title ──
                        FadeTransition(
                          opacity: _titleOpacity,
                          child: SlideTransition(
                            position: _titleOffset,
                            child: const Center(
                              child: GradientTitle(text: 'Create Account'),
                            ),
                          ),
                        ),
                        const SizedBox(height: AppSpacing.xxl),

                        // ── Card ──
                        FadeTransition(
                          opacity: _cardOpacity,
                          child: SlideTransition(
                            position: _cardOffset,
                            child: _RegisterCard(
                              fullNameController: _fullNameController,
                              emailController: _emailController,
                              phoneController: _phoneController,
                              passwordController: _passwordController,
                              confirmPasswordController:
                                  _confirmPasswordController,
                              obscurePassword: _obscurePassword,
                              obscureConfirm: _obscureConfirm,
                              onTogglePassword: () => setState(
                                () => _obscurePassword = !_obscurePassword,
                              ),
                              onToggleConfirm: () => setState(
                                () => _obscureConfirm = !_obscureConfirm,
                              ),
                              onSubmit: _submit,
                              isLoading: vm.isRegistering,
                              error: vm.registerError,
                              emailSent: vm.registrationEmailSent,
                              needsConfirmation:
                                  vm.registrationNeedsConfirmation,
                              confirmationEmail:
                                  vm.pendingConfirmationEmail,
                            ),
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}



// ═══════════════════════════════════════════════════════════
// Register card — shows form OR confirmation state
// ═══════════════════════════════════════════════════════════
class _RegisterCard extends StatelessWidget {
  final TextEditingController fullNameController;
  final TextEditingController emailController;
  final TextEditingController phoneController;
  final TextEditingController passwordController;
  final TextEditingController confirmPasswordController;
  final bool obscurePassword;
  final bool obscureConfirm;
  final VoidCallback onTogglePassword;
  final VoidCallback onToggleConfirm;
  final VoidCallback onSubmit;
  final bool isLoading;
  final String? error;
  final bool emailSent;
  final bool needsConfirmation;
  final String? confirmationEmail;

  const _RegisterCard({
    required this.fullNameController,
    required this.emailController,
    required this.phoneController,
    required this.passwordController,
    required this.confirmPasswordController,
    required this.obscurePassword,
    required this.obscureConfirm,
    required this.onTogglePassword,
    required this.onToggleConfirm,
    required this.onSubmit,
    required this.isLoading,
    this.error,
    required this.emailSent,
    required this.needsConfirmation,
    this.confirmationEmail,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.fromLTRB(35, 38, 35, 32),
      decoration: BoxDecoration(
        color: Colors.white.withValues(alpha: 0.94),
        borderRadius: AppRadius.borderLoginCard,
        border: Border.all(
          color: const Color(0xFF94A3B8).withValues(alpha: 0.2),
        ),
        boxShadow: AppShadows.loginCard,
      ),
      child: emailSent
          ? _buildConfirmationState(context)
          : _buildForm(context),
    );
  }

  // ── The form (as before) ──
  Widget _buildForm(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        if (error != null) ...[
          ErrorBanner(message: error!),
          const SizedBox(height: AppSpacing.lg),
        ],

        // ── Full Name ──
        AppTextField(
          controller: fullNameController,
          label: 'Full Name',
          hint: 'John Doe',
          prefixIcon: Icons.person_outline,
          textInputAction: TextInputAction.next,
          enabled: !isLoading,
          validator: (v) {
            if (v == null || v.trim().isEmpty) {
              return 'Full name is required';
            }
            return null;
          },
        ),
        const SizedBox(height: AppSpacing.md),

        // ── Email ──
        AppTextField(
          controller: emailController,
          label: 'Email',
          hint: 'name@company.com',
          prefixIcon: Icons.email_outlined,
          keyboardType: TextInputType.emailAddress,
          textInputAction: TextInputAction.next,
          enabled: !isLoading,
          validator: (v) {
            if (v == null || v.trim().isEmpty) {
              return 'Email is required';
            }
            if (!v.contains('@') || !v.contains('.')) {
              return 'Enter a valid email address';
            }
            return null;
          },
        ),
        const SizedBox(height: AppSpacing.md),

        // ── Phone (optional) ──
        AppTextField(
          controller: phoneController,
          label: 'Phone (optional)',
          hint: '012 345 6789',
          prefixIcon: Icons.phone_outlined,
          keyboardType: TextInputType.phone,
          textInputAction: TextInputAction.next,
          enabled: !isLoading,
        ),
        const SizedBox(height: AppSpacing.md),

        // ── Password ──
        AppTextField(
          controller: passwordController,
          label: 'Password',
          hint: 'At least 6 characters',
          prefixIcon: Icons.lock_outline,
          obscureText: obscurePassword,
          enabled: !isLoading,
          textInputAction: TextInputAction.next,
          suffixIcon: IconButton(
            icon: Icon(
              obscurePassword
                  ? Icons.visibility_outlined
                  : Icons.visibility_off_outlined,
              size: 20,
            ),
            onPressed: onTogglePassword,
          ),
          validator: (v) {
            if (v == null || v.isEmpty) {
              return 'Password is required';
            }
            if (v.length < 6) {
              return 'Password must be at least 6 characters';
            }
            return null;
          },
        ),
        const SizedBox(height: AppSpacing.md),

        // ── Confirm Password ──
        AppTextField(
          controller: confirmPasswordController,
          label: 'Confirm Password',
          hint: 'Re-enter your password',
          prefixIcon: Icons.lock_outline,
          obscureText: obscureConfirm,
          enabled: !isLoading,
          textInputAction: TextInputAction.done,
          suffixIcon: IconButton(
            icon: Icon(
              obscureConfirm
                  ? Icons.visibility_outlined
                  : Icons.visibility_off_outlined,
              size: 20,
            ),
            onPressed: onToggleConfirm,
          ),
          validator: (v) {
            if (v == null || v.isEmpty) {
              return 'Please confirm your password';
            }
            if (v != passwordController.text) {
              return 'Passwords do not match';
            }
            return null;
          },
        ),
        const SizedBox(height: AppSpacing.xl),

        // ── Submit ──
        AuthSubmitButton(
          label: 'Create Account',
          loadingLabel: 'Creating account...',
          onPressed: isLoading ? null : onSubmit,
          isLoading: isLoading,
        ),
        const SizedBox(height: AppSpacing.lg),

        // ── Back to login ──
        Center(
          child: TextButton(
            onPressed: isLoading
                ? null
                : () {
                    if (context.canPop()) {
                      context.pop();
                    } else {
                      context.go('/login');
                    }
                  },
            child: const Text('Already have an account? Sign in'),
          ),
        ),
      ],
    );
  }

  // ── The confirmation / success state ──
  Widget _buildConfirmationState(BuildContext context) {
    final email = confirmationEmail ?? 'your email';

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Center(
          child: Container(
            width: 72,
            height: 72,
            decoration: BoxDecoration(
              color: AppColors.successAlpha(0.12),
              shape: BoxShape.circle,
            ),
            child: Icon(
              needsConfirmation
                  ? Icons.mark_email_read_outlined
                  : Icons.check_circle_outline,
              color: AppColors.success,
              size: 36,
            ),
          ),
        ),
        const SizedBox(height: AppSpacing.lg),
        Center(
          child: Text(
            needsConfirmation ? 'Check your email' : 'Account created',
            style: const TextStyle(
              fontSize: 20,
              fontWeight: FontWeight.w800,
              color: AppColors.textPrimary,
              letterSpacing: -0.3,
            ),
          ),
        ),
        const SizedBox(height: AppSpacing.sm),
        Text(
          needsConfirmation
              ? 'We sent a confirmation link to $email. '
                  'Click the link to activate your account, then sign in.'
              : 'Your account was created successfully. '
                  'You can now sign in with $email.',
          textAlign: TextAlign.center,
          style: const TextStyle(
            fontSize: 14,
            color: AppColors.textMuted,
            height: 1.5,
          ),
        ),
        const SizedBox(height: AppSpacing.xl),
        Center(
          child: TextButton(
            onPressed: () {
              context.read<AuthViewModel>().clearRegisterError();
              if (context.canPop()) {
                context.pop();
              } else {
                context.go('/login');
              }
            },
            child: const Text('Back to sign in'),
          ),
        ),
      ],
    );
  }
}

