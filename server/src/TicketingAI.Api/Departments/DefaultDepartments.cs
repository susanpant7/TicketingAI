namespace TicketingAI.Api.Departments;

/// <summary>The departments an admin can load with "Load default departments".</summary>
public static class DefaultDepartments
{
    public static IReadOnlyList<DepartmentRequest> All { get; } =
    [
        new("IT", "laptops, accounts, access to systems, tools or data, VPN, software, network", "it-help@ticket.com or extension 100"),
        new("HR", "leave, benefits, policies, conduct, onboarding", "hr@ticket.com (payroll and salary: payroll@ticket.com)"),
        new("Finance", "purchases, payments, expenses, budgets, invoices", "finance@ticket.com")
    ];
}
