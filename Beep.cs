using Exiled.API.Extensions;
using HarmonyLib;

[HarmonyPatch(typeof(MirrorExtensions), "PlayBeepSound")]
public class Beep
{
	private static bool Prefix()
	{
		return false;
	}
}
