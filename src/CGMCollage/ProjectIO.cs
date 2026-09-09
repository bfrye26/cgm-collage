using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
namespace CGMCollage;
public static class ProjectIO
{
 static readonly JsonSerializerOptions Options=new(){PropertyNamingPolicy=JsonNamingPolicy.CamelCase,PropertyNameCaseInsensitive=true,WriteIndented=true,DefaultIgnoreCondition=JsonIgnoreCondition.WhenWritingNull};
 public static void Save(string path,Project p){p.Format=5;p.AppVersion="3.0.3";File.WriteAllText(path,JsonSerializer.Serialize(p,Options));}
 public static Project Load(string path){var p=JsonSerializer.Deserialize<Project>(File.ReadAllText(path),Options)??throw new InvalidDataException("The project file is empty.");Normalize(p);return p;}
 static void Normalize(Project p){if(p.CanvasWidth<64)p.CanvasWidth=1200;if(p.CanvasHeight<64)p.CanvasHeight=675;if(p.Frames.Count==0)p.Frames=Templates.Create("Angled Triptych");foreach(var f in p.Frames){if(f.Zoom<=0)f.Zoom=1;if(f.Contrast<=0)f.Contrast=1;if(f.Saturation<=0)f.Saturation=1;if(f.Opacity<=0)f.Opacity=1;if(string.IsNullOrWhiteSpace(f.Shape))f.Shape="rect";if(string.IsNullOrWhiteSpace(f.FitMode))f.FitMode="fill";}}
 public static EmbeddedImage ReadImage(string path){var bytes=File.ReadAllBytes(path);var ext=Path.GetExtension(path).ToLowerInvariant();var mime=ext switch{".png"=>"image/png",".webp"=>"image/webp",".gif"=>"image/gif",".bmp"=>"image/bmp",".tif" or ".tiff"=>"image/tiff",_=>"image/jpeg"};return new(Path.GetFileName(path),mime,Convert.ToBase64String(bytes));}
 public static string AutosavePath=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"CGM Collage","Recovery.cgmcollage");
}
