// lib/main.dart

import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
//==============================================
import 'app.dart';
import 'core/network/api_client.dart';
import 'viewmodels/auth_viewmodel.dart';
import 'viewmodels/ticket_viewmodel.dart';
import 'viewmodels/create_ticket_viewmodel.dart';
import 'viewmodels/notification_viewmodel.dart';
import 'viewmodels/asset_viewmodel.dart';
import 'viewmodels/dashboard_viewmodel.dart';
//================================================

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();

  // Initialize Dio + cookie jar before anything else
  await ApiClient.initialize();

  runApp(
    MultiProvider(
      providers: [
        ChangeNotifierProvider(
          create: (_) => AuthViewModel()..bootstrap(),
        ),
        ChangeNotifierProvider(
          create: (_) => TicketViewModel(),
          ),
        ChangeNotifierProvider(
          create: (_) => CreateTicketViewModel(),
          ),
        ChangeNotifierProvider(
          create: (_) => NotificationViewModel(),
        ),
        ChangeNotifierProvider(
          create: (_) => AssetViewModel(),
        ),
        ChangeNotifierProvider(
          create: (_) => DashboardViewModel(),
        ),
      ],
      child: const _AppLifecycleHandler(
        child: ServiceHubApp(),
      )
    ),
  );
}

class _AppLifecycleHandler extends StatefulWidget {
  final Widget child;
  const _AppLifecycleHandler({required this.child});

  @override
  State<_AppLifecycleHandler> createState() => _AppLifecycleHandlerState();
}

class _AppLifecycleHandlerState extends State<_AppLifecycleHandler> {
  AuthViewModel? _auth;
  NotificationViewModel? _notifications;

  @override
  void initState() {
    super.initState();
    // Defer until after the first frame so providers are fully mounted
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (!mounted) return;

      _auth = context.read<AuthViewModel>();
      _notifications = context.read<NotificationViewModel>();

      _auth!.addListener(_onAuthChanged);

      // Handle the case where the user is already logged in by the time we mount
      _onAuthChanged();
    });
  }

  void _onAuthChanged() {
    final auth = _auth;
    final notifications = _notifications;
    if (auth == null || notifications == null) return;

    if (auth.state == AuthState.authenticated) {
      notifications.startPolling();
    } else if (auth.state == AuthState.unauthenticated) {
      notifications.stopPolling();
    }
  }

  @override
  void dispose() {
    _auth?.removeListener(_onAuthChanged);
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => widget.child;
}