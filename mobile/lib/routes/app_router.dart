// lib/routes/app_router.dart

import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';
// ===================================================
import '../core/design/app_colors.dart';
import '../viewmodels/auth_viewmodel.dart';
import '../views/auth/login_view.dart';
import '../views/auth/register_view.dart';
import '../views/auth/forgot_password_view.dart';
import '../views/home/shell_scaffold.dart';
import '../views/home/client_home_view.dart';
import '../views/tickets/ticket_list_view.dart';
import '../views/tickets/ticket_details_view.dart';
import '../views/tickets/create_ticket_view.dart';
import '../views/notification/notifications_view.dart';
import '../views/assets/asset_details_view.dart';
import '../views/profile/profile_view.dart';
// ===================================================

class AppRouter {
  AppRouter._();

  static GoRouter build(BuildContext context) {
    final authVm = context.read<AuthViewModel>();
    final rootNavigatorKey = GlobalKey<NavigatorState>();

    return GoRouter(
      navigatorKey: rootNavigatorKey,
      initialLocation: '/splash',
      refreshListenable: authVm,
      redirect: (context, state) {
        final isAuthenticated = authVm.isAuthenticated;
        final isBooting = authVm.state == AuthState.unknown;
        final location = state.matchedLocation;

        // --- Failsafe role guard ---
        // The mobile app is Client-only. If somehow a non-client
        // session persists, kick them to login.
        if (isAuthenticated && authVm.user?.isClient != true) {
          return '/login';
        }

        const publicRoutes = <String>{
          '/login',
          '/auth/register',
          '/auth/forgot-password',
        };

        final isOnSplash = location == '/splash';
        final isPublic = publicRoutes.contains(location);

        if (isBooting) {
          return isOnSplash ? null : '/splash';
        }

        if (!isAuthenticated && !isPublic) return '/login';
        if (isAuthenticated && (isPublic || isOnSplash)) return '/home';

        return null;
      },
      routes: [
        GoRoute(
          path: '/splash',
          builder: (context, state) => const _SplashScreen(),
        ),
        GoRoute(
          path: '/login',
          builder: (context, state) => const LoginView(),
        ),
        GoRoute(
          path: '/auth/register',
          name: 'register',
          builder: (context, state) => const RegisterView(),
        ),
        GoRoute(
          path: '/auth/forgot-password',
          name: 'forgot-password',
          builder: (context, state) => const ForgotPasswordView(),
        ),

        // --- Full-screen detail routes (push OVER the shell) ---
        GoRoute(
          parentNavigatorKey: rootNavigatorKey,
          path: '/tickets/new',
          builder: (context, state) => const CreateTicketView(),
        ),
        GoRoute(
          parentNavigatorKey: rootNavigatorKey,
          path: '/tickets/:id',
          builder: (context, state) {
            final id = state.pathParameters['id'] ?? '';
            return TicketDetailsView(ticketId: id);
          },
        ),
        GoRoute(
          parentNavigatorKey: rootNavigatorKey,
          path: '/assets/:id',
          builder: (context, state) {
            final id = state.pathParameters['id'] ?? '';
            return AssetDetailsView(assetId: id);
          },
        ),
       

        // --- Screens reachable via top bar (push OVER the shell) ---
        GoRoute(
          parentNavigatorKey: rootNavigatorKey,
          path: '/profile',
          builder: (context, state) => const ProfileView(),
        ),
        GoRoute(
          parentNavigatorKey: rootNavigatorKey,
          path: '/notifications',
          builder: (context, state) => const NotificationsView(),
        ),
        
        
       

        // --- The 4-branch shell ---
        StatefulShellRoute.indexedStack(
          builder: (context, state, navigationShell) {
            return ShellScaffold(navigationShell: navigationShell);
          },
          branches: [
            // Branch 0 – Home
            StatefulShellBranch(
              routes: [
                GoRoute(
                  path: '/home',
                  builder: (context, state) => const _RoleHome(),
                ),
              ],
            ),
            // Branch 1 – Requests (tickets list)
            StatefulShellBranch(
              routes: [
                GoRoute(
                  path: '/tickets',
                  builder: (context, state) => const TicketListView(),
                ),
              ],
            ),
            // Branch 2 – New Request
            StatefulShellBranch(
              routes: [
                GoRoute(
                  path: '/branch2',
                  builder: (context, state) => const CreateTicketView(),
                ),
              ],
            ),
            // Branch 3 – Alerts
            StatefulShellBranch(
              routes: [
                GoRoute(
                  path: '/branch3',
                  builder: (context, state) => const NotificationsView(),
                ),
              ],
            ),
          ],
        ),
      ],
    );
  }
}

// --- Role dispatcher for the home branch ---
// Client-only now; kept as a class so it stays easy to re-expand later.
class _RoleHome extends StatelessWidget {
  const _RoleHome();

  @override
  Widget build(BuildContext context) {
    return const ClientHomeView();
  }
}

// --- Splash Screen ---

class _SplashScreen extends StatelessWidget {
  const _SplashScreen();

  @override
  Widget build(BuildContext context) {
    return const Scaffold(
      backgroundColor: Colors.white,
      body: Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            SizedBox(
              width: 48,
              height: 48,
              child: CircularProgressIndicator(
                strokeWidth: 3,
                valueColor: AlwaysStoppedAnimation<Color>(
                  AppColors.primary,
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}