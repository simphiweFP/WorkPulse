# WorkPulse

**Know what matters today.**

## Overview
WorkPulse is a task and delivery planning system for client work. It separates customer records, projects, tasks, identity, API endpoints, and the frontend so the codebase is easy to review and reason about.

## Core Features
- Public Developer registration
- Authentication
- Admin/Developer roles
- Client management
- Project management
- Task management
- Assignment
- My Tasks
- Today prioritisation

## Repository Structure
- `Frontend/` - Angular UI
- `WorkPulse.Domain/` - business entities, enums, and deterministic Today rules
- `WorkPulse.Integration.Identity/` - authentication, JWT, and role integration
- `WorkPulse.Integration.Sql/` - SQL access, repositories, migrations, and development seeding
- `WorkPulse.Web.API/` - HTTP controllers, request/response contracts, and API middleware
- `WorkPulse.Web.Main/` - ASP.NET Core backend startup and composition root
- `*.Tests/` - meaningful test projects for domain, SQL, API, and host behavior

## API Mapping
- Admin Dashboard → `GET /api/dashboard/admin`
- Developer Today → `GET /api/tasks/today`
- Clients → `GET /api/clients`
- Project details → `GET /api/projects/{id}`
- Tasks → `GET /api/tasks`
- My Tasks → `GET /api/tasks/my`
- Team / Developers → `GET /api/users/developers`
- Login → `POST /api/auth/login`
- Register → `POST /api/auth/register`
- Session restore → `GET /api/auth/me`

Primary Today endpoint for the developer home screen is `GET /api/tasks/today`.

## Architecture
- `WorkPulse.Web.Main` is the backend startup and composition root.
- `WorkPulse.Web.API` contains HTTP behavior, routing, validation boundaries, and authorization.
- `Frontend/` runs independently and consumes the backend API.
- `WorkPulse.Domain` stays independent of EF Core, SQL Server, controllers, and Angular.
- `WorkPulse.Integration.Sql` owns SQL persistence, migrations, repository implementations, and query optimization.
- `WorkPulse.Integration.Identity` owns password hashing, JWT creation, and role-based authentication integration.
- `WorkPulse.Application` contains use cases and orchestration for client, project, task, and dashboard workflows while staying independent of HTTP, SQL, and infrastructure implementations.

## Domain Model
- `Client`
  - `Project`
	- `TaskItem`
	  - `AssignedTo Developer`
- `Developer` is an authenticated application user.
- `Client` is a business/customer record and does not log in.

## Authentication
- Public registration creates `Developer` only.
- `Admin` users are seeded or managed by administrators.
- JWT carries the authenticated user identity and role claims.

## Today Prioritisation
WorkPulse uses a deterministic rules-based prioritisation engine.

### Chosen assumptions
- Only tasks that are not completed can appear in Today.
- The dashboard is scoped to the authenticated Developer for the personal view.
- Overdue work should outrank everything else.
- Due today should rank above upcoming work.
- High and Critical upcoming tasks can still appear.
- Low priority tasks far in the future should be excluded.
- Recommendation reasons must explain the ranking without claiming AI behavior.

### Rules
- Completed tasks are excluded
- Tasks belong to the authenticated Developer
- Overdue tasks are prioritized first
- Due-today tasks are prioritized next
- High/Critical upcoming tasks can appear
- Low distant tasks are excluded
- Ordering is deterministic
- Recommendation reasons are meaningful and testable

## Running Locally
Backend:
```sh
dotnet restore
dotnet user-secrets init --project WorkPulse.Web.Main
dotnet user-secrets set --project WorkPulse.Web.Main "Jwt:SecretKey" "replace-with-a-local-development-secret"
dotnet user-secrets set --project WorkPulse.Web.Main "DevelopmentSeed:AdminPassword" "WorkPulseAdmin123!"
dotnet run --project WorkPulse.Web.Main
```

Set `Jwt:SecretKey` locally through user secrets before starting the host.
Set `DevelopmentSeed:AdminPassword` locally so the seeded admin can authenticate.

The backend applies database migrations and development seeding on startup.

Frontend:
```sh
cd Frontend
npm install
npm start
```

## Demo Accounts
These are local development/demo credentials only.

- Admin: `admin@workpulse.local` / `WorkPulseAdmin123!`
- Developer: `developer@workpulse.local` / `WorkPulseDev123!`
- Developer 2: `developer2@workpulse.local` / `WorkPulseDev234!`


## Tests
Backend and solution tests:
```sh
dotnet clean WorkPulse.slnx
dotnet restore WorkPulse.slnx
dotnet build WorkPulse.slnx
dotnet test WorkPulse.slnx
```

Frontend build:
```sh
cd Frontend
npm run build
```

## Assumptions
- Clients are business records.
- Public registration creates Developer only.
- Today eligibility is rules-based and deterministic.
- Completed tasks do not return to active states.
- Delete operations are server-side and authorization-protected.

## Trade-offs
- No Client portal.
- No microservices.
- No complex permissions engine.
- No realtime updates.
- No notifications.
- No heavyweight Angular state store.
- No separate EF migration command because startup applies FluentMigrator migrations automatically.

## Production Improvements
- Microsoft Entra ID / enterprise SSO
- Refresh/session strategy
- HttpOnly authentication approach where appropriate
- Email verification
- Password reset
- Audit trail
- User administration
- Client portal if required
- Notifications
- Search
- Pagination
- Concurrency handling
- Rate limiting
- Health checks
- OpenTelemetry/Application Insights
- CI/CD
- Docker
- Secret management
- E2E tests
- Security testing
- Accessibility audit

## Known Limitations
- The repository is still a prototype and should not be treated as production-ready.
- The current architecture keeps a thin coordination layer in `WorkPulse.Application`.
- The demo data is intentionally small and development-oriented.
