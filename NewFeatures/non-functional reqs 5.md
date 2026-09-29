# Authorization Engine

## Non-Functional Requirements & Reusability Standards

---

# 1. الهدف المعماري

يجب بناء Authorization Engine باعتباره:

> **Standalone Authorization Infrastructure Library**

وليس جزءًا من Domain معين.

يجب أن يكون بالإمكان إضافته إلى أي مشروع .NET دون أن يفترض وجود:

* Company
* Branch
* Department
* Project
* Employee
* Customer
* Invoice

أو أي Entity خاصة بالنظام.

---

# 2. Domain Agnostic

المحرك يجب ألا يعتمد مباشرة على:

```csharp
Company
Department
Project
User
Employee
```

بل يستخدم abstractions عامة:

```csharp
ResourceReference
SubjectReference
Permission
Role
Scope
Relation
Policy
```

مثال:

```csharp
new ResourceReference(
    "project",
    projectId);
```

بدل:

```csharp
new ProjectAuthorization(projectId);
```

---

# 3. عدم فرض ORM معين

يجب ألا تكون Core Library مرتبطة مباشرة بـ:

```text
EF Core
```

بل تكون الطبقات الأساسية مستقلة.

التقسيم المقترح:

```text
Authorization.Core
Authorization.Abstractions
Authorization.Application
Authorization.Infrastructure
Authorization.EntityFrameworkCore
Authorization.AspNetCore
```

بحيث:

```text
Core
    ↓
لا يعرف EF Core

EF Core Adapter
    ↓
يطبق التخزين باستخدام EF Core
```

وهذا يسمح مستقبلًا بدعم:

```text
EF Core
Dapper
MongoDB
External Authorization Service
Custom Storage
```

---

# 4. Dependency Inversion

يجب أن تعتمد Core على Interfaces وليس Implementations.

مثال:

```csharp
public interface IPermissionStore
{
    Task<IReadOnlyCollection<Permission>> GetAsync(...);
}
```

وليس:

```csharp
public class AuthorizationEngine
{
    private readonly AppDbContext _db;
}
```

المحرك لا يعرف نوع قاعدة البيانات.

---

# 5. Database Agnostic

يجب ألا يحتوي Core على SQL خاص بـ:

```text
SQL Server
MySQL
PostgreSQL
Oracle
```

وأي Database-specific behavior يجب وضعه داخل Infrastructure Adapter.

---

# 6. دعم أكثر من Database Provider

إذا كان التنفيذ باستخدام EF Core فيجب تصميمه بحيث يعمل مع أكبر قدر ممكن من providers.

مثل:

```text
SQL Server
PostgreSQL
MySQL
SQLite
```

مع تجنب Features خاصة بـ SQL Server إلا داخل Adapter مخصص.

---

# 7. عدم التحكم في DbContext الخاص بالنظام

هذه نقطة مهمة جدًا.

لا يجب أن يطلب Authorization Package من النظام:

```csharp
AppDbContext
```

بل يمكن أن يستخدم:

```csharp
AuthorizationDbContext
```

مستقلًا.

أو Interfaces تسمح للنظام باستخدام نفس قاعدة البيانات دون فرض Context واحد.

مثال:

```text
Application Database
        │
        ├── Domain Tables
        │
        └── Authorization Tables
```

أو:

```text
Application Database
        │
        └── Authorization Schema

Authorization Database
        │
        └── Authorization Tables
```

ويجب أن يدعم التصميم الخيارين.

---

# 8. Storage Isolation

يجب ألا يفترض المحرك أن Authorization Data موجودة في نفس Database الخاصة بالنظام.

يجب دعم:

```text
Same Database
Separate Database
Separate Schema
External Authorization Store
```

وهذا مهم جدًا لجعل المنتج قابلًا للتوسع.

---

# 9. Pluggable Storage

يجب أن يكون بالإمكان استبدال:

```text
Permission Store
Role Store
Assignment Store
Scope Store
Policy Store
Relationship Store
Audit Store
```

دون تعديل Authorization Engine نفسه.

---

# 10. Pluggable Resource Graph

هذه من أهم نقاط التصميم.

يجب أن يكون:

```csharp
IResourceGraphProvider
```

قابلًا للاستبدال.

مثال:

```text
Authorization Engine
        │
        ▼
IResourceGraphProvider
        │
        ├── EF Core Adapter
        ├── Domain API Adapter
        ├── Cached Graph Adapter
        └── External Graph Adapter
```

وبالتالي يمكن لكل نظام تعريف طريقة معرفة العلاقات الخاصة به.

---

# 11. عدم فرض طريقة تعريف العلاقات

قد يكون النظام يستخدم:

```csharp
Project.DepartmentId
```

ونظام آخر:

```csharp
Project.OrganizationUnitId
```

ونظام ثالث:

```text
Graph Database
```

المحرك لا يهتم.

هو يعرف فقط:

```text
Project:500
    belongs_to
Department:50
```

أما كيفية اكتشاف هذه العلاقة فهي مسؤولية Adapter.

---

# 12. Dependency Injection First-Class

يجب أن يكون النظام مصممًا للعمل مباشرة مع:

```text
Microsoft.Extensions.DependencyInjection
```

ويقدم Extension Method مثل:

```csharp
services.AddAuthorizationEngine(options =>
{
    ...
});
```

ويجب تسجيل جميع الخدمات المطلوبة تلقائيًا.

---

# 13. ASP.NET Core Integration

يفضل توفير Package منفصل:

```text
Authorization.AspNetCore
```

يدعم:

```text
Middleware
Authorization Handler
Policy Provider
Endpoint Filters
MVC Filters
Minimal APIs
```

مثال:

```csharp
[AuthorizePermission("project.update")]
```

أو:

```csharp
.RequirePermission("project.update")
```

لكن يجب ألا تكون هذه الطبقة جزءًا من Core.

---

# 14. دعم Minimal APIs وMVC

يجب ألا يفترض النظام استخدام:

```text
Controllers
```

فقط.

يجب دعم:

```text
Minimal APIs
MVC Controllers
Razor Pages
Background Services
MediatR Handlers
Application Services
```

لأن Authorization يجب أن يعمل خارج HTTP أيضًا.

---

# 15. عدم الاعتماد على HttpContext

Core Engine لا يجب أن يستخدم:

```csharp
HttpContext
```

مباشرة.

بدل ذلك يستخدم:

```csharp
AuthorizationContext
```

ويمكن للـ ASP.NET Adapter تحويل:

```text
HttpContext
      ↓
AuthorizationContext
```

---

# 16. Background Jobs

يجب أن يعمل المحرك بدون User HTTP Session.

مثال:

```text
Background Worker
     ↓
Service Account
     ↓
Authorization Engine
```

لذلك يجب دعم:

```text
User
Service Account
System Agent
Application Identity
AI Agent
```

كـ Subjects.

---

# 17. Cancellation Support

كل العمليات I/O يجب أن تدعم:

```csharp
CancellationToken
```

مثال:

```csharp
Task<AuthorizationResult> AuthorizeAsync(
    AuthorizationContext context,
    CancellationToken cancellationToken);
```

وهذا يمنع استمرار عمليات Authorization بعد إلغاء HTTP Request.

---

# 18. Async First

يجب أن تكون جميع عمليات:

```text
Database
Cache
Graph
Policy
External Services
```

Async.

ولا يجب استخدام:

```text
.Result
.Wait()
```

داخل المكتبة.

---

# 19. Thread Safety

يجب أن تكون الخدمات الأساسية آمنة للاستخدام المتزامن.

خصوصًا:

```text
AuthorizationEngine
PermissionResolver
DecisionEngine
PolicyEvaluator
Cache
```

ولا يجب تخزين State خاص بمستخدم معين داخل Singleton Service.

---

# 20. Stateless Core

يفضل أن يكون:

```text
AuthorizationEngine
```

Stateless قدر الإمكان.

أي:

```text
Request A
Request B
Request C
```

لا تعتمد على بعضها.

هذا يجعل النظام مناسبًا لـ:

```text
Horizontal Scaling
Load Balancing
Containers
Kubernetes
Serverless
```

---

# 21. Configuration Options

يجب توفير Configuration مركزية:

```csharp
AuthorizationOptions
```

