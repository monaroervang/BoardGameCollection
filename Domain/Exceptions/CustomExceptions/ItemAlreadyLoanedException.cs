using Domain.Exceptions.Errors;

namespace Domain.Exceptions.CustomExceptions
{
    public class ItemAlreadyLoanedException : Exception
    {
        public ItemAlreadyLoanedException()
            : base(ErrorMessages.ItemAlreadyLoaned)
        {
        }
    }
}

   
