# ADR 002: Separate Domain Model from Persistence Configuration

## Status

Accepted

## Context

The application uses a domain model containing entities and value objects
such as BoardGame, Item, Loan, User, Name, and Address.

The domain model contains business rules and should not depend on Entity
Framework Core or SQL Server.

Persistence-specific concerns such as table mappings, relationships,
column constraints, and delete behavior still need to be configured.

## Decision

Entity Framework Core configuration is kept in the Persistence project
using separate `IEntityTypeConfiguration<T>` classes and the Fluent API.

The Domain project does not reference Entity Framework Core.

Database relationships can be configured using foreign key properties
such as `BoardGameId`, `OwnerId`, `ItemId`, and `BorrowerId` without
requiring persistence-specific navigation properties in the domain model.

Domain rules are enforced in the Domain layer. Database constraints are
also configured where appropriate to protect data integrity.

## Consequences

### Benefits

- The Domain model remains independent of EF Core and SQL Server.
- Persistence technology can change with less impact on Domain.
- Database configuration is located in one clearly defined place.
- Business rules can be tested without a database.
- Database constraints provide an additional layer of data integrity.

### Trade-offs

- Some rules or constraints may appear in both Domain and Persistence.
- Relationship configuration becomes more explicit.
- Additional configuration classes are required.

## Alternatives considered

### EF Core configuration inside Domain

Data annotations or other EF-specific configuration could be placed
directly on domain classes.

This was not selected because it would couple the Domain project to
persistence concerns.

### Rely entirely on EF Core conventions

EF Core conventions could configure parts of the model automatically.

This was not selected because explicit configuration makes important
database relationships and constraints easier to understand and control.

## Derived domain properties

Properties that can be derived from persisted data are not stored as
separate database columns.

For example, `Loan.IsReturned` is derived from `ReturnDate`, and
`Loan.IsOverdue` is derived from `DueDate`, `ReturnDate`, and the current date.

These properties are ignored by EF Core and calculated by the domain model.
This avoids storing redundant data that could become inconsistent with
the underlying persisted values.

## Value object persistence

Value objects are mapped according to how they are represented in the database.

`Name` and `Address` are mapped as EF Core complex types because they contain
multiple values that belong to the `User` entity and do not have their own
identity.

`Email` is also a domain value object, but it is persisted as a single string
column using an EF Core value converter.

This allows the Domain model to encapsulate email validation while the database
can store the email as a normal scalar column with a maximum length and a
unique index.

The persistence representation therefore does not determine whether a type is
a value object in the Domain model.