مثلاً:

```text
DefaultDecision
MaxGraphDepth
EnableAudit
EnableCaching
CacheDuration
EnablePolicies
EnableDelegation
EnableOverrides
FailMode
```

---

# 22. Secure Defaults

يجب أن تكون Default Configuration آمنة.

مثلاً:

```text
Default = Deny
MaxGraphDepth = Limited
Policies = Enabled
Audit = Enabled
CrossTenant = Deny
UnknownRelation = Deny
UnknownPermission = Deny
```

ولا يجب أن يحتاج المستخدم إلى معرفة تفاصيل أمنية حتى يحصل على Configuration آمنة.

---

# 23. Fail Secure

عند حدوث خطأ في Authorization Infrastructure يجب ألا يتحول الخطأ تلقائيًا إلى:

```text
ALLOW
```

مثال:

```text
Database unavailable
        ↓
Authorization cannot be verified
        ↓
DENY
```

إلا إذا اختار النظام صراحةً Fail-Open لحالة محددة جدًا.

---

# 24. Fail Mode قابل للتهيئة

يمكن دعم:

```text
FailClosed
FailOpen
```

لكن:

```text
FailClosed
```

يجب أن يكون Default.

ويجب ألا يسمح Fail-Open إلا من خلال Configuration واضحة ومقصودة.

---

# 25. Deterministic Decisions

نفس:

```text
Subject
Action
Resource
Context
Authorization State
```

يجب أن ينتج دائمًا نفس القرار.

لا يعتمد القرار على:

```text
SQL ordering
Dictionary ordering
Thread execution order
Cache race
```

---

# 26. Explainability

يجب أن يكون النظام قادرًا على تفسير القرار.

مثال:

```text
Decision:
DENY

Reason:
SCOPE_NOT_MATCHED

Permission:
task.update

Role:
ProjectManager

Assignment:
A-123

Scope:
Department:50

Resource:
Task:900
```

لكن يجب الفصل بين:

```text
Internal Diagnostic Detail
```

و:

```text
Public API Error
```

حتى لا يتم تسريب معلومات حساسة.

---

# 27. عدم تسريب Authorization Information

لا يجب أن يرجع API للمستخدم تفاصيل مثل:

```text
وجود Role معين
وجود Permission معين
وجود Resource لا يحق له معرفته
Policy Definition
Internal IDs
```

مثلاً يمكن إرجاع:

```text
403 Forbidden
```

بدل:

```text
You don't have project.update because Role X denied you
```

إلا إذا كان النظام يطلب ذلك داخليًا.

---

# 28. Structured Reason Codes

يجب استخدام Codes ثابتة:

```text
PERMISSION_GRANTED
NO_PERMISSION
SCOPE_NOT_MATCHED
RELATION_NOT_MATCHED
POLICY_DENIED
EXPLICIT_DENY
ASSIGNMENT_EXPIRED
SUBJECT_INACTIVE
TENANT_MISMATCH
RESOURCE_NOT_FOUND
AUTHORIZATION_ERROR
```

بدل الاعتماد على نصوص قابلة للتغيير.

---

# 29. Versioning

يجب أن يكون للمكتبة Versioning واضح.

مثلاً:

```text
Authorization.Core 1.x
Authorization.Core 2.x
```

وألا تؤدي إضافة Feature جديدة إلى كسر API القديم بدون Major Version.

---

# 30. Backward Compatibility

يجب اعتبار:

```text
Public Interfaces
Configuration
Database Schema
Permission Keys
Policy DSL
```

جزءًا من Contract.

أي تغيير Breaking يجب أن يكون مقصودًا وموثقًا.

---

# 31. Policy Versioning

كل Policy يجب أن يكون لها:

```text
Version
```

مثال:

```text
Policy 15
Version 3
```

حتى نستطيع معرفة أي Version اتخذ القرار.

---

# 32. Permission Versioning

يجب تجنب تغيير معنى Permission موجودة بشكل صامت.

مثال:

```text
project.update
```

إذا تغير معناها جذريًا، يجب:

```text
Create New Permission
```

أو Version واضح.

---

# 33. Migration Safety

