using Exiled.API.Features;
using RadioLightPlugin.Misc;

namespace RadioLightPlugin.Subclases;

internal static class DeadRinger
{
	public static bool Alternar(Player p)
	{
		if (Var.DeadRingerUsado.Contains(p))
		{
			EffectsHUD.Notify(p, "<color=#e32433>Ya usaste tu <color=#e3e024>Dead Ringer</color> en esta vida.</color>");
			return false;
		}
		bool value;
		bool flag = !(Var.DeadRinger.TryGetValue(p, out value) && value);
		Var.DeadRinger[p] = flag;
		EffectsHUD.Notify(p, "<color=#859CC7>Dead Ringer <b>" + (flag ? "ACTIVADO" : "desactivado") + "</b>.</color>");
		return true;
	}
}
