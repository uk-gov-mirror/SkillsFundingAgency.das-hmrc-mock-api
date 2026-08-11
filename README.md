## ⛔Never push sensitive information such as client id's, secrets or keys into repositories including in the README file⛔

# das-hmrc-mock-api

<img src="https://avatars.githubusercontent.com/u/9841374?s=200&v=4" align="right" alt="UK Government logo">

[![Build Status](https://dev.azure.com/sfa-gov-uk/Digital%20Apprenticeship%20Service/_apis/build/status%2Fdas-hmrc-mock-api?repoName=SkillsFundingAgency%2Fdas-hmrc-mock-api&branchName=main)](https://dev.azure.com/sfa-gov-uk/Digital%20Apprenticeship%20Service/_build/latest?definitionId=3649&repoName=SkillsFundingAgency%2Fdas-hmrc-mock-api&branchName=main)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=SkillsFundingAgency_das-hmrc-mock-api&metric=alert_status)](https://sonarcloud.io/dashboard?id=SkillsFundingAgency_das-hmrc-mock-api)
[![Jira Project](https://img.shields.io/badge/Jira-Project-blue)](https://skillsfundingagency.atlassian.net/secure/RapidBoard.jspa?rapidView=564&projectKey=_projectKey_)
[![Confluence Project](https://img.shields.io/badge/Confluence-Project-blue)](https://skillsfundingagency.atlassian.net/wiki/spaces/_pageurl_)
[![License](https://img.shields.io/badge/license-MIT-lightgrey.svg?longCache=true&style=flat-square)](https://en.wikipedia.org/wiki/MIT_License)

HMRC Apprenticeship Levy API mock used by lower environments in place of the real HMRC levy APIs. It provides:

1. Gateway sign-in UI for creating test employers (`LE_` / `NL_` user id conventions)
2. HMRC-shaped REST endpoints for levy declarations and English fractions
3. MongoDB-backed declaration storage for test emprefs
4. A test-only HTTP endpoint to append importable declarations for existing emprefs (E2E testing)

## How It Works

The web host (`SFA.DAS.HmrcMock.Web`) loads configuration from Azure Table Storage (standard DAS pattern), connects to MongoDB for gateway users / emprefs / declarations / fractions, and optionally uses Redis for data protection keys.

Consumer services (for example Employer Finance MessageHandlers) call authenticated HMRC-shaped routes such as:

* `GET /api/apprenticeship-levy/epaye/{empRef}/declarations`
* `GET /api/apprenticeship-levy/epaye/{empRef}/fractions`

Declarations for an empRef live in the MongoDB `declarations` collection. Historical declarations can be seeded at sign-in; additional importable declarations can be appended via the test endpoint described below.

## 🚀 Installation

### Pre-Requisites

* A clone of this repository
* .NET 10 SDK
* A code editor / IDE that supports ASP.NET Core
* A MongoDB instance (local or shared lower-env cluster)
* Azure Storage emulator (for example Azurite) when using `UseDevelopmentStorage=true` for table config locally
* Redis connection details if running data protection locally as configured

### Config

This service uses the standard Apprenticeship Service configuration. Config lives in the [das-employer-config repository](https://github.com/SkillsFundingAgency/das-employer-config/tree/master/das-hmrc-mock-api).

* Ensure Azure Table Storage (or Azurite) contains the config row for the environment
* Mongo connection string is mapped from env var `MongoUri` (`MongoDbOptions:ConnectionString`)
* Redis / data protection settings are under `HmrcMockConfiguration`

`appsettings.json` (local defaults):

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning"
    }
  },
  "ConfigurationStorageConnectionString": "UseDevelopmentStorage=true;",
  "ConfigNames": "SFA.DAS.HmrcMock.Web",
  "EnvironmentName": "LOCAL",
  "Version": "1.0",
  "APPINSIGHTS_INSTRUMENTATIONKEY": "",
  "AllowedHosts": "*",
  "cdn": {
    "url": "https://das-at-frnt-end.azureedge.net"
  }
}
```

Azure Table Storage config (see also [SFA.DAS.HmrcMock.json](https://github.com/SkillsFundingAgency/das-employer-config/blob/master/das-hmrc-mock-api/SFA.DAS.HmrcMock.json)):

Row Key / Partition Key follow the usual DAS naming for `ConfigNames` + `Version` + `EnvironmentName`.

Example payload shape:

```json
{
  "HmrcMockConfiguration": {
    "DataProtectionKeysDatabase": "DefaultDatabase=3",
    "RedisConnectionString": "<redis-connection-string>"
  },
  "MongoDbOptions": {
    "ConnectionString": "mongodb+srv://<username>:<password>@<server>/<database>?retryWrites=true&w=majority"
  }
}
```

### Running locally

```bash
dotnet restore
dotnet run --project src/SFA.DAS.HmrcMock.Web/SFA.DAS.HmrcMock.Web.csproj
```

Confirm Mongo and table storage config resolve before exercising sign-in or API routes.

## Seeding and test declarations

### New levy employer (sign-in)

Sign in via the gateway UI with a user id matching `LE_{count}_{amount}` (e.g. `LE_12_1000`). That creates a user, empRef, fractions, and `{count}` historical declarations with allowance `15000`.

`NL_{count}_{amount}` creates a non-levy user without declarations.

### Append an importable declaration (test HTTP)

Use this to add a declaration for an **existing** empRef so Employer Finance can import it.

```http
POST {HmrcMockBaseUrl}/api/test/declarations
Content-Type: application/json

{
  "empRef": "123/AB12345",
  "levyDueYTD": 1000,
  "levyAllowanceForFullYear": 15000
}
```

* Put `empRef` in the **JSON body** (it contains `/`). Do not put it in the URL path.
* Omit period fields → mock chooses the **latest Finance-importable** payroll period (not necessarily the current calendar month).
* Response echoes the declaration including `payrollPeriod` and `submissionTime`.
* Re-posting for the same payroll year/month **replaces** that declaration.

#### Importable period rule (Finance)

Finance drops “future” payroll periods. Period start is the **20th** of the calendar month; a period for calendar month **M** is importable only when **Now >= 20th of month M+1**.

This is **not** the employer home “levy due by 19th” UI banner (days 16–19).

If you override `payrollYear` / `payrollMonth` and the period is still future, the API returns `400`.

Optional fields: `payrollYear`, `payrollMonth`, `submissionTime`, `levyDueYTD`, `levyAllowanceForFullYear`.

### E2E: import via das-servicebus-tools

1. Append a declaration as above (under-allowance YTD such as `1000` / `15000` is useful for last-positive filter checks).
2. Trigger import (do **not** wait for the monthly levy job timer):

```http
POST {ServiceBusToolsBaseUrl}/api/ImportAccountLevyDeclarations
Content-Type: application/json
x-functions-key: {functionKey}

{
  "AccountId": "00000",
  "PayeRef": "ABC/123245"
}
```

See [das-servicebus-tools](https://github.com/SkillsFundingAgency/das-servicebus-tools) for `ImportAccountLevyDeclarations`.

3. Verify Finance `LevyDeclaration` / last-positive date and (if deployed) Accounts `EmployerAccountLevyStatus.LastLevyDeclarationDate`.

### Troubleshooting

| Symptom | Likely cause |
|---|---|
| Import succeeds but no new row | Declaration id already imported, or cleaner dropped a **future** period (before 20th of following month) |
| Mock append 404 | empRef has no `declarations` document yet — seed via `LE_` sign-in first |
| Mock GET missing row | Wrong empRef in body, or HMRC-shaped GET used unencoded `/` in path (`123%2FAB12345`) |
| Tools 200 but nothing happens | Wrong AccountId/PayeRef; check MessageHandlers logs |

Authenticated HMRC-shaped GET still requires empRef path encoding (`%2F`) and a Bearer token:

```http
GET {HmrcMockBaseUrl}/api/apprenticeship-levy/epaye/{urlEncodedEmpRef}/declarations?fromDate=yyyy-MM-dd
Authorization: Bearer {token}
```

## 🔗 External Dependencies

* MongoDB for gateway users, emprefs, declarations and fractions
* Redis for ASP.NET data protection key storage (as configured)
* Azure Table Storage / Azurite for DAS configuration
* Consumed by Employer Finance (and related) lower-env HMRC clients via `Hmrc.BaseUrl`

## Technologies

* .NET 10 / ASP.NET Core
* MongoDB.Driver
* Redis / StackExchange.Redis
* Azure Table Storage configuration (`SFA.DAS.Configuration.AzureTableStorage`)
* NUnit / Moq / FluentAssertions
* MediatR

## 🐛 Known Issues

* EmpRefs contain `/`. HMRC-shaped GET routes require URL encoding (`%2F`); the test append endpoint avoids this by accepting `empRef` in the JSON body.
* Jira / Confluence badge links above still use template placeholders — replace when project keys / space URLs are confirmed.