إذا تم تركيب Authorization Engine في نظام موجود، يجب ألا يقوم Package تلقائيًا بتعديل Domain Tables بشكل غير متوقع.

يفضل توفير:

```text
AddAuthorizationSchema()
```

ومigrations منفصلة.

---

# 34. Installation Experience

يجب أن تكون عملية التركيب بسيطة.

مثال:

```csharp
services.AddAuthorizationEngine(options =>
{
    options.UseEntityFrameworkCore(...);
});
```

ثم:

```text
Migration
Seed
Register Resources
Register Permissions
```

ويجب أن تكون خطوات الإعداد موثقة بوضوح.

---

# 35. Zero Domain Coupling

يجب ألا يحتاج النظام المستضيف إلى تعديل Entities الخاصة به إلا إذا أراد توفير معلومات للعلاقات.

مثلاً يمكن للنظام إضافة Adapter:

```csharp
public sealed class ProjectResourceGraphProvider
    : IResourceGraphProvider
{
    ...
}
```

دون تعديل Authorization Core.

---

# 36. Extensibility

أي Component رئيسي يجب أن يكون قابلًا للاستبدال:

```text
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
ICacheProvider
```

---

# 37. Plugin Architecture

يمكن لاحقًا السماح بإضافة:

```text
Custom Policy Operators
Custom Resource Resolvers
Custom Relation Providers
Custom Cache Providers
Custom Audit Providers
Custom Storage Providers
```

دون تعديل Core.

---

# 38. Policy Operator Extensibility

بالإضافة إلى Operators الأساسية:

```text
eq
neq
in
contains
gt
gte
lt
lte
```

يجب السماح بإضافة Operators من النظام المستضيف.

مثال:

```text
within_business_hours
has_clearance
owns_resource
```

لكن يجب تنفيذها من خلال Interface آمن، وليس Code مخزنًا في Database.

---

# 39. Resource Type Extensibility

لا يجب أن تكون Resource Types Enum ثابتة:

```csharp
enum ResourceType
{
    Company,
    Project,
    Task
}
```

بل تكون Dynamic:

```text
project
task
invoice
document
warehouse
anything
```

---

# 40. Action Extensibility

نفس الأمر بالنسبة إلى Actions.

لا تستخدم:

```csharp
enum Action
{
    Read,
    Update,
    Delete
}
```

فقط.

بل يمكن للنظام تعريف:

```text
read
create
update
delete
approve
publish
archive
assign
export
close
```

وأي Action آخر.

---

# 41. Naming Conventions

يجب وضع Naming Convention موحد.

مثال:

```text
resource.action
```

مثل:

```text
project.read
project.update
task.delete
invoice.approve
report.export
feature.reports.view
```

ويجب توثيق Convention ومنع التعارض.

---

# 42. Localization Independence

Authorization Engine لا يجب أن يعتمد على لغة معينة.

لا تخزن Decision Logic على أساس:

```text
"المستخدم مدير"
```

بل:

```text
role.key = project_manager
```

أما الاسم الظاهر:

```text
مدير المشاريع
Project Manager
```

فيكون مسؤولية النظام المستضيف.

---

# 43. Time Handling

يجب استخدام:

```text
UTC
```

داخل Authorization Engine.

خصوصًا:

```text
ValidFrom
ValidUntil
CreatedAt
ExpiresAt
Timestamp
```

أما Time Zone فيكون مسؤولية Application Layer.

---

# 44. Clock Abstraction

لا تستخدم:

```csharp
DateTime.UtcNow
```

مباشرة في كل مكان.

يفضل:

```csharp
IClock
```

أو abstraction مشابه.

هذا يسهل:

```text
Testing
Expiration
Delegation
Time-based Policies
```

---

# 45. ID Abstraction

لا يجب إجبار جميع الأنظمة على:

```text
Guid
```

إذا كان الهدف دعم أنظمة متعددة.

يمكن تصميم:

```text
ResourceId
```

بحيث يدعم:

```text
Guid
String
Long
```

أو اعتماد Guid كـ Contract داخلي مع Adapter واضح إذا كان ذلك قرارًا مقصودًا.

---

# 46. Serialization Stability

أي Object يستخدم في:

