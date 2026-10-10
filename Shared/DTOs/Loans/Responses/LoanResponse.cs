
namespace Shared.DTOs.Loans.Responses;

public sealed record LoanResponse
{
    public Guid Id { get; init; }
    public DateOnly LoanDate { get; init; }
    public DateOnly DueDate { get; init; }
    public DateOnly? ReturnDate { get; init; }
    public Guid ItemId { get; init; }
    public string BoardGameTitle { get; init; } = "";
    public Guid BorrowerId { get; init; }
    public bool IsReturned { get; init; }
    public bool IsOverdue { get; init; }

    public byte[] RowVersion { get; init; } = []; // For concurrency control. Byte array to hold the row version for optimistic concurrency.
}
