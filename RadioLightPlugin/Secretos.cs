namespace RadioLightPlugin;

/// <summary>
/// Claves y webhooks privados. Se leen del config del server (radio_light_plugin en el .yml),
/// así el código se puede subir a GitHub sin exponer nada.
/// </summary>
internal static class Secretos
{
	private static Config C => Plugin.Singleton?.Config;

	public static string WebhookVotacion => C?.WebhookVotacion?.Trim() ?? string.Empty;

	public static string WebhookVip => C?.WebhookVip?.Trim() ?? string.Empty;

	public static string WebhookVipCompras => C?.WebhookVipCompras?.Trim() ?? string.Empty;

	public static string WebhookVipCassie => C?.WebhookVipCassie?.Trim() ?? string.Empty;

	public static string SteamApiKey => C?.SteamApiKey?.Trim() ?? string.Empty;

	public static string GeminiApiKey => C?.GeminiApiKey?.Trim() ?? string.Empty;

	public static string GptApiUrl => C?.GptApiUrl?.Trim() ?? string.Empty;
}
