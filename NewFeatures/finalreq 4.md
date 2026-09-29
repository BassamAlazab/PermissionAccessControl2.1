# Authorization Engine — Remaining Requirements

## 1. الهدف

استكمال بناء محرك Authorization عام وقابل لإعادة الاستخدام، يعمل فوق نموذج:

* RBAC
* Scope-Based Authorization
* ReBAC
* Policy-Based Authorization
* Resource-Level Authorization
* Feature Authorization
* Multi-Tenant Authorization

يجب ألا يحتوي المحرك على أي افتراضات خاصة بمجال معين مثل:

* Company
* Branch
* Department
* Project
* Task
* Employee

بل يتعامل مع جميع الموارد بصورة عامة من خلال:

```text
ResourceType + ResourceId
```

---

# 2. Resource Graph Provider

## الهدف

توفير طبقة مسؤولة عن معرفة العلاقات بين الموارد داخل النظام، بحيث يستطيع Authorization Engine معرفة موقع المورد وعلاقاته دون معرفة تفاصيل Domain Models.

مثال:

```text
Department:50
    │
    ├── contains → Project:500
    │                 │
    │                 └── contains → Task:900
    │
    └── contains → Project:501
```

إذا كان المستخدم يمتلك Scope على:

```text
Department:50
```

يمكن للمحرك تحديد أن:

```text
Task:900
```

داخل هذا النطاق.

---

## 2.1 Interface

يجب إنشاء abstraction مشابه لـ:

```csharp
public interface IResourceGraphProvider
{
    Task<IReadOnlyCollection<ResourceRelation>> GetRelationsAsync(
        ResourceReference resource,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ResourceReference>> GetParentsAsync(
        ResourceReference resource,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ResourceReference>> GetChildrenAsync(
        ResourceReference resource,
        CancellationToken cancellationToken = default);

    Task<bool> HasRelationAsync(
        ResourceReference source,
        string relation,
        ResourceReference target,
        CancellationToken cancellationToken = default);
}
```

---

# 3. Resource Relationship Resolution

يجب دعم نوعين من العلاقات:

## 3.1 Domain Relationships

العلاقات الموجودة أصلًا في قاعدة بيانات النظام.

مثال:

```text
Project.DepartmentId
Task.ProjectId
Invoice.CustomerId
```

لا يجب نسخ هذه العلاقات بالكامل إلى Authorization Database.

بل يجب أن يستطيع:

```text
IResourceGraphProvider
```

قراءة العلاقات من Domain Layer.

---

## 3.2 Authorization Relationships

العلاقات الخاصة بالصلاحيات والتي لا تمثل بالضرورة علاقة Domain.

مثال:

```text
User:10
    └── reviewer_of → Document:500

User:20
    └── manager_of → Department:50
```

هذه يمكن تخزينها في:

```text
ResourceRelationships
```

---

# 4. Relation Definitions

يجب تعريف العلاقات المسموح بها.

مثال:

```text
department
    contains
        project

project
    contains
        task

user
    member_of
        project

user
    manager_of
        department
```

يجب أن يستطيع النظام التحقق من أن العلاقة المطلوبة معرفة مسبقًا.

يمنع إنشاء علاقات عشوائية غير معرفة.

---

# 5. Scope Resolver

## الهدف

تحديد ما إذا كان Resource معين يقع داخل Scope ممنوح للمستخدم.

مثال:

```text
Role Assignment

Subject:
User:10

Role:
ProjectManager

Scope:
Department:50
```

الطلب:

```text
Action:
task.update

Resource:
Task:900
```

يقوم المحرك بتحليل:

```text
Task:900
    ↓
Project:500
    ↓
Department:50
```

ثم:

```text
Task:900 ∈ Scope(Department:50)
```

وبالتالي يصبح الـ Scope صالحًا.

---

# 6. Scope Inheritance

يجب دعم عدة أوضاع:

```text
Exact
Children
Descendants
Ancestors
Related
Custom
```

مثال:

```text
Scope = Department:50
Mode = Descendants
```

يعني أن الصلاحية يمكن أن تنتقل إلى الموارد الموجودة أسفل هذا المورد وفقًا لقواعد العلاقات.

---

# 7. Scope Traversal Rules

يجب ألا يسمح المحرك بالسير في Graph بشكل مفتوح.

كل Scope يجب أن يحدد العلاقات المسموح traversal من خلالها.

مثال:

```text
Department
    ↓ contains
Project
    ↓ contains
Task
```

