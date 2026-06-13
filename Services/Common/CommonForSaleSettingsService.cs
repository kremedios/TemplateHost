using System.Text.Json;

using AppContractsSCO.Services.Common;
using AppContractsSCO.Models.Common;
using Microsoft.AspNetCore.Mvc;



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
        public CommonForSaleSettings Load(string webpageName,
                                    string settingsFilename,
                                    string area)
*/
         public PropertyPageModel Load(string webpageName,
                                    string settingsFilename,
                                    string area)
        {
            Console.WriteLine("TemplateHost:CommonForSaleSettingsService: Load(.) @A");

            _webpageName = webpageName;
            _settingsFilename = settingsFilename;

             string filePath = Path.Combine(
                                 _env.ContentRootPath,
                                "private",
                                area,
                                _webpageName,
                                _settingsFilename);


           
            var json = File.ReadAllText(filePath);


            //return JsonSerializer.Deserialize<CommonForSaleSettings>(json)
            //    ?? new CommonForSaleSettings();
                var model = JsonSerializer.Deserialize<PropertyPageModel>(json);

                return model;
        }



    public void Save(string webpageName, 
                     string settingsFilename, 
                     CommonForSaleSettings settings,
                     //PropertyPageModel settings,
                     string area)
    {
        var filePath = Path.Combine(
            _env.ContentRootPath,
            "private",
            //"realEstate",
            area,
            webpageName,
            settingsFilename);
        

        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(filePath, json);
    }
}