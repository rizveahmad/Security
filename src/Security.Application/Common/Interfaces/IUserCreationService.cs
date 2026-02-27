namespace Security.Application.Common.Interfaces;

public interface IUserCreationService
{
    Task<string> CreateUserAsync(string email, string firstName, string lastName, string password, CancellationToken ct = default);
}
