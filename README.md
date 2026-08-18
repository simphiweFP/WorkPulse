<div align="center">

# ⚡ WorkPulse

### Know what matters today.

A task and delivery-planning system for teams managing client work, projects, sprints and daily priorities.

[![.NET](https://img.shields.io/badge/.NET-10%2B-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/)
[![Angular](https://img.shields.io/badge/Angular-20%2B-DD0031?style=for-the-badge&logo=angular)](https://angular.dev/)
[![SQL Server](https://img.shields.io/badge/SQL_Server-Data-CC2927?style=for-the-badge&logo=microsoftsqlserver)](https://www.microsoft.com/sql-server)
[![Architecture](https://img.shields.io/badge/Architecture-Clean-2563EB?style=for-the-badge)](#architecture)

</div>

---

## ✨ Overview

WorkPulse helps teams plan, track and deliver client work. It gives Administrators visibility of delivery pressure and gives Developers a focused **Today** view that highlights the next task requiring attention.

The system connects client work through a simple delivery path:

```text
Client → Project → Task → Backlog → Sprint → Assignment → Today → In Progress → Completed
```

> **Goal:** make the right next task clear without needing to search through a full backlog or sprint manually.

---

## 🚀 Core Features

| Area | Capability |
|---|---|
| 🔐 Access | Public registration, JWT authentication and role-based access |
| 👥 User management | Pending, Developer and Admin roles |
| 🏢 Delivery setup | Client, Project and Task management |
| 📋 Planning | Backlog readiness, deadline ordering and Sprint planning |
| 🎯 Focus | Deterministic Today prioritisation |
| ✅ Execution | Start and complete assigned tasks |
| 📊 Visibility | Admin Dashboard, Today and My Tasks views |

---

## 👥 Roles and Access

| Role | Access |
|---|---|
| **Pending** | Can register and sign in, but waits for an Admin to assign an operational role. |
| **Developer** | Views assigned tasks, Today, My Tasks and available delivery context. |
| **Admin** | Manages users, clients, projects, tasks, backlog and sprints. |

### Registration flow

1. A user registers a WorkPulse account.
2. The new account is created as **Pending**.
3. An Admin opens **User Management**.
4. The Admin assigns the user a **Developer** or **Admin** role.

> A task assignment is not the same as a role assignment. A role controls system access; a task assignment controls who owns the work.

---

## 🧭 Main Workflows

### Administrator workflow

1. Create a **Client**.
2. Create a **Project** under that Client.
3. Create Tasks for the Project.
4. Add priority, deadline, task type, story points and an assignee.
5. Prepare incomplete tasks in the **Backlog**.
6. Create a **Sprint** and add tasks from the matching Project.
7. Monitor delivery pressure from Dashboard and Today.

### Developer workflow

1. Open **Today**.
2. Start the Top Priority task.
3. Keep the task status accurate while working.
4. Complete the task when finished.
5. Review upcoming and completed work in **My Tasks**.

---

## 📋 Task Model

Each task belongs to a Project and can be assigned to a Developer and optionally planned into a Sprint.

| Field | Purpose |
|---|---|
| Client | The customer receiving the work |
| Project | The delivery initiative for that Client |
| Task Type | Story, Bug or Support |
| Priority | Critical, High, Medium or Low |
| Status | Todo, In Progress or Completed |
| Story Points | Relative effort estimate used for planning |
| Assignee | Developer responsible for the task |
| Deadline | Expected delivery date |


```

---

## 📦 Backlog and Sprint Planning

### Backlog

The Backlog contains incomplete tasks that are not assigned to a Sprint.

A task is ready for planning when it has:

- A Project
- An Assignee
- Story Points
- A suitable deadline, where required

Backlog work should be reviewed by the nearest approaching deadline so delivery risks are considered first.

### Sprints

A Sprint is a time-boxed delivery window for a Project.

1. An Admin creates a Sprint with a Project, name, start date and end date.
2. The Admin adds relevant Backlog tasks from that Project.
3. When the Sprint start date arrives and it has tasks, it is shown as **Active**.
4. Sprint progress is based on the story points of completed Sprint tasks.

---

## 🎯 Today Prioritisation

WorkPulse uses deterministic, rules-based prioritisation. It does not claim AI behaviour; every recommendation is explainable and testable.

### Ranking order

1. **In Progress** tasks
2. **Overdue** tasks
3. Tasks **Due Today**
4. **Recommended Next** planned work

### Today sections

- Top Priority
- Overdue
- Due Today
- Recommended Next
- Recently Completed

### Rules

- Completed tasks are excluded from active recommendations.
- Developer Today is scoped to the authenticated user’s assigned work.
- Overdue work ranks ahead of work due today.
- Approaching deadlines rank ahead of later planned work.
- The ranking is deterministic and can be tested.

---

## 🏗️ Architecture

WorkPulse uses a clean separation between domain logic, application workflows, infrastructure and HTTP concerns.

```text
WorkPulse
├── Frontend/                         # Angular UI
├── WorkPulse.Domain/                 # Entities, enums and business rules
├── WorkPulse.Application/            # Use cases and workflow orchestration
├── WorkPulse.Integration.Identity/   # JWT, password hashing and roles
├── WorkPulse.Integration.Sql/        # Dapper, SQL, migrations and seed data
├── WorkPulse.Web.API/                # Controllers and HTTP contracts
├── WorkPulse.Web.Main/               # ASP.NET Core startup host
└── *.Tests/                          # Domain, SQL, API and host tests
```

### Key decisions

- `WorkPulse.Web.Main` is the startup host and composition root.
- `WorkPulse.Web.API` owns routing, validation and authorization boundaries.
- `WorkPulse.Domain` remains independent of Angular, SQL Server and HTTP concerns.
- `WorkPulse.Application` coordinates use cases without depending on infrastructure implementations.
- `WorkPulse.Integration.Sql` owns Dapper repositories, migrations and SQL queries.
- `WorkPulse.Integration.Identity` owns authentication and JWT generation.
- `Frontend/` runs independently and consumes the backend API.

---

## 🔌 API Mapping

| Feature | Endpoint |
|---|---|
| Admin Dashboard | `GET /api/dashboard/admin` |
| Admin Today | `GET /api/dashboard/admin/today` |
| Developer Today | `GET /api/tasks/today` |
| Clients | `GET /api/clients` |
| Projects | `GET /api/projects` |
| Tasks | `GET /api/tasks` |
| My Tasks | `GET /api/tasks/my` |
| Backlog | `GET /api/tasks/backlog` |
| Sprints | `GET /api/sprints` |
| Developers | `GET /api/users/developers` |
| Login | `POST /api/auth/login` |
| Register | `POST /api/auth/register` |
| Current user | `GET /api/auth/me` |

---

## 🛠️ Technology Stack

| Layer | Technology |
|---|---|
| Frontend | Angular, TypeScript, SCSS |
| Backend | ASP.NET Core, C# |
| Data | SQL Server, Dapper, FluentMigrator |
| Security | JWT authentication and role-based authorization |
| Testing | xUnit and integration tests |

---

## ▶️ Running Locally

### Backend

```bash
dotnet restore

dotnet user-secrets init --project WorkPulse.Web.Main

dotnet user-secrets set --project WorkPulse.Web.Main \
  "Jwt:SecretKey" "replace-with-a-local-development-secret"

dotnet user-secrets set --project WorkPulse.Web.Main \
  "DevelopmentSeed:AdminPassword" "WorkPulseAdmin123!"

dotnet run --project WorkPulse.Web.Main
```

The backend applies FluentMigrator migrations and development seed data when it starts.

### Frontend

```bash
cd Frontend
npm install
npm start
```

---

## 🧪 Tests

### Backend and solution tests

```bash
dotnet clean WorkPulse.slnx
dotnet restore WorkPulse.slnx
dotnet build WorkPulse.slnx
dotnet test WorkPulse.slnx
```

### Frontend build

```bash
cd Frontend
npm run build
```

---

## ⚠️ Prototype Scope and Future Improvements

WorkPulse is a working prototype and should not yet be treated as production-ready.

Planned production improvements include:

- Microsoft Entra ID / enterprise SSO
- Email verification and password reset
- Audit trail for role, task and sprint changes
- Notifications
- Search and pagination
- Concurrency handling
- Rate limiting and health checks
- OpenTelemetry and Application Insights
- CI/CD pipelines and Docker deployment
- End-to-end, security and accessibility testing
- Stronger project/team membership rules

---

<div align="center">

Built by **Simphiwe Dlamuka**  
Full-Stack Developer · ASP.NET Core · Angular · SQL Server

</div>
