[![Build](https://github.com/kemsky/projection-tools/actions/workflows/build.yml/badge.svg)](https://github.com/kemsky/projection-tools/actions/workflows/build.yml)

# Projection Tools

This package provides two primitives `Projection<TSource, TResult>` and `Specification<TSource>` for building reusable LINQ projections and predicates.

## Projections

My initial goal was to replace packages like AutoMapper and similar.

The common drawbacks of using mappers:

- Code "black hole" and dirty magic: IDE can not show code usages, mappings are resolved in runtime;
- Complex API: API is complex yet limited in many cases;
- Maintenance costs: authors often change APIs without considering other options;
- Do not properly separate instance API (mapping object instances) and expression API (mapping through LINQ projections) which leads to bugs in runtime;
- Bugs: despite all the claims you can not be sure in anything unless you manually test mapping of each field and each scenario (instance/LINQ);
- Poor testing experience;

In the most cases mapping splits into two independent stages:

- Fetch DTOs directly from DB using automatic projections and pass result to client;
- Map incoming DTOs to entities to apply changes from client and then save modified entities to DB;

In reality mapping from DTO to entity is rarely a good idea: there are validations, access rights, business logic. It means that you end up using custom code in each case.

`Projection<TSource, TResult>` - provides option to define mapping from entity to DTO.

Quick example, controller should return only active users and users should have only active departments:

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

## Specifications (reusable predicates)

Projection works but we have a problem: we do not reuse `Where(x => x.Active)` checks. There is one predicate in `UserController.GetUser` method and another in `UserDtoProjection`.

This predicate can be more complex, often it is a combination of different predicates depending on business logic.

There is a well-known specification pattern and there are many existing .NET implementations but they all share similar problems:

- Verbose syntax for declaration and usage;
- Many intrusive extensions methods that pollute project code;
- Can only be used in certain contexts;

This is how we can use `Specification<TSource>` to solve these problems:

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
    public static readonly Specification<DepartmentEntity> ActiveDepartment = new (
        x => x.Active
    );
    
    public static readonly Specification<UserEntity> ActiveUser = new (
        x => x.Active
    );

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
                                .Where(ActiveDepartment)
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

    // option 1: Db projection
    public Task<UserDto> GetUser(int id)
    {
        return context.Set<UserEntity>()
                .Where(ActiveUser)
                .Where(x => x.Id == id)
                .Select(UserProjections.UserProjection.ProjectExpression)
                .SingleAsync();
    }

    // option 2: in-memory projection
    public async Task<UserDto> GetUser(int id)
    {
        var user = await context.Set<UserEntity>()
                     .Include(x => x.Departments)
                     .Where(ActiveUser)
                     .Where(x => x.Id == id)
                     .SingleAsync();

        return UserProjections.UserProjection.Project(user);
    }
}
```
