namespace SmartGardenApi.Models;

public class Plant
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // e.g., "Tomato"
    public string Location { get; set; } = string.Empty; // e.g., "Greenhouse A"
    public double RequiredHumidity { get; set; }
    public DateTime LastWatered { get; set; }
}