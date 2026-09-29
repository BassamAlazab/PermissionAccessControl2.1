# 66. EF Core Configuration

يفضل استخدام `IEntityTypeConfiguration<T>` لكل Entity بدل وضع جميع الإعدادات داخل `OnModelCreating`.

البنية المقترحة:

```text
Authorization/
├── Domain/
│   ├── Entities/
│   ├── Enums/
│   └── ValueObjects/
│
├── Infrastructure/
│   └── Persistence/
│       ├── Configurations/
│       │   ├── SubjectConfiguration.cs
│       │   ├── RoleConfiguration.cs
│       │   ├── PermissionConfiguration.cs
│       │   ├── RolePermissionConfiguration.cs
│       │   ├── FeatureConfiguration.cs
│       │   ├── ScopeConfiguration.cs
│       │   ├── ScopeInheritanceRuleConfiguration.cs
│       │   ├── RoleAssignmentConfiguration.cs
│       │   ├── PermissionAssignmentConfiguration.cs
│       │   ├── ResourceRelationshipConfiguration.cs
│       │   ├── PolicyConfiguration.cs
│       │   ├── PermissionOverrideConfiguration.cs
│       │   ├── DelegationConfiguration.cs
│       │   └── AuthorizationAuditLogConfiguration.cs
│       │
│       └── AuthorizationDbContext.cs
│
└── Application/
    └── Authorization/
```

---

# 67. Common Conventions

يفضل توحيد:

```text
Primary Keys     = Guid
TenantId         = Guid?
CreatedAt        = UTC
UpdatedAt        = UTC
```

والـ strings الخاصة بالمفاتيح يجب أن يكون لها طول محدد.

مثال:

```csharp
builder.Property(x => x.Key)
    .HasMaxLength(200)
    .IsRequired();
```

ولا تستخدم:

```csharp
.HasColumnType("nvarchar(max)")
```

للقيم التي تدخل في Index.

---

# 68. Subject Configuration

```csharp
public sealed class SubjectConfiguration
    : IEntityTypeConfiguration<Subject>
{
    public void Configure(
        EntityTypeBuilder<Subject> builder)
    {
        builder.ToTable("Subjects");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SubjectType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.ExternalId)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.DisplayName)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.SubjectType,
            x.ExternalId
        })
        .IsUnique();

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.IsActive
        });
    }
}
```

## Constraint

لا يسمح بوجود نفس Subject الخارجي مرتين داخل نفس Tenant:

```text
Tenant + SubjectType + ExternalId = Unique
```

---

# 69. Role Configuration

```csharp
public sealed class RoleConfiguration
    : IEntityTypeConfiguration<Role>
{
    public void Configure(
        EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.Name
        })
        .IsUnique();

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.IsActive
        });
    }
}
```

الـ Role Name يكون unique داخل Tenant.

مثلاً:

```text
Tenant A → Project Manager
Tenant B → Project Manager
```

مسموح.

لكن:

```text
Tenant A → Project Manager
Tenant A → Project Manager
```

غير مسموح.

---

# 70. Permission Configuration

```csharp
public sealed class PermissionConfiguration
    : IEntityTypeConfiguration<Permission>
{
    public void Configure(
        EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Key)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.Property(x => x.ResourceType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Action)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.PermissionType)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.Key
        })
        .IsUnique();

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.ResourceType,
            x.Action
        });
    }
}
```

---

# 71. Permission Key Rule

يفضل أن يكون `Key` هو القيمة canonical.

مثال:

```text
project.read
project.update
project.delete

invoice.read
invoice.approve

feature.reports.view
```

ويجب عدم السماح بوجود:

```text
Project.Read
PROJECT.READ
project.read
```

كثلاث Permissions مختلفة.

يمكن فرض normalization في Application Layer:

```csharp
permission.Key =
    permission.Key.Trim().ToLowerInvariant();
```

---

# 72. RolePermission Configuration

