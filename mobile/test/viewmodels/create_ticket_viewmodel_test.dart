
import 'dart:io';

import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:clicks_computers_and_accessories_servicehub_flutter/api/service_catalog_api.dart';
import 'package:clicks_computers_and_accessories_servicehub_flutter/api/ticket_api.dart';
import 'package:clicks_computers_and_accessories_servicehub_flutter/models/service_catalog_item.dart';
import 'package:clicks_computers_and_accessories_servicehub_flutter/models/ticket.dart';
import 'package:clicks_computers_and_accessories_servicehub_flutter/services/service_catalog_service.dart';
import 'package:clicks_computers_and_accessories_servicehub_flutter/services/ticket_service.dart';
import 'package:clicks_computers_and_accessories_servicehub_flutter/viewmodels/create_ticket_viewmodel.dart';

class FakeServiceCatalogService extends ServiceCatalogService {
  FakeServiceCatalogService()
      : super(ServiceCatalogApi(Dio()));

  @override
  Future<List<ServiceCatalogItem>> getActive() async {
    return const [
      ServiceCatalogItem(
        id: 1,
        deviceType: 'Laptop',
        problemCategory: 'Hardware',
        problemType: 'Broken screen',
      ),
      ServiceCatalogItem(
        id: 2,
        deviceType: 'Laptop',
        problemCategory: 'Hardware',
        problemType: 'Keyboard problem',
      ),
      ServiceCatalogItem(
        id: 3,
        deviceType: 'Desktop',
        problemCategory: 'Hardware',
        problemType: 'Power problem',
      ),
      ServiceCatalogItem(
        id: 4,
        deviceType: 'Laptop',
        problemCategory: 'Software',
        problemType: 'Operating system issue',
      ),
    ];
  }
}

class FakeTicketService extends TicketService {
  FakeTicketService()
      : super(TicketApi(Dio()));

  Map<String, dynamic>? submittedValues;
  bool shouldFail = false;

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
    if (shouldFail) {
      throw Exception('Unable to create ticket');
    }

    submittedValues = {
      'title': title,
      'description': description,
      'deviceType': deviceType,
      'problemCategory': problemCategory,
      'problemType': problemType,
      'serviceCatalogId': serviceCatalogId,
      'priority': priority,
      'category': category,
      'images': List<File>.from(images),
    };

    return Ticket(
      id: 'ticket-1',
      title: title,
      description: description,
      requester: 'Test Customer',
      priority: priority,
      status: 'Open',
      category: category,
      deviceType: deviceType,
      problemCategory: problemCategory,
      problemType: problemType,
      serviceCatalogId: serviceCatalogId,
    );
  }
}