لكن لا يسمح تلقائيًا:

```text
Department
    ↓ unrelated_relation
User
```

إلا إذا كانت العلاقة معرفة في:

```text
ScopeInheritanceRules
```

---

# 8. Graph Traversal Safety

يجب تطبيق:

* Maximum traversal depth
* Cycle detection
* Relation allowlist
* Tenant isolation
* Resource type validation
* Timeout / cancellation
* Query limits

مثال:

```text
MaxDepth = 10
```

حتى لا يؤدي Graph غير صحيح إلى Loop لا نهائي.

---

# 9. Permission Resolver

إنشاء:

```csharp
IPermissionResolver
```

وظيفته اكتشاف جميع الصلاحيات المحتملة للمستخدم بالنسبة إلى الطلب.

يبحث في:

```text
Direct Permission Assignments
        ↓
Role Assignments
        ↓
Roles
        ↓
Role Permissions
        ↓
Scopes
        ↓
Relationships
```

مثال:

```text
User:10

Role:
Project Manager

Permissions:
project.read
project.update
task.read
task.update
```

---

# 10. Assignment Resolver

إنشاء:

```csharp
IAssignmentResolver
```

وظيفته تحديد الـ Assignments الفعالة حاليًا.

يجب مراعاة:

```text
IsActive
ValidFrom
ValidUntil
TenantId
SubjectId
RoleId
ScopeId
Priority
```

مثال:

```text
RoleAssignment

User:10
Role:ProjectManager
Scope:Project:500
ValidFrom: 2026-09-01
ValidUntil: 2026-12-01
```

لا يكون هذا Assignment فعالًا خارج الفترة الزمنية المحددة.

---

# 11. Relationship-Based Authorization

يجب دعم الصلاحيات التي تعتمد على علاقة المستخدم بالمورد.

مثال:

```text
User:10
    member_of
Project:500
```

يمكن بناء Permission:

```text
project.update
```

بحيث يتم السماح بها فقط إذا:

```text
subject member_of resource
```

مثال آخر:

```text
User:10
manager_of
Department:50
```

يمكن أن تمنحه صلاحيات مرتبطة بالـ Department.

---

# 12. Feature Authorization

يجب الفصل بين:

```text
Feature Authorization
```

و:

```text
Data Authorization
```

مثال:

```text
Feature:
Reports

Permission:
feature.reports.view
```

يمكن للمستخدم امتلاك:

```text
feature.reports.view
```

لكن لا يعني ذلك أنه يستطيع قراءة جميع التقارير.

يمكن أن يكون لديه:

```text
report.read
Scope = Department:50
```

وبالتالي:

```text
Feature Access = Yes
Data Access = Limited
```

---

# 13. Policy Engine

إنشاء:

```csharp
IPolicyEvaluator
```

وظيفته تقييم الشروط الإضافية المرتبطة بالعملية.

مثال:

```json
{
  "all": [
    {
      "fact": "subject.isMfaAuthenticated",
      "operator": "eq",
      "value": true
    },
    {
      "fact": "resource.status",
      "operator": "neq",
      "value": "Closed"
    }
  ]
}
```

---

# 14. Policy DSL

يجب عدم السماح بتخزين وتنفيذ C# أو Code ديناميكي من قاعدة البيانات.

يستخدم النظام DSL آمن مثل:

```json
{
  "all": [
    {
      "fact": "subject.departmentId",
      "operator": "eq",
      "resource": "departmentId"
    }
  ]
}
```

يدعم Operators مثل:

```text
eq
neq
in
not_in
gt
gte
lt
lte
contains
starts_with
exists
```

مع Logical Operators:

```text
all
any
not
```

---

# 15. Authorization Context

يجب أن يحتوي الطلب على Context موحد.

```csharp
public sealed record AuthorizationContext
{
    public Guid TenantId { get; init; }

    public Guid SubjectId { get; init; }

    public string SubjectType { get; init; } = "User";

    public string Action { get; init; } = null!;

    public ResourceReference Resource { get; init; } = null!;

    public string? IpAddress { get; init; }

    public string? DeviceId { get; init; }

    public bool IsMfaAuthenticated { get; init; }

    public DateTime Timestamp { get; init; }
}
```

---

# 16. Override Resolver

إنشاء:

```csharp
IOverrideResolver
```

لدعم الاستثناءات الخاصة بمورد محدد.

مثال:

المستخدم لديه:

```text
project.update
Scope = Department:50
```

لكن تم منعه من:

```text
Project:500
```

من خلال:

```text
PermissionOverride
```

فيكون القرار:

```text
Department permission = Allow
Project override = Deny

Final = Deny
```

---

# 17. Delegation

يجب دعم تفويض الصلاحيات مؤقتًا.

مثال:

```text
Manager A
    ↓ delegates
Manager B
    ↓
Project:500
```

مع:

```text
ValidFrom
ValidUntil
Permissions
Scope
```

مثال:

```text
Delegate:
User:20

Scope:
Project:500

Permissions:
task.read
task.update

ValidUntil:
2026-10-01
```

بعد انتهاء الفترة يصبح التفويض غير فعال تلقائيًا.

---

# 18. Decision Engine

يجب إنشاء مكون مستقل:

```csharp
IDecisionEngine
```

ولا يجب أن يكون اتخاذ القرار موزعًا داخل عدة Services.

يدخل إليه:

```text
Permissions
Assignments
Scopes
Relationships
Policies
Overrides
```

ويخرج:

```text
Allow / Deny
```

مع سبب القرار.

---

# 19. Decision Precedence

يجب تحديد ترتيب الأولوية بشكل رسمي وثابت.

المبدأ الأساسي:

```text
Tenant Boundary
        ↓
Subject Validation
        ↓
Assignment Validity
        ↓
Resource-Level Explicit Deny
        ↓
Policy Deny
        ↓
Scoped Deny
        ↓
Explicit Allow
        ↓
Scoped Allow
        ↓
Default Deny
```

يجب عدم الاعتماد على:

* ترتيب نتائج SQL
* ترتيب الإدخالات في DB
* ترتيب الـ LINQ
* ترتيب تحميل العلاقات

بل تكون الأولوية جزءًا صريحًا من `DecisionEngine`.

---

# 20. Default Deny

أي طلب لا توجد له صلاحية صريحة ومطابقة يجب أن تكون نتيجته:

```text
DENY
```

مثال:

```text
No Permission
    ↓
DENY
```

و:

```text
Permission Exists
Scope Does Not Match
    ↓
DENY
```

و:

```text
Permission Exists
Policy Fails
    ↓
DENY
```

---

# 21. Authorization Result

يجب ألا يرجع النظام `bool` فقط.

يفضل:

```csharp
public sealed class AuthorizationResult
{
    public bool Allowed { get; init; }

    public string ReasonCode { get; init; } = null!;

    public Guid? PermissionId { get; init; }

    public Guid? RoleId { get; init; }

    public Guid? AssignmentId { get; init; }

    public Guid? ScopeId { get; init; }

    public Guid? PolicyId { get; init; }
}
```

أمثلة:

```text
PERMISSION_GRANTED
NO_PERMISSION
SCOPE_NOT_MATCHED
POLICY_DENIED
EXPLICIT_DENY
ASSIGNMENT_EXPIRED
TENANT_MISMATCH
SUBJECT_INACTIVE
RESOURCE_NOT_FOUND
```

---

# 22. Authorization Service

الواجهة النهائية:

```csharp
public interface IAuthorizationService
{
    Task<AuthorizationResult> AuthorizeAsync(
        AuthorizationContext context,
        CancellationToken cancellationToken = default);

    Task<bool> CanAsync(
        Guid subjectId,
        string action,
        ResourceReference resource,
        CancellationToken cancellationToken = default);
}
```

---

# 23. Authorize Pipeline

يجب أن يكون التنفيذ بهذا الشكل:

```text
Authorize
   │
   ├── Validate Context
   │
   ├── Validate Tenant
   │
   ├── Validate Subject
   │
   ├── Resolve Permissions
   │
   ├── Resolve Assignments
   │
   ├── Resolve Scopes
   │
   ├── Resolve Relationships
   │
   ├── Evaluate Policies
   │
   ├── Resolve Overrides
   │
   ├── Decision Engine
   │
   └── Audit Decision
```

---

# 24. Authorization Pipeline Architecture

```text
IAuthorizationService
        │
        ▼
AuthorizationEngine
        │
        ├── ISubjectResolver
        ├── IPermissionResolver
        ├── IAssignmentResolver
        ├── IScopeResolver
        ├── IResourceGraphProvider
        ├── IRelationshipResolver
        ├── IPolicyEvaluator
        ├── IOverrideResolver
        ├── IDelegationResolver
        ├── IDecisionEngine
        └── IAuthorizationAuditService
```

كل component مسؤول عن وظيفة واحدة فقط.

---

# 25. Caching

