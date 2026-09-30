
namespace Shared.DTOs.Loans.Requests;

public sealed record ReturnLoanRequest
{
    public DateOnly ReturnDate { get; init; }

    public byte[] RowVersion { get; init; } = [];
}
