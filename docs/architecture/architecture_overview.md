# System Architecture

## Scope
### In Scope
- User registration and authentication
- Request submission and assignment
- Admin dashboard
- Flutter mobile app
- ASP.NET API

### Out of Scope
- Advanced analytics
- Mobile notifications (Phase 2)
- Third-party integrations (Phase 2)

## Technology Stack
| Component | Technology |
|-----------|------------|
| Frontend | Flutter |
| Backend | ASP.NET API |
| Database | Supabase (PostgreSQL) |
| Auth | Supabase Auth |
| Storage | Supabase Storage |

## Architecture Diagram

[Diagram placeholder – to be added]

## Data Flow
1. User submits request via Flutter app
2. API validates request → stores in Supabase
3. Admin views/assigns requests via API

## Integration Points
- Flutter ↔ ASP.NET (REST API)
- ASP.NET ↔ Supabase (PostgreSQL connection)