يجب دعم caching للأجزاء التي لا تتغير بشكل مستمر.

يمكن تخزين:

```text
Role Permissions
Permission Definitions
Resource Type Definitions
Relation Definitions
Active Assignments
Scope Rules
Policies
```

لكن يجب تجنب Cache غير صحيح للصلاحيات.

عند تغيير:

```text
Role
Permission
Assignment
Scope
Policy
Override
Delegation
```

يجب invalidation للـ cache المرتبط.

---

# 26. Cache Strategy

يفضل استخدام:

```text
L1:
Memory Cache

L2:
Distributed Cache
```

في الأنظمة متعددة الخوادم.

يمكن استخدام:

```text
Redis
```

كـ Distributed Cache.

يجب أن تحتوي Cache Keys على:

```text
TenantId
SubjectId
ResourceType
ResourceId
Action
```

عند الحاجة.

---

# 27. Performance

يجب تجنب:

```text
N+1 Queries
```

خصوصًا أثناء:

```text
Scope Traversal
Role Resolution
Relationship Resolution
```

ويفضل:

```text
Batch Queries
Projection
Compiled Queries
Caching
Graph Materialization
```

عند الحاجة.

---

# 28. Resource Graph Optimization

عند كبر حجم البيانات يمكن استخدام:

```text
Closure Table
```

أو:

```text
Materialized Path
```

أو:

```text
Resource Ancestors
```

لتسريع:

```text
IsDescendantOf()
```

بدل إجراء Graph Traversal في كل Authorization Request.

---

# 29. Multi-Tenant Isolation

يجب تطبيق Tenant Isolation في جميع المراحل.

كل Query Authorization يجب أن يحتوي على:

```text
TenantId
```

ولا يسمح لأي Subject بالوصول إلى:

```text
Resource
Assignment
Role
Permission
Policy
Scope
Relationship
```

تابع لـ Tenant مختلف.

Tenant validation يجب أن يحدث مبكرًا قبل إجراء عمليات Graph Traversal المكلفة.

---

# 30. Security of Authorization Configuration

تعديل نظام الصلاحيات نفسه يجب أن يخضع للصلاحيات.

مثال:

```text
authorization.role.create
authorization.role.update
authorization.role.delete

authorization.permission.grant
authorization.permission.revoke

authorization.assignment.create
authorization.assignment.delete

authorization.policy.manage

authorization.scope.manage
```

ولا يجوز أن تكون إدارة Authorization مفتوحة تلقائيًا لأي مستخدم.

---

# 31. Audit Logging

يجب تسجيل العمليات الحساسة مثل:

```text
Role Created
Role Updated
Permission Granted
Permission Revoked
Assignment Created
Assignment Removed
Scope Changed
Policy Changed
Override Created
Delegation Created
```

وكذلك قرارات Authorization المهمة:

```text
ALLOW
DENY
```

مع:

```text
Subject
Action
Resource
Tenant
Reason
Policy
Role
Scope
CorrelationId
Timestamp
```

---

# 32. Authorization Audit

مثال:

```text
Subject:
User:10

Action:
task.update

Resource:
Task:900

Decision:
DENY

Reason:
EXPLICIT_DENY

Role:
ProjectManager

Scope:
Department:50

Policy:
Policy:123

CorrelationId:
...
```

---

# 33. Transaction Rules

يجب أن تكون العمليات المتعلقة بالصلاحيات Atomic.

مثال عند إنشاء Role:

```text
Create Role
    ↓
Assign Permissions
    ↓
Commit
```

إذا فشلت إحدى العمليات:

```text
Rollback
```

ولا يجب ترك Role في حالة غير مكتملة.

نفس المبدأ ينطبق على:

```text
Role Assignment
Scope Assignment
Policy Changes
Delegation
Overrides
```

---

# 34. Concurrency

يجب دعم Optimistic Concurrency في الجداول الحساسة.

مثال:

```text
Roles
Policies
Scopes
Assignments
Overrides
```

باستخدام:

```text
RowVersion
```

لمنع قيام مستخدمين بتعديل Authorization Configuration بشكل متعارض دون اكتشاف ذلك.

---

# 35. Testing Strategy

يجب بناء Tests على عدة مستويات.

## Unit Tests

اختبار:

```text
PermissionResolver
ScopeResolver
RelationshipResolver
PolicyEvaluator
DecisionEngine
```

بشكل مستقل.

---

## Integration Tests

اختبار التكامل مع:

```text
EF Core
SQL Server
Domain Database
Resource Graph
Cache
```

