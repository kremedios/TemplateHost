using System.Text.Json;
using AppContractsSCO.Models.RealEstate.ForSale;
using AppContractsSCO.Services.RealEstate;


public class ForSaleSettingsService : IForSaleSettingsService
{
    private readonly IWebHostEnvironment _env;
    private string _webpageName;
    private string _settingsFilename;

    public ForSaleSettingsService(IWebHostEnvironment env)
    {
        _env = env;
    }

    private string FilePath =>
        Path.Combine(
            _env.ContentRootPath,
            "private",
            "realEstate",
            //"Keswick",
            _webpageName,
            //"KeswickForSaleSettings.json"
            _settingsFilename);


/*
    public ForSaleSettings Load()
    {
        var json = File.ReadAllText(FilePath);

        return JsonSerializer.Deserialize<ForSaleSettings>(json)
               ?? new ForSaleSettings();
    }
*/
        public ForSaleSettings Load(string webpageName,
                                    string settingsFilename)
    {
        _webpageName = webpageName;
        _settingsFilename = settingsFilename;

        var json = File.ReadAllText(FilePath);

        return JsonSerializer.Deserialize<ForSaleSettings>(json)
               ?? new ForSaleSettings();
    }


/*
    public void Save(ForSaleSettings settings)
    {
        var json = JsonSerializer.Serialize(
            settings,
            new JsonSerializerOptions { WriteIndented = true });

        File.WriteAllText(FilePath, json);
    }
*/
    public void Save(string webpageName, string settingsFilename, ForSaleSettings settings)
    {
        var filePath = Path.Combine(
            _env.ContentRootPath,
            "private",
            "realEstate",
            webpageName,
            settingsFilename);

        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(filePath, json);
    }
}