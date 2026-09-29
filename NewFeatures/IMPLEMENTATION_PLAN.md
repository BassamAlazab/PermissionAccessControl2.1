# خطة تنفيذ محرك الصلاحيات — Authorization Engine

## Generic Fine-Grained Authorization Engine for .NET

---

# ملخص المشروع

بناء **محرك صلاحيات عام ومرن (Domain-Agnostic)** يعمل كـ **مكتبة .NET مستقلة** قابلة لإعادة الاستخدام في أي نظام (ERP, CRM, SaaS, HR, مستشفيات, جامعات, إلخ)، يجمع بين:

```text
RBAC + Scope-Based + ReBAC + ABAC + Policy-Based Authorization
+ Feature Authorization + Field-Level Authorization
+ Delegation + Overrides + Multi-Tenancy + Audit
```

---

# المستندات المرجعية

| # | الملف | المحتوى |
|---|-------|---------|
| 1 | `updates 1.md` | الرؤية والمبادئ الأساسية — 44 قسمًا يغطي الهدف، المبدأ، الموارد، الإجراءات، الصلاحيات، الأدوار، النطاقات، العلاقات، السياسات، التفويض، الأمان، الأداء |
| 2 | `efmigration 2.md` | إعدادات EF Core — 48 قسمًا (66–113) يغطي Configurations لجميع الكيانات، القيود، الفهارس، Delete Behaviors، Concurrency، Transactions، بنية قاعدة البيانات |
| 3 | `DESIGN 3.MD` | التصميم المعماري — 65 قسمًا يغطي النموذج العام، الكيانات، ERD، الخوارزمية، Architecture، Data/Feature/Field Authorization، أمثلة شاملة |
| 4 | `finalreq 4.md` | المتطلبات النهائية — 42 قسمًا يغطي Resource Graph Provider, Resolvers, Policy Engine, Decision Engine, Caching, Testing, Performance, Security |
| 5 | `non-functional reqs 5.md` | المتطلبات غير الوظيفية — 93 قسمًا يغطي Domain/DB/ORM Independence, Pluggability, DI, ASP.NET Integration, Security, Observability, Extensibility, Packaging |
| 6 | `stages 6.md` | أولويات التنفيذ — P0 إلى P6 مع ترتيب صارم للأولويات وتحديد Milestone الأول |

---

# بنية المشاريع (Solution Structure)

```text
Authorization/
├── src/
│   ├── Authorization.Abstractions/       ← Interfaces + Value Objects فقط
│   ├── Authorization.Core/               ← Decision Engine + Policy Engine + Resolvers
│   ├── Authorization.Application/        ← Application Services + Commands/Queries
│   ├── Authorization.Infrastructure/     ← Storage Abstractions
│   ├── Authorization.EntityFrameworkCore/ ← EF Core Adapter + DbContext + Configurations + Migrations
│   ├── Authorization.AspNetCore/         ← ASP.NET Integration (Middleware, Filters, Attributes)
│   ├── Authorization.Caching/            ← Cache Providers (Memory + Distributed)
│   └── Authorization.Testing/            ← Test Builders + Fakes + Contract Tests
│
├── tests/
│   ├── Authorization.Core.Tests/         ← Unit Tests
│   ├── Authorization.EFCore.Tests/       ← Integration Tests
│   ├── Authorization.AspNetCore.Tests/   ← Integration Tests
│   └── Authorization.Security.Tests/     ← Security Tests
│
├── samples/
│   ├── Sample.Basic/
│   ├── Sample.AspNetCore/
│   ├── Sample.MinimalApi/
│   ├── Sample.MultiTenant/
│   ├── Sample.ReBAC/
│   └── Sample.Policy/
│
└── benchmarks/
    └── Authorization.Benchmarks/
```

---

# هيكل قاعدة البيانات النهائي

```text
Core Tables:
├── Subjects
├── ResourceTypes
├── ActionDefinitions
├── RelationDefinitions
│
├── Roles
├── Permissions
├── RolePermissions
│
├── Features
│
├── AuthorizationScopes
├── ScopeInheritanceRules
│
├── RoleAssignments
├── PermissionAssignments
│
├── ResourceRelationships
│
├── AuthorizationPolicies
├── PermissionOverrides
│
├── Delegations
├── DelegationPermissions
│
└── AuthorizationAuditLogs
```

---

# المراحل التنفيذية

---

## المرحلة P0 — الأساس الحرج (Foundation)

> **الأولوية: قصوى — لا يُبنى شيء فوقها قبل استقرارها**

