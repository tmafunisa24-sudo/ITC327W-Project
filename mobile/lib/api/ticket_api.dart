// lib/api/ticket_api.dart

import 'package:dio/dio.dart';
import '../core/network/api_client.dart';
import '../core/network/api_exception.dart';
import '../models/ticket.dart';
import 'dart:io';

class TicketApi {
  final Dio _dio;
  TicketApi([Dio? dio]) : _dio = dio ?? ApiClient.dio;

  /// GET /api/tickets?status=&priority=
  ///
  /// Role-aware:
  ///   - Admin      → all tickets
  ///   - Technician → tickets assigned to them
  ///   - Client     → tickets they requested
  ///
  /// Returns a list of [Ticket]. Throws [ApiException] on failure.
  Future<List<Ticket>> getTickets({
    String? status,
    String? priority,
  }) async {
    final queryParams = <String, dynamic>{};
    if (status != null && status.trim().isNotEmpty) {
      queryParams['status'] = status.trim();
    }
    if (priority != null && priority.trim().isNotEmpty) {
      queryParams['priority'] = priority.trim();
    }

    final response = await _dio.get(
      '/api/tickets',
      queryParameters: queryParams.isEmpty ? null : queryParams,
    );

    final code = response.statusCode ?? 0;

    if (code == 200) {
      final data = response.data;
      if (data is List) {
        return data
            .whereType<Map<String, dynamic>>()
            .map(Ticket.fromJson)
            .toList();
      }
      return const [];
    }

    throw ApiException(
      _extractError(response) ?? 'Unable to load tickets.',
      statusCode: code,
    );
  }
  /// GET /api/tickets/{id}
  ///
  /// Returns one [Ticket]. Throws [ApiException]:
  ///   - 401 if not authenticated
  ///   - 403 if the caller can't view this ticket
  ///   - 404 if the ticket doesn't exist
  Future<Ticket> getById(String id) async {
    final response = await _dio.get('/api/tickets/$id');
    final code = response.statusCode ?? 0;

    if (code == 200) {
      final data = response.data;
      if (data is Map<String, dynamic>) {
        return Ticket.fromJson(data);
      }
      throw ApiException('Invalid ticket response.', statusCode: code);
    }

    throw ApiException(
      _extractError(response) ?? 'Unable to load the ticket.',
      statusCode: code,
    );
  }
  /// POST /api/tickets
  ///
  /// Multipart form upload with optional images.
  /// Client-only endpoint (roles enforced server-side).
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
  }) async {
    final formData = FormData.fromMap({
      'title': title,
      'description': description,
      'deviceType': deviceType,
      'problemCategory': problemCategory,
      'problemType': problemType,
      'serviceCatalogId': serviceCatalogId.toString(),
      'priority': priority,
      'category': category,
    });

    // Attach images
    for (final image in images) {
      final filename = image.path.split(Platform.pathSeparator).last;
      formData.files.add(MapEntry(
        'images',
        await MultipartFile.fromFile(image.path, filename: filename),
      ));
    }

    final response = await _dio.post(
      '/api/tickets',
      data: formData,
      options: Options(
        contentType: 'multipart/form-data',
      ),
    );

    final code = response.statusCode ?? 0;

    if (code == 201 || code == 200) {
      final data = response.data;

      // Backend returns the ticket directly at 201,
      // or an object with `ticket` + `warning` if images failed.
      final ticketJson = (data is Map<String, dynamic> && data['ticket'] is Map)
          ? data['ticket'] as Map<String, dynamic>
          : data as Map<String, dynamic>;

      return Ticket.fromJson(ticketJson);
    }

    throw ApiException(
      _extractError(response) ?? 'Unable to create the ticket.',
      statusCode: code,
    );
  }

  /// PATCH /api/tickets/{id}/status
///
/// Updates ticket status. Role-aware (Admin/Technician only).
/// Throws [ApiException] on failure:
///   400 — invalid transition
///   401 — not authenticated
///   403 — wrong role or not assigned
///   404 — ticket not found
Future<Ticket> updateStatus({
  required String ticketId,
  required String status,
  String? assignedTechnician,
  String? comment,
  DateTime? dueDate,
}) async {
  final payload = <String, dynamic>{
    'status': status,
  };

  if (assignedTechnician != null && assignedTechnician.trim().isNotEmpty) {
    payload['assignedTechnician'] = assignedTechnician.trim();
  }

  if (comment != null && comment.trim().isNotEmpty) {
    payload['comment'] = comment.trim();
  }

  if (dueDate != null) {
    payload['dueDate'] = dueDate.toIso8601String();
  }

  final response = await _dio.patch(
    '/api/tickets/$ticketId/status',
    data: payload,
  );

  final code = response.statusCode ?? 0;

  if (code == 200) {
    final data = response.data;
    if (data is Map<String, dynamic>) {
      return Ticket.fromJson(data);
    }
    throw ApiException('Invalid response.', statusCode: code);
  }

  throw ApiException(
    _extractError(response) ?? 'Unable to update the ticket.',
    statusCode: code,
  );
}

/// PATCH /api/tickets/{id}/payment-method — client only
Future<Ticket> setPaymentMethod({
  required String ticketId,
  required String paymentMethod,
}) async {
  final response = await _dio.patch(
    '/api/tickets/$ticketId/payment-method',
    data: {'paymentMethod': paymentMethod},
  );

  final code = response.statusCode ?? 0;

  if (code == 200) {
    final data = response.data;
    if (data is Map<String, dynamic>) {
      return Ticket.fromJson(data);
    }
    throw ApiException('Invalid response.', statusCode: code);
  }

  throw ApiException(
    _extractError(response) ?? 'Unable to save the payment method.',
    statusCode: code,
  );
}

/// POST /api/tickets/{id}/confirm-payment — admin only
Future<Ticket> confirmPayment(String ticketId) async {
  final response = await _dio.post('/api/tickets/$ticketId/confirm-payment');
  final code = response.statusCode ?? 0;

  if (code == 200) {
    final data = response.data;
    if (data is Map<String, dynamic>) {
      return Ticket.fromJson(data);
    }
    throw ApiException('Invalid response.', statusCode: code);
  }

  throw ApiException(
    _extractError(response) ?? 'Unable to confirm the payment.',
    statusCode: code,
  );
}

  //===============================================================================
  String? _extractError(Response response) {
    final data = response.data;
    if (data is Map<String, dynamic>) {
      if (data['error'] is String) return data['error'] as String;
      if (data['detail'] is String) return data['detail'] as String;
      if (data['title'] is String) return data['title'] as String;
    }
    return null;
  }
}