```text
Policy
Audit
Cache
Events
```

يجب أن تكون Serialization الخاصة به مستقرة وقابلة للترقية.

يجب عدم الاعتماد على Binary Serialization غير الآمن.

---

# 47. Cache Consistency

Cache Authorization يجب ألا يؤدي إلى Grant غير صحيح بسبب بيانات قديمة.

عند تغيير:

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

يجب أن توجد استراتيجية واضحة لـ:

```text
Invalidation
Versioning
TTL
```

---

# 48. Distributed Cache Safety

في بيئة متعددة الخوادم يجب أن يكون Cache Key فريدًا.

مثال:

```text
auth:
{tenant}:
{subject}:
{action}:
{resourceType}:
{resourceId}:
{policyVersion}
```

ويجب منع أي إمكانية لخلط Cache بين Tenants.

---

# 49. Observability

يجب توفير:

```text
Logs
Metrics
Tracing
Health Checks
```

بدون ربط Core بمكتبة Logging محددة.

يفضل الاعتماد على abstractions مثل:

```text
ILogger
ActivitySource
Meter
```

عند التكامل مع .NET.

---

# 50. Health Checks

يجب توفير Health Checks اختيارية لـ:

```text
Authorization Database
Distributed Cache
External Graph Provider
Policy Provider
```

لكن لا يجب أن تكون كل هذه Dependencies إجبارية.

---

# 51. Metrics

يجب توفير Metrics مثل:

```text
authorization_requests_total
authorization_allowed_total
authorization_denied_total
authorization_duration
authorization_cache_hits
authorization_cache_misses
authorization_graph_depth
authorization_policy_duration
```

مع Tags آمنة لا تسبب Cardinality عالية جدًا.

---

# 52. Performance Isolation

يجب ألا يؤدي Authorization إلى تعطيل النظام بسبب Graph أو Policy بطيء.

يجب دعم:

```text
Timeout
Cancellation
Depth Limit
Query Limit
Policy Limit
```

---

# 53. No N+1 Authorization

يجب توفير APIs تسمح مستقبلًا بفحص مجموعة موارد دفعة واحدة.

مثال:

```csharp
AuthorizeManyAsync(...)
```

بدل:

```text
100 Resources
↓
100 Authorization Queries
```

---

# 54. Bulk Authorization

يجب دعم حالات مثل:

```text
هل يستطيع المستخدم قراءة هذه الـ 100 Projects؟
```

ويفضل إرجاع:

```text
ResourceId
Allowed
Reason
```

بعملية محسنة.

---

# 55. Query Authorization

في الأنظمة الكبيرة يجب دعم نمط:

```text
GetAuthorizedResourcesAsync(...)
```

بدل جلب جميع الموارد ثم تنفيذ:

```text
Authorize(resource)
```

واحدًا واحدًا.

مثال:

```text
SELECT Projects
WHERE User has access
```

ويجب أن يكون هذا Adapter-specific وليس جزءًا من Core Decision Engine.

---

# 56. Data Filtering vs Authorization

يجب التفريق بين:

```text
Can Access Resource
```

و:

```text
Filter Collection
```

مثال:

```text
User can read Projects
لكن فقط Projects داخل Department:50
```

يجب توفير abstraction مستقبلية لـ:

```text
IAuthorizationFilterProvider
```

لتحويل قواعد Authorization إلى Query Filters عندما يكون ذلك ممكنًا.

---

# 57. Security Against IDOR

كل API يتعامل مع:

```text
/resource/{id}
```

يجب أن يقوم Authorization على المورد الفعلي.

لا يكفي:

```text
User authenticated
```

بل:

```text
User authenticated
+
Permission
+
Scope
+
Relationship
+
Policy
```

---

# 58. Security Against Privilege Escalation

يجب منع المستخدم من منح نفسه أو غيره صلاحيات أعلى من صلاحياته.

مثال:

```text
User A
```

لديه:

```text
authorization.assignment.create
```

لكن يجب أن يتحقق النظام أيضًا من:

```text
هل يملك User A صلاحية منح هذا Role؟
هل يستطيع الوصول إلى هذا Scope؟
هل يستطيع منح هذه Permissions؟
```

---

