// lib/core/design/widgets/app_text_field.dart

import 'package:flutter/material.dart';

import '../app_colors.dart';
import '../app_radius.dart';

/// Standard text input — matching `.form-control` from `site.css`.
///
/// Values from spec:
/// - padding 14.08 / 16
/// - radius 14px
/// - border 1px rgba(148,163,184,.38)
/// - focus border `rgba(37,99,235,.62)` + ring `0 0 0 .2rem rgba(37,99,235,.12)`
/// - white background
/// - placeholder color rgba(15,23,42,.42)
class AppTextField extends StatelessWidget {
  final TextEditingController? controller;
  final String? label;
  final String? hint;
  final IconData? prefixIcon;
  final Widget? suffixIcon;
  final bool obscureText;
  final bool enabled;
  final int? maxLines;
  final int? minLines;
  final TextInputType? keyboardType;
  final String? Function(String?)? validator;
  final ValueChanged<String>? onChanged;
  final VoidCallback? onTap;
  final bool readOnly;
  final TextInputAction? textInputAction;
  final FocusNode? focusNode;
  final bool autofocus;

  const AppTextField({
    super.key,
    this.controller,
    this.label,
    this.hint,
    this.prefixIcon,
    this.suffixIcon,
    this.obscureText = false,
    this.enabled = true,
    this.maxLines = 1,
    this.minLines,
    this.keyboardType,
    this.validator,
    this.onChanged,
    this.onTap,
    this.readOnly = false,
    this.textInputAction,
    this.focusNode,
    this.autofocus = false,
  });

  @override
  Widget build(BuildContext context) {
    return TextFormField(
      controller: controller,
      focusNode: focusNode,
      autofocus: autofocus,
      obscureText: obscureText,
      enabled: enabled,
      maxLines: maxLines,
      minLines: minLines,
      keyboardType: keyboardType,
      validator: validator,
      onChanged: onChanged,
      onTap: onTap,
      readOnly: readOnly,
      textInputAction: textInputAction,
      style: const TextStyle(
        fontSize: 14,
        color: AppColors.textPrimary,
        fontWeight: FontWeight.w500,
      ),
      decoration: InputDecoration(
        labelText: label,
        hintText: hint,
        prefixIcon: prefixIcon != null
            ? Icon(prefixIcon, size: 20, color: AppColors.textSubtle)
            : null,
        suffixIcon: suffixIcon,
        filled: true,
        fillColor: enabled ? AppColors.surface : AppColors.surfaceSoft,
        contentPadding: const EdgeInsets.symmetric(
          horizontal: 16,
          vertical: 14,
        ),
        border: _border(AppColors.borderInput, 1),
        enabledBorder: _border(AppColors.borderInput, 1),
        focusedBorder: _border(AppColors.primaryAlpha(0.62), 2),
        errorBorder: _border(AppColors.danger, 1),
        focusedErrorBorder: _border(AppColors.danger, 2),
        disabledBorder: _border(AppColors.borderDivider, 1),
        labelStyle: const TextStyle(
          fontSize: 13,
          color: AppColors.textMuted,
          fontWeight: FontWeight.w500,
        ),
        floatingLabelStyle: const TextStyle(
          fontSize: 13,
          color: AppColors.primary,
          fontWeight: FontWeight.w600,
        ),
        hintStyle: TextStyle(
          fontSize: 14,
          color: AppColors.textPrimary.withValues(alpha: 0.42),
        ),
      ),
    );
  }

  OutlineInputBorder _border(Color color, double width) {
    return OutlineInputBorder(
      borderRadius: AppRadius.borderLg,
      borderSide: BorderSide(color: color, width: width),
    );
  }
}