namespace Shared.DTOs.Loans.Responses;

public sealed record LoanResponse
{
    public Guid Id { get; init; }
    public DateOnly LoanDate { get; init; }
    public DateOnly DueDate { get; init; }
    public DateOnly? ReturnDate { get; init; }
    public Guid ItemId { get; init; }
    public Guid BorrowerId { get; init; }
    public bool IsReturned { get; init; }
    public bool IsOverdue { get; init; }
}