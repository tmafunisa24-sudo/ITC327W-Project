// lib/core/design/app_shadows.dart

import 'package:flutter/material.dart';

/// Shadow system — extracted from `wwwroot/css/site.css` + `tickets.css`.
///
/// The web app declares ~30 distinct box-shadows. They cluster into a
/// manageable set of semantic values below.
///
/// Note on performance: CSS blur ≈ Flutter blurRadius 1:1, but the web
/// shadows here use large blur radii (36–68px). On mobile these are
/// expensive when stacked. The spec's recommendation to halve blur and
/// offset on Android is honored via [compact] variants of the big ones.
class AppShadows {
  AppShadows._();

  // ═══════════════════════════════════════════════════════
  // 1. Card / panel shadows (light surfaces)
  // ═══════════════════════════════════════════════════════

  /// `.main-content` — the big white panel that holds page content.
  /// `0 18px 40px rgba(15,23,42,.08)`
  static const List<BoxShadow> soft = [
    BoxShadow(
      color: Color(0x140F172A),
      blurRadius: 40,
      offset: Offset(0, 18),
    ),
  ];

  /// `.card`, `.hero-panel`, `.kpi-card`, `.table-card` (site).
  /// `0 18px 36px rgba(15,23,42,.12)`
  static const List<BoxShadow> card = [
    BoxShadow(
      color: Color(0x1F0F172A),
      blurRadius: 36,
      offset: Offset(0, 18),
    ),
  ];

  /// `.card:hover`.
  /// `0 22px 46px rgba(15,23,42,.12)`
  static const List<BoxShadow> cardHover = [
    BoxShadow(
      color: Color(0x1F0F172A),
      blurRadius: 46,
      offset: Offset(0, 22),
    ),
  ];

  /// `.ticket-*-card`, `.ticket-container`.
  /// `0 18px 40px rgba(15,23,42,.06)`
  static const List<BoxShadow> ticketCard = [
    BoxShadow(
      color: Color(0x0F0F172A),
      blurRadius: 40,
      offset: Offset(0, 18),
    ),
  ];

  /// `.ticket-header`.
  /// `0 18px 40px rgba(15,23,42,.07)`
  static const List<BoxShadow> ticketHeader = [
    BoxShadow(
      color: Color(0x120F172A),
      blurRadius: 40,
      offset: Offset(0, 18),
    ),
  ];

  /// `.stat-card` (tickets).
  /// `0 14px 28px rgba(15,23,42,.06)`
  static const List<BoxShadow> statCard = [
    BoxShadow(
      color: Color(0x0F0F172A),
      blurRadius: 28,
      offset: Offset(0, 14),
    ),
  ];

  /// `.stat-card:hover`.
  /// `0 18px 36px rgba(15,23,42,.09)`
  static const List<BoxShadow> statCardHover = [
    BoxShadow(
      color: Color(0x170F172A),
      blurRadius: 36,
      offset: Offset(0, 18),
    ),
  ];

  /// `.summary-card` (dashboard).
  /// `0 16px 32px rgba(15,23,42,.06)`
  static const List<BoxShadow> summaryCard = [
    BoxShadow(
      color: Color(0x0F0F172A),
      blurRadius: 32,
      offset: Offset(0, 16),
    ),
  ];

  /// `.dashboard-header`.
  /// `0 16px 34px rgba(15,23,42,.07)`
  static const List<BoxShadow> dashboardHeader = [
    BoxShadow(
      color: Color(0x120F172A),
      blurRadius: 34,
      offset: Offset(0, 16),
    ),
  ];

  // ═══════════════════════════════════════════════════════
  // 2. Chrome shadows (navy sidebar, topbar)
  // ═══════════════════════════════════════════════════════

  /// `.sidebar`.
  /// `0 28px 68px rgba(15,23,42,.15)`
  static const List<BoxShadow> sidebar = [
    BoxShadow(
      color: Color(0x260F172A),
      blurRadius: 68,
      offset: Offset(0, 28),
    ),
  ];

  /// `.sidebar` — compact variant for mobile.
  /// Halved blur + offset.
  static const List<BoxShadow> sidebarCompact = [
    BoxShadow(
      color: Color(0x260F172A),
      blurRadius: 34,
      offset: Offset(0, 14),
    ),
  ];

  /// `.topbar`.
  /// `0 24px 46px rgba(15,23,42,.14)`
  static const List<BoxShadow> topbar = [
    BoxShadow(
      color: Color(0x240F172A),
      blurRadius: 46,
      offset: Offset(0, 24),
    ),
  ];

  /// `.topbar` — compact variant for mobile.
  static const List<BoxShadow> topbarCompact = [
    BoxShadow(
      color: Color(0x240F172A),
      blurRadius: 23,
      offset: Offset(0, 12),
    ),
  ];

  // ═══════════════════════════════════════════════════════
  // 3. Overlays / dropdowns / menus
  // ═══════════════════════════════════════════════════════

  /// `.dropdown-menu`.
  /// `0 24px 50px rgba(15,23,42,.12)`
  static const List<BoxShadow> dropdown = [
    BoxShadow(
      color: Color(0x1F0F172A),
      blurRadius: 50,
      offset: Offset(0, 24),
    ),
  ];

  /// `.alert`.
  /// `0 12px 28px rgba(15,23,42,.08)`
  static const List<BoxShadow> alert = [
    BoxShadow(
      color: Color(0x140F172A),
      blurRadius: 28,
      offset: Offset(0, 12),
    ),
  ];