### 🎯 الهدف
إثبات أن Authorization Core يستطيع اتخاذ قرار صحيح **دون معرفة أي شيء عن Domain النظام**.

### المهام

#### P0.1 — إنشاء مشاريع الحل (Solution)
- [ ] إنشاء Solution بالبنية المحددة أعلاه
- [ ] تكوين `Authorization.Abstractions` بدون أي reference إلى EF Core أو ASP.NET
- [ ] تكوين `Authorization.Core` بـ reference إلى Abstractions فقط

#### P0.2 — Core Value Objects
- [ ] `ResourceReference(string Type, Guid Id)`
- [ ] `SubjectReference(string Type, Guid Id)`
- [ ] `AuthorizationContext` (TenantId, SubjectId, SubjectType, Action, Resource, IpAddress, DeviceId, IsMfaAuthenticated, Timestamp)
- [ ] `AuthorizationResult` (Allowed, ReasonCode, PermissionId?, RoleId?, AssignmentId?, ScopeId?, PolicyId?)
- [ ] `PermissionEffect` enum (Allow = 1, Deny = 2)
- [ ] `ScopeInheritanceMode` enum (Exact, Children, Descendants, Ancestors, Related, Custom)

#### P0.3 — Core Interfaces (Public Contracts)
- [ ] `IAuthorizationService` — نقطة الدخول الرئيسية
- [ ] `IPermissionResolver` — اكتشاف الصلاحيات المحتملة
- [ ] `IAssignmentResolver` — تحديد التعيينات الفعالة
- [ ] `IScopeResolver` — فحص النطاقات
- [ ] `IRelationshipResolver` — فحص العلاقات (ReBAC)
- [ ] `IResourceGraphProvider` — معرفة العلاقات بين الموارد
- [ ] `IPolicyEvaluator` — تقييم السياسات
- [ ] `IOverrideResolver` — الاستثناءات
- [ ] `IDelegationResolver` — التفويض
- [ ] `IDecisionEngine` — القرار النهائي
- [ ] `IAuthorizationAuditService` — التسجيل

#### P0.4 — Storage Interfaces
- [ ] `IPermissionStore`
- [ ] `IRoleStore`
- [ ] `IAssignmentStore`
- [ ] `IScopeStore`
- [ ] `IRelationshipStore`
- [ ] `IPolicyStore`
- [ ] `IOverrideStore`
- [ ] `IDelegationStore`
- [ ] `IAuditStore`

#### P0.5 — Default Deny + Fail Closed
- [ ] تنفيذ `IDecisionEngine` بقاعدة Default Deny
- [ ] تحديد Decision Precedence الصريح:
  ```text
  Tenant Boundary → Subject Validation → Assignment Validity
  → Resource-Level Explicit Deny → Policy Deny → Scoped Deny
  → Explicit Allow → Scoped Allow → Default Deny
  ```
- [ ] ضمان أن أي خطأ في Infrastructure يُنتج DENY (Fail Closed)

#### P0.6 — Tenant Isolation
- [ ] تصميم `TenantId` في جميع الكيانات من البداية
- [ ] `ITenantResolver` — فحص Tenant مبكرًا
- [ ] ضمان ألا يصل Subject إلى بيانات Tenant آخر

#### P0.7 — Security Boundary
- [ ] Fail Closed
- [ ] Tenant Validation
- [ ] Subject Validation
- [ ] Resource Validation
- [ ] `CancellationToken` في جميع العمليات الـ I/O
- [ ] جميع العمليات `async`

#### P0.8 — Deterministic Decision Engine
- [ ] عدم الاعتماد على ترتيب SQL أو LINQ أو Cache
- [ ] Priority صريح في كل مكان
- [ ] نفس المدخلات = نفس النتيجة دائمًا

#### P0.9 — Clock Abstraction
- [ ] `IClock` interface بدل `DateTime.UtcNow`
- [ ] استخدام UTC في جميع التواريخ

### ✅ Milestone P0
```text
Given:
  Subject = User:10, Role = ProjectManager
  Permission = task.update, Scope = Department:50
  Graph: Task:900 → Project:500 → Department:50
When:
  User:10 requests task.update on Task:900
Then: ALLOW

When:
  User:10 requests task.update on Task:901
  (Task:901 → Project:700 → Department:80)
Then: DENY
```

**يجب أن ينجح هذا السيناريو باستخدام `Core + FakeResourceGraph` بدون EF Core وبدون ASP.NET.**

---

## المرحلة P1 — محرك التفويض الأساسي (Authorization Core)

> **بناء المحرك الفعلي بعد تثبيت الأساس**