```csharp
public sealed class RolePermissionConfiguration
    : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(
        EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");

        builder.HasKey(x => new
        {
            x.RoleId,
            x.PermissionId
        });

        builder.Property(x => x.Effect)
            .HasConversion<int>()
            .IsRequired();

        builder.HasOne(x => x.Role)
            .WithMany(x => x.Permissions)
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Permission)
            .WithMany(x => x.RolePermissions)
            .HasForeignKey(x => x.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

## Important

لا نحتاج:

```text
Id
```

في `RolePermissions`.

الـ Composite PK يكفي:

```text
(RoleId, PermissionId)
```

وبالتالي لا يمكن إضافة نفس Permission إلى Role مرتين.

---

# 73. Feature Configuration

```csharp
public sealed class FeatureConfiguration
    : IEntityTypeConfiguration<Feature>
{
    public void Configure(
        EntityTypeBuilder<Feature> builder)
    {
        builder.ToTable("Features");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Key)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.Key
        })
        .IsUnique();

        builder.HasOne(x => x.ParentFeature)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentFeatureId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

## لماذا Restrict؟

حتى لا يؤدي حذف Feature أب إلى حذف شجرة Features كاملة عن طريق الخطأ.

---

# 74. AuthorizationScope Configuration

```csharp
public sealed class AuthorizationScopeConfiguration
    : IEntityTypeConfiguration<AuthorizationScope>
{
    public void Configure(
        EntityTypeBuilder<AuthorizationScope> builder)
    {
        builder.ToTable("AuthorizationScopes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.ResourceType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.InheritanceMode)
            .HasConversion<int>()
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.ResourceType,
            x.ResourceId
        });

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.IsActive
        });
    }
}
```

---

# 75. Scope Uniqueness

هنا توجد نقطة تصميمية مهمة.

لا يفضل جعل:

```text
TenantId + ResourceType + ResourceId
```

Unique بشكل مطلق.

لأن نفس Resource يمكن أن يكون له أكثر من Scope Definition.

مثلاً:

```text
Scope = Department 50 - Full
Scope = Department 50 - ReadOnly
```

لذلك نترك إمكانية تعدد Scopes، ونمنع التكرار فقط إذا كان التصميم يتطلب ذلك.

إذا أردت Scope واحدًا فقط لكل Resource:

```csharp
builder.HasIndex(x => new
{
    x.TenantId,
    x.ResourceType,
    x.ResourceId
})
.IsUnique();
```

لكن هذا قرار Business Rule وليس قاعدة عامة للمحرك.

---

# 76. ScopeInheritanceRule Configuration

```csharp
public sealed class ScopeInheritanceRuleConfiguration
    : IEntityTypeConfiguration<ScopeInheritanceRule>
{
    public void Configure(
        EntityTypeBuilder<ScopeInheritanceRule> builder)
    {
        builder.ToTable("ScopeInheritanceRules");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SourceResourceType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Relation)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.TargetResourceType)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasOne(x => x.Scope)
            .WithMany(x => x.InheritanceRules)
            .HasForeignKey(x => x.ScopeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new
        {
            x.ScopeId,
            x.SourceResourceType,
            x.Relation,
            x.TargetResourceType
        })
        .IsUnique();
    }
}
```

هذا يمنع تكرار نفس inheritance rule.

---

# 77. RoleAssignment Configuration

```csharp
public sealed class RoleAssignmentConfiguration
    : IEntityTypeConfiguration<RoleAssignment>
{
    public void Configure(
        EntityTypeBuilder<RoleAssignment> builder)
    {
        builder.ToTable("RoleAssignments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Priority)
            .IsRequired();

        builder.HasOne(x => x.Role)
            .WithMany(x => x.Assignments)
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Scope)
            .WithMany()
            .HasForeignKey(x => x.ScopeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.SubjectId,
            x.IsActive
        });

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.RoleId
        });

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.SubjectId,
            x.RoleId,
            x.ScopeId
        });
    }
}
```

