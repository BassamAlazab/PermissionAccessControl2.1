# Authorization Engine — Implementation Priorities

## مبدأ الترتيب

الأولوية ليست حسب سهولة التنفيذ، وإنما حسب:

1. صحة نموذج الصلاحيات.
2. الأمان.
3. استقلال المحرك عن الـ Domain.
4. قابلية الاختبار.
5. الأداء.
6. سهولة دمجه في أي مشروع .NET.
7. الميزات المتقدمة.

---

# P0 — الأساس الحرج

> **يجب تنفيذها أولًا، ولا ينبغي بناء أي شيء فوقها قبل استقرارها.**

## 1. Core Abstractions

إنشاء:

```text
Authorization.Core
Authorization.Abstractions
```

وتعريف:

```text
SubjectReference
ResourceReference
Permission
Role
Scope
Relation
AuthorizationContext
AuthorizationResult
PermissionEffect
```

---

## 2. Domain Independence

يجب التأكد منذ البداية أن Core لا يعرف:

```text
EF Core
ASP.NET Core
DbContext
HttpContext
Redis
Domain Entities
```

---

## 3. Public Contracts

تحديد الـ Interfaces الأساسية:

```text
IAuthorizationService
IPermissionResolver
IAssignmentResolver
IScopeResolver
IRelationshipResolver
IResourceGraphProvider
IPolicyEvaluator
IOverrideResolver
IDelegationResolver
IDecisionEngine
IAuthorizationAuditService
```

---

## 4. Default Deny

القاعدة الأساسية:

```text
No Matching Authorization
        ↓
DENY
```

ويجب أن تكون هذه القاعدة موجودة من أول إصدار.

---

## 5. Deterministic Decision Engine

بناء:

```text
IDecisionEngine
```

وتحديد precedence بشكل صريح.

لا يسمح بأن تعتمد النتيجة على:

```text
SQL order
LINQ order
Cache order
Execution order
```

---

## 6. Tenant Isolation

إذا كان المحرك يدعم Multi-Tenant، فهذه ليست Feature لاحقة.

يجب تصميمها من البداية.

كل Authorization Data يجب أن تكون مرتبطة بحدود Tenant واضحة.

---

## 7. Security Boundary

من البداية يجب تطبيق:

```text
Fail Closed
Tenant Validation
Subject Validation
Resource Validation
```

---

# P1 — Authorization Core

> بعد تثبيت الأساس، نبني المحرك الفعلي.

## 8. Permission Model

تنفيذ:

```text
Permissions
Actions
ResourceTypes
```

مع:

```text
Permission Key
Resource Type
Action
```

---

## 9. Roles

تنفيذ:

```text
Roles
RolePermissions
```

مع دعم:

```text
Allow
Deny
```

---

## 10. Assignments

تنفيذ:

```text
RoleAssignments
PermissionAssignments
```

مع:

```text
ValidFrom
ValidUntil
IsActive
Priority
TenantId
```

---

## 11. Scope Model

تنفيذ:

```text
AuthorizationScopes
ScopeInheritanceRules
```

والـ Scope يكون:

```text
ResourceType + ResourceId
```

وليس:

```text
CompanyId
DepartmentId
ProjectId
```

---

## 12. Resource Graph

تنفيذ:

```text
IResourceGraphProvider
```

ودعم:

```text
Parent
Child
Relation
Descendant
```

مع:

```text
MaxDepth
Cycle Detection
Relation Allowlist
```

---

## 13. Scope Resolver

تنفيذ:

```text
IScopeResolver
```

بحيث يستطيع الإجابة:

```text
Is Resource X inside Scope Y?
```

---

## 14. Relationship Resolver

دعم:

```text
subject member_of resource
subject manager_of resource
resource belongs_to resource
resource contains resource
```

---

## 15. Permission Resolver

ربط:

```text
Subject
    ↓
Assignments
    ↓
Roles
    ↓
Permissions
    ↓
Scopes
```

---

# P2 — Policy & Advanced Authorization

> بعد أن يصبح RBAC + Scope + ReBAC مستقرًا.

## 16. Policy Engine

تنفيذ:

```text
IPolicyEvaluator
```

ودعم Policy DSL آمن.

---

## 17. Policy Validation

يجب التحقق من:

```text
Unknown Fact
Unknown Operator
Invalid Expression
Excessive Depth
Excessive Conditions
```

---

## 18. Policy Versioning

دعم:

```text
Policy
Version
IsActive
```

حتى نستطيع معرفة أي Policy تم استخدامها في القرار.