### المهام

#### P1.1 — Permission Model
- [ ] Entity: `Permission` (Id, TenantId, Key, Name, Description, ResourceType, Action, PermissionType, IsSystem, IsActive, CreatedAt, UpdatedAt, RowVersion)
- [ ] `PermissionType`: Feature, Resource, Field, System
- [ ] Key normalization: `key.Trim().ToLowerInvariant()`
- [ ] Entity: `ResourceType` (Id, TenantId, Key, Name, IsSystem, IsActive)
- [ ] Entity: `ActionDefinition` (Id, TenantId, Key, Name, IsActive)
- [ ] Permission Key = `ResourceType.Action` (e.g. `project.update`)
- [ ] Unique: `TenantId + Key`
- [ ] التحقق من وجود ResourceType و Action قبل إنشاء Permission

#### P1.2 — Role Model
- [ ] Entity: `Role` (Id, TenantId, Name, Description, IsSystem, IsActive, CreatedAt, UpdatedAt, RowVersion)
- [ ] Unique: `TenantId + Name`
- [ ] Entity: `RolePermission` (RoleId, PermissionId, Effect)
- [ ] Composite PK: `(RoleId, PermissionId)` — لا حاجة لـ Id
- [ ] دعم Allow / Deny في RolePermission

#### P1.3 — Subject Model
- [ ] Entity: `Subject` (Id, TenantId, SubjectType, ExternalId, DisplayName, IsActive, CreatedAt, UpdatedAt)
- [ ] SubjectType: User, Team, Group, ServiceAccount, SystemAgent
- [ ] Unique: `TenantId + SubjectType + ExternalId`

#### P1.4 — Assignment Model
- [ ] Entity: `RoleAssignment` (Id, TenantId, SubjectId, RoleId, ScopeId, Priority, ValidFrom, ValidUntil, IsActive, CreatedAt, CreatedBy, RowVersion)
- [ ] Entity: `PermissionAssignment` (Id, TenantId, SubjectId, PermissionId, ScopeId, Effect, ValidFrom, ValidUntil, CreatedAt, CreatedBy)
- [ ] فحص `ValidFrom <= ValidUntil` في كل مكان
- [ ] فحص `now >= ValidFrom && (ValidUntil == null || now <= ValidUntil)` أثناء Authorize
- [ ] Index: `(TenantId, SubjectId, IsActive)` و `(TenantId, SubjectId, RoleId, ScopeId)`

#### P1.5 — Scope Model
- [ ] Entity: `AuthorizationScope` (Id, TenantId, Name, ResourceType, ResourceId, InheritanceMode, IsActive, CreatedAt, UpdatedAt, RowVersion)
- [ ] Scope = `ResourceType + ResourceId` (وليس CompanyId أو DepartmentId)
- [ ] Entity: `ScopeInheritanceRule` (Id, ScopeId, SourceResourceType, Relation, TargetResourceType, Allowed)
- [ ] Unique: `(ScopeId, SourceResourceType, Relation, TargetResourceType)`
- [ ] أوضاع الوراثة: Exact, Children, Descendants, Ancestors, Related, Custom

#### P1.6 — Resource Graph
- [ ] Entity: `ResourceRelationship` (Id, TenantId, SourceType, SourceId, Relation, TargetType, TargetId, ExpiresAt, CreatedAt)
- [ ] Unique: `(TenantId, SourceType, SourceId, Relation, TargetType, TargetId)`
- [ ] لا يوجد FK — Logical Reference (polymorphic)
- [ ] Check Constraint: منع Self Reference
- [ ] Cycle Detection في Application Layer
- [ ] Entity: `RelationDefinition` (Id, TenantId, SourceType, Relation, TargetType, IsDirectional, IsActive)
- [ ] Unique: `(TenantId, SourceType, Relation, TargetType)`
- [ ] التحقق من وجود RelationDefinition قبل إنشاء Relationship
- [ ] تنفيذ `IResourceGraphProvider`:
  - `GetRelationsAsync()`
  - `GetParentsAsync()`
  - `GetChildrenAsync()`
  - `HasRelationAsync()`
- [ ] MaxDepth + Cycle Detection + Relation Allowlist + Tenant Isolation + Timeout

#### P1.7 — Scope Resolver
- [ ] تنفيذ `IScopeResolver` — هل Resource X داخل Scope Y?
- [ ] Traversal عبر Resource Graph حسب ScopeInheritanceRules
- [ ] دعم جميع أوضاع الوراثة

