using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization; // נוסף עבור AOT JSON

Console.WriteLine("Fetching Bing daily image...");

// הגדרת ה-Client
using var client = new HttpClient { BaseAddress = new Uri("https://www.bing.com") };
var bingApiUrl = "/HPImageArchive.aspx?format=js&idx=0&n=1&mkt=he-IL";

// 1. קבלת ה-JSON וחילוץ כתובת התמונה
// שינוי: שימוש ב-Source Generator כדי לעשות דיסריאליזציה ללא Reflection
var response = await client.GetFromJsonAsync(bingApiUrl, BingJsonContext.Default.BingResponse);
if (response?.Images is not { Length: > 0 })
{
    Console.WriteLine("Failed to parse Bing image URL.");
    return;
}

var imageUrl = response.Images[0].Url;

// 2. הורדת התמונה ושמירתה על הדיסק
var imageBytes = await client.GetByteArrayAsync(imageUrl);
var picturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
var savePath = Path.Combine(picturesPath, "BingWallpaper.jpg");

await File.WriteAllBytesAsync(savePath, imageBytes);
Console.WriteLine($"Image saved locally to: {savePath}");

// 3. הגדרת התמונה כרקע של Windows
int result = WindowsWallpaper.SystemParametersInfo(
    WindowsWallpaper.SPI_SETDESKWALLPAPER,
    0,
    savePath,
    WindowsWallpaper.SPIF_UPDATEINIFILE | WindowsWallpaper.SPIF_SENDWININICHANGE);

Console.WriteLine(result != 0 ? "Wallpaper updated successfully!" : "Failed to update wallpaper.");

// ==========================================
// מודלים של נתונים וקריאות P/Invoke
// ==========================================

// שימוש ב-Records למיפוי מהיר של ה-JSON
record BingResponse([property: JsonPropertyName("images")] ImageInfo[] Images);
record ImageInfo([property: JsonPropertyName("url")] string Url);

// הוספת JsonSerializerContext עבור AOT
[JsonSerializable(typeof(BingResponse))]
internal partial class BingJsonContext : JsonSerializerContext
{
}

// עטיפה לקריאות ה-API של Windows
partial class WindowsWallpaper
{
    public const int SPI_SETDESKWALLPAPER = 0x0014;
    public const int SPIF_UPDATEINIFILE = 0x01;
    public const int SPIF_SENDWININICHANGE = 0x02;

    // LibraryImport הוא הסטנדרט החדש ב-.NET להחלפת DllImport - עובד נהדר עם Source Generators
    [LibraryImport("user32.dll", EntryPoint = "SystemParametersInfoW", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);
}
