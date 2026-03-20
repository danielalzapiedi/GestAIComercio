namespace GestAI.Domain.Enums;

public enum InternalUserRole
{
    Owner = 0,
    Employee = 1
}

public enum SaasPlanCode
{
    Starter = 0,
    Pro = 1,
    Manager = 2
}

public enum SaasModule
{
    Dashboard = 0,
    Users = 1,
    Configuration = 2,
    AuditLog = 3,
    Plans = 4,
    PlatformTenants = 5,
    Branches = 6,
    Warehouses = 7,
    Categories = 8,
    Products = 9,
    Customers = 10,
    Suppliers = 11
}

public enum UnitOfMeasure
{
    Unit = 0,
    Kilogram = 1,
    Meter = 2,
    SquareMeter = 3,
    CubicMeter = 4,
    Liter = 5,
    Bag = 6,
    Bundle = 7
}

public enum CustomerType
{
    Consumer = 0,
    Company = 1,
    Mixed = 2
}