#### P1.8 — Relationship Resolver
- [ ] تنفيذ `IRelationshipResolver`
- [ ] دعم: `member_of`, `manager_of`, `belongs_to`, `contains`, وأي علاقة مخصصة
- [ ] الجمع بين Scope و Relationship في القرار

#### P1.9 — Permission Resolver
- [ ] تنفيذ `IPermissionResolver`
- [ ] Subject → Assignments → Roles → RolePermissions → Direct Permissions → Scopes
- [ ] إنتاج Candidate Permissions

#### P1.10 — Assignment Resolver
- [ ] تنفيذ `IAssignmentResolver`
- [ ] مراعاة: IsActive, ValidFrom, ValidUntil, TenantId, SubjectId, Priority

#### P1.11 — Authorization Engine (Pipeline)
```text
Authorize
├── Validate Context
├── Validate Tenant
├── Validate Subject
├── Resolve Permissions
├── Resolve Assignments
├── Resolve Scopes
├── Resolve Relationships
├── Evaluate Policies (stub في هذه المرحلة)
├── Resolve Overrides (stub)
├── Decision Engine
└── Audit Decision
```

### ✅ Milestone P1
- السيناريوهات من P0 تعمل مع بيانات حقيقية (In-Memory Store)
- RBAC + Scope + ReBAC يعمل بشكل مستقر
- جميع Reason Codes تعمل:
  ```text
  PERMISSION_GRANTED, NO_PERMISSION, SCOPE_NOT_MATCHED,
  RELATION_NOT_MATCHED, ASSIGNMENT_EXPIRED, SUBJECT_INACTIVE,
  TENANT_MISMATCH, RESOURCE_NOT_FOUND
  ```

---

## المرحلة P2 — السياسات والصلاحيات المتقدمة (Policy & Advanced)

> **بعد استقرار RBAC + Scope + ReBAC**

### المهام

#### P2.1 — Policy Engine
- [ ] Entity: `AuthorizationPolicy` (Id, TenantId, Name, Description, Effect, Priority, DefinitionJson, Version, IsActive, CreatedAt, UpdatedAt, RowVersion)
- [ ] Unique: `(TenantId, Name, Version)`
- [ ] تنفيذ `IPolicyEvaluator`
- [ ] Policy DSL آمن (JSON-based):
  ```json
  {
    "all": [
      { "fact": "resource.status", "operator": "neq", "value": "posted" },
      { "fact": "resource.amount", "operator": "lte", "valueFrom": "subject.approvalLimit" }
    ]
  }
  ```
- [ ] دعم Operators: eq, neq, in, not_in, gt, gte, lt, lte, contains, starts_with, exists
- [ ] دعم Logical Operators: all, any, not

#### P2.2 — Policy Validation
- [ ] JSON Schema Validation عند إنشاء Policy
- [ ] Semantic Validation (Unknown Fact, Operator, Field)
- [ ] رفض Policy غير صالحة قبل التخزين
- [ ] حدود التعقيد: Maximum Depth, Maximum Conditions

#### P2.3 — Policy Versioning
- [ ] Policy + Version + IsActive
- [ ] تسجيل أي Version اتخذ القرار
- [ ] دعم Rollback للإصدار السابق

#### P2.4 — Permission Overrides
- [ ] Entity: `PermissionOverride` (Id, TenantId, SubjectId, PermissionId, ResourceType, ResourceId, Effect, Priority, Reason, ValidFrom, ValidUntil, CreatedAt, CreatedBy, RowVersion)
- [ ] تنفيذ `IOverrideResolver`
- [ ] دعم Resource-specific Allow / Deny
- [ ] Priority واضح
- [ ] Index: `(TenantId, SubjectId, PermissionId, ResourceType, ResourceId)`

#### P2.5 — Delegation
- [ ] Entity: `Delegation` (Id, TenantId, DelegatorSubjectId, DelegateeSubjectId, ScopeId, ValidFrom, ValidUntil, IsActive, CreatedAt)
- [ ] Entity: `DelegationPermission` (DelegationId, PermissionId, Effect)
- [ ] Composite PK: `(DelegationId, PermissionId)`
- [ ] تنفيذ `IDelegationResolver`
- [ ] القاعدة: `Delegated Permissions ⊆ Delegator Permissions`
- [ ] التحقق من صلاحية التفويض الزمنية

#### P2.6 — Feature Authorization
- [ ] Entity: `Feature` (Id, TenantId, Key, Name, Description, ParentFeatureId, IsActive, CreatedAt, UpdatedAt)
- [ ] دعم شجرة Features (Parent → Children)
- [ ] Delete Behavior: Restrict (منع حذف Feature أب)
- [ ] Unique: `(TenantId, Key)`
- [ ] `feature.reports.view`, `feature.invoices.create`, etc.
- [ ] الفصل بين Feature Access و Data Access

