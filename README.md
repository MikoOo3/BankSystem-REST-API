# Core Banking REST API

A learning project: a banking REST API built with **ASP.NET Core** and **Entity Framework Core** using a 3-tier architecture. It focuses on authentication, role-based authorization, money movement between accounts (including cross-currency transfers), and an audit trail.

> **Status:** educational project, not production-ready.

---

## Features

### Authentication and authorization
- **JWT access tokens** (default lifetime 30 minutes, configurable via `Jwt:DurationInMinutes`).
- **Refresh tokens** with rotation: each refresh revokes the used token and issues a new one. Tokens are random (64 bytes), stored as **SHA-256 hashes**, and expire after 7 days. Logout revokes the supplied refresh token.
- **Role-based access control** with three roles (`Manager`, `Employee`, `Customer`) and three policies: `BankManager`, `BankStaff`, `ActiveUser`.
- **Ownership checks:** customers can only read or operate on their own users, accounts, and transactions. Staff can access all.
- **Password security:** BCrypt hashing, and automatic lockout for 15 minutes after 5 consecutive failed logins.

### Banking operations
- Open, update, close, and reactivate accounts (`Savings` or `Current`).
- **Deposits** and **withdrawals** (staff), and **transfers** between accounts (account owner or staff).
- Operations run inside a database transaction (commit or rollback).
- **Multi-currency:** each account has a currency. Rates are stored relative to USD, and conversion uses `amount / fromRate * toRate`, rounded to 2 decimals. Transfers between accounts of different currencies are converted automatically.
- Managers can update exchange rates (the USD rate is fixed at 1.0). An update applies to every row that shares the same currency code.

### Security and reliability
- **Audit trail:** actions such as logins, failed logins, account changes, role changes, deposits, withdrawals, transfers, and rate updates are recorded with the acting user (taken from the JWT) and before/after values.
- **Rate limiting** (sliding window): a global limit, a per-IP limit for auth endpoints, and a per-user limit for general endpoints.
- **Global exception handling** that returns a `ProblemDetails` response with a `traceId` and never exposes stack traces.
- **Pagination** on list endpoints through `PagedResult<T>` (page size is clamped to a maximum of 50, newest first).
- Read queries use `AsNoTracking()` and DTO projections.

---

## Architecture

The solution is split into three projects plus a SQL folder:

| Project | Responsibility |
|---|---|
| `BankSystem.API` | Controllers, JWT token service, claims extensions, authorization policies, rate limiting, CORS, Swagger, global exception handler |
| `BankSystem.Business` | Business logic: `UserBusiness`, `AccountBusiness`, `TransactionBusiness`, `CurrencyBusiness`, `AuditLogBusiness`, `RefreshTokenBusiness`, password hashing |
| `BankSystem.DataAccess` | EF Core `DbContext`, entities, migrations, DTOs, `PagedResult<T>` |
| `Database` | SQL scripts, including `Currencies_Seed.sql` for reference exchange rates |

### Data model

`User` 1—* `Account` 1—* `Transaction` (as source or destination), plus `RefreshToken` and `AuditLog` linked to users, and a standalone `Currency` table with one row per country.

---

## Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download). All projects target `net10.0`, and the solution uses the `.slnx` format.
- SQL Server (LocalDB, Express, or Developer Edition).
- The EF Core CLI tools, matching the EF Core 10 packages: `dotnet tool install --global dotnet-ef --version 10.0.14`

### Setup

1. **Clone the repository**

   ```bash
   git clone https://github.com/MikoOo3/BankSystem-REST-API.git
   cd BankSystem-REST-API
   ```

2. **Configure secrets** with .NET User Secrets, so nothing sensitive is committed:

   ```bash
   dotnet user-secrets init --project BankSystem.API
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=.;Database=BankSystemDB;Trusted_Connection=True;TrustServerCertificate=True;" --project BankSystem.API
   dotnet user-secrets set "Jwt:Key" "REPLACE_WITH_A_RANDOM_SECRET_OF_AT_LEAST_32_CHARACTERS" --project BankSystem.API
   dotnet user-secrets set "Jwt:Issuer" "BankSystemAPI" --project BankSystem.API
   dotnet user-secrets set "Jwt:Audience" "BankSystemClients" --project BankSystem.API
   ```

3. **Apply the database migrations**

   ```bash
   dotnet ef database update --project BankSystem.DataAccess --startup-project BankSystem.API
   ```

4. **Seed the currency rates.** Run `Database/Currencies_Seed.sql` against `BankSystemDB` (for example in SSMS). The script creates the `Currencies` table if it does not exist and inserts the data only when the table is empty, so it is safe to run more than once. Currency conversion and cross-currency transfers need this data.

   The seed has one row per country, so shared currencies such as `EUR`, `XOF`, `XAF`, and `USD` appear in several rows with the same rate. Each rate is the number of units of that currency per 1 USD, and the values are a static snapshot, not live rates.

5. **Run the API**

   ```bash
   dotnet run --project BankSystem.API --launch-profile https
   ```

   If your browser does not trust the local HTTPS certificate, run `dotnet dev-certs https --trust` once.

6. **Open Swagger** at `https://localhost:7151/swagger` (or `http://localhost:5154/swagger` with the `http` profile). Swagger is only enabled in the Development environment, which both launch profiles set.

