using System.Linq;
using System.Reflection;
using Exiled.API.Features;
using HarmonyLib;
using Hints;
using RadioLightPlugin.Misc;

namespace RadioLightPlugin.Patches;

internal static class HudRespetaHints
{
	public static void Aplicar(Harmony harmony)
	{
		MethodInfo methodInfo = typeof(Player).GetMethods(BindingFlags.Instance | BindingFlags.Public).FirstOrDefault((MethodInfo m) => m.Name == "ShowHint" && m.GetParameters().Length == 4 && m.GetParameters()[0].ParameterType == typeof(string) && m.GetParameters()[3].ParameterType == typeof(float));
		if (methodInfo == null)
		{
			Log.Warn("[EffectsHUD] No se encontró Player.ShowHint para el parche; los hints pueden pisarse entre sí.");
		}
		else
		{
			harmony.Patch(methodInfo, new HarmonyMethod(typeof(HudRespetaHints), "Prefix"));
		}
	}

	private static bool Prefix(Player __instance, ref string message, HintParameter[] hintParameters, HintEffect[] hintEffects, float duration)
	{
		try
		{
			if (__instance == null || __instance.IsNPC || !__instance.IsConnected)
			{
				return true;
			}
			if (SinEfectosEspeciales(hintParameters, hintEffects) && message != null && !message.Contains("{0}"))
			{
				message = HudRueI.SinLlaves(message);
			}
			if (HudRueI.EsPanelMvp(message))
			{
				return !HudRueI.Disponible || !HudRueI.MostrarPanelLobby(__instance, message, duration);
			}
			// [HUD RadioLight] jugadores vivos: el hint va a la zona central de la HUD propia
			if (__instance.IsAlive && SinEfectosEspeciales(hintParameters, hintEffects))
			{
				// la lista de ScpOverlay ya no se usa: RadioLight arma la suya (InfoScp). Se ignora para que no se duplique.
				if (message != null && message.IndexOf("<align=left><size=80%><b>", System.StringComparison.Ordinal) >= 0)
				{
					return false;
				}
				// paneles alineados a la izquierda de otros plugins (p. ej. ScpOverlay: lista de SCPs aliados)
				// van a la zona de paneles de arriba a la izquierda, no al centro de la pantalla
				if (message != null && message.IndexOf("align=left", System.StringComparison.OrdinalIgnoreCase) >= 0)
				{
					EffectsHUD.Panel(__instance, "externo", Compactar(message), duration);
					EffectsHUD.RefrescarAhora(__instance);
					return false;
				}
				if (string.IsNullOrEmpty(message))
				{
					EffectsHUD.QuitarPanel(__instance, "externo");
				}
				HudRL.Hint(__instance, message, duration);
				return false;
			}
			if (HudRueI.Disponible && SinEfectosEspeciales(hintParameters, hintEffects) && HudRueI.MostrarHint(__instance, message, duration))
			{
				return false;
			}
			EffectsHUD.OtroHint(__instance, duration);
		}
		catch
		{
		}
		return true;
	}

	private static readonly System.Text.RegularExpressions.Regex SinVida = new System.Text.RegularExpressions.Regex("<color=#[0-9A-Fa-f]{3,8}>(\ud83d\udc9a|\ud83d\udc99) ?0/0</color>( \\| )?", System.Text.RegularExpressions.RegexOptions.Compiled);

	private static readonly System.Text.RegularExpressions.Regex EspacioTrasIcono = new System.Text.RegularExpressions.Regex("(\ud83d\udc9a|\ud83d\udc99|\ud83d\udc64|\ud83d\udc65|\ud83d\udccf) ", System.Text.RegularExpressions.RegexOptions.Compiled);

	/// <summary>
	/// Achica la lista de SCPs de ScpOverlay para que cada SCP entre en UNA línea del panel:
	/// sin " | ", sin espacio después de los íconos, sin "0/0" (SCP-079) y con zonas abreviadas.
	/// </summary>
	private static string Compactar(string m)
	{
		try
		{
			if (m.IndexOf(" | ", System.StringComparison.Ordinal) < 0)
			{
				return m;
			}
			m = SinVida.Replace(m, string.Empty);
			m = m.Replace(" | ", " ");
			m = EspacioTrasIcono.Replace(m, "$1");
			return m.Replace("HeavyContainment", "HCZ").Replace("LightContainment", "LCZ").Replace("Entrance", "EZ").Replace("Surface", "Surf").Replace("Pocket", "Dim.").Replace("Unspecified", "?").Replace("Other", "?");
		}
		catch
		{
			return m;
		}
	}

	private static bool SinEfectosEspeciales(HintParameter[] parametros, HintEffect[] efectos)
	{
		if (efectos != null && efectos.Length != 0)
		{
			return false;
		}
		if (parametros == null || parametros.Length == 0)
		{
			return true;
		}
		return parametros.Length == 1 && parametros[0] is StringHintParameter;
	}
}