#### P2.7 — Field-Level Authorization
- [ ] Permissions مثل: `invoice.cost_price.read`, `employee.salary.read`
- [ ] آلية فحص حقل محدد عبر Permission Key

#### P2.8 — Explainable Authorization
- [ ] `ExplainAsync()` — تفسير القرار بالتفصيل
- [ ] فصل Internal Diagnostic Detail عن Public API Error
- [ ] عدم تسريب معلومات حساسة للمستخدم النهائي

### ✅ Milestone P2
- Policy Engine يعمل مع DSL آمن
- Overrides تعمل (Resource-specific Allow/Deny)
- Delegation يعمل مع قيود زمنية
- Feature Authorization يعمل مع الفصل عن Data Authorization
- `ExplainAsync()` يعطي تفسيرًا كاملًا للقرار

---

## المرحلة P3 — طبقة التكامل (Integration Layer)

> **جعل المكتبة قابلة للتركيب في أي مشروع .NET**

### المهام

#### P3.1 — EF Core Adapter
- [ ] إنشاء `Authorization.EntityFrameworkCore`
- [ ] `AuthorizationDbContext` مع جميع DbSets
- [ ] `ApplyConfigurationsFromAssembly()` لتطبيق IEntityTypeConfiguration لكل Entity
- [ ] جميع Entity Configurations حسب ملف `efmigration 2.md`:
  - SubjectConfiguration
  - RoleConfiguration
  - PermissionConfiguration
  - RolePermissionConfiguration
  - FeatureConfiguration
  - AuthorizationScopeConfiguration
  - ScopeInheritanceRuleConfiguration
  - RoleAssignmentConfiguration
  - PermissionAssignmentConfiguration
  - ResourceRelationshipConfiguration
  - RelationDefinitionConfiguration
  - PolicyConfiguration
  - PermissionOverrideConfiguration
  - DelegationConfiguration
  - DelegationPermissionConfiguration
  - AuthorizationAuditLogConfiguration
  - ResourceTypeConfiguration
- [ ] Common Conventions: Guid PKs, UTC timestamps, MaxLength on indexed strings
- [ ] Delete Behaviors:
  - Cascade: Role→RolePermissions, Scope→ScopeInheritanceRules, Delegation→DelegationPermissions
  - Restrict: Role→RoleAssignments, Permission→RolePermissions, Permission→PermissionAssignments, Scope→Assignments
- [ ] Check Constraints: ValidFrom ≤ ValidUntil, No Self Reference
- [ ] RowVersion (Optimistic Concurrency) على: Roles, Policies, Permissions, Scopes, Assignments
- [ ] Database Migration
- [ ] دعم: SQL Server, PostgreSQL, SQLite كحد أدنى

#### P3.2 — Storage Implementations
- [ ] تنفيذ جميع Store Interfaces باستخدام EF Core
- [ ] التأكد أن Core يعمل مع أي تنفيذ بديل

#### P3.3 — Storage Isolation
- [ ] دعم Same Database, Separate Database, Separate Schema
- [ ] Authorization Schema مستقل عن Domain Tables
- [ ] لا يتحكم الـ Package في Domain DbContext

#### P3.4 — Global Tenant Filtering
- [ ] `ITenantEntity` interface
- [ ] الحذر من Global Query Filters عند وجود SuperAdmin/CrossTenant
- [ ] Tenant Isolation جزء من Authorization Engine نفسه

#### P3.5 — ASP.NET Core Integration
- [ ] إنشاء `Authorization.AspNetCore`
- [ ] `[AuthorizePermission("project.update")]` Attribute
- [ ] `.RequirePermission("project.update")` Extension
- [ ] Endpoint Filters / MVC Filters
- [ ] Middleware
- [ ] Authorization Handler
- [ ] HttpContext → AuthorizationContext Adapter
- [ ] لا اعتماد على HttpContext في Core

#### P3.6 — DI Registration
- [ ] `services.AddAuthorizationEngine(options => { ... })`
- [ ] `options.UseEntityFrameworkCore(...)`
- [ ] تسجيل تلقائي لجميع الخدمات
- [ ] `AuthorizationOptions`: DefaultDecision, MaxGraphDepth, EnableAudit, EnableCaching, EnablePolicies, EnableDelegation, EnableOverrides, FailMode

