// lib/views/auth/login_view.dart

import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:go_router/go_router.dart';
// ========================================================
import '../../core/design/app_colors.dart';
import '../../core/design/app_radius.dart';
import '../../core/design/app_shadows.dart';
import '../../core/design/app_spacing.dart';
import '../../core/design/widgets/app_text_field.dart';
import '../../viewmodels/auth_viewmodel.dart';
import 'widgets/auth_widgets.dart';
// ========================================================

class LoginView extends StatefulWidget {
  const LoginView({super.key});

  @override
  State<LoginView> createState() => _LoginViewState();
}

class _LoginViewState extends State<LoginView>
    with TickerProviderStateMixin {
  final _formKey = GlobalKey<FormState>();
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  bool _rememberMe = false;
  bool _obscurePassword = true;

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

    // Staggered start
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
    _emailController.dispose();
    _passwordController.dispose();
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
    await vm.login(
      email: _emailController.text.trim(),
      password: _passwordController.text,
      rememberMe: _rememberMe,
    );
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
                              child: GradientTitle(text: 'ServiceHub IT'),
                            ),
                          ),
                        ),
                        const SizedBox(height: AppSpacing.xxl),

                        // ── Card ──
                        FadeTransition(
                          opacity: _cardOpacity,
                          child: SlideTransition(
                            position: _cardOffset,
                            child: _LoginCard(
                              formKey: _formKey,
                              emailController: _emailController,
                              passwordController: _passwordController,
                              obscurePassword: _obscurePassword,
                              onTogglePassword: () {
                                setState(() =>
                                    _obscurePassword = !_obscurePassword);
                              },
                              rememberMe: _rememberMe,
                              onRememberMeChanged: (v) {
                                setState(() => _rememberMe = v);
                              },
                              onSubmit: _submit,
                              isLoading: vm.isLoading,
                              error: vm.error,
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
// The white frosted login card
// ═══════════════════════════════════════════════════════════
class _LoginCard extends StatelessWidget {
  final GlobalKey<FormState> formKey;
  final TextEditingController emailController;
  final TextEditingController passwordController;
  final bool obscurePassword;
  final VoidCallback onTogglePassword;
  final bool rememberMe;
  final ValueChanged<bool> onRememberMeChanged;
  final VoidCallback onSubmit;
  final bool isLoading;
  final String? error;

  const _LoginCard({
    required this.formKey,
    required this.emailController,
    required this.passwordController,
    required this.obscurePassword,
    required this.onTogglePassword,
    required this.rememberMe,
    required this.onRememberMeChanged,
    required this.onSubmit,
    required this.isLoading,
    this.error,
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
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          // ── Error banner ──
          if (error != null) ...[
            ErrorBanner(message: error!),
            const SizedBox(height: AppSpacing.lg),
          ],

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

          // ── Password ──
          AppTextField(
            controller: passwordController,
            label: 'Password',
            hint: 'Enter password',
            prefixIcon: Icons.lock_outline,
            obscureText: obscurePassword,
            enabled: !isLoading,
            textInputAction: TextInputAction.done,
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
              return null;
            },
          ),
          const SizedBox(height: AppSpacing.xs),

          // ── Remember me ──
          Row(
            children: [
              SizedBox(
                width: 20,
                height: 20,
                child: Checkbox(
                  value: rememberMe,
                  onChanged: isLoading
                      ? null
                      : (v) => onRememberMeChanged(v ?? false),
                  activeColor: AppColors.primaryBootstrap,
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(3.2),
                  ),
                  side: BorderSide(
                    color: const Color(0xFF94A3B8).withValues(alpha: 0.8),
                    width: 1,
                  ),
                  materialTapTargetSize: MaterialTapTargetSize.shrinkWrap,
                ),
              ),
              const SizedBox(width: 10),
              Text(
                'Remember me',
                style: TextStyle(
                  fontSize: 15,
                  color: AppColors.textStrong,
                  height: 1.2,
                ),
              ),
            ],
          ),
          const SizedBox(height: AppSpacing.xl),

          // ── Submit button ──
          _LoginSubmitButton(
            label: 'Sign In',
            loadingLabel: 'Signing in...',
            onPressed: isLoading ? null : onSubmit,
            isLoading: isLoading,
          ),
          const SizedBox(height: AppSpacing.xl),

          // ── Forgot password link ──
          Center(
            child: TextButton(
              onPressed: isLoading
                  ? null
                  : () => context.push('/auth/forgot-password'),
              child: const Text('Forgot password?'),
            ),
          ),

          // ── Sign up link ──
          Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              const Text(
                "Don't have an account?",
                style: TextStyle(fontSize: 13, color: AppColors.textSubtle),
              ),
              TextButton(
                onPressed: isLoading
                    ? null
                    : () => context.push('/auth/register'),
                child: const Text('Create one'),
              ),
            ],
          ),
          const SizedBox(height: AppSpacing.lg),

          // ── Footer ──
          const Center(
            child: Text(
              'Click computer and accessories',
              style: TextStyle(
                fontSize: 12,
                color: AppColors.textSubtle,
              ),
            ),
          ),
        ],
      ),
    );
  }
}

// ═══════════════════════════════════════════════════════════
// Login submit button — bright cyan-blue pill with shine sweep
// ═══════════════════════════════════════════════════════════
class _LoginSubmitButton extends StatefulWidget {
  final String label;
  final String loadingLabel;
  final VoidCallback? onPressed;
  final bool isLoading;

  const _LoginSubmitButton({
    required this.label,
    required this.loadingLabel,
    required this.onPressed,
    required this.isLoading,
  });

  @override
  State<_LoginSubmitButton> createState() => _LoginSubmitButtonState();
}

class _LoginSubmitButtonState extends State<_LoginSubmitButton> {
  bool _hovered = false;

  @override
  Widget build(BuildContext context) {
    return MouseRegion(
      onEnter: (_) => setState(() => _hovered = true),
      onExit: (_) => setState(() => _hovered = false),
      cursor: widget.isLoading
          ? SystemMouseCursors.basic
          : SystemMouseCursors.click,
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 225),
        curve: Curves.easeOut,
        height: 56,
        transform: Matrix4.translationValues(
          0,
          _hovered && !widget.isLoading ? -2 : 0,
          0,
        ),
        decoration: BoxDecoration(
          gradient: widget.isLoading ? null : AppColors.loginCta,
          color: widget.isLoading ? AppColors.loginCtaStart.withValues(alpha: 0.6) : null,
          borderRadius: AppRadius.borderPill,
          boxShadow: _hovered && !widget.isLoading
              ? AppShadows.loginButtonHover
              : AppShadows.loginButton,
        ),
        child: Material(
          color: Colors.transparent,
          child: InkWell(
            onTap: widget.onPressed,
            borderRadius: AppRadius.borderPill,
            child: Center(
              child: widget.isLoading
                  ? Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        const SizedBox(
                          width: 14,
                          height: 14,
                          child: CircularProgressIndicator(
                            strokeWidth: 2,
                            valueColor:
                                AlwaysStoppedAnimation<Color>(Colors.white),
                          ),
                        ),
                        const SizedBox(width: 8),
                        Text(
                          widget.loadingLabel,
                          style: const TextStyle(
                            fontSize: 19.2,
                            fontWeight: FontWeight.w700,
                            color: Colors.white,
                          ),
                        ),
                      ],
                    )
                  : Text(
                      widget.label,
                      style: const TextStyle(
                        fontSize: 19.2,
                        fontWeight: FontWeight.w700,
                        color: Colors.white,
                      ),
                    ),
            ),
          ),
        ),
      ),
    );
  }
}

