// lib/app.dart

import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import 'core/design/app_theme.dart';
import 'routes/app_router.dart';

class ServiceHubApp extends StatefulWidget {
  const ServiceHubApp({super.key});

  @override
  State<ServiceHubApp> createState() => _ServiceHubAppState();
}

class _ServiceHubAppState extends State<ServiceHubApp> {
  late final GoRouter _router;

  @override
  void initState() {
    super.initState();
    _router = AppRouter.build(context);
  }

  @override
  Widget build(BuildContext context) {
    return MaterialApp.router(
      title: 'ServiceHub IT',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.light,
      routerConfig: _router,
    );
  }
}