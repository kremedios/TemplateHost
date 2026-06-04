using System.Text.Json;
using AppContractsSCO.Models.RealEstate.ForSale;
using AppContractsSCO.Services.RealEstate;


public class ForSaleSettingsService : IForSaleSettingsService
{
    private readonly IWebHostEnvironment _env;

    public ForSaleSettingsService(IWebHostEnvironment env)
    {
        _env = env;
    }

    private string FilePath =>
        Path.Combine(
            _env.ContentRootPath,
            "private",
            "realEstate",
            "Keswick",
            "KeswickForSaleSettings.json");



    public ForSaleSettings Load()
    {
        var json = File.ReadAllText(FilePath);

        return JsonSerializer.Deserialize<ForSaleSettings>(json)
               ?? new ForSaleSettings();
    }



    public void Save(ForSaleSettings settings)
    {
        var json = JsonSerializer.Serialize(
            settings,
            new JsonSerializerOptions { WriteIndented = true });

        File.WriteAllText(FilePath, json);
    }
}