---

# 78. RoleAssignment Constraints

يجب تطبيق قواعد:

```text
ValidFrom <= ValidUntil
```

إذا كان الاثنان موجودين.

مثال SQL Server:

```sql
ALTER TABLE RoleAssignments
ADD CONSTRAINT CK_RoleAssignments_Validity
CHECK (
    ValidFrom IS NULL
    OR ValidUntil IS NULL
    OR ValidFrom <= ValidUntil
);
```

---

# 79. PermissionAssignment Configuration

```csharp
public sealed class PermissionAssignmentConfiguration
    : IEntityTypeConfiguration<PermissionAssignment>
{
    public void Configure(
        EntityTypeBuilder<PermissionAssignment> builder)
    {
        builder.ToTable("PermissionAssignments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Effect)
            .HasConversion<int>()
            .IsRequired();

        builder.HasOne(x => x.Permission)
            .WithMany()
            .HasForeignKey(x => x.PermissionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Scope)
            .WithMany()
            .HasForeignKey(x => x.ScopeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.SubjectId,
            x.PermissionId
        });

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.SubjectId,
            x.ScopeId
        });
    }
}
```

---

# 80. ResourceRelationship Configuration

```csharp
public sealed class ResourceRelationshipConfiguration
    : IEntityTypeConfiguration<ResourceRelationship>
{
    public void Configure(
        EntityTypeBuilder<ResourceRelationship> builder)
    {
        builder.ToTable("ResourceRelationships");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SourceType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Relation)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.TargetType)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.SourceType,
            x.SourceId,
            x.Relation
        });

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.TargetType,
            x.TargetId,
            x.Relation
        });

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.SourceType,
            x.SourceId,
            x.Relation,
            x.TargetType,
            x.TargetId
        })
        .IsUnique();
    }
}
```

---

# 81. لماذا لا يوجد Foreign Key في ResourceRelationship؟

لأن:

```text
SourceType + SourceId
```

قد يشير إلى:

```text
Project
Department
Company
Invoice
Document
User
```

ولا يمكن لقاعدة البيانات إنشاء FK واحد polymorphic إلى عدة جداول.

لذلك:

```text
SourceType
SourceId
```

هو Logical Reference وليس Database FK.

وهذا مقصود.

---

# 82. منع Self Relationship

يمكن منع:

```text
Project:500
    contains
Project:500
```

عن طريق:

```sql
ALTER TABLE ResourceRelationships
ADD CONSTRAINT CK_ResourceRelationships_NoSelfReference
CHECK (
    NOT (
        SourceType = TargetType
        AND SourceId = TargetId
    )
);
```

لكن هذا يمنع فقط self-reference المباشر.

ولا يمنع Cycles مثل:

```text
A → B
B → C
C → A
```

ومنع الـ Cycles يجب أن يتم في Application/Graph Layer.

---

# 83. Relation Definitions

يفضل إضافة جدول مهم إلى التصميم المتقدم:

```text
RelationDefinitions
-------------------
Id
TenantId
SourceType
Relation
TargetType
IsDirectional
IsActive
```

مثال:

```text
project
belongs_to
department
```

و:

```text
department
contains
project
```

---

# 84. لماذا RelationDefinitions مهمة؟

حتى لا يستطيع أي كود إدخال علاقة عشوائية:

```text
project
is_parent_of
invoice
```

إذا لم تكن هذه العلاقة معرفة.

المحرك يتحقق:

```text
Is this relation allowed?
```

قبل إنشاء ResourceRelationship.

---

# 85. RelationDefinition Configuration

```csharp
public sealed class RelationDefinitionConfiguration
    : IEntityTypeConfiguration<RelationDefinition>
{
    public void Configure(
        EntityTypeBuilder<RelationDefinition> builder)
    {
        builder.ToTable("RelationDefinitions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SourceType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Relation)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.TargetType)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.SourceType,
            x.Relation,
            x.TargetType
        })
        .IsUnique();
    }
}
```

