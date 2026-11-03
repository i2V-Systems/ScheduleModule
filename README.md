# 🗓️ ScheduleModule

**ScheduleModule** is a robust, modular backend solution designed for enterprise-grade schedule management. Leveraging **Clean Architecture**, **Quartz.NET**, and scalable patterns, this module provides a solid foundation for creating, updating, tracking, and dispatching scheduled tasks in your system.

---

## 📌 Features

* ✅ **CRUD Operations**: Create, update, retrieve, and delete schedules.
* 🔁 **Enable/Disable Schedules**: Status management with validation.
* 📦 **Bulk Operations**: Process multiple schedules efficiently.
* ⏱️ **Quartz.NET Integration**: Automated scheduling and execution.
* 📬 **Topic-Based Dispatching**: Events routed to relevant handlers.
* 🔧 **Extensible Types**: Easily add custom schedule types and rules.

---

## 🏗️ Architecture Overview

This module follows **Clean Architecture**, separating concerns into distinct layers:

### 🔹 Domain Layer

* Core entities: `Schedule`, `ScheduleType`, `ScheduleStatus`
* Value Objects and Business Rules

### 🔹 Application Layer

* Use cases: `CreateSchedule`, `UpdateSchedule`, `ChangeStatus`, etc.
* Application Services and Command Handlers

### 🔹 Infrastructure Layer

* Persistence: Repositories (EF Core), Quartz.NET setup
* External Integrations (e.g., dispatchers, logs)

### 🔹 Presentation Layer *(Optional)*

* RESTful APIs for interacting with the module

---

## ⚙️ Design Patterns & Technologies

| Area                 | Technology/Pattern               |
| -------------------- | -------------------------------- |
| Data Access          | Repository Pattern, Unit of Work |
| Scheduling           | Quartz.NET                       |
| Dependency Injection | Service/Job registration         |
| Messaging            | Topic-based event dispatching    |
| Communication        | DTOs between layers              |

---

## 🚀 Use Cases

* **Create a Schedule**: Define a schedule with type, cron/timing, and topic.
* **Update Schedule**: Modify details and rebind job if needed.
* **Enable/Disable**: Toggle active status with proper validations.
* **Bulk Operations**: Batch update or delete with resilience.
* **Track Execution**: Job run history and failure logging via Quartz.
* **Event Dispatch**: Send scheduled events to topic handlers.

---

## 🧪 Testing Strategy

* ✅ **Unit Tests**: Domain rules, services, handlers.
* 🧩 **Integration Tests**: Repositories, Quartz job executions.
* 🧪 **Manual Testing**: UI/API interaction, edge cases, and concurrency.

---

## ⚠️ Edge Cases Handled

* Duplicate schedule names.
* Invalid/missing fields.
* Partial failures in bulk operations.
* Misfire handling with Quartz.NET.
* Concurrency-safe status changes and deletions.
* Retry and error logging for job failures.

---

## 📊 Metrics & Extensibility

* 🧪 **Code Coverage**: Includes unit and integration test suites.
* 🚄 **Performance**: Optimized batch processing.
* 🔊 **Extensible**: Add new schedule types, business rules, or job types.
* 🤭 **Documentation**: Inline comments and architectural diagrams included.

---

## 🧰 Getting Started

1. **Configure Quartz.NET** in your host application.
2. **Register module services** via dependency injection.
3. **Use Application Services** or REST APIs to manage schedules.

```csharp
services.AddScheduleModule(); // example extension method
```

---

## 🗂️ Database Scripts

The module depends on specific schema structures to support scheduling and job tracking:

* `Scheduling.Infrastructure/Data/quartz.sql`: Contains the standard Quartz.NET schema for job store tables (triggers, jobs, job history, etc.).
* `Scheduling.Infrastructure/Data/schedule-schema.sql`: Defines module-specific tables needed for `ScheduleModule` operations.

Ensure both scripts are executed on your database server before running the application.

---

## 🧩 Topic-Aware Job Handlers

ScheduleModule allows schedules to trigger topic-specific behavior via event handlers. These handlers implement an internal contract and listen to dispatched events based on their declared interests.

For example, a handler can listen to `Resources.Config` and apply logic (such as updating analytic servers) when the schedule fires. This allows developers to plug in their own event-based logic cleanly, without changing the scheduling engine.

To integrate your own logic:

* Implement a topic-aware handler class.
* Declare which topics the handler listens to.
* Handle incoming scheduled events and perform business logic (e.g., update resources, trigger external workflows).

This promotes a decoupled and extensible design where schedule actions can drive real-time decisions in downstream systems.

---

## 📂 Example Directory Structure

```
ScheduleModule/
├── Domain/
│   ├── Entities/
│   ├── ValueObjects/
│   └── Enums/
├── Application/
│   ├── UseCases/
│   ├── DTOs/
│   └── Interfaces/
├── Infrastructure/
│   ├── Persistence/
│   ├── Quartz/
│   └── Dispatchers/
├── Presentation/
│   └── Controllers/ (optional)
└── ScheduleModule.csproj
```


