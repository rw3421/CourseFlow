CourseFlow
CourseFlow is a robust, enterprise-grade Education Management System (EMS) built with .NET 8. It provides a secure and scalable backend for managing students, staff, and academic lifecycles with a deep focus on security, data integrity, and forensic auditability.

Features
1) Enterprise-Grade Security
i) Token Rotation & Revocation: Implements a secure JWT lifecycle with Refresh Token Rotation. Every time a token is refreshed, the old one is revoked and linked to the new one (ReplacedByToken), effectively detecting and preventing replay attacks.
ii) Single-Session Enforcement: The system can optionally revoke all existing refresh tokens upon a new login, ensuring a clean and secure user posture.
iii) BCrypt Protection: Industry-standard hashing for all user credentials.

2) Production-Ready Data Governance
i) Automated Sarawak-Time Auditing: A custom AppDbContext override automatically handles CreatedAt and UpdatedAt timestamps using UTC+8, ensuring all logs match local Malaysian time.
ii) Advanced Soft-Delete Engine: Data is protected against accidental loss. Deleted records are marked as IsDeleted and filtered globally via EF Core Global Query Filters, ensuring they remain in the DB for audit trails but are invisible to standard API calls.
iii) JSON-Based Approval Workflow: Change requests for sensitive entities (like Courses) are stored as JSON snapshots (PayloadJson) for Admin review before being officially committed to the production tables.

3) Resilient Architecture
i) Global Exception Middleware: A centralized interceptor ensures the API never leaks sensitive stack traces. It returns a consistent ApiResponse<T> structure, even during 500-level errors.
ii) Strict Data Contracts (DTOs): A comprehensive suite of DTOs decouples the database schema from the API layer, preventing "over-posting" vulnerabilities and ensuring strict input validation.
iii)Forensic Audit Logging: Captures detailed metadata for every significant action, including IpAddress, UserAgent, and the specific entity modified.

Tech Stack
Framework: .NET 8 Web API
Database: MySQL / MariaDB (Optimized for utf8mb4)
ORM: Entity Framework Core (with Fluent API & Migrations)
Auth: JWT (JSON Web Tokens)
Timezone Logic: UTC+8 (Sarawak, Malaysia)

Project Organization
Layer          Responsibility
Controllers    Secure REST endpoints with Role-Based Access Control (RBAC).
Models         Domain entities including complex relationships (1:1 Staff-User).
Data           AppDbContext containing auditing logic and soft-delete filters.
DTOs           "Input/Output contracts for clean, validated data transfer."
Middleware     Global error handling and security pipeline layers.
ViewModels     "Flattened, human-readable data structures for Admin dashboards."

Getting Started
1. Prerequisites
.NET 8 SDK

2. Configuration
MySQL Server 8.0+
Update the connection string in appsettings.json:
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Port=3306;Database=courseflow;User=root;Password=your_password;"
}

3. Database Migration & Seeding
Apply the schema and seed the default administrator:
dotnet ef database update

The system will automatically seed an Admin account on first run:
- User: admin@courseflow.com
- Pass: 1234

Access Levels
ADMIN: Full system governance, staff management, and course approval.
STAFF: Manage student rosters and view assigned course details.
STUDENT: Self-service profile management and course enrollment.










