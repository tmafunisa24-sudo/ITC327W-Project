// lib/core/design/app_colors.dart

import 'package:flutter/material.dart';

/// ServiceHub IT color palette.
///
/// Source of truth: `wwwroot/css/site.css` + `tickets.css`.
/// Values verified against the design spec extracted from the ASP.NET project.
///
/// Notes on the port:
/// - Colors are grouped by role, not by CSS variable, because the CSS `:root`
///   block only defines ~8 of the ~40 colors actually used in the app.
/// - Opaque equivalents are provided for translucent colors (composited on white)
///   so you can choose which to use. Translucent preserves the "tint over surface"
///   feel; opaque is faster to render.
/// - The web app uses Bootstrap defaults for some components (e.g. `.bg-primary`
///   badges use Bootstrap `#0d6efd`, not our `#2563eb`). Those are flagged
///   separately so we can decide per component.
class AppColors {
  AppColors._();

  // ═══════════════════════════════════════════════════════
  // 1. Brand blues
  // ═══════════════════════════════════════════════════════

  /// `--primary`. Main brand blue. Links, buttons, focus, icons.
  static const Color primary = Color(0xFF2563EB);

  /// Darker blue used as gradient end on ticket-assign buttons and
  /// leaderboard header. In Tailwind: blue-700.
  static const Color primaryDark = Color(0xFF1D4ED8);

  /// Bootstrap's stock primary. Appears wherever the author didn't override
  /// (checkbox checked, progress bar, bg-primary badges, outline buttons hover).
  /// Use this ONLY when replicating Bootstrap's default. Do not use it for new
  /// custom buttons — use [primary] instead.
  static const Color primaryBootstrap = Color(0xFF0D6EFD);

  /// Very soft primary background. Approximates `rgba(37,99,235,.08)` on white.
  static const Color primarySoft = Color(0xFFEEF3FD);

  /// Login page CTA gradient start — brighter, more cyan than [primary].
  static const Color loginCtaStart = Color(0xFF1D9BF0);
  static const Color loginCtaEnd = Color(0xFF0A7AE5);

  // ═══════════════════════════════════════════════════════
  // 2. Semantic colors
  // ═══════════════════════════════════════════════════════

  static const Color success = Color(0xFF16A34A);
  static const Color successDark = Color(0xFF059669);
  static const Color warning = Color(0xFFF59E0B);
  static const Color warningDark = Color(0xFFD97706);
  static const Color danger = Color(0xFFDC2626);
  static const Color dangerDark = Color(0xFFB91C1C);
  static const Color purple = Color(0xFF8B5CF6);
  static const Color secondary = Color(0xFF1D9BF0);
  static const Color accent = Color(0xFF22C55E);

  // Bootstrap semantic defaults that leak into components
  // Use only when replicating a Bootstrap-defaulted element.
  static const Color successBootstrap = Color(0xFF198754);
  static const Color dangerBootstrap = Color(0xFFDC3545);
  static const Color warningBootstrap = Color(0xFFFFC107);
  static const Color infoBootstrap = Color(0xFF0DCAF0);
  static const Color secondaryBootstrap = Color(0xFF6C757D);

  // ═══════════════════════════════════════════════════════
  // 3. Text (Tailwind slate ramp)
  // ═══════════════════════════════════════════════════════

  /// Headings, input text, table headers. Tailwind slate-900.
  static const Color textPrimary = Color(0xFF0F172A);

  /// Body text. Tailwind gray-900.
  static const Color textBody = Color(0xFF111827);

  /// Slightly muted body text. Tailwind slate-700.
  static const Color textStrong = Color(0xFF334155);

  /// Default muted. `--muted`. Tailwind slate-600.
  static const Color textMuted = Color(0xFF475569);

  /// Captions, small labels. Tailwind slate-500.
  static const Color textSubtle = Color(0xFF64748B);

  /// Italic placeholders ("Not set", "Unassigned"). Tailwind slate-400.
  static const Color textPlaceholder = Color(0xFF94A3B8);

  /// Text on navy chrome (sidebar links, topbar pills). Tailwind slate-200.
  static const Color textOnNavy = Color(0xFFE2E8F0);

