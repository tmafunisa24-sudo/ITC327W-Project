import 'dart:io';

import 'package:flutter_test/flutter_test.dart';

import '../lib/models/service_catalog_item.dart';
import '../lib/models/ticket.dart';
import '../lib/services/service_catalog_service.dart';
import '../lib/services/ticket_service.dart';
import '../lib/viewmodels/create_ticket_viewmodel.dart';

class FakeCatalogService implements ServiceCatalogService {
  @override
  Future<List<ServiceCatalogItem>> getActive() async {
    return const [
      ServiceCatalogItem(
        id: 1,
        deviceType: 'Laptop',
        problemCategory: 'Hardware',
        problemType: 'Screen damage',
      ),
    ];
  }

  @override
  dynamic noSuchMethod(Invocation invocation) =>
      super.noSuchMethod(invocation);
}

class FakeTicketService implements TicketService {
  bool shouldFail;
  bool createCalled = false;

  FakeTicketService({this.shouldFail = false});

  @override
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
    createCalled = true;

    if (shouldFail) {
      throw Exception('Network unavailable');
    }

    return Ticket(
      id: 'test-ticket-1',
      title: title,
      description: description,
      requester: 'Test User',
      category: category,
      priority: priority,
      deviceType: deviceType,
      problemCategory: problemCategory,
      problemType: problemType,
      serviceCatalogId: serviceCatalogId,
    );
  }
@override
dynamic noSuchMethod(Invocation invocation) =>
    super.noSuchMethod(invocation);
}

void main() {
  late FakeTicketService ticketService;
  late CreateTicketViewModel viewModel;

  setUp(() {
    ticketService = FakeTicketService();
    viewModel = CreateTicketViewModel(
      catalogService: FakeCatalogService(),
      ticketService: ticketService,
    );
  });

  tearDown(() {
    viewModel.dispose();
  });

  Future<void> selectValidService() async {
    await viewModel.loadCatalog();
    viewModel.setDeviceType('Laptop');
    viewModel.setProblemCategory('Hardware');
    viewModel.setProblemType('Screen damage');
  }

  test('rejects submission when required selections are missing', () async {
    viewModel.setDescription('The laptop screen is damaged.');

    expect(viewModel.canSubmit, isFalse);

    final result = await viewModel.submit();

    expect(result, isFalse);
    expect(ticketService.createCalled, isFalse);
  });

  test('rejects submission when the description is blank', () async {
    await selectValidService();
    viewModel.setDescription('   ');

    expect(viewModel.canSubmit, isFalse);

    final result = await viewModel.submit();

    expect(result, isFalse);
    expect(ticketService.createCalled, isFalse);
  });

  test('creates a ticket with valid information', () async {
    await selectValidService();
    viewModel.setDescription('The laptop screen is damaged.');

    expect(viewModel.canSubmit, isTrue);

    final result = await viewModel.submit();

    expect(result, isTrue);
    expect(viewModel.submitted, isTrue);
    expect(viewModel.isSubmitting, isFalse);
    expect(viewModel.submitError, isNull);
    expect(ticketService.createCalled, isTrue);
  });

  test('handles service failure and clears submitting state', () async {
    ticketService.shouldFail = true;

    await selectValidService();
    viewModel.setDescription('The laptop screen is damaged.');

    final result = await viewModel.submit();

    expect(result, isFalse);
    expect(viewModel.submitted, isFalse);
    expect(viewModel.isSubmitting, isFalse);
    expect(viewModel.submitError, contains('Network unavailable'));
    expect(ticketService.createCalled, isTrue);
  });
}
