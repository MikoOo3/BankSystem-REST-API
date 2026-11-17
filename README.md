\# 🏦 Core Banking REST API



A production-grade Core Banking REST API engineered with \*\*ASP.NET Core\*\* and \*\*Entity Framework Core\*\* using a clean \*\*3-Tier Architecture\*\*. Built with enterprise banking security, multi-currency financial integrity, and scalability in mind.



\---



\## ✨ Key Features



\### 🔐 Authentication \& Authorization

\* \*\*JWT Bearer Authentication:\*\* Stateless access tokens paired with secure \*\*Refresh Token Rotation\*\* and revocation upon logout.

\* \*\*Role-Based \& Policy-Based Access Control (RBAC):\*\* Fine-grained authorization policies (`BankManager`, `BankStaff`, `ActiveUser`) protecting sensitive financial and administrative operations.

\* \*\*Account Lockout \& Password Security:\*\* BCrypt password hashing with automatic account lockout after consecutive failed login attempts.



\### 🛡️ Enterprise Security \& Resilience

\* \*\*Event-Driven Audit Trail:\*\* Centralized audit logging capturing actor identity from JWT claims alongside pre- and post-operation snapshots to ensure full \*\*non-repudiation\*\*.

\* \*\*Multi-Layered Sliding-Window Rate Limiting:\*\* Protects endpoints against brute-force attacks and traffic spikes using \*\*Global\*\*, \*\*Per-IP\*\* (`AuthLimiter`), \*\*Per-User\*\* (`GeneralApiLimiter`), and \*\*Per-Endpoint\*\* policies.

\* \*\*Global Exception Handling:\*\* Centralized `IExceptionHandler` returning RFC-compliant `ProblemDetails` JSON responses with a unique `TraceId` while preventing sensitive stack trace disclosure.



\### 💱 Financial Integrity, Multi-Currency \& Performance

\* \*\*Atomic Database Transactions:\*\* Ensures deposits, withdrawals, and fund transfers execute with full ACID compliance (`Commit` / `Rollback`).

\* \*\*Currency Exchange \& Cross-Currency Transfers:\*\* Real-time USD-bridged currency conversion calculator (`Amount / FromRate \* ToRate`), manager-audited exchange rate updates, and automatic currency conversion during cross-currency account transfers.

\* \*\*Standardized Offset Pagination:\*\* Consistent `PagedResult<T>` responses across list endpoints with built-in page size clamping (`MaxPageSize = 50`) and chronological ordering.

\* \*\*Optimized Read Queries:\*\* Uses `AsNoTracking()` and DTO projections across read operations.



\---



\## 🏗️ Architecture



The solution follows a clean \*\*3-Tier Architecture\*\* to maintain separation of concerns:



\* \*\*`BankSystem.API`\*\*: RESTful Controllers, JWT Token Services, Custom Claims Extensions, Rate Limiting, CORS, Swagger/OpenAPI configuration, and Global Exception Handling Middleware.

\* \*\*`BankSystem.Business`\*\*: Core domain logic, validation rules, atomic transaction orchestration, currency exchange calculations, BCrypt hashing, and business objects (`UserBusiness`, `AccountBusiness`, `TransactionBusiness`, `CurrencyBusiness`, `AuditLogBusiness`).

\* \*\*`BankSystem.DataAccess`\*\*: Entity Framework Core `DbContext`, database entities, migrations, and Data Transfer Objects (`DTOs`, `PagedResult<T>`).

\* \*\*`Database`\*\*: Standalone SQL initialization scripts (`Currencies\_Seed.sql`) for seeding reference exchange rates.



\---



\## 🚀 Getting Started



\### Prerequisites