  /// Titles on navy chrome. Tailwind slate-50.
  static const Color textOnNavyStrong = Color(0xFFF8FAFC);

  /// Text on dark navy (login page). Tailwind slate-50.
  static const Color textOnDark = Color(0xFFF8FAFC);

  // ═══════════════════════════════════════════════════════
  // 4. Surfaces
  // ═══════════════════════════════════════════════════════

  /// Page background (bottom of page gradient).
  static const Color background = Color(0xFFF8FAFC);

  /// Page background (top of page gradient).
  static const Color backgroundTop = Color(0xFFEEF5FF);

  /// Page background (midpoint of page gradient).
  static const Color backgroundMid = Color(0xFFF8FBFF);

  /// Card background.
  static const Color surface = Color(0xFFFFFFFF);

  /// Soft secondary surface — form input backgrounds, detail blocks.
  static const Color surfaceSoft = Color(0xFFF8FAFC);

  /// Light blue surface — table header backgrounds, unread rows.
  static const Color surfaceAlt = Color(0xFFEEF4FF);

  /// Hero/card gradient end tint.
  static const Color surfaceHeroEnd = Color(0xFFEEF6FF);

  /// Stat-card gradient start.
  static const Color statCardStart = Color(0xFFF5F8FF);

  /// Stat-card gradient end.
  static const Color statCardEnd = Color(0xFFEBF3FF);

  // ═══════════════════════════════════════════════════════
  // 5. Borders
  // ═══════════════════════════════════════════════════════

  /// Card border (blue-tinted). `rgba(59,130,246,.12)` on white.
  static const Color border = Color(0xFFE7F0FE);

  /// Input border. `rgba(148,163,184,.38)` on white.
  static const Color borderInput = Color(0xFFD6DCE4);

  /// Table divider. `rgba(226,232,240,.9)` on white.
  static const Color borderDivider = Color(0xFFE5EAF2);

  /// Near-neutral border used by ticket cards. `rgba(15,23,42,.07)` on white.
  static const Color borderTicket = Color(0xFFECECEE);

  /// Blue-tinted border used by alerts (even success/danger alerts).
  static const Color borderAlert = Color(0xFFDCE9FD);

  /// Dashed empty-state border.
  static const Color borderDashed = Color(0xFFDCE9FD);

  // ═══════════════════════════════════════════════════════
  // 6. Chrome (sidebar, topbar, login background)
  // ═══════════════════════════════════════════════════════

  static const Color sidebarTop = Color(0xFF0F2B5D);
  static const Color sidebarBottom = Color(0xFF122F68);
  static const Color topbar = Color(0xFF152A4F);

  static const Color loginBgTop = Color(0xFF071B35);
  static const Color loginBgMid = Color(0xFF0D294F);
  static const Color loginBgBottom = Color(0xFF102B55);

  // ═══════════════════════════════════════════════════════
  // 7. Status & priority colors
  //    (background + foreground pairs from tickets.css)
  // ═══════════════════════════════════════════════════════

  // Status
  static const Color statusOpenBg = Color(0xFFE5ECFD);
  static const Color statusOpenFg = Color(0xFF1D4ED8);

  static const Color statusAssignedBg = Color(0xFFEFE8FE);
  static const Color statusAssignedFg = Color(0xFF7C3AED);

  static const Color statusInProgressBg = Color(0xFFFDEFD8);
  static const Color statusInProgressFg = Color(0xFFB45309);

  static const Color statusResolvedBg = Color(0xFFDEF5ED);
  static const Color statusResolvedFg = Color(0xFF047857);

  static const Color statusClosedBg = Color(0xFFE6E9EC);
  static const Color statusClosedFg = Color(0xFF475569);

  // Priority
  static const Color priorityLowBg = Color(0xFFDEF5ED);
  static const Color priorityLowFg = Color(0xFF047857);

  static const Color priorityMediumBg = Color(0xFFFDEFD8);
  static const Color priorityMediumFg = Color(0xFFB45309);

  static const Color priorityHighBg = Color(0xFFFCE1E1);
  static const Color priorityHighFg = Color(0xFFB91C1C);