#### P3.7 — Authentication Integration
- [ ] لا يبني المحرك نظام Authentication
- [ ] يستقبل Identity ويحولها إلى SubjectReference
- [ ] دعم: ASP.NET Identity, JWT, Keycloak, Auth0, Custom

#### P3.8 — Seed / Bootstrap
- [ ] `AuthorizationSeeder` لتسجيل: ResourceTypes, Actions, Permissions, Features, Relations, System Policies
- [ ] Seed: Idempotent + Versioned + Safe
- [ ] System Permissions: `authorization.role.manage`, `authorization.assignment.manage`, إلخ

### ✅ Milestone P3
- المحرك يعمل داخل ASP.NET Core API حقيقي
- EF Core + SQL Server Migration كاملة
- DI Registration بسطر واحد
- `[AuthorizePermission]` يعمل على Controllers و Minimal APIs

---

## المرحلة P4 — الجاهزية للإنتاج (Production Hardening)

> **بعد استقرار الوظائف والتكامل**

### المهام

#### P4.1 — Caching
- [ ] `ICacheProvider` interface
- [ ] L1: Memory Cache (IMemoryCache)
- [ ] L2: Distributed Cache (IDistributedCache)
- [ ] اختياريًا: Redis Adapter
- [ ] Cache Keys: `auth:{tenant}:{subject}:{action}:{resourceType}:{resourceId}:{policyVersion}`
- [ ] منع خلط Cache بين Tenants

#### P4.2 — Cache Invalidation
- [ ] ربط التغييرات بـ invalidation: Role, Permission, Assignment, Scope, Policy, Override, Delegation, Relationship
- [ ] Versioning + TTL strategy
- [ ] Cache Stampede Protection (Single Flight / Request Coalescing)

#### P4.3 — Performance Optimization
- [ ] Batch Queries (منع N+1)
- [ ] Projection (عدم تحميل بيانات غير مطلوبة)
- [ ] Compiled Queries عند الإمكان
- [ ] Graph Optimization

#### P4.4 — Bulk Authorization
- [ ] `AuthorizeManyAsync()` — فحص مجموعة موارد دفعة واحدة
- [ ] `GetAuthorizedResourcesAsync()` — إرجاع الموارد المسموحة فقط

#### P4.5 — Data Query Authorization
- [ ] `IDataAuthorizationService.ApplyScope<T>(query, subjectId, permission)`
- [ ] `IAuthorizationFilterProvider` — تحويل Authorization Rules إلى Query Filters
- [ ] الهدف: Authorized Query على مستوى Database وليس Filter في Memory

#### P4.6 — Graph Optimization (اختياري)
- [ ] Closure Table
- [ ] Materialized Path
- [ ] Resource Ancestors
- [ ] لتسريع `IsDescendantOf()` عند كبر البيانات

#### P4.7 — Transaction Rules
- [ ] عمليات Assignment و Role كـ Atomic Transactions
- [ ] `Create Role → Add Permissions → Create Assignment → Audit → Invalidate Cache` — إما تنجح كاملة أو تفشل

#### P4.8 — Authorization Timeout
- [ ] Authorization Timeout configurable (default ~100ms)
- [ ] Fail Closed عند تجاوز الحد

### ✅ Milestone P4
- أداء مقبول: Cached < 5ms, DB-backed < 20-50ms
- Cache يعمل مع invalidation صحيح
- Bulk Authorization متاح
- Data Query Authorization يعمل على مستوى DB

---

## المرحلة P5 — المراقبة والحوكمة (Observability & Governance)

### المهام

#### P5.1 — Audit
- [ ] Entity: `AuthorizationAuditLog` (Id, TenantId, SubjectId, Action, ResourceType, ResourceId, Decision, ReasonCode, PolicyId, CorrelationId, CreatedAt)
- [ ] تنفيذ `IAuthorizationAuditService`
- [ ] تسجيل: Allow, Deny, Role Changes, Permission Changes, Assignment Changes, Policy Changes
- [ ] Audit Logs: Append-oriented, Tamper-resistant, Tenant isolated
- [ ] Indexes: `(TenantId, SubjectId, CreatedAt)`, `(TenantId, ResourceType, ResourceId, CreatedAt)`, `(TenantId, Decision, CreatedAt)`

#### P5.2 — Structured Logging
- [ ] استخدام `ILogger` (لا فرض Logging Framework خاص)
- [ ] Structured Reason Codes ثابتة:
  ```text
  PERMISSION_GRANTED, NO_PERMISSION, SCOPE_NOT_MATCHED,
  RELATION_NOT_MATCHED, POLICY_DENIED, EXPLICIT_DENY,
  ASSIGNMENT_EXPIRED, SUBJECT_INACTIVE, TENANT_MISMATCH,
  RESOURCE_NOT_FOUND, AUTHORIZATION_ERROR
  ```

