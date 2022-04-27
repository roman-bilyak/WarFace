using Microsoft.Extensions.Configuration;
using TL;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appconfig.json", true, true)
    .AddUserSecrets<AppConfig>()
    .AddEnvironmentVariables(prefix: "WARFACE_")
    .AddCommandLine(args)
    .Build();

AppConfig? appConfig = configuration.Get<AppConfig>();

using var telegram = new WTelegram.Client(x => appConfig?.TelegramAPI?.GetValue(x) ?? TelegramConfigProvider(x));
User user = await telegram.LoginUserIfNeeded();
Console.WriteLine($"We are logged-in as {user.username ?? user.first_name + " " + user.last_name} (id {user.id})");

Console.WriteLine("Done!");
Console.ReadLine();

static string? TelegramConfigProvider(string what)
{
    switch (what)
    {
        case "api_hash":
        case "api_id":
        case "phone_number":
        case "verification_code":
        case "first_name":
        case "last_name":
        case "password":
            {
                Console.Write($"Telegram {what.Replace("_", " ")}: ");
                return Console.ReadLine();
            }
        default:
            return null;
    }
}