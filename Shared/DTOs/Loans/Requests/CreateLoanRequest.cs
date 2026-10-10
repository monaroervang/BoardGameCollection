
namespace Shared.DTOs.Loans.Requests;

public sealed record CreateLoanRequest
{
    public DateOnly LoanDate { get; init; }

    public DateOnly DueDate { get; init; }

    public Guid ItemId { get; init; }
}