---

# 86. Policy Configuration

```csharp
public sealed class PolicyConfiguration
    : IEntityTypeConfiguration<AuthorizationPolicy>
{
    public void Configure(
        EntityTypeBuilder<AuthorizationPolicy> builder)
    {
        builder.ToTable("AuthorizationPolicies");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.Property(x => x.DefinitionJson)
            .IsRequired();

        builder.Property(x => x.Version)
            .IsRequired();

        builder.Property(x => x.Priority)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.Name,
            x.Version
        })
        .IsUnique();

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.IsActive,
            x.Priority
        });
    }
}
```

---

# 87. Policy JSON Validation

قاعدة البيانات لا ينبغي أن تكون مسؤولة عن فهم DSL بالكامل.

عند إنشاء Policy:

```text
Request
   ↓
JSON Schema Validation
   ↓
Semantic Validation
   ↓
Compile
   ↓
Store
```

مثال:

```text
Unknown operator
       ↓
Reject

Unknown field
       ↓
Reject

Invalid resource type
       ↓
Reject
```

---

# 88. PermissionOverride Configuration

```csharp
public sealed class PermissionOverrideConfiguration
    : IEntityTypeConfiguration<PermissionOverride>
{
    public void Configure(
        EntityTypeBuilder<PermissionOverride> builder)
    {
        builder.ToTable("PermissionOverrides");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ResourceType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Reason)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(x => x.Effect)
            .HasConversion<int>()
            .IsRequired();

        builder.HasOne(x => x.Permission)
            .WithMany()
            .HasForeignKey(x => x.PermissionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.SubjectId,
            x.PermissionId,
            x.ResourceType,
            x.ResourceId
        });
    }
}
```

---

# 89. Delegation Configuration

```csharp
public sealed class DelegationConfiguration
    : IEntityTypeConfiguration<Delegation>
{
    public void Configure(
        EntityTypeBuilder<Delegation> builder)
    {
        builder.ToTable("Delegations");

        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Scope)
            .WithMany()
            .HasForeignKey(x => x.ScopeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.DelegateeSubjectId,
            x.IsActive
        });

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.DelegatorSubjectId
        });
    }
}
```

---

# 90. DelegationPermission Configuration

```csharp
public sealed class DelegationPermissionConfiguration
    : IEntityTypeConfiguration<DelegationPermission>
{
    public void Configure(
        EntityTypeBuilder<DelegationPermission> builder)
    {
        builder.ToTable("DelegationPermissions");

        builder.HasKey(x => new
        {
            x.DelegationId,
            x.PermissionId
        });

        builder.HasOne(x => x.Delegation)
            .WithMany(x => x.Permissions)
            .HasForeignKey(x => x.DelegationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Permission)
            .WithMany()
            .HasForeignKey(x => x.PermissionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

---

# 91. Audit Log Configuration

```csharp
public sealed class AuthorizationAuditLogConfiguration
    : IEntityTypeConfiguration<AuthorizationAuditLog>
{
    public void Configure(
        EntityTypeBuilder<AuthorizationAuditLog> builder)
    {
        builder.ToTable("AuthorizationAuditLogs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Action)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.ResourceType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Decision)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.ReasonCode)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.CorrelationId)
            .HasMaxLength(100);

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.SubjectId,
            x.CreatedAt
        });

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.ResourceType,
            x.ResourceId,
            x.CreatedAt
        });

        builder.HasIndex(x => new
        {
            x.TenantId,
            x.Decision,
            x.CreatedAt
        });
    }
}
```

---

# 92. Delete Behavior Rules

يجب أن تكون قواعد Delete محافظة.

## Cascade مسموح غالبًا في:

```text
Role
    ↓
RolePermissions

Scope
    ↓