#### P5.3 — Metrics
- [ ] استخدام `Meter` (.NET Metrics API)
- [ ] Metrics:
  ```text
  authorization_requests_total
  authorization_allowed_total, authorization_denied_total
  authorization_duration, authorization_cache_hits/misses
  authorization_graph_depth, authorization_policy_duration
  ```

#### P5.4 — Distributed Tracing
- [ ] استخدام `ActivitySource`
- [ ] تتبع: HTTP Request → Authorization → Database → Graph → Policy

#### P5.5 — Health Checks
- [ ] Health Checks اختيارية: Database, Cache, External Providers

#### P5.6 — Sensitive Data Protection
- [ ] عدم تسجيل: Passwords, Tokens, Secrets
- [ ] Redaction للـ Context
- [ ] فصل Internal Diagnostic عن Public API Error

### ✅ Milestone P5
- Audit كامل لجميع القرارات والتغييرات
- Metrics و Tracing يعملان مع OpenTelemetry
- Health Checks متاحة

---

## المرحلة P6 — الجودة والنضج (Quality & Ecosystem)

> **تجعل المنتج ناضجًا كمكتبة عامة قابلة لإعادة الاستخدام**

### المهام

#### P6.1 — Unit Tests
- [ ] اختبار كل Component بشكل مستقل:
  - DecisionEngine, ScopeResolver, PermissionResolver
  - RelationshipResolver, PolicyEvaluator, OverrideResolver
- [ ] استخدام Fakes: FakeResourceGraphProvider, FakePermissionStore, FakePolicyEvaluator

#### P6.2 — Integration Tests
- [ ] اختبار التكامل مع: EF Core, SQL Server, Domain Database, Cache

#### P6.3 — Authorization Scenario Tests
- [ ] سيناريوهات شاملة:
  ```text
  User has direct permission, User has role permission
  Role has scoped permission, Scope includes/excludes resource
  Explicit deny, Explicit allow, Policy deny/allow
  Expired assignment, Future assignment, Delegation
  Cross-tenant access, Inactive user/role/scope
  ```

#### P6.4 — Security Tests
- [ ] اختبارات أمنية:
  ```text
  Tenant Escape, IDOR, Privilege Escalation
  Horizontal/Vertical Access, Scope Bypass
  Relationship Bypass, Policy Bypass
  Expired Assignment, Cache Leakage
  Cross-Tenant Cache Collision
  ```

#### P6.5 — Authorization Test Matrix

| Permission | Role | Scope    | Relationship | Policy | Override | Result |
|-----------|------|----------|-------------|--------|----------|--------|
| Allow     | Yes  | Match   | -           | Pass   | -        | Allow  |
| Allow     | Yes  | No Match| -           | Pass   | -        | Deny   |
| -         | -    | -       | Match       | Pass   | -        | Allow  |
| Allow     | Yes  | Match   | -           | Fail   | -        | Deny   |
| Allow     | Yes  | Match   | -           | Pass   | Deny     | Deny   |
| -         | -    | -       | -           | -      | -        | Deny   |

#### P6.6 — Contract Tests
- [ ] كل Provider يمر بنفس Contract Tests
- [ ] EF Permission Store = Custom Store = External Store → نفس السلوك

#### P6.7 — Performance Benchmarks
- [ ] BenchmarkDotNet:
  ```text
  Simple Authorization, Role Authorization
  Scoped Authorization, ReBAC, Policy Authorization
  Cached Authorization, Complex Graph
  ```
- [ ] أهداف: Cached < 5ms, DB-backed < 20-50ms

#### P6.8 — Load Tests
- [ ] 100 / 1,000 / 10,000 concurrent requests
- [ ] قياس: Latency, CPU, Memory, Database Load, Cache Load

#### P6.9 — Testing Package
- [ ] `Authorization.Testing` مع `AuthorizationTestBuilder`:
  ```text
  Given User → And Role → And Permission → And Scope
  → When Action → Then Allow/Deny
  ```

#### P6.10 — Documentation
- [ ] Getting Started, Architecture, Installation, Configuration
- [ ] Database, Roles, Permissions, Scopes, Relationships, Policies
- [ ] ASP.NET Integration, Background Jobs, Caching, Performance
- [ ] Security, Testing, Migration, Troubleshooting