# 59. Meta-Authorization

إدارة Authorization نفسها يجب أن تمر عبر Authorization Engine.

مثال:

```text
Can User A assign Role X to User B on Resource Y?
```

يتم فحص:

```text
authorization.assignment.create
+
Role X management authority
+
Scope Y authority
```

وهذا يمنع وجود "God Operation" مخفية داخل Admin API.

---

# 60. Secure Audit

Audit Logs يجب أن تكون:

```text
Append-oriented
Tamper-resistant
Tenant isolated
Traceable
```

ويجب تسجيل:

```text
Who
What
When
Where
Against What
Result
Reason
Correlation
```

---

# 61. Sensitive Data Protection

لا يجب تسجيل:

```text
Passwords
Tokens
Secrets
Full Policy Secrets
Sensitive Personal Data
```

في Authorization Audit.

ويجب توفير Redaction للـ Context.

---

# 62. Exception Handling

يجب ألا تنتشر Exceptions الداخلية إلى المستخدم النهائي.

يجب الفصل بين:

```text
AuthorizationException
StorageException
PolicyEvaluationException
GraphResolutionException
ConfigurationException
```

والـ API Response.

---

# 63. Configuration Validation

عند تشغيل النظام يجب اكتشاف أخطاء Configuration مبكرًا.

مثل:

```text
Missing Permission Store
Invalid Policy
Invalid Relation
Invalid Scope Rule
Duplicate Permission Key
Invalid Cache Configuration
```

ويفضل اكتشافها أثناء:

```text
Application Startup
```

بدل ظهورها أول مرة أثناء Request حقيقي.

---

# 64. Seed / Bootstrap

يجب توفير طريقة منظمة لتسجيل:

```text
Resource Types
Actions
Permissions
Features
Relations
System Policies
```

مثال:

```csharp
services.AddAuthorizationEngine(...)
```

ثم:

```csharp
AuthorizationSeeder
```

ويجب أن يكون Seed:

```text
Idempotent
Versioned
Safe
```

---

# 65. No Hardcoded System Roles

يمنع داخل Package تعريف أدوار مثل:

```text
Admin
Manager
CompanyManager
Employee
```

إلا إذا كانت مجرد Samples خارج Core.

المحرك يقدم:

```text
Permissions
Roles
Assignments
```

كـ Dynamic Data.

---

# 66. System Permissions

يمكن للمكتبة تعريف Permissions خاصة بها مثل:

```text
authorization.role.manage
authorization.permission.manage
authorization.assignment.manage
authorization.policy.manage
```

لكن يجب ألا يتم فرض Role معين لامتلاكها.

---

# 67. Package Separation

التقسيم المقترح:

```text
Authorization.Core
Authorization.Abstractions
Authorization.Application
Authorization.Infrastructure
Authorization.EntityFrameworkCore
Authorization.AspNetCore
Authorization.Caching
Authorization.Testing
```

اختياريًا:

```text
Authorization.Redis
Authorization.OpenTelemetry
```

---

# 68. Core Package

يحتوي فقط على:

```text
Domain-independent Models
Value Objects
Authorization Rules
Decision Engine
Policy Contracts
Interfaces
Exceptions
```

ولا يحتوي:

```text
EF Core
ASP.NET Core
Redis
SQL
HttpContext
```

---

# 69. EF Core Package

يحتوي:

```text
DbContext
Entity Configurations
Repositories
Migrations
EF-specific Graph Providers
```

---

# 70. ASP.NET Core Package

يحتوي:

```text
Authorize Attributes
Endpoint Extensions
Policy Provider
Authorization Handlers
HttpContext Adapter
Middleware
```

---

# 71. Testing Package

يجب توفير Utilities تساعد النظام المستضيف على كتابة Tests.

مثال:

```csharp
AuthorizationTestBuilder
```

يسمح بإنشاء سيناريو:

```text
Given User
And Role
And Permission
And Scope
When Action
Then Allow
```

---

# 72. Contract Tests

كل Adapter يجب أن يمر بمجموعة Contract Tests مشتركة.

مثلاً:

```text
EF Permission Store
Redis Permission Store
Custom Permission Store
```

