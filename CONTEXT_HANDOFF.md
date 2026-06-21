# CONTEXT HANDOFF — SanctionsAlertService (.NET)

## מה בוצע עד עכשיו
- נוצר repository חדש: `SanctionsAlertService.NetCore`
- בוצעה אתחול Git (`git init`)
- נוצר Solution: `SanctionsAlertService.slnx`
- נוצרו פרויקטים:
  - `src/SanctionsAlertService.Api`
  - `src/SanctionsAlertService.Application`
  - `src/SanctionsAlertService.Domain`
  - `src/SanctionsAlertService.Infrastructure`
  - `tests/SanctionsAlertService.Domain.Tests`
  - `tests/SanctionsAlertService.Application.Tests`
  - `tests/SanctionsAlertService.Api.Tests`
  - `tests/SanctionsAlertService.Infrastructure.Tests`
- הוגדרו Project References בין השכבות
- בוצע מימוש ראשוני מלא (vertical slice) + Build ירוק

## סטטוס נוכחי
- `dotnet build SanctionsAlertService.slnx` עובר בהצלחה.
- השירות כבר כולל endpoints, domain rules, repository in-memory, tenant isolation בסיסי, ו-event publishing לוגי.

## קבצים מרכזיים שמומשו

### API
- `src/SanctionsAlertService.Api/Program.cs`
- `src/SanctionsAlertService.Api/Controllers/AlertsController.cs`
- `src/SanctionsAlertService.Api/Contracts/CreateAlertRequest.cs`
- `src/SanctionsAlertService.Api/Contracts/DecisionRequest.cs`
- `src/SanctionsAlertService.Api/Contracts/AlertResponse.cs`
- `src/SanctionsAlertService.Api/Tenant/TenantMiddleware.cs`
- `src/SanctionsAlertService.Api/Tenant/TenantContext.cs`
- `src/SanctionsAlertService.Api/Errors/GlobalExceptionHandler.cs`
- `src/SanctionsAlertService.Api/Errors/ApiError.cs`

### Application
- `src/SanctionsAlertService.Application/Services/AlertService.cs`
- `src/SanctionsAlertService.Application/Repositories/IAlertRepository.cs`
- `src/SanctionsAlertService.Application/Events/IAlertEventPublisher.cs`

### Domain
- `src/SanctionsAlertService.Domain/Entities/Alert.cs`
- `src/SanctionsAlertService.Domain/Enums/AlertStatus.cs`
- `src/SanctionsAlertService.Domain/Enums/DecisionOutcome.cs`
- `src/SanctionsAlertService.Domain/ValueObjects/AlertFilter.cs`
- `src/SanctionsAlertService.Domain/ValueObjects/Decision.cs`
- `src/SanctionsAlertService.Domain/Events/AlertEvent.cs`
- `src/SanctionsAlertService.Domain/Events/AlertEscalated.cs`
- `src/SanctionsAlertService.Domain/Events/AlertDecided.cs`
- `src/SanctionsAlertService.Domain/Exceptions/*.cs`

### Infrastructure
- `src/SanctionsAlertService.Infrastructure/Repositories/InMemoryAlertRepository.cs`
- `src/SanctionsAlertService.Infrastructure/Events/LoggingAlertEventPublisher.cs`

## החלטות מימוש שנלקחו
- Tenant מזוהה רק מה-header: `X-Tenant-Id`
- missing tenant => `400`
- גישה ל-alert של tenant אחר => `404`
- החלטה היא write-once (re-decide => `409`)
- Escalation מותר רק מ-`OPEN` (אחרת `409`)
- תזמון אירועים: Persist ואז Publish
- אם publish נכשל: מצב נשמר, הבקשה לא נכשלת (v1 policy)
- optimistic concurrency לפי `Version`

## Endpoints קיימים
- `POST /api/v1/alerts`
- `GET /api/v1/alerts`
- `GET /api/v1/alerts/{id}`
- `POST /api/v1/alerts/{id}/escalation`
- `POST /api/v1/alerts/{id}/decision`

## איך להריץ
```powershell
cd SanctionsAlertService.NetCore
dotnet build SanctionsAlertService.slnx
dotnet run --project src/SanctionsAlertService.Api
```

## מה נשאר לביצוע (next steps)
1. כתיבת Unit Tests ל-Domain (state transitions + invariants)
2. כתיבת Unit Tests ל-Service (orchestration + events)
3. כתיבת Tests ל-Repository (tenant partition + optimistic lock)
4. כתיבת Integration Tests ל-API + tenant isolation
5. שיפור ולידציות/API error details עקביים
6. README מלא עם דוגמאות curl והסברי design trade-offs
7. commit ראשון מסודר + branch strategy

## טקסט מוכן להדבקה בצ'אט חדש
הקונטקסט:
- אני עובד על פרויקט `SanctionsAlertService.NetCore` ב-C#/.NET (ASP.NET Core Web API) עם שכבות Api/Application/Domain/Infrastructure.
- המימוש הראשוני כבר קיים ועובר build.
- יש Domain entity בשם Alert עם state machine: OPEN -> ESCALATED -> (CLEARED/CONFIRMED_HIT), והחלטה היא write-once.
- יש בידוד tenant דרך header `X-Tenant-Id` + middleware + repository tenant-scoped.
- Repository הוא InMemory עם optimistic concurrency לפי Version.
- יש endpoints: create/list/get/escalate/decision תחת `/api/v1/alerts`.
- אני רוצה להמשיך עכשיו לשלב הבא: כתיבת tests ו-README, ואז hardening של validation ו-error handling.