ScopeInheritanceRules

Delegation
    ↓
DelegationPermissions
```

## Restrict مفضل في:

```text
Role
    ↓
RoleAssignments

Permission
    ↓
RolePermissions

Permission
    ↓
PermissionAssignments

Permission
    ↓
Overrides

Scope
    ↓
Assignments
```

الهدف هو منع حذف Role أو Permission مستخدم حاليًا بدون عملية صريحة.

---

# 93. Soft Delete

لا يفضل حذف:

```text
Role
Permission
Policy
Feature
Scope
```

في الأنظمة الحساسة.

بدلًا من ذلك:

```text
IsActive = false
```

ويتم الاحتفاظ بالتاريخ.

خصوصًا:

```text
Permission
Role
Policy
Assignment
```

---

# 94. Temporal Validity

كل Assignment حساس يجب أن يدعم:

```text
ValidFrom
ValidUntil
```

مثال:

```text
Ahmed
Project Manager
Project 500

ValidFrom:
2026-10-01

ValidUntil:
2026-12-31
```

بعد انتهاء:

```text
Assignment
    ↓
Inactive logically
    ↓
DENY
```

ولا يجب الاعتماد فقط على Job يقوم بتغيير `IsActive`.

الـ Authorize نفسه يجب أن يتحقق من الوقت:

```csharp
now >= ValidFrom
&&
(ValidUntil == null || now <= ValidUntil)
```

---

# 95. Priority Rules

عند وجود عدة Assignments:

```text
Role A → Allow
Role B → Deny
Override → Deny
```

لا ينبغي أن يكون القرار مبنيًا على ترتيب Query عشوائي.

يجب أن يكون لدينا Decision Precedence واضح.

مثال:

```text
Explicit Resource Deny
        ↓
Explicit Resource Allow
        ↓
Policy Deny
        ↓
Scoped Deny
        ↓
Scoped Allow
        ↓
Default Deny
```

لكن هذه القاعدة يجب أن تكون configurable/explicit في `DecisionEngine` ولا تعتمد على EF Core.

---

# 96. Database Constraints vs Authorization Rules

ليس كل شيء يجب وضعه في Database Constraint.

## Database مسؤول عن:

```text
Unique Keys
Required Values
Foreign Keys
Data Types
Basic Check Constraints
```

## Authorization Engine مسؤول عن:

```text
Scope Resolution
Relationship Traversal
Policy Evaluation
Priority
Deny/Allow
Delegation
Context
```

## Domain مسؤول عن:

```text
Project.DepartmentId
Task.ProjectId
Invoice.SupplierId
Employee.CompanyId
```

وهذا الفصل مهم جدًا.

---

# 97. Domain Relationships لا تكرر داخل Authorization DB

مثلاً Domain:

```csharp
public class Project
{
    public Guid Id { get; set; }

    public Guid DepartmentId { get; set; }
}
```

لا نحتاج إنشاء:

```text
AuthorizationProjectDepartment
```

فقط لأن Authorization يحتاج العلاقة.

بل ResourceGraphProvider يستخدم Domain relationship:

```text
Project.DepartmentId
```

بينما العلاقات الخاصة بالـ Authorization فقط تخزن في:

```text
ResourceRelationships
```

مثال:

```text
User:10
reviewer_of
Document:500
```

إذا كانت هذه العلاقة غير موجودة أصلًا في Domain.

---

# 98. Resource Type Definitions

لزيادة قوة النظام، يفضل إضافة:

```text
ResourceTypes
-------------
Id
TenantId
Key
Name
IsSystem
IsActive
```

أمثلة:

```text
company
branch
department
project
task
invoice
supplier
document
employee
```

هذا يمنع استخدام:

```text
ResourceType = "abc123"
```

بشكل عشوائي.

---

# 99. ResourceType Configuration

```csharp
builder.HasIndex(x => new
{
    x.TenantId,
    x.Key
})
.IsUnique();

