using System.Text.Json;

using AppContractsSCO.Services.Common;
using AppContractsSCO.Models.Common;
using Microsoft.AspNetCore.Mvc;
//using AppContractsSCO.Services.RealEstate;


public class CommonForSaleSettingsService : ICommonForSaleSettingsService
{
    private readonly IWebHostEnvironment _env;
    private string _webpageName;
    private string _settingsFilename;

    public CommonForSaleSettingsService(IWebHostEnvironment env)
    {
        _env = env;
    }

    /*
    private string FilePath =>
        Path.Combine(
            _env.ContentRootPath,
            "private",
            "realEstate",
            //"Keswick",
            _webpageName,
            //"KeswickForSaleSettings.json"
            _settingsFilename);
    */


        public CommonForSaleSettings Load(string webpageName,
                                    string settingsFilename,
                                    string area)
        {
            _webpageName = webpageName;
            _settingsFilename = settingsFilename;

             string filePath = Path.Combine(
                                 _env.ContentRootPath,
                                "private",
                                area,
                                _webpageName,
                                _settingsFilename);

            //var json = File.ReadAllText(FilePath);
            var json = File.ReadAllText(filePath);


            return JsonSerializer.Deserialize<CommonForSaleSettings>(json)
                ?? new CommonForSaleSettings();
        }



    public void Save(string webpageName, 
                     string settingsFilename, 
                     CommonForSaleSettings settings,
                     string area)
    {
        Console.WriteLine($"TemplateHost:ForSaleSettingsService: %%%%%%%%%%%% BEFORE");
        var filePath = Path.Combine(
            _env.ContentRootPath,
            "private",
            //"realEstate",
            area,
            webpageName,
            settingsFilename);
        Console.WriteLine($"TemplateHost:ForSaleSettingsService: %%%%%%%%%%%% filePath= {filePath}");

        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(filePath, json);
    }
}