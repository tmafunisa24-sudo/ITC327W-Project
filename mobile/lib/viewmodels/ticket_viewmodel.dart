// lib/viewmodels/ticket_viewmodel.dart

import 'package:flutter/foundation.dart';
import '../models/ticket.dart';
import '../services/ticket_service.dart';

class TicketViewModel extends ChangeNotifier {
  final TicketService _service;
  TicketViewModel([TicketService? service])
      : _service = service ?? TicketService();

  List<Ticket> _tickets = [];
  bool _isLoading = false;
  String? _error;
  String? _statusFilter;
  String? _priorityFilter;

  // ── Getters ──
  List<Ticket> get tickets => _tickets;
  bool get isLoading => _isLoading;
  String? get error => _error;
  String? get statusFilter => _statusFilter;
  String? get priorityFilter => _priorityFilter;

  // ── Derived stats ──
  int get totalCount => _tickets.length;

  int get activeCount => _tickets
      .where((t) =>
          !_equalsIgnoreCase(t.status, 'Resolved') &&
          !_equalsIgnoreCase(t.status, 'Closed'))
      .length;

  int get resolvedCount =>
      _tickets.where((t) => _equalsIgnoreCase(t.status, 'Resolved')).length;

  int get unassignedCount => _tickets
      .where((t) =>
          t.assignedTechnician == null ||
          t.assignedTechnician!.trim().isEmpty)
      .length;

  // Detail state
  Ticket? _currentTicket;
  bool _isLoadingDetail = false;
  String? _detailError;

  Ticket? get currentTicket => _currentTicket;
  bool get isLoadingDetail => _isLoadingDetail;
  String? get detailError => _detailError;

  // Submit state for the update sheet
  bool _isUpdating = false;
  String? _updateError;

  bool get isUpdating => _isUpdating;
  String? get updateError => _updateError;

  // ── Actions ──

  Future<void> load() async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      _tickets = await _service.getTickets(
        status: _statusFilter,
        priority: _priorityFilter,
      );
      _error = null;
    } catch (e) {
      _error = e.toString();
      _tickets = [];
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<void> refresh() => load();

  void setStatusFilter(String? status) {
    _statusFilter = (status == null || status.isEmpty) ? null : status;
    load();
  }

  void setPriorityFilter(String? priority) {
    _priorityFilter = (priority == null || priority.isEmpty) ? null : priority;
    load();
  }

  void clearFilters() {
    _statusFilter = null;
    _priorityFilter = null;
    load();
  }

  /// Load a single ticket for the details screen.
  Future<void> loadTicket(String id) async {
    _isLoadingDetail = true;
    _detailError = null;
    _currentTicket = null;
    notifyListeners();

    try {
      _currentTicket = await _service.getTicket(id);
      _detailError = null;
    } catch (e) {
      _detailError = e.toString();
      _currentTicket = null;
    } finally {
      _isLoadingDetail = false;
      notifyListeners();
    }
  }

  void clearCurrentTicket() {
    _currentTicket = null;
    _detailError = null;
    _isLoadingDetail = false;
    notifyListeners();
  }

  /// Update the currently-loaded ticket's status.
/// On success, refreshes the current ticket + the list.
Future<bool> updateStatus({
  required String ticketId,
  required String status,
  String? assignedTechnician,
  String? comment,
  DateTime? dueDate,
}) async {
  _isUpdating = true;
  _updateError = null;
  notifyListeners();

  try {
    final updated = await _service.updateStatus(
      ticketId: ticketId,
      status: status,
      assignedTechnician: assignedTechnician,
      comment: comment,
      dueDate: dueDate,
    );

    // Update in-memory detail view
    _currentTicket = updated;

    // Also update the ticket in the list (so the list reflects the change)
    final idx = _tickets.indexWhere((t) => t.id == updated.id);
    if (idx >= 0) {
      final copy = List<Ticket>.from(_tickets);
      copy[idx] = updated;
      _tickets = copy;
    }

    _isUpdating = false;
    notifyListeners();
    return true;
  } catch (e) {
    _updateError = e.toString();
    _isUpdating = false;
    notifyListeners();
    return false;
  }
}

/// Client selects payment method for a ticket.
Future<bool> setPaymentMethod({
  required String ticketId,
  required String paymentMethod,
}) async {
  _isUpdating = true;
  _updateError = null;
  notifyListeners();

  try {
    final updated = await _service.setPaymentMethod(
      ticketId: ticketId,
      paymentMethod: paymentMethod,
    );

    _currentTicket = updated;
    final idx = _tickets.indexWhere((t) => t.id == updated.id);
    if (idx >= 0) {
      final copy = List<Ticket>.from(_tickets);
      copy[idx] = updated;
      _tickets = copy;
    }

    _isUpdating = false;
    notifyListeners();
    return true;
  } catch (e) {
    _updateError = e.toString();
    _isUpdating = false;
    notifyListeners();
    return false;
  }
}

/// Admin confirms payment for a ticket.
Future<bool> confirmPayment(String ticketId) async {
  _isUpdating = true;
  _updateError = null;
  notifyListeners();

  try {
    final updated = await _service.confirmPayment(ticketId);

    _currentTicket = updated;
    final idx = _tickets.indexWhere((t) => t.id == updated.id);
    if (idx >= 0) {
      final copy = List<Ticket>.from(_tickets);
      copy[idx] = updated;
      _tickets = copy;
    }

    _isUpdating = false;
    notifyListeners();
    return true;
  } catch (e) {
    _updateError = e.toString();
    _isUpdating = false;
    notifyListeners();
    return false;
  }
}

void clearUpdateError() {
  _updateError = null;
  notifyListeners();
}

  // ── Helpers ──

  static bool _equalsIgnoreCase(String? a, String b) =>
      a != null && a.toLowerCase() == b.toLowerCase();
}