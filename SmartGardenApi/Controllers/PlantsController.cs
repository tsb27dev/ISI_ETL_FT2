using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using SmartGardenApi.Data;
using SmartGardenApi.Models;
using SmartGardenApi.Services;
using Microsoft.AspNetCore.Authorization;

namespace SmartGardenApi.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class PlantsController : ControllerBase
{
    private readonly IGardenRepository _repo;
    private readonly WeatherService _weatherService;

    public PlantsController(IGardenRepository repo, WeatherService weatherService)
    {
        _repo = repo;
        _weatherService = weatherService;
    }

    // --- 1. CRUD REST ---

    [HttpGet] 
    public async Task<ActionResult<IEnumerable<Plant>>> GetPlants() 
        => await _repo.GetPlantsAsync();

    [HttpPost] 
    public async Task<ActionResult<Plant>> CreatePlant(Plant plant)
    {
        await _repo.CreatePlantAsync(plant);
        return CreatedAtAction(nameof(GetPlants), new { id = plant.Id }, plant);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePlant(int id, [FromBody] Plant updatedPlant)
    {
        if (id != updatedPlant.Id) return BadRequest("ID do URL difere do ID do corpo.");

        var affected = await _repo.UpdatePlantAsync(updatedPlant);
        if (affected == 0) return NotFound();
        return NoContent(); 
    }
    
    [HttpDelete("{id}")] 
    public async Task<IActionResult> DeletePlant(int id)
    {
        var affected = await _repo.DeletePlantAsync(id);
        if (affected == 0) return NotFound();
        return NoContent();
    }

    // --- 2. EXTERNAL SERVICE ---
    
    [HttpGet("weather-check")]
    public async Task<IActionResult> CheckWeather(double lat, double lon)
    {
        var temp = await _weatherService.GetGardenTemperature(lat, lon);
        return Ok(new { Message = "External Weather Data", Temperature = temp });
    }

    // --- 3. EXCEL EXPORT (ATUALIZADO) ---
    
    [HttpGet("export")]
    public async Task<IActionResult> ExportExcel()
    {
        var plants = await _repo.GetPlantsAsync();
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
        var usedRange = worksheet.RangeUsed();
        if (usedRange == null) return BadRequest("Excel vazio (sem dados).");
        
        // 1) Ler tudo do Excel para memória (mantém lógica atual, mas sem EF)
        var rows = new List<(int? Id, string Name, string Location, double RequiredHumidity)>();

        foreach (var row in usedRange.RowsUsed().Skip(1)) // Skip header
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
            // --- Leitura de Dados ---
            var name = row.Cell(2).GetValue<string>() ?? "Sem Nome";
            var location = row.Cell(3).GetValue<string>() ?? "Sem Local";
            double humidity = 0;
            if (row.Cell(4).TryGetValue<double>(out var dVal)) humidity = dVal;
            else if (row.Cell(4).TryGetValue<int>(out var iVal)) humidity = iVal;

            rows.Add((id > 0 ? id : null, name, location, humidity));
        }

        // 2) Aplicar sincronização via SQL (delete-not-in + upsert)
        var (updatedCount, createdCount, deletedCount) = await _repo.SyncPlantsFromExcelAsync(rows);

        return Ok(new { 
            Mensagem = "Sincronização Completa.", 
            Atualizados = updatedCount, 
            Criados = createdCount, 
            Apagados = deletedCount 
        });
    }
}