---

## 19. Overrides

تنفيذ:

```text
PermissionOverrides
IOverrideResolver
```

لدعم:

```text
Resource-specific Allow
Resource-specific Deny
```

مع Priority واضح.

---

## 20. Delegation

تنفيذ:

```text
Delegations
DelegationPermissions
IDelegationResolver
```

مع:

```text
ValidFrom
ValidUntil
Scope
Permissions
```

---

## 21. Feature Authorization

إضافة:

```text
Features
feature.* permissions
```

مع الفصل الكامل بين:

```text
Feature Access
```

و:

```text
Data Access
```

---

# P3 — Integration Layer

> الآن نجعل المكتبة قابلة للتركيب في أي مشروع .NET.

## 22. EF Core Adapter

إنشاء:

```text
Authorization.EntityFrameworkCore
```

يحتوي:

```text
AuthorizationDbContext
Entity Configurations
Repositories
Migrations
```

---

## 23. Storage Abstractions

التأكد أن Core يستطيع العمل مع:

```text
EF Core
Custom Storage
External Storage
```

بدون تعديل Core.

---

## 24. ASP.NET Core Integration

إنشاء:

```text
Authorization.AspNetCore
```

ودعم:

```text
Minimal APIs
MVC
Endpoint Filters
Authorization Handlers
Middleware
```

---

## 25. DI Integration

توفير:

```csharp
services.AddAuthorizationEngine(...)
```

بحيث يكون التسجيل بسيطًا.

---

## 26. Authentication Integration

لا يبني Authorization Engine نظام Authentication.

بل يستقبل Identity من النظام الموجود:

```text
ASP.NET Identity
JWT
Keycloak
Auth0
Firebase
Custom Authentication
```

ويحولها إلى:

```text
SubjectReference
```

---

# P4 — Production Hardening

> بعد استقرار الوظائف الأساسية والتكامل.

## 27. Caching

إضافة:

```text
ICacheProvider
```

ثم:

```text
Memory Cache
Distributed Cache
```

اختياريًا:

```text
Redis Adapter
```

---

## 28. Cache Invalidation

يجب ربط التغييرات بـ:

```text
Role
Permission
Assignment
Scope
Policy
Override
Delegation
Relationship
```

---

## 29. Performance Optimization

تنفيذ:

```text
Batch Resolution
Projection
Compiled Queries
Graph Optimization
Cache
```

ومنع:

```text
N+1 Queries
```

---

## 30. Bulk Authorization

إضافة:

```text
AuthorizeManyAsync()
```

للحالات التي تحتاج فحص مجموعة موارد.

---

## 31. Query Authorization

إضافة abstraction مثل:

```text
IAuthorizationFilterProvider
```

حتى يمكن للنظام المستضيف تطبيق Authorization على مستوى Query.

مثال:

```text
GetAuthorizedProjects()
```

بدل:

```text
GetAllProjects()
    ↓
Authorize() × 1000
```

---

## 32. Graph Optimization

عند الحاجة:

```text
Closure Table
Materialized Path
Resource Ancestors
```

لكن لا يجب فرضها على كل نظام.

---

# P5 — Observability & Governance

## 33. Audit

تنفيذ:

```text
IAuthorizationAuditService
```

وتسجيل:

```text
Allow
Deny
Role Changes
Permission Changes
Assignment Changes
Policy Changes
Scope Changes
```

---

## 34. Structured Logging

الاعتماد على:

```text
ILogger
```

بدل فرض Logging Framework خاص.

---

## 35. Metrics

دعم:

```text
Authorization Duration
Allow Count
Deny Count
Cache Hit
Cache Miss
Policy Duration
Graph Depth
```

---

## 36. Distributed Tracing

استخدام:

```text
ActivitySource
```

بحيث يمكن تتبع:

```text
HTTP Request
    ↓
Authorization
    ↓
Database
    ↓
Graph
    ↓
Policy
```

---

## 37. Health Checks

دعم اختياري لـ:

```text
Database
Cache
External Providers
```

---

# P6 — Quality & Ecosystem

> هذه المرحلة تجعل المنتج ناضجًا كمكتبة عامة.

## 38. Testing Framework

### Unit Tests

```text
DecisionEngine
ScopeResolver
PermissionResolver
RelationshipResolver
PolicyEvaluator
```

### Integration Tests

```text
EF Core
Database
Cache
ASP.NET
```

### Security Tests

```text
IDOR
Privilege Escalation
Tenant Escape
Scope Bypass
Policy Bypass
Cache Leakage
```

---

