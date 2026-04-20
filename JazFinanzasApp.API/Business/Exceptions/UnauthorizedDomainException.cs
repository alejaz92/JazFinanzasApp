namespace JazFinanzasApp.API.Business.Exceptions
{
    public class UnauthorizedDomainException : Exception
    {
        public UnauthorizedDomainException() : base("Access denied.") { }
        public UnauthorizedDomainException(string message) : base(message) { }
    }
}
