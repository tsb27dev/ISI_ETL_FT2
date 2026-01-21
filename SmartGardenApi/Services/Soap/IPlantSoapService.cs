using CoreWCF;
using SmartGardenApi.Models;

namespace SmartGardenApi.Services.Soap;

[ServiceContract]
public interface IPlantSoapService
{
    [OperationContract]
    Task<List<Plant>> GetAllPlants();

    [OperationContract]
    Task AddPlant(string name, string location, double humidity);

    [OperationContract]
    Task UpdatePlant(int id, string name, string location, double humidity);
}