  /// `.toast` (Bootstrap default).
  /// `0 8px 16px rgba(0,0,0,.15)`
  static const List<BoxShadow> toast = [
    BoxShadow(
      color: Color(0x26000000),
      blurRadius: 16,
      offset: Offset(0, 8),
    ),
  ];

  // ═══════════════════════════════════════════════════════
  // 4. Brand & button shadows (colored)
  // ═══════════════════════════════════════════════════════

  /// `.brand-mark` — the gradient "S" tile in the sidebar.
  /// `0 18px 34px rgba(37,99,235,.23)`
  static const List<BoxShadow> brandMark = [
    BoxShadow(
      color: Color(0x3B2563EB),
      blurRadius: 34,
      offset: Offset(0, 18),
    ),
  ];

  /// `.btn-primary`.
  /// `0 14px 32px rgba(37,99,235,.24)`
  static const List<BoxShadow> primaryButton = [
    BoxShadow(
      color: Color(0x3D2563EB),
      blurRadius: 32,
      offset: Offset(0, 14),
    ),
  ];

  /// Login submit button (rest state).
  /// `0 14px 28px rgba(14,116,206,.3), inset 0 1px 0 rgba(255,255,255,.18)`
  static const List<BoxShadow> loginButton = [
    BoxShadow(
      color: Color(0x4D0E74CE),
      blurRadius: 28,
      offset: Offset(0, 14),
    ),
    BoxShadow(
      color: Color(0x2EFFFFFF),
      blurRadius: 0,
      offset: Offset(0, 1),
      spreadRadius: 1,
    ),
  ];

  /// Login submit button (hover state).
  /// `0 16px 30px rgba(14,116,206,.38), inset 0 1px 0 rgba(255,255,255,.18)`
  static const List<BoxShadow> loginButtonHover = [
    BoxShadow(
      color: Color(0x610E74CE),
      blurRadius: 30,
      offset: Offset(0, 16),
    ),
    BoxShadow(
      color: Color(0x2EFFFFFF),
      blurRadius: 0,
      offset: Offset(0, 1),
      spreadRadius: 1,
    ),
  ];

  /// Login brand tile (rest state).
  /// `0 24px 64px rgba(15,23,42,.22), inset 0 1px 0 rgba(255,255,255,.22)`
  static const List<BoxShadow> loginBrandTile = [
    BoxShadow(
      color: Color(0x380F172A),
      blurRadius: 64,
      offset: Offset(0, 24),
    ),
    BoxShadow(
      color: Color(0x38FFFFFF),
      blurRadius: 0,
      offset: Offset(0, 1),
      spreadRadius: 1,
    ),
  ];

  /// Login brand tile (hover state).
  /// `0 28px 72px rgba(15,23,42,.28), inset 0 1px 0 rgba(255,255,255,.22)`
  static const List<BoxShadow> loginBrandTileHover = [
    BoxShadow(
      color: Color(0x470F172A),
      blurRadius: 72,
      offset: Offset(0, 28),
    ),
    BoxShadow(
      color: Color(0x38FFFFFF),
      blurRadius: 0,
      offset: Offset(0, 1),
      spreadRadius: 1,
    ),
  ];

  /// Login card (auth card on the login page).
  /// `0 14px 42px rgba(15,23,42,.14), inset 0 1px 0 rgba(255,255,255,.7)`
  static const List<BoxShadow> loginCard = [
    BoxShadow(
      color: Color(0x240F172A),
      blurRadius: 42,
      offset: Offset(0, 14),
    ),
    BoxShadow(
      color: Color(0xB3FFFFFF),
      blurRadius: 0,
      offset: Offset(0, 1),
      spreadRadius: 1,
    ),
  ];

  // ═══════════════════════════════════════════════════════
  // 5. Inset & focus shadows
  // ═══════════════════════════════════════════════════════

  /// Standard inset shadow on inputs.
  /// `inset 0 1px 2px rgba(15,23,42,.04)`
  static const List<BoxShadow> inputInset = [
    BoxShadow(
      color: Color(0x0A0F172A),
      blurRadius: 2,
      offset: Offset(0, 1),
      spreadRadius: 0,
    ),
  ];

  /// Input focus ring (site.css).
  /// `0 0 0 0.2rem rgba(37,99,235,.12)`
  static const List<BoxShadow> focusRing = [
    BoxShadow(
      color: Color(0x1F2563EB),
      blurRadius: 0,
      offset: Offset.zero,
      spreadRadius: 3.2,
    ),
  ];

  /// Input focus ring (login page).
  /// `0 0 0 0.18rem rgba(59,130,246,.12)`
  static const List<BoxShadow> focusRingLogin = [
    BoxShadow(
      color: Color(0x1F3B82F6),
      blurRadius: 0,
      offset: Offset.zero,
      spreadRadius: 2.88,
    ),
  ];

  /// Empty state has no shadow.
  static const List<BoxShadow> none = [];

  // ═══════════════════════════════════════════════════════
  // 6. Backward-compat aliases
  //     Older code referenced these names. Kept so existing screens
  //     keep compiling.
  // ═══════════════════════════════════════════════════════

  /// Alias for [card]. Older code called this `cardElevated`.
  static const List<BoxShadow> cardElevated = card;

  /// Alias for [primaryButton]. Older code called this `floating`.
  static const List<BoxShadow> floating = primaryButton;
}