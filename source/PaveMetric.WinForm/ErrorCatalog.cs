using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace PaveMetric
{
    /// <summary>One entry of the error catalog: numeric code, display name and color.</summary>
    public class ErrorType
    {
        public int Code { get; set; }
        public string Name { get; set; }
        public int ColorArgb { get; set; }

        public ErrorType() { }

        public ErrorType(int code, string name, Color color)
        {
            Code = code;
            Name = name;
            ColorArgb = color.ToArgb();
        }

        public Color Color => Color.FromArgb(ColorArgb);
    }

    /// <summary>
    /// User-editable list of error types, persisted as JSON in the user's application data folder
    /// so it is shared by every project.
    /// </summary>
    public static class ErrorCatalog
    {
        public static string FilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PaveMetric", "ErrorCatalog.json");

        public static List<ErrorType> Defaults() => new List<ErrorType>
        {
            new ErrorType((int)ErrorCodes.SurfacePeelOff,      "Felületi hámlás",           Color.Yellow),
            new ErrorType((int)ErrorCodes.LongitudinalCrack,   "Hosszirányú repedés",       Color.FromArgb(234, 111,  21)),
            new ErrorType((int)ErrorCodes.Raveling,            "Pergés",                    Color.FromArgb(180, 105,  25)),
            new ErrorType((int)ErrorCodes.Wear,                "Kopás",                     Color.FromArgb(150, 150, 150)),
            new ErrorType((int)ErrorCodes.MapCrack,            "Hálós repedés",             Color.Pink),
            new ErrorType((int)ErrorCodes.AlligatorCrack,      "Hálós repedés deformációval", Color.Red),
            new ErrorType((int)ErrorCodes.CrossCrack,          "Keresztirányú repedés",     Color.FromArgb(124,  58, 237)),
            new ErrorType((int)ErrorCodes.EdgeDamage,          "Burkolatszél hiba",         Color.FromArgb( 13, 148, 136)),
            new ErrorType((int)ErrorCodes.ShoulderDeposit,     "Padkafelhordás",            Color.FromArgb(120,  85,  60)),
            new ErrorType((int)ErrorCodes.SurfaceContamination,"Burkolatszennyeződés",      Color.FromArgb( 90,  90,  90)),
            new ErrorType((int)ErrorCodes.FilledPothole,       "Kitöltött kátyú",           Color.LightBlue),
            new ErrorType((int)ErrorCodes.Pothole,             "Kátyú",                     Color.Blue),
            new ErrorType((int)ErrorCodes.PavementChange,      "Burkolatváltás (javítás)",  Color.FromArgb(219,  39, 119)),
            new ErrorType((int)ErrorCodes.TotalDeterioration,  "Teljes burkolatleromlás",   Color.FromArgb(120,   0,   0)),
            new ErrorType((int)ErrorCodes.SurfacePerspiration, "Izzadás",                   Color.Green),
        };

        /// <summary>Loads the saved catalog, falling back to the defaults if it is missing or unreadable.</summary>
        public static List<ErrorType> Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var list = JsonSerializer.Deserialize<List<ErrorType>>(File.ReadAllText(FilePath));
                    if (list != null && list.Count > 0)
                        return list.GroupBy(t => t.Code).Select(g => g.First()).ToList();
                }
            }
            catch (Exception)
            {
                // corrupt file: start over from the defaults
            }
            return Defaults();
        }

        public static void Save(IEnumerable<ErrorType> types)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                var options = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(FilePath, JsonSerializer.Serialize(types.ToList(), options));
            }
            catch (Exception)
            {
                // the catalog is a convenience; a failed save must not break editing
            }
        }
    }
}