يجب أن تحقق جميعها نفس Contract.

---

# 73. Documentation

يجب أن تكون المكتبة موثقة على عدة مستويات:

```text
Getting Started
Architecture
Installation
Configuration
Database
Permissions
Roles
Scopes
Relationships
Policies
ASP.NET Integration
Background Jobs
Caching
Performance
Security
Testing
Migration
Troubleshooting
```

---

# 74. Samples

يجب توفير مشاريع Sample مستقلة:

```text
Sample.Basic
Sample.MultiTenant
Sample.AspNetCore
Sample.MinimalApi
Sample.EFCore
Sample.ReBAC
Sample.Policy
Sample.BackgroundService
```

---

# 75. API Stability

يجب تقليل Public API قدر الإمكان.

كل Class يتم جعله:

```csharp
internal
```

ما لم تكن هناك حاجة حقيقية ليكون:

```csharp
public
```

كلما قل Public API:

```text
Less Breaking Changes
Easier Maintenance
Better Versioning
```

---

# 76. No Leaky Abstractions

لا يجب أن يظهر:

```text
EF Core Entity
DbContext
SQL
Redis
HttpContext
```

في Core APIs.

---

# 77. Testability

كل Component يجب أن يكون قابلًا للاختبار بدون:

```text
Real Database
Real Redis
Real HTTP Request
```

مثلاً:

```text
FakeResourceGraphProvider
FakePermissionStore
FakePolicyEvaluator
```

---

# 78. Deterministic Policy Testing

كل Policy يجب أن يمكن اختبارها باستخدام:

```text
Input Context
+
Resource Data
+
Subject Data
```

وتحصل على:

```text
Same Input
→
Same Result
```

---

# 79. Load Testing

يجب إجراء Load Tests لحالات:

```text
100 concurrent users
1,000 concurrent users
10,000 concurrent authorization requests
```

مع قياس:

```text
Latency
CPU
Memory
Database Load
Cache Load
```

الأرقام النهائية تحدد بعد Benchmark على البيئة المستهدفة.

---

# 80. Memory Management

يجب الحذر من:

```text
Large Graphs
Large Policy Definitions
Large Permission Sets
Large Authorization Contexts
```

ويجب استخدام:

```text
Pagination
Limits
Streaming
Projection
Caching
```

حيث يلزم.

---

# 81. Resource Graph Cycle Protection

إذا كان Graph:

```text
A → B
B → C
C → A
```

يجب ألا يؤدي إلى Infinite Traversal.

يجب استخدام:

```text
Visited Set
+
MaxDepth
```

---

# 82. Policy Complexity Limits

يجب تحديد حدود مثل:

```text
Maximum expression depth
Maximum conditions
Maximum nested operators
Maximum evaluation time
```

لحماية النظام من Policies مكلفة أو غير صحيحة.

---

# 83. Authorization Timeout

يجب أن يكون هناك Timeout واضح.

مثال:

```text
Authorization Timeout = 100ms
```

والقيمة النهائية يتم تحديدها بناءً على Benchmark.

عند تجاوز الحد:

```text
Fail Closed
```

بشكل افتراضي.

---

# 84. Cache Stampede Protection

عند انتهاء Cache لمورد يستخدم بكثرة، يجب منع:

```text
100 Requests
      ↓
100 Database Queries
```

ويفضل استخدام:

```text
Single Flight
Request Coalescing
Distributed Lock
```

بحسب الحاجة.

---

# 85. Security Boundary

يجب اعتبار Authorization Engine:

```text
Security Boundary
```

وليس مجرد Helper Library.

أي خطأ في:

```text
Scope
Relationship
Policy
Cache
Tenant
Decision
```

قد يؤدي إلى Unauthorized Access.

لذلك يجب التعامل مع كل تغيير في Authorization Logic كـ Security-sensitive Change.

---

# 86. Versioned Database Schema

يجب أن يكون Authorization Schema مستقلًا وقابلًا للترقية.

مثال:

```text
Authorization Schema v1
Authorization Schema v2
```

ويجب ألا تعتمد Migration على Domain Migrations الخاصة بالنظام المستضيف.

---

# 87. Safe Upgrade

ترقية Package من:

```text
1.5 → 1.6
```

يجب ألا تؤدي إلى تغيير الصلاحيات الحالية بشكل صامت.

أي Migration تؤثر على:

```text
Permissions
Policies
Assignments
Scopes
```

يجب أن تكون واضحة وقابلة للمراجعة.

---

# 88. Rollback Strategy

يجب وضع خطة للتراجع عن:

```text
Package Upgrade
Database Migration
Policy Version
Permission Changes
```

خصوصًا في Production.

---

# 89. Security Review Before Release

قبل إصدار أي Version يجب مراجعة:

```text
Authentication Boundary
Authorization Boundary
Tenant Isolation
IDOR
Privilege Escalation
Scope Bypass
Policy Bypass
Cache Leakage
Race Conditions
Injection
Serialization
Audit Integrity
```

---

# 90. Final Quality Attributes

يجب اعتبار الصفات التالية أهدافًا أساسية للمشروع:

```text
                    Authorization Engine
                            │
       ┌────────────────────┼────────────────────┐
       │                    │                    │
       ▼                    ▼                    ▼
   Flexibility          Reliability           Security
       │                    │                    │
       ▼                    ▼                    ▼
 Domain Agnostic       Fail Secure          Tenant Isolation
 Pluggable             Deterministic        IDOR Protection
 Extensible             Testable             Least Privilege
 Provider Agnostic      Observable           Audit
       │                    │                    │
       └────────────────────┼────────────────────┘
                            ▼
                     Reusability
                            │
             ┌──────────────┼──────────────┐
             ▼              ▼              ▼
           .NET           EF Core       ASP.NET
         Integration     Optional       Optional
```

---

# 91. معيار النجاح النهائي

يعتبر Authorization Engine جاهزًا لإعادة الاستخدام عندما يستطيع مطور أخذ Package وإضافتها إلى نظام .NET جديد دون تغيير Core.

مثال:

```text
Existing .NET System
        │
        ├── Existing DbContext
        ├── Existing Users
        ├── Existing Domain Entities
        └── Existing Authentication
                  │
                  ▼
          Authorization Package
                  │
        ┌─────────┼─────────┐
        ▼         ▼         ▼
      RBAC      Scopes    ReBAC
        │         │         │
        └─────────┼─────────┘
                  ▼
               Policies
                  │
                  ▼
             Final Decision
```

ويحتاج النظام المستضيف فقط إلى توفير Adapters المطلوبة:

```text
ISubjectResolver
IResourceGraphProvider
IResourceDataProvider
```

ثم يستطيع استخدام المحرك.

---

# 92. المبدأ الأهم

يجب أن يكون التصميم قائمًا على:

```text
Core = Authorization Logic
Adapters = Integration
Domain = Business Logic
Infrastructure = Storage
```

ولا يجب عكس العلاقة.

أي:

```text
❌ Domain → Authorization internals

❌ Authorization Core → Domain Entities

❌ Authorization Core → EF Core

❌ Authorization Core → ASP.NET

❌ Authorization Core → Redis
```

بينما التصميم الصحيح:

```text
                    Authorization Core
                           ▲
                           │
             ┌─────────────┼─────────────┐
             │             │             │
             │             │             │
        EF Adapter    ASP.NET Adapter   Redis
             ▲             ▲
             │             │
       Application      Application
             │
          Domain
```

---

# 93. النتيجة

الهدف ليس فقط بناء:

```text
RBAC Library
```

بل بناء:

```text
Generic Authorization Platform
```

تستطيع التعامل مع:

```text
RBAC
+
Scopes
+
ReBAC
+
Policies
+
Overrides
+
Delegation
+
Feature Authorization
+
Data Authorization
+
Multi-Tenancy
+
Audit
```

مع المحافظة على:

```text
Domain Independence
Database Independence
ORM Independence
ASP.NET Independence
Provider Extensibility
Security
Performance
Testability
Observability
Backward Compatibility
```

وبذلك يكون المنتج قابلًا لأن يصبح **مكتبة .NET مستقلة / NuGet Package** يمكن إدخالها إلى أنظمة مختلفة بدل أن تكون Authorization Module مرتبطة بنظام واحد.