void main() {
  late FakeServiceCatalogService catalogService;
  late FakeTicketService ticketService;
  late CreateTicketViewModel viewModel;

  setUp(() {
    catalogService = FakeServiceCatalogService();
    ticketService = FakeTicketService();

    viewModel = CreateTicketViewModel(
      catalogService: catalogService,
      ticketService: ticketService,
    );
  });

  tearDown(() {
    viewModel.dispose();
  });

  group('CreateTicketViewModel', () {
    test('loads the service catalog', () async {
      await viewModel.loadCatalog();

      expect(viewModel.isLoadingCatalog, isFalse);
      expect(viewModel.catalogError, isNull);
      expect(viewModel.allItems, isNotEmpty);
      expect(viewModel.deviceTypes, contains('Laptop'));
      expect(viewModel.deviceTypes, contains('Desktop'));
    });

    test('filters categories and problem types by device type', () async {
      await viewModel.loadCatalog();

      viewModel.setDeviceType('Laptop');

      expect(viewModel.problemCategories, contains('Hardware'));
      expect(viewModel.problemCategories, contains('Software'));

      viewModel.setProblemCategory('Hardware');

      expect(viewModel.problemTypes, contains('Broken screen'));
      expect(viewModel.problemTypes, contains('Keyboard problem'));
      expect(viewModel.problemTypes, isNot(contains('Power problem')));
    });

    test('changing device type clears dependent selections', () async {
      await viewModel.loadCatalog();

      viewModel.setDeviceType('Laptop');
      viewModel.setProblemCategory('Hardware');
      viewModel.setProblemType('Broken screen');

      expect(viewModel.selectedDeviceType, 'Laptop');
      expect(viewModel.selectedProblemCategory, 'Hardware');
      expect(viewModel.selectedProblemType, 'Broken screen');
      expect(viewModel.selectedService?.id, 1);

      viewModel.setDeviceType('Desktop');

      expect(viewModel.selectedDeviceType, 'Desktop');
      expect(viewModel.selectedProblemCategory, isNull);
      expect(viewModel.selectedProblemType, isNull);
      expect(viewModel.selectedService, isNull);
      expect(viewModel.title, isEmpty);
    });

    test('does not allow submission without required details', () async {
      await viewModel.loadCatalog();

      expect(viewModel.canSubmit, isFalse);

      viewModel.setDeviceType('Laptop');
      viewModel.setProblemCategory('Hardware');
      viewModel.setProblemType('Broken screen');

      viewModel.setTitle('');
      viewModel.setDescription('The screen is damaged.');

      expect(viewModel.canSubmit, isFalse);

      viewModel.setTitle('Laptop screen repair');

      expect(viewModel.canSubmit, isTrue);
    });

    test('successfully submits a ticket with the supplied values',
        () async {
      await viewModel.loadCatalog();

      viewModel.setDeviceType('Laptop');
      viewModel.setProblemCategory('Hardware');
      viewModel.setProblemType('Broken screen');
      viewModel.setTitle('Laptop screen repair');
      viewModel.setDescription('The laptop screen is damaged.');
      viewModel.setPriority('High');

      expect(viewModel.canSubmit, isTrue);

      final result = await viewModel.submit();

      expect(result, isTrue);
      expect(viewModel.isSubmitting, isFalse);
      expect(viewModel.submitted, isTrue);
      expect(viewModel.submitError, isNull);

      expect(ticketService.submittedValues, isNotNull);
      expect(ticketService.submittedValues!['title'],
          'Laptop screen repair');
      expect(ticketService.submittedValues!['description'],
          'The laptop screen is damaged.');
      expect(ticketService.submittedValues!['deviceType'], 'Laptop');
      expect(ticketService.submittedValues!['problemCategory'], 'Hardware');
      expect(ticketService.submittedValues!['problemType'], 'Broken screen');
      expect(ticketService.submittedValues!['serviceCatalogId'], 1);
      expect(ticketService.submittedValues!['priority'], 'High');
    });

    test('handles ticket submission errors', () async {
      await viewModel.loadCatalog();

      viewModel.setDeviceType('Laptop');
      viewModel.setProblemCategory('Hardware');
      viewModel.setProblemType('Broken screen');
      viewModel.setTitle('Laptop screen repair');
      viewModel.setDescription('The laptop screen is damaged.');

      ticketService.shouldFail = true;

      final result = await viewModel.submit();

      expect(result, isFalse);
      expect(viewModel.isSubmitting, isFalse);
      expect(viewModel.submitted, isFalse);
      expect(viewModel.submitError, isNotNull);
    });

    test('limits attachments to five images', () {
      viewModel.addImage(File('image1.jpg'));
      viewModel.addImage(File('image2.jpg'));
      viewModel.addImage(File('image3.jpg'));
      viewModel.addImage(File('image4.jpg'));
      viewModel.addImage(File('image5.jpg'));
      viewModel.addImage(File('image6.jpg'));

      expect(viewModel.images.length, 5);
    });

    test('removes an attachment at the specified index', () {
      viewModel.addImage(File('image1.jpg'));
      viewModel.addImage(File('image2.jpg'));

      viewModel.removeImageAt(0);

      expect(viewModel.images.length, 1);
      expect(viewModel.images.first.path, 'image2.jpg');
    });

    test('ignores invalid attachment indexes', () {
      viewModel.addImage(File('image1.jpg'));

      viewModel.removeImageAt(-1);
      viewModel.removeImageAt(5);

      expect(viewModel.images.length, 1);
      expect(viewModel.images.first.path, 'image1.jpg');
    });

    test('reset clears ticket details and attachments', () async {
      await viewModel.loadCatalog();

      viewModel.setDeviceType('Laptop');
      viewModel.setProblemCategory('Hardware');
      viewModel.setProblemType('Broken screen');
      viewModel.setTitle('Laptop screen repair');
      viewModel.setDescription('The screen is damaged.');
      viewModel.setPriority('High');
      viewModel.addImage(File('image1.jpg'));

      viewModel.reset();

      expect(viewModel.selectedDeviceType, isNull);
      expect(viewModel.selectedProblemCategory, isNull);
      expect(viewModel.selectedProblemType, isNull);
      expect(viewModel.selectedService, isNull);
      expect(viewModel.title, isEmpty);
      expect(viewModel.description, isEmpty);
      expect(viewModel.priority, 'Medium');
      expect(viewModel.images, isEmpty);
      expect(viewModel.canSubmit, isFalse);
    });
  });
}
