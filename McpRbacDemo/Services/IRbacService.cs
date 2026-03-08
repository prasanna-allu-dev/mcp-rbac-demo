namespace McpRbacDemo.Services;

public interface IRbacService
{
    Task<List<string>> GetRolesAsync(string username);
    Task<bool> IsAuthorizedAsync(string username, string toolName);
}
