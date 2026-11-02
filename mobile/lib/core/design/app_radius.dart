// lib/core/design/app_radius.dart

import 'package:flutter/widgets.dart';

/// Border radii — extracted from `wwwroot/css/site.css` + `tickets.css`.
///
/// The web app uses a wide range of radii. Rather than a single scale,
/// this exposes semantic values matched to the actual components.
///
/// Rules of thumb (from the spec):
/// - **Buttons are pills** (`999px`), except ticket action buttons (`8px`) and
///   `.btn-create-ticket` (`12px`).
/// - **Badges are pills** (`999px`), except nothing.
/// - **Cards are 22px** by default, but ticket cards use 22px and dashboard
///   headers use 20px, and detail blocks use 18px.
/// - **Inputs are 14px** globally, 16px on the login page.
/// - **Sidebar is 26px**, main content panel is 30px, topbar is 24px.
class AppRadius {
  AppRadius._();

  // ── Scale values (raw doubles) ──
  static const double none = 0;
  static const double xs = 6; // 6px — small images (.rounded)
  static const double sm = 8; // 8px — ticket action buttons, mapped images
  static const double md = 12; // 12px — btn-create-ticket, icon-btn
  static const double lg = 14; // 14px — inputs, icon tiles
  static const double xl = 16; // 16px — .rounded-4, login inputs, empty-state (tickets)
  static const double xxl = 18; // 18px — stat-cards, detail blocks, alert, dropdown
  static const double xxxl = 20; // 20px — table-cards, dashboard headers, empty-state (site)
  static const double card = 22; // 22px — every .card + ticket cards
  static const double topbar = 24; // 24px — topbar, hero-panel
  static const double sidebar = 26; // 26px — sidebar (--radius-xl)
  static const double mainContent = 30; // 30px — main content panel
  static const double loginCard = 32; // 32px — login card + logo tile
  static const double pill = 999; // 999px — buttons, badges, pills

  // ── Semantic BorderRadius constants ──

  // Small images, thumbnails
  static const BorderRadius borderXs = BorderRadius.all(Radius.circular(xs));
  static const BorderRadius borderSm = BorderRadius.all(Radius.circular(sm));
  static const BorderRadius borderMd = BorderRadius.all(Radius.circular(md));
  static const BorderRadius borderLg = BorderRadius.all(Radius.circular(lg));
  static const BorderRadius borderXl = BorderRadius.all(Radius.circular(xl));
  static const BorderRadius borderXxl = BorderRadius.all(Radius.circular(xxl));
  static const BorderRadius borderXxxl = BorderRadius.all(Radius.circular(xxxl));
  static const BorderRadius borderCard = BorderRadius.all(Radius.circular(card));
  static const BorderRadius borderTopbar =
      BorderRadius.all(Radius.circular(topbar));
  static const BorderRadius borderSidebar =
      BorderRadius.all(Radius.circular(sidebar));
  static const BorderRadius borderMainContent =
      BorderRadius.all(Radius.circular(mainContent));
  static const BorderRadius borderLoginCard =
      BorderRadius.all(Radius.circular(loginCard));
  static const BorderRadius borderPill =
      BorderRadius.all(Radius.circular(pill));

  // ── Directional radii (for bottom sheets, tabs, etc.) ──

  /// Only the top corners rounded — used by bottom sheets.
  static const BorderRadius borderTopXxl = BorderRadius.only(
    topLeft: Radius.circular(xxl),
    topRight: Radius.circular(xxl),
  );

  /// Only the top corners rounded, larger — used by main bottom sheet modals.
  static const BorderRadius borderTopSheet = BorderRadius.only(
    topLeft: Radius.circular(xxl),
    topRight: Radius.circular(xxl),
  );

  // ── Convenience helpers ──

  /// Named radii for the default card type.
  /// `.card`, `.ticket-*-card`, `.ticket-container`, `.ticket-header`.
  static const BorderRadius cardRadius = borderCard;

  /// Named radii for the standard input field.
  /// `.form-control`, `.form-select`, `textarea`, date inputs.
  static const BorderRadius inputRadius = borderLg;

  /// Named radii for the login input field.
  static const BorderRadius loginInputRadius = borderXl;

  /// Named radii for icon tiles (44×44 / 46×46 tiles).
  static const BorderRadius iconTileRadius = borderLg;

  /// Named radii for ticket action buttons (`.btn-assign`, `.btn-view`, ...).
  static const BorderRadius ticketButtonRadius = borderSm;

  /// Named radii for `.btn-create-ticket`.
  static const BorderRadius createTicketButtonRadius = borderMd;
}