  static const Color priorityCriticalBg = Color(0xFFEBDBDB);
  static const Color priorityCriticalFg = Color(0xFF7F1D1D);

  // Alert backgrounds
  static const Color alertSuccessBg = Color(0xFFE7F8F2);
  static const Color alertSuccessFg = Color(0xFF065F46);
  static const Color alertDangerBg = Color(0xFFFDECEC);
  static const Color alertDangerFg = Color(0xFF991B1B);

  // ═══════════════════════════════════════════════════════
  // 8. Alpha helpers (translucent versions of the above)
  //    Use these when you want a tint over an arbitrary surface.
  // ═══════════════════════════════════════════════════════

  static Color primaryAlpha(double a) => primary.withValues(alpha: a);
  static Color successAlpha(double a) => success.withValues(alpha: a);
  static Color warningAlpha(double a) => warning.withValues(alpha: a);
  static Color dangerAlpha(double a) => danger.withValues(alpha: a);
  static Color purpleAlpha(double a) => purple.withValues(alpha: a);
  static Color secondaryAlpha(double a) => secondary.withValues(alpha: a);
  static Color whiteAlpha(double a) => Colors.white.withValues(alpha: a);
  static Color blackAlpha(double a) => Colors.black.withValues(alpha: a);

  // ═══════════════════════════════════════════════════════
  // 9. Gradients
  // ═══════════════════════════════════════════════════════

  /// Brand gradient — logo tile, welcome cards, leaderboard header.
  /// `linear-gradient(135deg, #2563EB, #1D4ED8)`
  static const LinearGradient brand = LinearGradient(
    colors: [primary, primaryDark],
    begin: Alignment.topLeft,
    end: Alignment.bottomRight,
  );

  /// Primary button gradient — `linear-gradient(135deg, #2563EB, #0D6EFD)`
  static const LinearGradient primaryButton = LinearGradient(
    colors: [primary, primaryBootstrap],
    begin: Alignment.topLeft,
    end: Alignment.bottomRight,
  );

  /// Login CTA button gradient — brighter, more cyan.
  static const LinearGradient loginCta = LinearGradient(
    colors: [loginCtaStart, loginCtaEnd],
    begin: Alignment.topLeft,
    end: Alignment.bottomRight,
  );

  /// Secondary button gradient (grey).
  static const LinearGradient secondaryButton = LinearGradient(
    colors: [Color(0xFF64748B), Color(0xFF475569)],
    begin: Alignment.topLeft,
    end: Alignment.bottomRight,
  );

  /// Danger button gradient.
  static const LinearGradient dangerButton = LinearGradient(
    colors: [Color(0xFFEF4444), dangerDark],
    begin: Alignment.topLeft,
    end: Alignment.bottomRight,
  );

  /// Success button gradient.
  static const LinearGradient successButton = LinearGradient(
    colors: [success, successDark],
    begin: Alignment.topLeft,
    end: Alignment.bottomRight,
  );

  /// Sidebar gradient — top to bottom navy.
  static const LinearGradient sidebar = LinearGradient(
    colors: [sidebarTop, sidebarBottom],
    begin: Alignment.topCenter,
    end: Alignment.bottomCenter,
  );

  /// Page background gradient — very subtle blue tint fading to white.
  static const LinearGradient pageBackground = LinearGradient(
    colors: [backgroundTop, backgroundMid, surface],
    stops: [0.0, 0.55, 1.0],
    begin: Alignment.topCenter,
    end: Alignment.bottomCenter,
  );

  /// Login page background — layered radials over a navy vertical gradient.
  /// Use inside a Stack with two Positioned radials for full fidelity.
  static const LinearGradient loginBackground = LinearGradient(
    colors: [loginBgTop, loginBgMid, loginBgBottom],
    stops: [0.0, 0.38, 1.0],
    begin: Alignment.topCenter,
    end: Alignment.bottomCenter,
  );

  /// Hero-panel card gradient (white → very light blue).
  static const LinearGradient heroPanel = LinearGradient(
    colors: [Color(0xFFFFFFFF), surfaceHeroEnd],
    begin: Alignment.topLeft,
    end: Alignment.bottomRight,
  );

