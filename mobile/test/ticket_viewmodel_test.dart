
import 'package:flutter_test/flutter_test.dart';
import 'package:clicks_computers_and_accessories_servicehub_flutter/models/ticket.dart';
import 'package:clicks_computers_and_accessories_servicehub_flutter/services/ticket_service.dart';
import 'package:clicks_computers_and_accessories_servicehub_flutter/viewmodels/ticket_viewmodel.dart';

class FakeTicketService implements TicketService {
  List<Ticket> tickets = [];
  String? receivedStatus;
  String? receivedPriority;
  bool shouldFail = false;

  @override
  Future<List<Ticket>> getTickets({
    String? status,
    String? priority,
  }) async {
    receivedStatus = status;
    receivedPriority = priority;

    if (shouldFail) {
      throw Exception('Failed to load tickets');
    }

    return tickets;
  }

  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}

Ticket makeTicket({
  String id = '1',
  String status = 'Open',
  String? assignedTechnician,
}) {
  return Ticket(
    id: id,
    title: 'Test ticket $id',
    description: 'Test description',
    requester: 'Test requester',
    category: 'Hardware',
    status: status,
    assignedTechnician: assignedTechnician,
  );
}

void main() {
  late FakeTicketService service;
  late TicketViewModel viewModel;

  setUp(() {
    service = FakeTicketService();
    viewModel = TicketViewModel(service);
  });

  tearDown(() {
    viewModel.dispose();
  });

  test('loads tickets and calculates summary counts', () async {
    service.tickets = [
      makeTicket(id: '1', status: 'Open', assignedTechnician: 'Alex'),
      makeTicket(id: '2', status: 'In Progress'),
      makeTicket(id: '3', status: 'Resolved', assignedTechnician: 'Sam'),
      makeTicket(id: '4', status: 'Closed'),
    ];

    await viewModel.load();

    expect(viewModel.tickets.length, 4);
    expect(viewModel.totalCount, 4);
    expect(viewModel.activeCount, 2);
    expect(viewModel.resolvedCount, 1);
    expect(viewModel.unassignedCount, 2);
    expect(viewModel.isLoading, false);
    expect(viewModel.error, isNull);
  });

  test('applies the status filter when loading tickets', () async {
    service.tickets = [makeTicket()];

    viewModel.setStatusFilter('Open');
    await Future<void>.delayed(Duration.zero);

    expect(viewModel.statusFilter, 'Open');
    expect(service.receivedStatus, 'Open');
  });

  test('applies the priority filter when loading tickets', () async {
    service.tickets = [makeTicket()];

    viewModel.setPriorityFilter('High');
    await Future<void>.delayed(Duration.zero);

    expect(viewModel.priorityFilter, 'High');
    expect(service.receivedPriority, 'High');
  });

  test('clears filters and reloads tickets', () async {
    viewModel.setStatusFilter('Open');
    viewModel.setPriorityFilter('High');

    await Future<void>.delayed(Duration.zero);

    viewModel.clearFilters();
    await Future<void>.delayed(Duration.zero);

    expect(viewModel.statusFilter, isNull);
    expect(viewModel.priorityFilter, isNull);
    expect(service.receivedStatus, isNull);
    expect(service.receivedPriority, isNull);
  });

  test('handles loading errors and clears loading state', () async {
    service.shouldFail = true;

    await viewModel.load();

    expect(viewModel.error, contains('Failed to load tickets'));
    expect(viewModel.tickets, isEmpty);
    expect(viewModel.isLoading, false);
  });
}
