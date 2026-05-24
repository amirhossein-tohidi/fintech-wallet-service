namespace Wallet.Domain.Exceptions
{
    public class NotFoundException : Exception
    {
        public string EntityName { get; }
        public object? Identifier { get; }

        public NotFoundException(string message) : base(message)
        {
            EntityName = string.Empty;
            Identifier = null;
        }

        public NotFoundException(string entityName, object identifier)
            : base($"{entityName} with identifier '{identifier}' was not found.")
        {
            EntityName = entityName;
            Identifier = identifier;
        }

        public NotFoundException(string entityName, object identifier, string message)
            : base(message)
        {
            EntityName = entityName;
            Identifier = identifier;
        }
    }
}