---

## Authorization Scenario Tests

يجب اختبار سيناريوهات مثل:

```text
User has direct permission
User has role permission
Role has scoped permission
Scope includes resource
Scope does not include resource
Explicit deny
Explicit allow
Policy deny
Policy allow
Expired assignment
Future assignment
Delegation
Cross-tenant access
Inactive user
Inactive role
Inactive scope
```

---

# 36. Security Tests

اختبار:

```text
Tenant Escape
IDOR
Privilege Escalation
Horizontal Access
Vertical Access
Scope Bypass
Relationship Bypass
Policy Bypass
Expired Assignment
Cache Leakage
Cross-Tenant Cache Collision
```

---

# 37. Authorization Test Matrix

يجب إنشاء Matrix تغطي:

| Permission | Role | Scope    | Relationship | Policy | Override | Result |
| ---------- | ---- | -------- | ------------ | ------ | -------- | ------ |
| Allow      | Yes  | Match    | -            | Pass   | -        | Allow  |
| Allow      | Yes  | No Match | -            | Pass   | -        | Deny   |
| -          | -    | -        | Match        | Pass   | -        | Allow  |
| Allow      | Yes  | Match    | -            | Fail   | -        | Deny   |
| Allow      | Yes  | Match    | -            | Pass   | Deny     | Deny   |
| -          | -    | -        | -            | -      | -        | Deny   |

يجب توسيع هذه المصفوفة أثناء تنفيذ الاختبارات.

---

# 38. Observability

يجب دعم:

```text
Structured Logging
Metrics
Tracing
Correlation ID
```

ومراقبة:

```text
Authorization latency
Cache hit rate
Denied requests
Policy evaluation time
Graph traversal depth
Database query count
```

مع عدم تسجيل معلومات حساسة غير ضرورية.

---

# 39. Performance Targets

يجب تحديد أهداف أداء قبل Production.

مثال مبدئي:

```text
Simple cached authorization:
< 5ms

Database-backed authorization:
< 20-50ms

Complex graph authorization:
يتم قياسه حسب عمق Graph وحجم البيانات
```

الأرقام النهائية يجب تحديدها من Benchmark فعلي للنظام، وليس اعتبارها ضمانًا عامًا.

---

# 40. Final Architecture

الشكل النهائي للمحرك:

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
          ┌────────────────────┼────────────────────┐
          │                    │                    │
          ▼                    ▼                    ▼
 Permission Resolver    Assignment Resolver   Scope Resolver
          │                    │                    │
          │                    │                    ▼
          │                    │          Resource Graph Provider
          │                    │                    │
          └────────────────────┼────────────────────┘
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

# 41. النتيجة المطلوبة

بعد تنفيذ هذه المراحل يجب أن يصبح المحرك قادرًا على التعامل مع حالات مثل:

```text
User
  ↓
Role
  ↓
Permission
  ↓
Scope
  ↓
Resource Graph
  ↓
Relationship
  ↓
Policy
  ↓
Override
  ↓
Final Decision
```

دون أن يحتوي Authorization Engine على أي معرفة خاصة بـ:

```text
Company
Branch
Department
Project
Task
Invoice
Customer
Employee
```

وبالتالي يمكن استخدام نفس المحرك في:

```text
ERP
CRM
Project Management
HR
Accounting
Multi-tenant SaaS
Government Systems
Internal Admin Platforms
```

مع تغيير الـ Domain Resources والعلاقات فقط.

---

# 42. المرحلة التالية بعد إكمال هذه المتطلبات

بعد الانتهاء من التصميم النظري، تكون الخطوة العملية التالية هي تنفيذ المحرك بهذا الترتيب:

```text
1. Core Authorization Entities
2. EF Core Configurations
3. Database Migration
4. Resource Type Registry
5. Action/Permission Registry
6. Relation Registry
7. Resource Graph Provider
8. Scope Resolver
9. Assignment Resolver
10. Permission Resolver
11. Relationship Resolver
12. Policy Engine
13. Override Resolver
14. Decision Engine
15. AuthorizationService
16. Caching
17. Audit
18. Unit Tests
19. Integration Tests
20. Security Tests
21. Performance Benchmarks
```

ويجب اعتبار:

```text
AuthorizationEngine
```

طبقة مستقلة وقابلة لإعادة الاستخدام، بينما يكون تكاملها مع Domain Application من خلال Interfaces وAdapters، وليس من خلال ربطها مباشرةً بـ Entities الخاصة بالنظام.
