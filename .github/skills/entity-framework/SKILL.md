---
name: entity-framework
description: 'Use when: adding or updating EF Core in SideSeat, including DbContext, entities, connection strings, DI, or migrations.'
argument-hint: 'e.g., add EF annotations, create migration, update DbContext'
user-invocable: true
---

# SideSeat Entity Framework Skill

## When to Use
- Update entity classes with EF annotations and navigation properties.
- Add or change SideSeatDbContext, DbSet entries, or DI wiring.
- Create or update EF migrations and apply database updates.
- Update connection strings or SQL Server provider usage.

## Procedure
1. Review entity models in src/SideSeat/Models/Lab1/Entities.
2. Ensure each entity has [Key] on Id and [ForeignKey] on FK fields.
3. Convert collection navigations to virtual ICollection<T>.
4. Update src/SideSeat/Data/SideSeatDbContext.cs with DbSet entries.
5. Verify Program.cs has AddDbContext and the correct connection string key.
6. Update appsettings.json with a valid SQL Server connection string.
7. If repository changes are required, switch controllers to EF repositories.
8. Create migrations:
   - Run from src/SideSeat: dotnet ef migrations add <Name>
   - Apply: dotnet ef database update
9. Validate by running dotnet build (or dotnet build -t:Compile if locked).

## Notes
- Keep entity classes and DbContext names consistent (SideSeatDbContext).
- Add Includes in read queries when views depend on navigation properties.