\* \[.NET 8 SDK](https://microsoft.com) (or newer)

\* SQL Server (LocalDB, Express, or Developer Edition)



\### Setup \& Installation



1\. \*\*Clone the repository\*\*

&#x20;  ```bash

&#x20;  git clone https://github.com<your-username>/<your-repo-name>.git

&#x20;  cd <your-repo-name>

&#x20;  ```



2\. \*\*Configure Application Secrets (via .NET User Secrets)\*\*

&#x20;  Initialize user secrets in `BankSystem.API` so sensitive keys are never committed to source control:

&#x20;  ```bash

&#x20;  dotnet user-secrets init --project BankSystem.API

&#x20;  dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=.;Database=BankSystemDB;Trusted\_Connection=True;TrustServerCertificate=True;" --project BankSystem.API

&#x20;  dotnet user-secrets set "Jwt:Key" "YOUR\_SUPER\_SECRET\_KEY\_AT\_LEAST\_32\_CHARACTERS\_LONG" --project BankSystem.API

&#x20;  dotnet user-secrets set "Jwt:Issuer" "BankSystemAPI" --project BankSystem.API

&#x20;  dotnet user-secrets set "Jwt:Audience" "BankSystemClients" --project BankSystem.API

&#x20;  ```



3\. \*\*Apply Database Migrations\*\*

&#x20;  ```bash

&#x20;  dotnet ef database update --project BankSystem.DataAccess --startup-project BankSystem.API

&#x20;  ```



4\. \*\*Seed Currency Exchange Rates\*\*

&#x20;  Open SQL Server Management Studio (SSMS) and execute the SQL script located at `Database/Currencies\_Seed.sql` against `BankSystemDB` to create and populate the `Currencies` table.



5\. \*\*Run the Application\*\*

&#x20;  ```bash

&#x20;  dotnet run --project BankSystem.API

&#x20;  ```



6\. \*\*Explore \& Test via Swagger\*\*

&#x20;  \* Open `https://localhost:<port>/swagger` in your browser.

&#x20;  \* Register or log in via `/api/Auth/login`, copy the `accessToken`, click \*\*Authorize\*\*, and enter your token to test protected endpoints.



\---



\## 📡 API Endpoints Overview



| Controller | Endpoint | Access Policy | Description |

| :--- | :--- | :--- | :--- |

| \*\*Auth\*\* | `POST /api/Auth/login` | Public (`AuthLimiter`) | Authenticate user and issue JWT + Refresh Token |

| \*\*Auth\*\* | `POST /api/Auth/refresh` | Public (`AuthLimiter`) | Rotate refresh token and issue a new access token |

| \*\*Auth\*\* | `POST /api/Auth/logout` | `ActiveUser` | Revoke active refresh token and log event |

| \*\*Users\*\* | `POST /api/Users/register` | Public (`AuthLimiter`) | Register a new customer account |

| \*\*Users\*\* | `GET /api/Users/All` | `BankStaff` | Retrieve paginated list of users |

| \*\*Users\*\* | `PUT /api/Users/profile/{id}` | Owner (`ActiveUser`) | Update user profile details |

| \*\*Users\*\* | `PUT /api/Users/{id}/role` | `BankManager` | Update user role (`Manager`, `Employee`, `Customer`) |

| \*\*Users\*\* | `PUT /api/Users/deactivate/{id}` | `BankManager` | Soft-deactivate a user account |

| \*\*Users\*\* | `PUT /api/Users/{id}/activate` | `BankManager` | Reactivate a user account |

| \*\*Users\*\* | `PUT /api/Users/{id}/unlock` | `BankManager` | Unlock a locked user account |

| \*\*Accounts\*\* | `POST /api/Accounts` | `ActiveUser` | Open a new bank account |

| \*\*Accounts\*\* | `GET /api/Accounts/All` | `BankStaff` | Retrieve paginated list of all bank accounts |

| \*\*Accounts\*\* | `GET /api/Accounts/customer/{id}` | Owner / `BankStaff` | Retrieve paginated accounts for a specific customer |

| \*\*Accounts\*\* | `PUT /api/Accounts/close/{id}` | `BankManager` | Close (deactivate) a bank account |

| \*\*Transactions\*\* | `POST /api/Transactions/deposit` | `BankStaff` | Deposit funds into an account |

| \*\*Transactions\*\* | `POST /api/Transactions/withdraw` | Owner / `BankStaff` | Withdraw funds within daily withdrawal limits |

| \*\*Transactions\*\* | `POST /api/Transactions/transfer` | Owner / `BankStaff` | Transfer funds atomically (supports cross-currency conversion) |

| \*\*Transactions\*\* | `GET /api/Transactions/All` | `BankStaff` | Retrieve paginated global transaction history |

| \*\*Transactions\*\* | `GET /api/Transactions/account-history/{id}` | Owner / `BankStaff` | Retrieve paginated transaction history for an account |

| \*\*Currencies\*\* | `GET /api/Currencies/All` | `ActiveUser` | Retrieve all supported currencies and exchange rates |

| \*\*Currencies\*\* | `GET /api/Currencies/code/{code}` | `ActiveUser` | Find currency details by ISO currency code |

| \*\*Currencies\*\* | `GET /api/Currencies/country/{country}` | `ActiveUser` | Find currency details by country name |

| \*\*Currencies\*\* | `GET /api/Currencies/name/{name}` | `ActiveUser` | Find currency details by currency name |

| \*\*Currencies\*\* | `GET /api/Currencies/calculate` | `ActiveUser` | Calculate currency exchange conversion via USD bridge |

| \*\*Currencies\*\* | `PUT /api/Currencies/{code}/rate` | `BankManager` | Update currency exchange rate with audit trail logging |

| \*\*AuditLogs\*\* | `GET /api/AuditLogs/All` | `BankManager` | Retrieve paginated system-wide audit logs |

| \*\*AuditLogs\*\* | `GET /api/AuditLogs/user/{userId}` | `BankManager` | Retrieve paginated audit logs for a specific user |



