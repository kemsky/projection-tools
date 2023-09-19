[![Build](https://github.com/kemsky/projection-tools/actions/workflows/build.yml/badge.svg)](https://github.com/kemsky/projection-tools/actions/workflows/build.yml)

# Projection Tools

This package provides primitives for building reusable LINQ projections and specifications.

Package is available on [Nuget](https://www.nuget.org/packages/ProjectionTools/).

Install using dotnet CLI:
```commandline
dotnet add package ProjectionTools
```
Install using Package-Manager console:
```commandline
PM> Install-Package ProjectionTools
```

I've also published an article on Medium [Alternative specification pattern implementation in C#](https://medium.com/@nimrod97/alternative-specification-pattern-implementation-in-c-f5d88a7ed364).

## Specifications

Predicates can be complex, often a combination of different predicates depending on business logic.

There is a well-known specification pattern and there are many existing .NET implementations but they all share similar problems:

- Verbose syntax for declaration and usage;
- Many intrusive extensions methods that pollute project code;
- Can only be used in certain contexts (delegates vs expressions);

`Specification<TSource>` can solve all of these problems.

You can create specification using an expression:
```csharp
    Specification<DepartmentEntity> ActiveDepartment = new (
        x => x.Active
    );
```
or a delegate:
```csharp
    Specification<DepartmentEntity> ActiveDepartment = new (
        default,
        x => x.Active
    );
```
or both (e.g. when you have to use EF specific DbFunctions):
```csharp
    Specification<DepartmentEntity> ActiveDepartment = new (
        x => x.Active,
        x => x.Active
    );
```

You can also easily combine specifications (using `&&`, `||`,`!` operators):
```csharp
    Specification<DepartmentEntity> CustomerServiceDepartment = new (
        x => x.Name == "Customer Service"
    );
    
    Specification<DepartmentEntity> ActiveCustomerServiceDepartment =  ActiveDepartment && CustomerServiceDepartment;
```

Specifications can be nested:
```csharp
    Specification<DepartmentEntity> CustomerServiceDepartment = new (
        x => x.Name == "Customer Service"
    );
    
    Specification<UserEntity> ActiveUserInCustomerServiceDepartment = new (
        x => x.Active && x.Departments.Any(CustomerServiceDepartment.IsSatisfiedBy)
    );
```

Full example:

```csharp
public class UserEntity
{
    public int Id { get; set; }

    public string Name { get; set; }

    public bool Active { get; set; }

    public bool IsAdmin { get; set; }

    public List<DepartmentEntity> Departments { get; set; }
}

public class DepartmentEntity
{
    public int Id { get; set; }

    public bool Active { get; set; }

    public string Name { get; set; }
}

public class UserDto
{
    public string Name { get; set; }

    public List<DepartmentDto> Departments { get; set; }
}

public class DepartmentDto
{
    public string Name { get; set; }
}

public static class UserSpec
{
    public static readonly Specification<DepartmentEntity> ActiveDepartment = new(
        x => x.Active
    );

    public static readonly Specification<UserEntity> ActiveUser = new(
        x => x.Active
    );

    public static readonly Specification<UserEntity> AdminUser = new(
        x => x.IsAdmin
    );

    public static readonly Specification<UserEntity> ActiveAdminUser = ActiveUser && AdminUser;

    public static readonly Specification<UserEntity> ActiveUserInActiveDepartment = new(
        x => x.Active && x.Departments.Any(ActiveDepartment)
    );
}

public class UserController : Controller
{
    private readonly DbContext _context;

    public UserController(DbContext context)
    {
        _context = context;
    }

    public Task<UserEntity> GetUser(int id)
    {
        return context.Set<UserEntity>()
            .Where(ActiveUserInActiveDepartment)
            .Where(x => x.Id == id)
            .SingleAsync();
    }

    public Task<UserEntity> GetAdminUser(int id)
    {
        return context.Set<UserEntity>()
            .Where(ActiveAdminUser)
            .Where(x => x.Id == id)
            .SingleAsync();
    }
}
```

## Projections

My initial goal was to replace packages like AutoMapper and similar.

The common drawbacks of using mappers:

- IDE can not show code usages, mappings are resolved in runtime (sometimes source generators are used);
- API is complex yet limited in many cases;
- Maintenance costs are high, authors frequently change APIs without considering other options;
- Do not properly separate instance API (mapping object instances) and expression API (mapping through LINQ projections) which leads to bugs in runtime;
- Despite all the claims you can not be sure in anything unless you manually test mapping of each field and each scenario (instance/LINQ);
- Poor testing experience, sometimes you have to create your own "tools" specifically for testing mappings;
- Compatibility with LINQ providers, recently AutoMapper has broken compatibility with EF6 for no reason at all;

In most cases mapping splits into two independent scenarios:

1. Fetch DTOs from DB using automatic projections;
2. Map DTOs to entities and then save modified entities to DB;

In reality direct mapping from DTO to entity is rarely viable: there are validations, access rights, business logic. It means that you end up writing custom code for each save operation.

In case we want to support only 1st scenario there is no need to deal with complex mapper configurations.

`Projection<TSource, TResult>` - provides an option to define reusable mapping.

You can create projection using mapping expression:

```csharp
    Projection<DepartmentEntity, DepartmentDto> DepartmentDtoProjection = new (
        x => new DepartmentDto
        {
            Name = x.Name
        }
    );
```
or delegate:

```csharp
    Projection<DepartmentEntity, DepartmentDto> DepartmentDtoProjection = new (
        default,
        x => new DepartmentDto
        {
            Name = x.Name
        }
    );
```

or both (e.g. when DB only features are used like DBFunctions, delegate should match DB behavior):

```csharp
    Projection<DepartmentEntity, DepartmentDto> DepartmentDtoProjection = new (
        x => new DepartmentDto
        {
            Name = x.Name
        },
        x => new DepartmentDto
        {
            Name = x.Name
        }
    );
```

You can use projections in other projections.

Thanks to `DelegateDecompiler` package and built-in ability to compile expression trees all of the options above will work but with different performance implications.

Full example, controller should return only active users and users should have only active departments:

```csharp
public class UserEntity
{
    public int Id { get; set; }

    public string Name { get; set; }

    public bool Active { get; set; }

    public List<DepartmentEntity> Departments { get; set; }
}

public class DepartmentEntity
{
    public int Id { get; set; }

    public bool Active { get; set; }

    public string Name { get; set; }
}

public class UserDto
{
    public string Name { get; set; }

    public List<DepartmentDto> Departments { get; set; }
}

public class DepartmentDto
{
    public string Name { get; set; }
}

public static class UserProjections
{
    public static readonly Projection<DepartmentEntity, DepartmentDto> DepartmentDtoProjection = new (
        x => new DepartmentDto
        {
            Name = x.Name
        }
    );

    public static readonly Projection<UserEntity, UserDto> UserDtoProjection = new (
        x => new UserDto
        {
            Name = x.Name,
            Departments = x.Departments
                                .Where(z => z.Active)
                                .Select(DepartmentDtoProjection.Project)
                                .ToList()
        }
    );
}

public class UserController : Controller 
{
    private readonly DbContext _context;

    public UserController(DbContext context)
    {
        _context = context;
    }

    // option 1: DB projection
    public Task<UserDto> GetUser(int id)
    {
        return context.Set<UserEntity>()
                .Where(x => x.Active)
                .Where(x => x.Id == id)
                .Select(UserProjections.UserProjection.ProjectExpression)
                .SingleAsync();
    }

    // option 2: in-memory projection
    public async Task<UserDto> GetUser(int id)
    {
        var user = await context.Set<UserEntity>()
                     .Include(x => x.Departments)
                     .Where(x => x.Active)
                     .Where(x => x.Id == id)
                     .SingleAsync();

        return UserProjections.UserProjection.Project(user);
    }
}
```