builder.Property(x => x.Key)
    .HasMaxLength(100)
    .IsRequired();
```

---

# 100. Action Definitions

يمكن أيضًا تعريف Actions:

```text
Actions
-------
read
create
update
delete
approve
publish
archive
assign
export
```

ثم:

```text
Permission
=
ResourceType + Action
```

مثال:

```text
project + update
invoice + approve
document + publish
```

هذا يقلل من الأخطاء الإملائية.

---

# 101. Permission Integrity

قبل إنشاء:

```text
invoice.approve
```

يمكن التحقق من:

```text
ResourceType = invoice
Action = approve
```

وأن:

```text
invoice
```

ResourceType موجود، و:

```text
approve
```

Action معرف.

---

# 102. Composite Unique Rules

القواعد المهمة:

```text
Subjects:
TenantId + SubjectType + ExternalId

Roles:
TenantId + Name

Permissions:
TenantId + Key

Features:
TenantId + Key

RelationDefinitions:
TenantId + SourceType + Relation + TargetType

ResourceRelationships:
TenantId + Source + Relation + Target

RolePermissions:
RoleId + PermissionId

DelegationPermissions:
DelegationId + PermissionId

ScopeInheritanceRules:
ScopeId + SourceType + Relation + TargetType
```

---

# 103. Concurrency

Authorization Configuration بيانات حساسة، لذلك يفضل استخدام Concurrency Token.

مثلاً في:

```text
Roles
Policies
Permissions
Features
Scopes
Assignments
```

يمكن استخدام:

```csharp
public byte[] RowVersion { get; set; } = null!;
```

وفي SQL Server:

```csharp
builder.Property(x => x.RowVersion)
    .IsRowVersion();
```

هذا يمنع تحديث Configuration قديم فوق تعديل أحدث.

---

# 104. Transaction Rules

عملية إنشاء Assignment ليست مجرد Insert واحد.

مثلاً:

```text
Create RoleAssignment
```

يجب التحقق من:

```text
Tenant
Subject
Role
Scope
Validity
Permission availability
```

ويفضل تنفيذ التعديلات الحساسة داخل Transaction.

مثال:

```text
Create Role
   ↓
Add Permissions
   ↓
Create Assignment
   ↓
Audit
   ↓
Invalidate Cache
```

إما أن تنجح العملية كاملة أو تفشل.

---

# 105. Authorization Configuration Security

من أخطر العمليات:

```text
Grant Permission
Create Role
Assign Role
Create Override
Create Delegation
Change Policy
```

لذلك يجب أن تكون نفسها محمية عبر Authorization Engine.

مثلاً:

```text
authorization.role.create
authorization.role.update
authorization.role.delete

authorization.assignment.create
authorization.assignment.revoke

authorization.permission.grant
authorization.permission.revoke

