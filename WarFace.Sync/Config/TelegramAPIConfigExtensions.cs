internal static class TelegramAPIConfigExtensions
{
    public static string? GetValue(this TelegramAPIConfig telegramAPIConfig, string what)
    {
        switch (what)
        {
            case "api_hash":
                return telegramAPIConfig.Hash;
            case "api_id":
                return telegramAPIConfig.Id;
            case "phone_number":
                return telegramAPIConfig.PhoneNumber;
            default:
                return null;
        }
    }
}