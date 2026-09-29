# نظام التحكم والصلاحيات المرن

## Generic Fine-Grained Authorization Engine

## 1. الهدف من النظام

يهدف النظام إلى بناء **محرك تحكم وصلاحيات عام ومرن** يمكن استخدامه داخل مختلف أنواع الأنظمة الإدارية والمؤسسية، دون افتراض هيكل تنظيمي ثابت مثل:

* شركة ← فروع ← أقسام ← مشاريع
* مجموعة شركات ← شركات ← فروع
* جامعة ← كليات ← أقسام ← مقررات
* مستشفى ← إدارات ← أقسام ← فرق
* مؤسسة حكومية ← قطاعات ← إدارات ← أقسام
* أو أي هيكل تنظيمي آخر.

لا يعتمد النظام على مجموعة أدوار ثابتة مثل:

```text
Admin
Manager
Employee
Accountant
Supervisor
```

بل يسمح بإنشاء الأدوار والصلاحيات والعلاقات والنطاقات بشكل ديناميكي.

الهدف الأساسي هو الإجابة عن السؤال:

> **هل يستطيع هذا المستخدم تنفيذ هذا الإجراء على هذا المورد، في هذا النطاق، وبناءً على علاقته بالمورد والسياسات والشروط الحالية؟**

---

# 2. المبدأ الأساسي

لا يتم بناء الصلاحيات على أساس:

```text
User → Role → Permissions
```

فقط.

بل تعتمد عملية اتخاذ القرار على مجموعة من العناصر:

```text
User
+
Role
+
Permission
+
Scope
+
Resource
+
Relationship
+
Inheritance
+
Policy
+
Context
+
Overrides
=
Authorization Decision
```

وبالتالي لا تكون كلمة "مدير" ذات معنى ثابت داخل النظام.

فيمكن إنشاء دور باسم:

```text
مدير المشاريع
```

ويمكن منحه صلاحيات محددة على نطاق محدد.

كما يمكن إنشاء دور آخر باسم:

```text
مشرف المهام
```

ومنحه صلاحيات مختلفة تمامًا.

ويكون معنى الدور ناتجًا عن الصلاحيات والنطاقات والسياسات التي تم تكوينها له، وليس عن اسمه.

---

# 3. خصائص النظام الرئيسية

يجب أن يدعم النظام:

* Dynamic Roles
* Dynamic Permissions
* Fine-Grained Authorization
* RBAC
* Scope-Based Authorization
* ReBAC
* ABAC
* Policy-Based Authorization
* Resource Hierarchies
* Permission Inheritance
* Relationship-Based Access
* Allow / Deny
* Permission Overrides
* Delegation
* Temporary Permissions
* Context-Aware Authorization
* Multi-Tenant Authorization
* Resource-Level Permissions
* User-Level Permissions
* Group/Team Permissions
* Audit Trail
* Authorization Decision Explanation
* Dynamic Organizational Structures

---

# 4. الموارد Resources

المورد هو أي كيان يمكن تطبيق صلاحية عليه.

لا يجب أن يفرض النظام أنواع موارد محددة.

يمكن للنظام تعريف:

```text
Company
Branch
Department
Project
Task
Employee
Invoice
Document
Team
Warehouse
Product
Customer
Account
```

أو أي نوع آخر.

مثلاً في نظام جامعي:

```text
University
Faculty
Department
Course
Student
```

وفي نظام مستشفى:

```text
Hospital
Department
Doctor
Patient
MedicalRecord
```

نفس Authorization Engine يجب أن يستطيع التعامل مع جميع هذه الأنظمة.

---

# 5. Resource Type

كل مورد ينتمي إلى نوع معين:

```text
ResourceType
```

مثال:

```text
resource_type = project
```

ثم توجد موارد فعلية:

```text
project:100
project:101
project:102
```

أو:

```text
resource_type = department
department:10
department:11
```

يجب ألا يحتوي محرك الصلاحيات على منطق خاص مثل:

```text
if resource == company
```

بل يتعامل مع الموارد بشكل عام.

---

# 6. Actions

الإجراء هو العملية التي يريد المستخدم تنفيذها على المورد.

أمثلة:

```text
create
read
view
update
delete
manage
approve
reject
assign
publish
archive
export
restore
```

ويمكن تعريف إجراءات جديدة حسب الحاجة.

مثلاً:

```text
invoice.approve
project.update
task.assign
document.publish
```

الصيغة المنطقية:

```text
ResourceType + Action
```

مثال:

```text
project + update
```

يعني:

```text
project.update
```

---

# 7. Permissions

الصلاحية هي السماح بتنفيذ Action على Resource Type.

مثلاً:

```text
project.view
project.create
project.update
project.delete
```

أو:

```text
task.view
task.create
task.update
task.assign
```

أو:

```text
invoice.approve
invoice.export
```

يجب أن تكون الصلاحيات بيانات قابلة للإدارة، وليست مجموعة ثابتة Hard-Coded داخل التطبيق.

---

# 8. Roles

الدور هو مجموعة قابلة لإعادة الاستخدام من الصلاحيات.

الدور لا يمثل منصبًا ثابتًا في النظام.

مثلاً يمكن إنشاء:

```text
Project Supervisor
```

ويمنح:

```text
project.view
project.update

task.view
task.create
task.update
task.assign
```

ويمكن إنشاء:

```text
Task Manager
```

بصلاحيات مختلفة.

ويمكن إنشاء:

```text
Custom Employee
```

بالصلاحيات التي يحددها مدير النظام.

---

# 9. Dynamic Roles

يجب أن يستطيع النظام إنشاء عدد غير محدود من الأدوار المخصصة.

مثلاً:

```text
Role:
    Project Operations Supervisor
```

أو:

```text
Role:
    Regional Reviewer
```

أو:

```text
Role:
    Finance Approver
```

ولا يجب أن يحتوي الكود على شروط مثل:

```csharp
if (user.Role == "Manager")
```

بل يجب أن يعتمد على Authorization Engine.

---

# 10. Role Assignment

وجود الدور وحده لا يكفي.

يجب أن يتم تعيين الدور للمستخدم ضمن نطاق معين.

مثال:

```text
User:
    Ahmed

Role:
    Project Supervisor

Scope:
    Department 10
```

وهذا يعني أن أحمد يمتلك صلاحيات الدور داخل Department 10.

يمكن لنفس المستخدم الحصول على نفس الدور في نطاق آخر:

```text
Ahmed
    Project Supervisor
    Scope = Department 20
```

وبصلاحيات أو قيود مختلفة حسب إعداد النظام.

---

# 11. Scope

الـ Scope هو النطاق الذي تنطبق عليه الصلاحية.

يجب ألا يكون النظام مرتبطًا بأنواع Scope محددة.

يمكن أن يكون:

```text
Organization
Company
Branch
Department
Project
Team
Region
Warehouse
```

أو أي Resource آخر.

مثال:

```text
Scope:
    department:10
```

أو:

```text
Scope:
    project:100
```

أو:

```text
Scope:
    organization:1
```

---

# 12. Scope Inheritance

يمكن للصلاحية أن تنتقل من مورد إلى الموارد التابعة له.

مثال:

```text
Department A
    ├── Project 1
    │     └── Task 1
    │
    └── Project 2
          └── Task 2
```

إذا كان المستخدم لديه:

```text
task.manage
```

على:

```text
Department A
```

مع:

```text
Inheritance = descendants
```

فيمكن أن تنتقل الصلاحية إلى:

```text
Project 1
Project 2
Task 1
Task 2
```

حسب قواعد الوراثة المحددة.

---

# 13. التحكم الدقيق في Inheritance

لا يجب أن تكون الوراثة:

```text
inherit everything
```

فقط.

يجب أن يكون بالإمكان تحديد الصلاحيات التي تنتقل.

مثلاً:

```text
project.manage
    → descendants = true

task.manage
    → descendants = true

employee.view
    → descendants = false

finance.manage
    → descendants = false
```

وبالتالي يمكن التحكم في كل Permission بشكل مستقل.

---

# 14. Resource Relationships

يجب أن يدعم النظام العلاقات بين الموارد والمستخدمين.

أمثلة:

```text
user:10
    member_of
project:100
```

أو:

```text
user:20
    manager_of
department:10
```

أو:

```text
user:30
    assigned_to
task:500
```

أو:

```text
team:5
    member_of
project:100
```

أو:

```text
project:100
    belongs_to
department:10
```

هذه العلاقات تشكل Resource Graph.

---

# 15. العلاقات يجب أن تكون Dynamic

لا يجب أن يفترض النظام أن العلاقات هي فقط:

```text
owner
member
manager
```

بل يمكن تعريف:

```text
owns
manages
member
supervises
assigned_to
reviews
approves
works_on
belongs_to
reports_to
```

حسب النظام.

مثلاً في نظام مشاريع:

```text
Ahmed
    manages
Project A
```

وفي نظام جامعي:

```text
Professor
    teaches
Course
```

وفي نظام مستشفى:

```text
Doctor
    treats
Patient
```

المحرك يتعامل مع العلاقة كمعلومة Authorization وليس كمنطق خاص بنظام معين.

---

# 16. ReBAC

يجب أن يستطيع النظام اتخاذ قرار بناءً على علاقة المستخدم بالمورد.

مثلاً:

```text
Ahmed
    member_of
Project 100
```

ولديه:

```text
project.view
```

فيستطيع مشاهدة:

```text
Project 100
```

لكن لا يستطيع بالضرورة مشاهدة:

```text
Project 200
```

إذا لم تكن لديه علاقة أو Scope يسمح بذلك.

---

# 17. الجمع بين Scope وRelationship

يمكن أن يعتمد القرار على أكثر من عامل.

مثلاً:

```text
User:
    Ahmed

Permission:
    task.update

Scope:
    Department 10

Relationship:
    member_of(Project 100)
```

ويصبح القرار:

```text
ALLOW
```

إذا كان:

```text
Task
    belongs_to Project 100

AND

Project 100
    belongs_to Department 10

AND

Ahmed
    member_of Project 100
```

---

# 18. ABAC

يجب أن يدعم النظام الشروط المبنية على Attributes.

مثلاً:

```text
user.department_id == resource.department_id
```

أو:

```text
user.region == resource.region
```

أو:

```text
resource.status != "archived"
```

أو:

```text
user.level >= resource.required_level
```

هذه الشروط تسمح بتطبيق قواعد لا يمكن تمثيلها بمجرد Role.

---

# 19. Policy Engine

يجب فصل تعريف الصلاحيات عن قواعد اتخاذ القرار.

يمكن إنشاء Policy مثل:

```text
ALLOW project.update
IF:

user has permission project.update

AND

project belongs to user's scope

AND

project.status != archived
```

وسيكون الـ Policy Engine مسؤولًا عن تقييم هذه الشروط.

---

# 20. Allow / Deny

يجب دعم نوعين من القرارات:

```text
ALLOW
DENY
```

مثلاً:

```text
ALLOW:
    project.manage

DENY:
    project.delete
```

ويجب تعريف أولوية واضحة عند وجود تعارض.

القاعدة المقترحة:

```text
Explicit DENY
    >
Explicit ALLOW
    >
Inherited ALLOW
```

مع إمكانية تخصيص هذه الأولوية حسب نوع الـ Policy.

---

# 21. Permission Override

يمكن إعطاء المستخدم استثناءً من صلاحيات دوره.

مثلاً:

```text
Role:
    Project Manager

Permissions:
    project.view
    project.update
    project.delete
```

لكن المستخدم Ahmed:

```text
DENY:
    project.delete
```

فتصبح النتيجة:

```text
Ahmed
✓ view
✓ update
✗ delete
```

---

# 22. Direct User Permissions

يمكن منح المستخدم صلاحية مباشرة دون إنشاء Role.

مثلاً:

```text
Ahmed
    project.approve
    Scope = Project 100
```

وهذا مفيد للاستثناءات والحالات الخاصة.

---

# 23. Groups / Teams

يمكن أن تكون الصلاحية للمستخدم أو لمجموعة.

مثلاً:

```text
Team:
    Project Team A
```

أعضاء الفريق:

```text
Ahmed
Mohammed
Salem
```

ثم:

```text
Team A
    project.update
    task.manage
```

وبذلك يحصل جميع أعضاء الفريق على الصلاحيات حسب Scope الفريق.

