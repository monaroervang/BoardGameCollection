using Domain.Exceptions.Errors;

public class RequiredFieldException : Exception
{
    public RequiredFieldException(string fieldName)
        : base(string.Format(ErrorMessages.RequiredField, fieldName))
    {
    }
}