## 39. Contract Tests

كل Provider يجب أن يمر بنفس Contract.

مثال:

```text
EF Permission Store
Custom Permission Store
External Permission Store
```

كلها يجب أن تعطي نفس السلوك.

---

## 40. Benchmarking

إنشاء Benchmarks لـ:

```text
Simple Authorization
Role Authorization
Scoped Authorization
ReBAC
Policy Authorization
Cached Authorization
Complex Graph
```

---

## 41. Documentation

توثيق:

```text
Architecture
Installation
Configuration
Database
Roles
Permissions
Scopes
ReBAC
Policies
Caching
Security
Testing
Migration
ASP.NET Integration
```

---

## 42. Samples

توفير:

```text
Sample.Basic
Sample.EFCore
Sample.AspNetCore
Sample.MinimalApi
Sample.MultiTenant
Sample.ReBAC
Sample.Policy
Sample.BackgroundService
```

---

## 43. Package Versioning

اعتماد:

```text
Semantic Versioning
```

والفصل بين:

```text
Core Version
Database Schema Version
Policy Version
```

---

# الأولوية النهائية

يمكن تلخيص التنفيذ هكذا:

```text
                    AUTHORIZATION ENGINE
                           │
                           ▼
                    ┌──────────────┐
                    │     P0       │
                    │ Foundation   │
                    └──────┬───────┘
                           │
                           ▼
                    ┌──────────────┐
                    │     P1       │
                    │ Core Auth    │
                    └──────┬───────┘
                           │
                           ▼
                    ┌──────────────┐
                    │     P2       │
                    │ Policy/Adv.  │
                    └──────┬───────┘
                           │
                           ▼
                    ┌──────────────┐
                    │     P3       │
                    │ Integration  │
                    └──────┬───────┘
                           │
                           ▼
                    ┌──────────────┐
                    │     P4       │
                    │ Production   │
                    └──────┬───────┘
                           │
                           ▼
                    ┌──────────────┐
                    │     P5       │
                    │ Observability│
                    └──────┬───────┘
                           │
                           ▼
                    ┌──────────────┐
                    │     P6       │
                    │ Ecosystem    │
                    └──────────────┘
```

---

# ما يجب ألا نؤجله

هناك أشياء قد تبدو "تحسينات"، لكنها في الحقيقة يجب أن تدخل من اليوم الأول:

```text
1. Domain Independence
2. Dependency Inversion
3. Tenant Isolation
4. Default Deny
5. Fail Closed
6. Deterministic Decision
7. ResourceType + ResourceId
8. Resource Graph Abstraction
9. CancellationToken
10. Async
11. Testability
12. Public API Design
13. Versioning Strategy
14. Security Boundary
```

إذا أخّرت هذه الأشياء، قد تضطر لاحقًا إلى إعادة بناء Core بالكامل.

---

# ما يمكن تأجيله

يمكن تأجيل:

```text
Redis
Distributed Cache
Closure Tables
Bulk Authorization
Query Authorization
OpenTelemetry Integration
Advanced Delegation
Advanced Policy Operators
Health Checks
Sample Projects
```

لكن يجب أن تكون **Interfaces الخاصة بها قابلة للإضافة لاحقًا**.

---

# أول Milestone حقيقي

لا أنصح أن يكون أول هدف:

> "إنشاء جميع الجداول."

بل:

> **إثبات أن Authorization Core يستطيع اتخاذ قرار صحيح دون معرفة أي شيء عن Domain النظام.**

يجب أن نصل إلى Test مثل:

```text
Given:

Subject = User:10

Role = ProjectManager

Permission = task.update

Scope = Department:50

Graph:
Task:900
    ↓ belongs_to
Project:500
    ↓ belongs_to
Department:50

When:

User:10 requests task.update on Task:900

Then:

ALLOW
```

ثم:

```text
Task:901
    ↓
Project:700
    ↓
Department:80
```

والنتيجة:

```text
DENY
```

إذا نجح هذا السيناريو من خلال **Core + Fake Resource Graph** بدون EF Core وبدون ASP.NET وبدون Domain Entities، فهذا يعني أن أهم أساس معماري للمشروع صحيح.

بعدها نربط:

```text
Fake Graph
      ↓
Real Domain Graph Adapter
```

ثم:

```text
In-Memory Store
      ↓
EF Core Store
```

ثم:

```text
Core
      ↓
ASP.NET Core
```

وهذه الطريقة تقلل جدًا احتمال أن يتحول المشروع إلى Authorization Module مرتبط بتطبيق واحد.