### Creating the first Manager

Registration always creates a `Customer`, and only a Manager can change roles. To bootstrap the first Manager, register a user through the API, then promote it directly in the database:

```sql
UPDATE Users SET Role = 'Manager' WHERE Email = 'your@email.com';
```

After that, log in again to get a token that contains the new role.

### Trying it out in Swagger

1. Register through `POST /api/Users/register`, then log in through `POST /api/Auth/login`.
2. Copy the `accessToken`, click **Authorize**, and paste the token.
3. Use `POST /api/Auth/refresh` with the `refreshToken` when the access token expires.

---

## API Endpoints

**Access levels:** *Public* = no token. *ActiveUser* = any logged-in active user. *Owner/Staff* = the owning customer, or a Manager/Employee. *BankStaff* = Manager or Employee. *BankManager* = Manager only.

### Auth (rate limited by `AuthLimiter`)

| Method | Endpoint | Access | Description |
|---|---|---|---|
| POST | `/api/Auth/login` | Public | Authenticate and receive an access token and a refresh token |
| POST | `/api/Auth/refresh` | Public | Rotate the refresh token and receive a new token pair |
| POST | `/api/Auth/logout` | Public (requires a valid refresh token in the body) | Revoke the supplied refresh token |

### Users

| Method | Endpoint | Access | Description |
|---|---|---|---|
| POST | `/api/Users/register` | Public (`AuthLimiter`) | Register a new customer |
| GET | `/api/Users/All` | BankStaff | Paginated list of users |
| GET | `/api/Users/{id}` | Owner/Staff | Get a user by ID |
| GET | `/api/Users/{email}` | BankStaff | Get a user by email |
| PUT | `/api/Users/profile/{id}` | Owner only | Update own name, email, or password |
| PUT | `/api/Users/{id}/role` | BankManager | Change a user's role |
| PUT | `/api/Users/deactivate/{id}` | BankManager | Deactivate a user |
| PUT | `/api/Users/{id}/activate` | BankManager | Reactivate a user |
| PUT | `/api/Users/{id}/unlock` | BankManager | Clear a lockout |

### Accounts

| Method | Endpoint | Access | Description |
|---|---|---|---|
| POST | `/api/Accounts/create` | BankStaff | Open an account for a customer |
| GET | `/api/Accounts/All` | BankStaff | Paginated list of all accounts |
| GET | `/api/Accounts/{id}` | Owner/Staff | Get an account by ID |
| GET | `/api/Accounts/{accountNumber}` | Owner/Staff | Get an account by account number |
| GET | `/api/Accounts/customer/{customerId}` | Owner/Staff | Paginated accounts of one customer |
| PUT | `/api/Accounts/{id}` | BankStaff | Update account type and daily withdrawal limit |
| PUT | `/api/Accounts/{id}/activate` | BankManager | Reactivate an account |
| PUT | `/api/Accounts/close/{id}` | BankManager | Close (deactivate) an account |

### Transactions

| Method | Endpoint | Access | Description |
|---|---|---|---|
| POST | `/api/Transactions/deposit` | BankStaff | Deposit into an account |
| POST | `/api/Transactions/withdraw` | BankStaff | Withdraw from an account |
| POST | `/api/Transactions/transfer` | Owner of source account / Staff | Transfer between accounts (converts currency if needed) |
| GET | `/api/Transactions/All` | BankStaff | Paginated list of all transactions |
| GET | `/api/Transactions/account-history/{accountId}` | Owner/Staff | Paginated history for an account |
| GET | `/api/Transactions/{id}` | Owner/Staff | Get a transaction by ID |

### Currencies (all require ActiveUser)

| Method | Endpoint | Access | Description |
|---|---|---|---|
| GET | `/api/Currencies/All` | ActiveUser | List currencies and rates |
| GET | `/api/Currencies/{id}` | ActiveUser | Get a currency by ID |
| GET | `/api/Currencies/code/{code}` | ActiveUser | Find by ISO code |
| GET | `/api/Currencies/country/{country}` | ActiveUser | Find by country |
| GET | `/api/Currencies/CurrencyName/{name}` | ActiveUser | Find by currency name |
| GET | `/api/Currencies/calculate?fromCurrency=&toCurrency=&amount=` | ActiveUser | Convert an amount through the USD base |
| PUT | `/api/Currencies/{code}/rate` | BankManager | Update a rate (audited) |

### Audit logs (all require BankManager)

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/AuditLogs/All` | Paginated audit log |
| GET | `/api/AuditLogs/{id}` | Get one entry |
| GET | `/api/AuditLogs/user/{userId}` | Paginated entries for one user |

### Rate limits

| Policy | Applies to | Limit |
|---|---|---|
| Global | Whole server (one shared bucket) | 500 requests per minute |
| `AuthLimiter` | Auth endpoints and registration, per IP | 5 requests per minute |
| `GeneralApiLimiter` | All other controllers, per user (or per IP if anonymous) | 60 requests per minute |

---

## Roadmap

- Automated tests (unit and integration)
- Strengthen transaction handling and validation
- Separate request and response models
- Consistent error responses
- A frontend built with HTML, CSS, JavaScript, TypeScript, Tailwind, and React

---

## License

No license has been specified yet.
