namespace OnleyiciBakim.Infrastructure;

public sealed class ResourceNotFoundException(string message) : Exception(message);
public sealed class ConflictException(string message) : Exception(message);
public sealed class DomainValidationException(string message) : Exception(message);
public sealed class ExternalServiceException(string message) : Exception(message);