authorization.policy.manage
```

---

# 106. Migration Checklist

عند إنشاء Migration يجب التأكد من:

```text
[ ] Primary Keys
[ ] Foreign Keys
[ ] Composite Keys
[ ] Unique Indexes
[ ] Search Indexes
[ ] Check Constraints
[ ] Delete Behaviors
[ ] RowVersion
[ ] Tenant Isolation
```

---

# 107. DbContext

```csharp
public sealed class AuthorizationDbContext
    : DbContext
{
    public DbSet<Subject> Subjects => Set<Subject>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<RolePermission> RolePermissions
        => Set<RolePermission>();

    public DbSet<Feature> Features
        => Set<Feature>();

    public DbSet<AuthorizationScope> AuthorizationScopes
        => Set<AuthorizationScope>();

    public DbSet<ScopeInheritanceRule>
        ScopeInheritanceRules
        => Set<ScopeInheritanceRule>();

    public DbSet<RoleAssignment>
        RoleAssignments
        => Set<RoleAssignment>();

    public DbSet<PermissionAssignment>
        PermissionAssignments
        => Set<PermissionAssignment>();

    public DbSet<ResourceRelationship>
        ResourceRelationships
        => Set<ResourceRelationship>();

    public DbSet<AuthorizationPolicy>
        Policies
        => Set<AuthorizationPolicy>();

    public DbSet<PermissionOverride>
        PermissionOverrides
        => Set<PermissionOverride>();

    public DbSet<Delegation>
        Delegations
        => Set<Delegation>();

    public DbSet<DelegationPermission>
        DelegationPermissions
        => Set<DelegationPermission>();

    public DbSet<AuthorizationAuditLog>
        AuthorizationAuditLogs
        => Set<AuthorizationAuditLog>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(AuthorizationDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
```

---

# 108. Global Tenant Filtering

إذا كان Authorization DB منفصلًا لكل Tenant يمكن تبسيط الأمر.

أما إذا كان Database متعدد المستأجرين:

```text
TenantId
```

يجب أن يدخل في كل Query.

يفضل وجود:

```csharp
public interface ITenantEntity
{
    Guid? TenantId { get; }
}
```

لكن يجب الحذر من Global Query Filters إذا كان النظام يدعم:

```text
SuperAdmin
CrossTenant
System Operations
```

في هذه الحالة يكون Tenant Context صريحًا.

---

# 109. Critical Security Rule

لا تعتمد على:

```text
Global Query Filter
```

وحده لحماية Authorization.

يجب أن تكون جميع عمليات Authorization واعية بالـ Tenant.

أي:

```text
Tenant Isolation
```

جزء من Authorization Engine نفسه.

---

# 110. Recommended Final Database Structure

```text
Core
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
├── Policies
├── PermissionOverrides
│
├── Delegations
├── DelegationPermissions
│
└── AuthorizationAuditLogs
```

---

# 111. Final Architectural Separation

```text
                   DOMAIN DATABASE
                         │
                         │
              Business Relationships
                         │
                         ▼
                Resource Graph Provider
                         │
                         ▼
┌────────────────────────────────────────────────────┐
│                 AUTHORIZATION ENGINE               │
│                                                    │
│  Subjects                                          │
│  Roles                                             │
│  Permissions                                       │
│  Assignments                                       │
│  Scopes                                            │
│  Relationships                                     │
│  Policies                                          │
│  Overrides                                         │
│  Delegations                                       │
│                                                    │
│              Decision Engine                       │
└───────────────────────┬────────────────────────────┘
                        │
                        ▼
                 ALLOW / DENY
```

---

# 112. أهم قاعدة في التصميم

يجب عدم خلط هذه المفاهيم:

```text
Membership
Role
Permission
Assignment
Scope
Relationship
Policy
Feature
```

مثال:

```text
Ahmed
│
├── Company Membership
│      └── Member
│
├── Role Assignment
│      ├── Role: Project Manager
│      └── Scope: Department:50
│
└── Relationship
       └── reviewer_of → Document:500
```

فـ `Member` ليس بالضرورة Authorization Role.

وRole لا يحدد Resource.

وScope لا يحتوي بالضرورة قائمة Resources.

وRelationship ليست Role.

وPolicy ليست Permission.

هذا الفصل هو الذي يجعل المحرك قابلًا للتوسع وإعادة الاستخدام.

---

# 113. قاعدة تصميم نهائية للـ EF Core

**EF Core يجب أن يضمن سلامة الـ Authorization Configuration، لكنه لا يجب أن يحتوي منطق اتخاذ القرار.**

أي:

```text
EF Core
    ↓
Data Integrity

Authorization Engine
    ↓
Authorization Decision

Domain
    ↓
Business Relationships
```

وبذلك نحصل على فصل واضح بين:

```text
Persistence
Business Domain
Authorization
Policy Evaluation
```

وهو الأساس المناسب لبناء Authorization Engine إنتاجي وقابل لإعادة الاستخدام.
