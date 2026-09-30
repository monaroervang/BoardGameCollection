# ADR 004: Concurrency Strategy

## Status

Accepted

## Context

The system allows users to lend physical board games to other users.

Concurrent operations introduce two potential problems.

First, two users may attempt to borrow the same physical item simultaneously. Both requests could determine that the item is available before either loan is saved. This could result in multiple active loans for the same item.

Second, concurrent updates to an entity may result in lost updates. If two users retrieve and modify the same entity, one user's changes may overwrite the other's without detecting the conflict.

The system therefore needs a concurrency strategy that prevents double lending and detects conflicting updates.

## Decision

The system will use two complementary concurrency mechanisms.

### Unique filtered index

A unique filtered index will be created on the `Loans` table using `ItemId`, filtered to include only loans where `ReturnDate` is `NULL`.

This ensures that an item can have only one active loan while allowing multiple historical loans.

The API will check item availability before creating a loan. However, the database constraint will provide the final protection against concurrent requests.

Database violations of this constraint will result in HTTP `409 Conflict`.

### Optimistic concurrency

Optimistic concurrency will be implemented using SQL Server `rowversion` and Entity Framework Core concurrency tokens.

Entities that support updates will have a `RowVersion` property.

The API will expose the version through DTOs. When updating an entity, the client must provide the version originally retrieved.

If another operation has modified the entity, the update will be rejected with HTTP `409 Conflict`.

This prevents outdated updates from silently overwriting previously saved changes.

## Alternatives Considered

### Application-level checks

Checking whether an item is available before creating a loan is straightforward.

However, concurrent requests may both pass the check before either creates a loan.

This approach alone cannot guarantee that double lending is prevented.

### Pessimistic locking

Database transactions and locking can prevent conflicting operations by controlling concurrent access.

However, they require additional transaction management and may introduce unnecessary locking overhead.

### Optimistic concurrency alone

RowVersion can detect conflicting updates to an existing entity.

However, two concurrent requests inserting separate loan records would not necessarily conflict.

Therefore, optimistic concurrency alone is insufficient to prevent double lending.

## Consequences

The selected strategy prevents multiple active loans for the same physical item and detects conflicting updates.

Historical loans remain available after an item has been returned.

The solution requires an additional database migration and changes to the relevant entities and DTOs.

The API must distinguish concurrency conflicts from unrelated database errors.

Integration tests will verify that concurrent loan requests cannot create multiple active loans and that outdated updates do not overwrite previously saved changes.