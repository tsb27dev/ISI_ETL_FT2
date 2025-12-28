using SmartGardenApi.Data;
using SmartGardenApi.Models;
using Microsoft.EntityFrameworkCore;
using CoreWCF;

namespace SmartGardenApi.Services.Soap;

[ServiceBehavior(IncludeExceptionDetailInFaults = true)]
public class PlantSoapService : IPlantSoapService
{
    private readonly GardenContext _context;

    public PlantSoapService(GardenContext context)
    {
        _context = context;
    }

    public async Task<List<Plant>> GetAllPlants()
    {
        return await _context.Plants.ToListAsync();
    }

    public async Task AddPlant(string name, string location, double humidity)
{
    // ADICIONA ESTA LINHA:
    Console.WriteLine($"[SOAP RECEBIDO] A tentar adicionar: {name} na localização {location}");

    var plant = new Plant 
    { 
        Name = name, 
        Location = location, 
        RequiredHumidity = humidity,
        LastWatered = DateTime.Now 
    };
    
    _context.Plants.Add(plant);
    await _context.SaveChangesAsync();
    
    // ADICIONA ESTA LINHA:
    Console.WriteLine("[SOAP SUCESSO] Planta gravada na Base de Dados!");
}

public async Task UpdatePlant(int id, string name, string location, double humidity)
    {
        Console.WriteLine($"[SOAP UPDATE] A atualizar ID: {id}");
        
        var plant = await _context.Plants.FindAsync(id);
        if (plant == null)
        {
            // Lança erro visível no SOAP Fault
            throw new FaultException($"Planta com ID {id} não encontrada.");
        }

        plant.Name = name;
        plant.Location = location;
        plant.RequiredHumidity = humidity;
        // Não atualizamos o LastWatered num update de info geral

        await _context.SaveChangesAsync();
        Console.WriteLine("[SOAP UPDATE] Sucesso!");
    }
    
}