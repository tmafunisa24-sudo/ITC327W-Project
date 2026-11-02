// lib/viewmodels/notification_viewmodel.dart

import 'dart:async';
import 'package:flutter/foundation.dart';

import '../models/notification.dart';
import '../services/notification_service.dart';

class NotificationViewModel extends ChangeNotifier {
  final NotificationService _service;
  NotificationViewModel([NotificationService? service])
      : _service = service ?? NotificationService();

  // ── State ──
  List<Notification> _all = [];
  List<Notification> _recent = [];
  int _unreadCount = 0;
  bool _isLoading = false;
  String? _error;
  Timer? _pollTimer;

  // ── Getters ──
  List<Notification> get all => _all;
  List<Notification> get recent => _recent;
  int get unreadCount => _unreadCount;
  bool get isLoading => _isLoading;
  String? get error => _error;

  /// Called by the app after login. Polls every 60 seconds.
  void startPolling() {
    _pollTimer?.cancel();

    // Initial fetch without waiting on the timer callback.
    unawaited(refreshBadge());

    // Poll every 60 seconds and avoid overlapping refresh requests.
    _pollTimer = Timer.periodic(
      const Duration(seconds: 60),
      (_) => unawaited(refreshBadge()),
    );
  }

  @override
  void dispose() {
    _pollTimer?.cancel();
    _pollTimer = null;
    super.dispose();
  }

  /// Stops polling (called on logout).
  void stopPolling() {
    _pollTimer?.cancel();
    _pollTimer = null;
    _all = [];
    _recent = [];
    _unreadCount = 0;
    notifyListeners();
  }

  /// Loads full list + refresh badge. Called when opening the notifications screen.
  Future<void> loadAll() async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      _all = await _service.getAll();
      // Recompute count locally for consistency
      _unreadCount = _all.where((n) => !n.isRead).length;
      _error = null;
    } catch (e) {
      _error = e.toString();
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  /// Refreshes the badge + recent list. Called by polling.
  Future<void> refreshBadge() async {
    try {
      final bundle = await _service.getRecent(limit: 6);
      _recent = bundle.notifications;
      _unreadCount = bundle.unreadCount;
      notifyListeners();
    } catch (_) {
      // Silent failure — the badge just doesn't update
    }
  }

  /// Marks one notification as read, then refreshes.
  Future<void> markAsRead(String id) async {
    try {
      await _service.markAsRead(id);

      // Update local state
      final idx = _all.indexWhere((n) => n.id == id);
      if (idx >= 0) {
        _all[idx] = _all[idx].copyWith(isRead: true);
      }

      // Recompute count
      _unreadCount = _all.where((n) => !n.isRead).length;
      notifyListeners();
    } catch (e) {
      _error = e.toString();
      notifyListeners();
    }
  }

  /// Marks all as read, then refreshes.
  Future<void> markAllAsRead() async {
    try {
      await _service.markAllAsRead();

      _all = _all.map((n) => n.copyWith(isRead: true)).toList();
      _unreadCount = 0;
      notifyListeners();
    } catch (e) {
      _error = e.toString();
      notifyListeners();
    }
  }
}