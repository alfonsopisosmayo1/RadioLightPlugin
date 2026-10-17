using System.Collections.Generic;
using Exiled.API.Enums;
using Exiled.API.Features;
using InventorySystem.Items.Usables.Scp330;
using MEC;
using PlayerRoles;
using RadioLightPlugin.Misc;
using UnityEngine;

namespace RadioLightPlugin.Subclases;

public static class ClaseD
{
	private const float CandyIntervalSeconds = 50f;

	public static void SmallSpawn(Player player)
	{
		Util.Pasiva(player, EffectType.MovementBoost, 25);
		player.Scale = new Vector3(0.8f, 0.8f, 0.8f);
		Util.SetVida(player, 75f);
		player.ClearInventory();
		Util.Bc(player, 8, "<color=#FF8E00>[ Eres un: <color=#fc5e03>Small.</color> ]</color>", "<color=#FF8E00>[ Você é um: <color=#fc5e03>Small.</color> ]</color>");
		Util.Bc(player, 10, "<color=#FF8E00>[ Tienes 75 de vida, pero corres un poco más rápido y eres más pequeño. ]</color>", "<color=#FF8E00>[ Você tem 75 de vida, mas corre um pouco mais rápido e é menor. ]</color>");
	}

	public static void CandymanSpawn(Player player)
	{
		Util.SetVida(player, 125f);
		player.ClearInventory();
		player.TryAddCandy(Var.RandomCandy());
		Util.Bc(player, 8, "<color=#FF8E00>[ Eres un: <color=#f403fc>Candy man.</color> ]</color>", "<color=#FF8E00>[ Você é um: <color=#f403fc>Candy man.</color> ]</color>");
		Util.Bc(player, 10, $"<color=#FF8E00>[ Tienes 125 de vida y recibes 1 caramelo nuevo cada {50} segundos mientras estés vivo. ]</color>", $"<color=#FF8E00>[ Você tem 125 de vida e recebe 1 doce novo a cada {50} segundos enquanto estiver vivo. ]</color>");
		Var.Coroutines.Add(Timing.RunCoroutine(CandymanLoop(player, player.Role.Type)));
	}

	private static IEnumerator<float> CandymanLoop(Player player, RoleTypeId rol)
	{
		while (Util.Vivo(player) && player.Role.Type == rol)
		{
			yield return Timing.WaitForSeconds(50f);
			if (!Util.Vivo(player) || player.Role.Type != rol)
			{
				break;
			}
			player.TryAddCandy(Var.RandomCandy());
		}
	}

	public static void VeteranoSpawn(Player player)
	{
		Util.SetVida(player, 110f);
		player.ClearInventory();
		player.AddItem(ItemType.KeycardScientist);
		player.AddItem(ItemType.SCP500);
		player.AddItem(ItemType.SCP244a);
		Util.Bc(player, 8, "<color=#FF8E00>[ Eres un: <color=#ff3dd5>Veterano</color> ]</color>", "<color=#FF8E00>[ Você é um: <color=#ff3dd5>Veterano</color> ]</color>");
		Util.Bc(player, 10, "<color=#FF8E00>[ Tienes 110 de vida, una tarjeta de científico, un SCP-500 y un SCP-244-A. ]</color>", "<color=#FF8E00>[ Você tem 110 de vida, um cartão de cientista, um SCP-500 e um SCP-244-A. ]</color>");
	}

	public static void KamikazeSpawn(Player player)
	{
		Util.SetVida(player, 50f);
		player.ClearInventory();
		player.AddItem(ItemType.GrenadeHE, 2);
		Util.Pasiva(player, EffectType.MovementBoost, 30);
		player.TryAddCandy(CandyKindID.Pink);
		Util.Bc(player, 8, "<color=#FF8E00>[ Eres un: <color=#4A6F22>Kamikaze</color> ]</color>", "<color=#FF8E00>[ Você é um: <color=#4A6F22>Kamikaze</color> ]</color>");
		Util.Bc(player, 10, "<color=#FF8E00>[ Tienes 2 granadas, un caramelo rosa y corres más rápido, pero tienes 50 de vida. ]</color>", "<color=#FF8E00>[ Você tem 2 granadas, um doce rosa e corre mais rápido, mas tem 50 de vida. ]</color>");
	}

	public static void ImpersonatorSpawn(Player player)
	{
		player.ClearInventory();
		Util.SetVida(player, 130f);
		player.AddItem(ItemType.Flashlight);
		Util.Bc(player, 5, "[ Eres un: <color=orange>Impersonator</color> ]", "[ Você é um: <color=orange>Impersonator</color> ]");
		Util.Bc(player, 8, "[ <color=orange>Eres un imitador que puede copiar la apariencia de un <color=#FFFF7CFF>Científico</color> al usar la <color=yellow>linterna</color>. Puedes disparar mientras estés disfrazado, pero tu disfraz solo dura 40 segundos.</color> ]", "[ <color=orange>Você é um imitador que pode copiar a aparência de um <color=#FFFF7CFF>Cientista</color> ao usar a <color=yellow>lanterna</color>. Pode atirar enquanto estiver disfarçado, mas seu disfarce dura apenas 40 segundos.</color> ]");
		Var.CooldownImp.Remove(player);
		Var.Impersonator.Add(player);
		Var.Coroutines.Add(Timing.RunCoroutine(Coroutines.GuardCheck(player)));
	}

	public static void SilenciosoSpawn(Player player)
	{
		Util.Pasiva(player, EffectType.SilentWalk, byte.MaxValue);
		Util.Bc(player, 5, "<color=#e6a200>[ <color=#226077>Eres un:</color> <color=#addfba>Silencioso</color> ]</color>", "<color=#e6a200>[ <color=#226077>Você é um:</color> <color=#addfba>Silencioso</color> ]</color>");
		Util.Bc(player, 8, "<color=#e6a200>[ <color=#226077>Eres un Clase-D normal, pero tus pasos son totalmente silenciosos.</color> ]</color>", "<color=#e6a200>[ <color=#226077>Você é um Classe-D normal, mas seus passos são totalmente silenciosos.</color> ]</color>");
	}
}