#### P6.11 — Sample Projects
- [ ] Sample.Basic, Sample.AspNetCore, Sample.MinimalApi
- [ ] Sample.MultiTenant, Sample.ReBAC, Sample.Policy, Sample.BackgroundService

#### P6.12 — Package Versioning
- [ ] Semantic Versioning
- [ ] فصل: Core Version, Database Schema Version, Policy Version
- [ ] Safe Upgrade: عدم تغيير الصلاحيات بشكل صامت
- [ ] Rollback Strategy

### ✅ Milestone P6
- تغطية اختبارية شاملة (Unit + Integration + Security + Performance)
- توثيق كامل
- مشاريع Sample جاهزة
- المكتبة جاهزة كـ NuGet Package

---

# ما يجب تنفيذه من اليوم الأول (لا يُؤجَّل)

```text
 1. Domain Independence
 2. Dependency Inversion
 3. Tenant Isolation
 4. Default Deny
 5. Fail Closed
 6. Deterministic Decision
 7. ResourceType + ResourceId (Generic)
 8. Resource Graph Abstraction
 9. CancellationToken
10. Async
11. Testability
12. Public API Design
13. Versioning Strategy
14. Security Boundary
```

> ⚠️ إذا تم تأجيل هذه الأشياء، قد تضطر لإعادة بناء Core بالكامل لاحقًا.

---

# ما يمكن تأجيله

```text
Redis, Distributed Cache, Closure Tables
Bulk Authorization, Query Authorization
OpenTelemetry Integration, Advanced Delegation
Advanced Policy Operators, Health Checks
Sample Projects
```

> لكن يجب أن تكون **Interfaces الخاصة بها** قابلة للإضافة لاحقًا.

---

# خوارزمية Authorization النهائية

```text
Request (Subject + Action + Resource + Context)
    │
    ▼
Validate Context
    │
    ▼
Tenant Isolation ─── DENY if mismatch
    │
    ▼
Resolve Permissions ─── DENY if none found
    │
    ▼
Resolve Assignments ─── DENY if no valid assignment
    │
    ▼
Resolve Scopes ─── DENY if out of scope
    │
    ▼
Resolve Relationships (ReBAC)
    │
    ▼
Evaluate Policies ─── DENY if policy fails
    │
    ▼
Resolve Overrides ─── Explicit Deny wins
    │
    ▼
Decision Engine
    │
    ├── ALLOW
    └── DENY
    │
    ▼
Record Audit
```

---

# الهيكل المعماري النهائي

```text
                    ┌─────────────────────┐
                    │   Application/API   │
                    └──────────┬──────────┘
                               │
                               ▼
                    ┌─────────────────────┐
                    │ IAuthorizationService│
                    └──────────┬──────────┘
                               │
                               ▼
                    ┌─────────────────────┐
                    │ AuthorizationEngine │
                    └──────────┬──────────┘
                               │
         ┌─────────────────────┼─────────────────────┐
         │                     │                     │
         ▼                     ▼                     ▼
Permission Resolver   Assignment Resolver    Scope Resolver
         │                     │                     │
         │                     │                     ▼
         │                     │          Resource Graph Provider
         │                     │                     │
         └─────────────────────┼─────────────────────┘
                               │
                               ▼
                    Relationship Resolver
                               │
                               ▼
                       Policy Evaluator
                               │
                               ▼
                       Override Resolver
                               │
                               ▼
                       Decision Engine
                               │
                  ┌────────────┴────────────┐
                  ▼                         ▼
               ALLOW                      DENY
                  │                         │
                  └────────────┬────────────┘
                               ▼
                    Authorization Audit
```

---

# المعيار النهائي للنجاح

> يعتبر Authorization Engine جاهزًا عندما يستطيع مطور أخذ Package وإضافتها إلى نظام .NET جديد **دون تغيير Core**.

يحتاج النظام المستضيف فقط إلى:

```text
1. ISubjectResolver        ← من يطلب الصلاحية
2. IResourceGraphProvider  ← ما هي العلاقات بين الموارد
3. IResourceDataProvider   ← كيف يتم جلب بيانات الموارد للـ Policies
```

ثم يستطيع استخدام المحرك الكامل في:

```text
ERP, CRM, Project Management, HR, Accounting
Multi-tenant SaaS, Government Systems
Universities, Hospitals, Internal Admin Platforms
```

**دون إعادة كتابة Authorization Engine.**

---

> 📅 تاريخ إنشاء الخطة: 2026-09-29
>
> 📌 هذه الخطة مبنية على تحليل كامل للملفات: updates 1, efmigration 2, DESIGN 3, finalreq 4, non-functional reqs 5, stages 6
