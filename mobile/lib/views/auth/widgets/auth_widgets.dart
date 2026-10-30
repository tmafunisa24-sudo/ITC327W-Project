// lib/views/auth/widgets/auth_widgets.dart
//
// Shared widgets used by all three auth screens:
//   - login_view.dart
//   - register_view.dart
//   - forgot_password_view.dart
//
// These were previously duplicated in each file. Now they live here.

import 'dart:math' as math;

import 'package:flutter/material.dart';

import '../../../core/design/app_colors.dart';
import '../../../core/design/app_radius.dart';
import '../../../core/design/app_shadows.dart';

// ═══════════════════════════════════════════════════════════
// Glass logo tile
// ═══════════════════════════════════════════════════════════
class GlassLogoTile extends StatelessWidget {
  /// The tile is 130×115 in register/forgot, but 170×150 in login.
  /// Pass the larger size for login.
  final double width;
  final double height;
  final double radius;
  final double logoWidth;
  final double logoHeight;

  const GlassLogoTile({
    super.key,
    this.width = 130,
    this.height = 115,
    this.radius = 28,
    this.logoWidth = 90,
    this.logoHeight = 68,
  });

  /// The login-screen variant (bigger, uses backdrop blur).
  const GlassLogoTile.large({super.key})
      : width = 170,
        height = 150,
        radius = 32,
        logoWidth = 120,
        logoHeight = 90;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: width,
      height: height,
      decoration: BoxDecoration(
        color: Colors.white.withValues(alpha: 0.12),
        borderRadius: BorderRadius.circular(radius),
        border: Border.all(
          color: Colors.white.withValues(alpha: 0.18),
        ),
        boxShadow: AppShadows.loginBrandTile,
      ),
      child: Center(
        child: Container(
          width: logoWidth,
          height: logoHeight,
          decoration: const BoxDecoration(
            image: DecorationImage(
              image: AssetImage('assets/images/click-laptop-phone-logo.png'),
              fit: BoxFit.contain,
            ),
          ),
        ),
      ),
    );
  }
}

// ═══════════════════════════════════════════════════════════
// Subtitle pill — "Click computer and accessories"
// ═══════════════════════════════════════════════════════════
class SubtitlePill extends StatelessWidget {
  final String text;

  /// Login uses 20px font; register/forgot use 18px.
  final double fontSize;

  const SubtitlePill({
    super.key,
    required this.text,
    this.fontSize = 18,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 18, vertical: 6),
      decoration: BoxDecoration(
        color: Colors.white.withValues(alpha: 0.05),
        borderRadius: AppRadius.borderPill,
        border: Border.all(
          color: Colors.white.withValues(alpha: 0.08),
        ),
      ),
      child: Text(
        text,
        textAlign: TextAlign.center,
        style: TextStyle(
          fontSize: fontSize,
          fontWeight: FontWeight.w400,
          letterSpacing: -0.5,
          color: Colors.white.withValues(alpha: 0.96),
          shadows: const [
            Shadow(
              color: Color(0x330F172A),
              blurRadius: 10,
              offset: Offset(0, 2),
            ),
          ],
        ),
      ),
    );
  }
}

// ═══════════════════════════════════════════════════════════
// Gradient title
// ═══════════════════════════════════════════════════════════
class GradientTitle extends StatelessWidget {
  final String text;

  /// Min and max font size. Login: 43–76. Register/forgot: 36–64.
  final double minSize;
  final double maxSize;

  const GradientTitle({
    super.key,
    required this.text,
    this.minSize = 36,
    this.maxSize = 64,
  });

  /// Login-screen variant (bigger scale).
  const GradientTitle.login({super.key, required this.text})
      : minSize = 43,
        maxSize = 76;

  @override
  Widget build(BuildContext context) {
    final width = MediaQuery.of(context).size.width;
    final size = math.max(minSize, math.min(maxSize, width * 0.09));

    return ShaderMask(
      blendMode: BlendMode.srcIn,
      shaderCallback: (bounds) {
        return AppColors.loginTitleText.createShader(
          Rect.fromLTWH(0, 0, bounds.width, bounds.height),
        );
      },
      child: Text(
        text,
        textAlign: TextAlign.center,
        style: TextStyle(
          fontSize: size,
          fontWeight: FontWeight.w500,
          letterSpacing: -0.05 * size,
          height: 1.0,
        ),
      ),
    );
  }
}

// ═══════════════════════════════════════════════════════════
// Cyan gradient pill submit button with hover lift
// ═══════════════════════════════════════════════════════════
class AuthSubmitButton extends StatefulWidget {
  final String label;
  final String loadingLabel;
  final VoidCallback? onPressed;
  final bool isLoading;
  final double height;

  const AuthSubmitButton({
    super.key,
    required this.label,
    required this.loadingLabel,
    required this.onPressed,
    required this.isLoading,
    this.height = 56,
  });

  @override
  State<AuthSubmitButton> createState() => _AuthSubmitButtonState();
}

class _AuthSubmitButtonState extends State<AuthSubmitButton> {
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
        height: widget.height,
        transform: Matrix4.translationValues(
          0,
          _hovered && !widget.isLoading ? -2 : 0,
          0,
        ),
        decoration: BoxDecoration(
          gradient: widget.isLoading ? null : AppColors.loginCta,
          color: widget.isLoading
              ? AppColors.loginCtaStart.withValues(alpha: 0.6)
              : null,
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
                            valueColor: AlwaysStoppedAnimation<Color>(
                              Colors.white,
                            ),
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

// ═══════════════════════════════════════════════════════════
// Red danger banner
// ═══════════════════════════════════════════════════════════
class ErrorBanner extends StatelessWidget {
  final String message;

  const ErrorBanner({super.key, required this.message});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: AppColors.dangerAlpha(0.08),
        borderRadius: AppRadius.borderMd,
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
              message,
              style: const TextStyle(
                color: AppColors.danger,
                fontSize: 13,
              ),
            ),
          ),
        ],
      ),
    );
  }
}