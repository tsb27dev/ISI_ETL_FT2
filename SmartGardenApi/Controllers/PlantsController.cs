using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartGardenApi.Data;
using SmartGardenApi.Models;
using SmartGardenApi.Services;
using Microsoft.AspNetCore.Authorization; // Necessário para a segurança

namespace SmartGardenApi.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize] // <--- 1. Protege todo o controller (exige Token por defeito)
public class PlantsController : ControllerBase
{
    private readonly GardenContext _context;
    private readonly WeatherService _weatherService;

    public PlantsController(GardenContext context, WeatherService weatherService)
    {
        _context = context;
        _weatherService = weatherService;
    }

    // --- 1. CRUD REST ---

    [HttpGet] 
    public async Task<ActionResult<IEnumerable<Plant>>> GetPlants() 
        => await _context.Plants.ToListAsync();

    [HttpPost] 
    public async Task<ActionResult<Plant>> CreatePlant(Plant plant)
    {
        _context.Plants.Add(plant);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetPlants), new { id = plant.Id }, plant);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePlant(int id, [FromBody] Plant updatedPlant)
    {
        if (id != updatedPlant.Id) return BadRequest("ID do URL difere do ID do corpo.");

        var plant = await _context.Plants.FindAsync(id);
        if (plant == null) return NotFound();

        plant.Name = updatedPlant.Name;
        plant.Location = updatedPlant.Location;
        plant.RequiredHumidity = updatedPlant.RequiredHumidity;
        
        await _context.SaveChangesAsync();
        return NoContent(); 
    }
    
    [HttpDelete("{id}")] 
    public async Task<IActionResult> DeletePlant(int id)
    {
        var plant = await _context.Plants.FindAsync(id);
        if (plant == null) return NotFound();
        _context.Plants.Remove(plant);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // --- 2. EXTERNAL SERVICE ---
    
    [HttpGet("weather-check")]
    [AllowAnonymous] // <--- 2. Permite acesso sem token (público)
    public async Task<IActionResult> CheckWeather(double lat, double lon)
    {
        var temp = await _weatherService.GetGardenTemperature(lat, lon);
        return Ok(new { Message = "External Weather Data", Temperature = temp });
    }

    // --- 3. EXCEL EXPORT (ATUALIZADO) ---
    
    [HttpGet("export")]
    public async Task<IActionResult> ExportExcel()
    {
        var plants = await _context.Plants.ToListAsync();
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("GardenData");

        // Headers
        worksheet.Cell(1, 1).Value = "ID";
        worksheet.Cell(1, 2).Value = "Name";
        worksheet.Cell(1, 3).Value = "Location";
        worksheet.Cell(1, 4).Value = "Humidity"; // <--- 3. Novo Campo

        // Data
        for (int i = 0; i < plants.Count; i++)
        {
            worksheet.Cell(i + 2, 1).Value = plants[i].Id;
            worksheet.Cell(i + 2, 2).Value = plants[i].Name;
            worksheet.Cell(i + 2, 3).Value = plants[i].Location;
            worksheet.Cell(i + 2, 4).Value = plants[i].RequiredHumidity; // <--- Valor
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Garden.xlsx");
    }

    // --- 4. EXCEL IMPORT (ATUALIZADO: UPSERT) ---

    [HttpPut("import")]
    public async Task<IActionResult> ImportExcel(IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest("No file uploaded.");

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheet(1);
        
        int updatedCount = 0;
        int createdCount = 0;
        
        // 1. Lista para guardar os IDs que existem no Excel
        var idsNoExcel = new List<int>();

        foreach (var row in worksheet.RangeUsed().RowsUsed().Skip(1)) // Skip header
        {
            // --- Leitura do ID ---
            int id = 0;
            var idCell = row.Cell(1);
            if (!idCell.IsEmpty())
            {
                if (!idCell.TryGetValue<int>(out id)) 
                {
                    int.TryParse(idCell.GetValue<string>(), out id);
                }
            }

            // Se encontrámos um ID válido, guardamo-lo na lista de "Sobreviventes"
            if (id > 0)
            {
                idsNoExcel.Add(id);
            }

            // --- Leitura de Dados ---
            var name = row.Cell(2).GetValue<string>() ?? "Sem Nome";
            var location = row.Cell(3).GetValue<string>() ?? "Sem Local";
            int humidity = 0;
            if (row.Cell(4).TryGetValue<int>(out int val)) humidity = val;

            // --- Upsert ---
            Plant? existingPlant = null;
            if (id > 0) existingPlant = await _context.Plants.FindAsync(id);

            if (existingPlant != null)
            {
                existingPlant.Name = name;
                existingPlant.Location = location;
                existingPlant.RequiredHumidity = humidity;
                updatedCount++;
            }
            else
            {
                var newPlant = new Plant
                {
                    Name = name,
                    Location = location,
                    RequiredHumidity = humidity,
                    LastWatered = DateTime.Now
                };
                _context.Plants.Add(newPlant);
                // Nota: Plantas novas ainda não têm ID gerado, por isso não vão para a lista 'idsNoExcel',
                // mas isso não faz mal porque elas estão a ser adicionadas agora.
                createdCount++;
            }
        }

        // --- 2. PASSO NOVO: APAGAR O QUE NÃO ESTÁ NO EXCEL ---
        
        // Vamos buscar TODAS as plantas que existem na BD
        var todasPlantasDb = await _context.Plants.ToListAsync();

        // Filtramos: Queremos as plantas cujo ID *NÃO* está na lista do Excel
        var plantasParaApagar = todasPlantasDb
                                .Where(p => !idsNoExcel.Contains(p.Id))
                                .ToList();

        if (plantasParaApagar.Any())
        {
            _context.Plants.RemoveRange(plantasParaApagar);
        }

        await _context.SaveChangesAsync();

        return Ok(new { 
            Mensagem = "Sincronização Completa.", 
            Atualizados = updatedCount, 
            Criados = createdCount, 
            Apagados = plantasParaApagar.Count 
        });
    }
}