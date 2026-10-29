// lib/services/ticket_service.dart

import '../api/ticket_api.dart';
import '../models/ticket.dart';
import 'dart:io';

class TicketService {
  final TicketApi _api;
  TicketService([TicketApi? api]) : _api = api ?? TicketApi();

  Future<List<Ticket>> getTickets({
    String? status,
    String? priority,
  }) {
    return _api.getTickets(status: status, priority: priority);
  }
  Future<Ticket> getTicket(String id) => _api.getById(id);
  Future<Ticket> createTicket({
  required String title,
  required String description,
  required String deviceType,
  required String problemCategory,
  required String problemType,
  required int serviceCatalogId,
  String priority = 'Medium',
  String category = '',
  List<File> images = const [],
}) {
  return _api.createTicket(
    title: title,
    description: description,
    deviceType: deviceType,
    problemCategory: problemCategory,
    problemType: problemType,
    serviceCatalogId: serviceCatalogId,
    priority: priority,
    category: category,
    images: images,
  );
}

Future<Ticket> updateStatus({
  required String ticketId,
  required String status,
  String? assignedTechnician,
  String? comment,
  DateTime? dueDate,
}) {
  return _api.updateStatus(
    ticketId: ticketId,
    status: status,
    assignedTechnician: assignedTechnician,
    comment: comment,
    dueDate: dueDate,
  );
}

Future<Ticket> setPaymentMethod({
  required String ticketId,
  required String paymentMethod,
}) {
  return _api.setPaymentMethod(
    ticketId: ticketId,
    paymentMethod: paymentMethod,
  );
}

Future<Ticket> confirmPayment(String ticketId) {
  return _api.confirmPayment(ticketId);
}
}