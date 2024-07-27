namespace Reserve.API.Infrastructure.Exceptions;

public class ReserveDomainException: Exception
{
    public ReserveDomainException()
    { }

    public ReserveDomainException(string message)
        : base(message)
    { }

    public ReserveDomainException(string message, Exception innerException)
        : base(message, innerException)
    { }
}