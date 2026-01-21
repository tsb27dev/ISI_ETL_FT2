using SmartGardenApi.Models;

namespace SmartGardenApi.Data;

public interface IGardenRepository
{
    // Users
    Task<bool> UsernameExistsAsync(string username);
    Task<User?> GetUserByUsernameAsync(string username);
    Task<int> CreateUserAsync(User user);
    Task<int> UpdateUserPasswordHashAsync(int userId, string newPasswordHash);
    Task<int> DeleteUserAsync(int userId);

    // Plants
    Task<List<Plant>> GetPlantsAsync();
    Task<Plant?> GetPlantByIdAsync(int id);
    Task<int> CreatePlantAsync(Plant plant);
    Task<int> UpdatePlantAsync(Plant plant);
    Task<int> DeletePlantAsync(int id);

    // Excel sync behavior (matches current controller logic)
    Task<(int Updated, int Created, int Deleted)> SyncPlantsFromExcelAsync(
        IReadOnlyList<(int? Id, string Name, string Location, double RequiredHumidity)> rows);
}