  /// Stat-card gradient (Admin/Users style).
  static const LinearGradient statCard = LinearGradient(
    colors: [statCardStart, statCardEnd],
    begin: Alignment.topCenter,
    end: Alignment.bottomCenter,
  );

  /// Stat-card gradient (Tickets style).
  static const LinearGradient statCardTicket = LinearGradient(
    colors: [Color(0xFFFFFFFF), Color(0xFFF8FBFF)],
    begin: Alignment.topCenter,
    end: Alignment.bottomCenter,
  );

  /// Gradient text fill for the login title.
  static const LinearGradient loginTitleText = LinearGradient(
    colors: [Color(0xFAFFFFFF), Color(0xE6E2E8F0)],
    begin: Alignment.topCenter,
    end: Alignment.bottomCenter,
  );

  /// Login button shine sweep — overlay gradient, animated on hover.
  static const LinearGradient loginButtonShine = LinearGradient(
    colors: [Colors.transparent, Color(0x38FFFFFF), Colors.transparent],
    stops: [0.0, 0.5, 1.0],
    begin: Alignment.centerLeft,
    end: Alignment.centerRight,
  );

  // ═══════════════════════════════════════════════════════
  // 10. Radial glows (for decorative circle overlays)
  // ═══════════════════════════════════════════════════════

  /// Sidebar decorative glow — bottom-right corner.
  static RadialGradient sidebarGlow = RadialGradient(
    colors: [primaryAlpha(0.18), Colors.transparent],
    stops: const [0.0, 0.65],
  );

  /// Hero-panel decorative glow — bottom-right corner.
  static RadialGradient heroGlow = RadialGradient(
    colors: [const Color(0xFF3B82F6).withValues(alpha: 0.18), Colors.transparent],
    stops: const [0.0, 0.68],
  );

  /// Login page top-left glow.
  static RadialGradient loginGlowTopLeft = RadialGradient(
    colors: [const Color(0xFF93C5FD).withValues(alpha: 0.32), Colors.transparent],
    stops: const [0.0, 0.18],
  );

  /// Login page bottom-right glow.
  static RadialGradient loginGlowBottomRight = RadialGradient(
    colors: [primaryAlpha(0.22), Colors.transparent],
    stops: const [0.0, 0.20],
  );

  // ═══════════════════════════════════════════════════════
  // 11. Utility constructors
  // ═══════════════════════════════════════════════════════

  /// Returns the (background, foreground) pair for a status string.
  /// Case-insensitive. Matches the web app's fallback to "Closed" colours.
  static (Color bg, Color fg) forStatus(String status) {
    switch (status.toLowerCase()) {
      case 'open':
        return (statusOpenBg, statusOpenFg);
      case 'assigned':
        return (statusAssignedBg, statusAssignedFg);
      case 'in progress':
        return (statusInProgressBg, statusInProgressFg);
      case 'resolved':
        return (statusResolvedBg, statusResolvedFg);
      case 'closed':
      default:
        return (statusClosedBg, statusClosedFg);
    }
  }

  // ═══════════════════════════════════════════════════════
  // 12. Backward-compat aliases
  //     Older code references these names. Kept so existing screens
  //     keep compiling. Prefer the newer names for new code.
  // ═══════════════════════════════════════════════════════

  /// Alias for [brand]. Older code called this `brandGradient`.
  static const LinearGradient brandGradient = brand;

  /// Alias for [backgroundTop]. Older code called this `primaryLight`.
  static const Color primaryLight = backgroundTop;

  /// Returns the (background, foreground) pair for a priority string.
  /// Falls back to "Medium" as per the web app.
  static (Color bg, Color fg) forPriority(String priority) {
    switch (priority.toLowerCase()) {
      case 'low':
        return (priorityLowBg, priorityLowFg);
      case 'high':
        return (priorityHighBg, priorityHighFg);
      case 'critical':
        return (priorityCriticalBg, priorityCriticalFg);
      case 'medium':
      default:
        return (priorityMediumBg, priorityMediumFg);
    }
  }
}