---

# 24. Delegation

يجب دعم تفويض الصلاحيات.

مثلاً:

```text
Manager
    delegates
    project.approve
    to
    Employee
```

لكن التفويض لا يجوز أن يسمح للمستلم بتجاوز صلاحيات المفوِّض.

القاعدة:

```text
Delegated Permissions
    ⊆
Delegator Permissions
```

ويجب دعم:

```text
Start Date
End Date
Scope
Allowed Actions
```

---

# 25. Temporary Permissions

يمكن أن تكون الصلاحية مؤقتة:

```text
Role:
    Project Manager

Valid From:
    2026-10-01

Valid Until:
    2026-10-31
```

بعد انتهاء الفترة يتم رفض الصلاحية تلقائيًا.

---

# 26. Context-Aware Authorization

يمكن أن يعتمد القرار على Context.

مثلاً:

```text
Current Time
Current Organization
Current Tenant
IP
Device
Session
MFA Status
Request Source
```

مثال:

```text
ALLOW

IF:
    user has invoice.approve

AND:
    MFA = true

AND:
    current organization = user's organization
```

---

# 27. Multi-Tenant Support

يجب أن يكون المحرك قادرًا على العمل مع SaaS متعدد المؤسسات.

مثلاً:

```text
Tenant A
    Users
    Resources
    Roles

Tenant B
    Users
    Resources
    Roles
```

ولا يجوز أن يستطيع مستخدم Tenant A الوصول إلى موارد Tenant B إلا إذا كان لديه Authorization صريح يسمح بذلك.

---

# 28. Authorization Decision

يجب أن تكون نقطة الدخول الرئيسية:

```text
Authorize(
    Subject,
    Action,
    Resource,
    Context
)
```

مثال:

```text
Authorize(
    user:100,
    "update",
    project:500,
    context
)
```

النتيجة:

```text
ALLOW
```

أو:

```text
DENY
```

---

# 29. Explainable Authorization

لا يجب أن يرجع النظام:

```json
{
  "allowed": false
}
```

فقط.

بل يجب أن يستطيع النظام تفسير القرار.

مثال:

```json
{
  "allowed": true,
  "reason": {
    "permission": "project.update",
    "source": "role",
    "role": "Project Supervisor",
    "scope": {
      "type": "department",
      "id": "10"
    },
    "inherited": true,
    "relationship": "descendant"
  }
}
```

وعند الرفض:

```json
{
  "allowed": false,
  "reason": {
    "code": "OUT_OF_SCOPE",
    "message": "The resource is outside the user's assigned scope."
  }
}
```

هذه المعلومة مهمة جدًا لتسهيل Debugging ودعم المستخدمين.

---

# 30. Authorization Evaluation Flow

عند وصول طلب:

```text
PUT /projects/500
```

يجب أن تمر عملية التحقق بالمراحل التالية:

```text
1. Identify User
        ↓
2. Identify Tenant / Organization
        ↓
3. Identify Resource
        ↓
4. Identify Action
        ↓
5. Load Direct Permissions
        ↓
6. Load Assigned Roles
        ↓
7. Resolve Scopes
        ↓
8. Resolve Resource Relationships
        ↓
9. Resolve Inheritance
        ↓
10. Evaluate Policies
        ↓
11. Evaluate Conditions
        ↓
12. Apply Overrides
        ↓
13. Apply Deny Rules
        ↓
14. Produce Authorization Decision
        ↓
15. Record Audit
```

---

# 31. لا يجب وضع Authorization داخل Controllers

يجب ألا يكون الكود:

```csharp
if (user.Role == "Manager")
{
    ...
}
```

ولا:

```csharp
if (user.IsAdmin)
{
    ...
}
```

بل:

```csharp
await authorizationService.AuthorizeAsync(
    user,
    "project.update",
    project,
    context
);
```

ثم يقرر Authorization Engine.

وبذلك يبقى Business Logic منفصلًا عن Authorization Logic.

---

# 32. Authorization Service

يجب أن يكون هناك abstraction مثل:

```csharp
IAuthorizationService
```

وتوفر عمليات مثل:

```csharp
Task<AuthorizationResult> AuthorizeAsync(...);

Task<bool> CanAsync(...);

Task<IReadOnlyList<Permission>> GetEffectivePermissionsAsync(...);

Task<AuthorizationExplanation> ExplainAsync(...);
```

---

# 33. Effective Permissions

يجب أن يستطيع النظام حساب:

```text
Effective Permissions
```

للمستخدم.

مثلاً:

```text
Ahmed
```

قد يحصل على:

```text
Role Permissions
+
Direct Permissions
+
Team Permissions
+
Inherited Permissions
+
Delegated Permissions
-
Explicit Denials
```

والنتيجة هي:

```text
Effective Permissions
```

لكن يجب عدم اعتبار هذه القائمة وحدها كافية لاتخاذ القرار؛ لأن الـ Scope والـ Relationship والـ Context قد تغير النتيجة.

---

# 34. مثال شامل

لدينا:

```text
Organization
    ├── Division A
    │     ├── Department 1
    │     │      ├── Project 1
    │     │      └── Project 2
    │     │
    │     └── Department 2
    │
    └── Division B
```

المستخدم:

```text
Ahmed
```

الدور:

```text
Project Supervisor
```

الصلاحيات:

```text
project.view
project.update
task.view
task.create
task.update
task.assign
```

النطاق:

```text
Division A
```

والوراثة:

```text
descendants
```

لكن يوجد:

```text
DENY:
    project.delete
```

النتيجة:

```text
Division A
    ✓

Department 1
    ✓

Project 1
    ✓ view
    ✓ update
    ✗ delete

Project 2
    ✓ view
    ✓ update
    ✗ delete

Department 2
    ✓ حسب العلاقة والوراثة

Division B
    ✗
```

---

# 35. مثال بدون Hierarchy

يمكن استخدام النظام بدون أي هيكل هرمي.

لدينا:

```text
Ahmed
```

علاقة:

```text
Ahmed
    member_of
Project 100
```

والدور:

```text
Employee
```

والصلاحية:

```text
task.update
```

Policy:

```text
Employee can update tasks
only in projects where he is a member.
```

النتيجة:

```text
Project 100
    ✓

Project 200
    ✗
```

---

# 36. مثال متعدد العلاقات

يمكن أن يكون المستخدم:

```text
Ahmed
```

لديه:

```text
member_of Project A
manager_of Department B
reviewer_of Document C
```

وبالتالي قد يمتلك صلاحيات مختلفة حسب المورد:

```text
Project A
    member permissions

Department B
    manager permissions

Document C
    reviewer permissions
```

ولا يحتاج النظام إلى إنشاء Role جديد لكل حالة.

---

# 37. Audit Log

كل قرار مهم يجب أن يكون قابلًا للتسجيل.

مثلاً:

```text
User:
    Ahmed

Action:
    project.update

Resource:
    project:500

Result:
    ALLOW

Source:
    Role: Project Supervisor

Scope:
    Department:10

Timestamp:
    ...

CorrelationId:
    ...
```

وعند الرفض:

```text
Result:
    DENY

Reason:
    OUT_OF_SCOPE
```

---

# 38. Security Requirements

يجب مراعاة:

* عدم الاعتماد على بيانات الواجهة Frontend في اتخاذ القرار.
* كل Authorization Decision يجب أن يتم في Backend.
* عدم الوثوق بـ Role مرسل من العميل.
* عدم الاعتماد على Hidden UI Elements كوسيلة حماية.
* تطبيق Authorization على API والعمليات الحساسة.
* منع Privilege Escalation.
* منع Delegation إلى صلاحيات أعلى من صلاحيات المفوض.
* عزل Tenants.
* تسجيل العمليات الحساسة.
* دعم مراجعة الصلاحيات.
* دعم إبطال الصلاحيات فورًا.

---

# 39. Performance

بسبب أن Authorization قد يحتاج إلى فحص:

```text
Roles
Permissions
Scopes
Relationships
Policies
Inheritance
Overrides
```

يجب تصميم المحرك ليكون قابلًا للتخزين المؤقت.

يمكن استخدام:

```text
Authorization Cache
Permission Cache
Relationship Cache
Policy Cache
```

مع إبطال Cache عند تغيير:

```text
Role
Permission
Assignment
Scope
Relationship
Policy
Override
```

---

# 40. عدم ربط Authorization بقاعدة البيانات الخاصة بالمجال

يفضل أن يكون Authorization Layer منفصلًا منطقيًا عن Business Domain.

مثلاً:

```text
Business Domain
    Company
    Project
    Invoice
    Employee
```

بينما:

```text
Authorization Domain
    User
    Role
    Permission
    Scope
    Relationship
    Policy
    Assignment
```

ويتم الربط بينهما باستخدام:

```text
ResourceType
ResourceId
```

وبذلك يمكن إضافة Domain جديد دون إعادة تصميم محرك الصلاحيات.

---

# 41. المبدأ المعماري النهائي

يجب أن يكون Authorization Engine:

```text
Domain Agnostic
```

أي لا يعرف:

```text
Company
Branch
Department
Project
Hospital
University
```

بل يعرف فقط:

```text
Subject
Resource
Action
Relation
Scope
Policy
Context
Decision
```

وهذه هي النقطة الأساسية التي تجعل النظام Generic.

---

# 42. النموذج المفاهيمي النهائي

```text
                    ┌──────────────┐
                    │    Subject   │
                    │ User / Team  │
                    └──────┬───────┘
                           │
                           ▼
                    ┌──────────────┐
                    │ Role / Direct│
                    │ Assignment   │
                    └──────┬───────┘
                           │
                           ▼
                    ┌──────────────┐
                    │ Permissions  │
                    └──────┬───────┘
                           │
                           ▼
                    ┌──────────────┐
                    │    Scope     │
                    └──────┬───────┘
                           │
                           ▼
                 ┌────────────────────┐
                 │ Resource Graph     │
                 │ Relationships      │
                 │ Hierarchy          │
                 └─────────┬──────────┘
                           │
                           ▼
                    ┌──────────────┐
                    │   Policies   │
                    └──────┬───────┘
                           │
                           ▼
                    ┌──────────────┐
                    │   Context    │
                    └──────┬───────┘
                           │
                           ▼
                  ┌──────────────────┐
                  │ Authorization    │
                  │     Engine       │
                  └────────┬─────────┘
                           │
                    ┌──────┴──────┐
                    ▼             ▼
                  ALLOW          DENY
```

---

# 43. النتيجة المطلوبة

النظام النهائي يجب أن يسمح بإنشاء أي نموذج صلاحيات تقريبًا، مثل:

```text
Owner
    ↓
Company
    ↓
Branch
    ↓
Department
    ↓
Project
    ↓
Task
```

أو:

```text
University
    ↓
Faculty
    ↓
Department
    ↓
Course
    ↓
Student
```

أو:

```text
Hospital
    ↓
Department
    ↓
Team
    ↓
Patient
```

أو حتى:

```text
User
    ├── member_of → Project A
    ├── reviewer_of → Document B
    └── manager_of → Team C
```

دون تغيير Authorization Engine.

---

# 44. الخلاصة المعمارية

النظام المقترح ليس:

```text
RBAC System
```

بالمعنى التقليدي.

بل هو:

```text
Generic Fine-Grained Authorization Engine
```

يجمع بين:

```text
RBAC
+
ReBAC
+
ABAC
+
Scope-Based Authorization
+
Hierarchical Authorization
+
Dynamic Roles
+
Permission Inheritance
+
Policy-Based Authorization
+
Delegation
+
Temporary Permissions
+
Allow / Deny
+
Overrides
+
Context-Aware Authorization
```

ويتمحور حول:

```text
WHO
    ↓
CAN DO WHAT
    ↓
ON WHICH RESOURCE
    ↓
WITHIN WHICH SCOPE
    ↓
THROUGH WHICH RELATIONSHIP
    ↓
UNDER WHICH POLICY
    ↓
IN WHICH CONTEXT
    ↓
ALLOW / DENY
```

**الهدف النهائي هو أن تكون بنية المؤسسة أو النظام نفسها مجرد بيانات داخل Resource Graph، وليست جزءًا من منطق Authorization Engine.**

وبالتالي يمكن استخدام نفس المحرك في أي مشروع مهما اختلفت بنيته التنظيمية أو طبيعة موارده.
