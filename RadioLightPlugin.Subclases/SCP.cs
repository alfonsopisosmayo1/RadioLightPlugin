using Exiled.API.Enums;
using Exiled.API.Features;
using RadioLightPlugin.Misc;
using UnityEngine;

namespace RadioLightPlugin.Subclases;

public static class SCP
{
	public static void InfectadoSpawn(Player player)
	{
		Var.ZombiesInfectados.Add(player);
		Util.Bc(player, 8, "<color=#42f545>[ Eres un: <color=#EC2121>Zombie</color> <color=#42f545>infectado.</color> ]</color>", "<color=#42f545>[ Você é um: <color=#EC2121>Zumbi</color> <color=#42f545>infectado.</color> ]</color>");
		Util.Bc(player, 10, "<color=#42f545>[ Si matas a un jugador, se convertirá en zombie al instante. ]</color>", "<color=#42f545>[ Se você matar um jogador, ele se transformará em zumbi instantaneamente. ]</color>");
	}

	public static void AtletaSpawn(Player player)
	{
		player.Scale = new Vector3(0.7f, 0.7f, 0.7f);
		Util.SetVida(player, 200f);
		Util.Pasiva(player, EffectType.MovementBoost, 25);
		Util.Bc(player, 8, "<color=#F0FFFF>[ Eres un: <color=#EC2121>Zombie</color> <color=#A52A2A>atleta, de tanto correr te quedaste pequeño.</color> ]</color>", "<color=#F0FFFF>[ Você é um: <color=#EC2121>Zumbi</color> <color=#A52A2A>atleta.</color> ]</color>");
		Util.Bc(player, 10, "<color=#F0FFFF>[ Tienes 200 de vida y corres un poco más rápido. ]</color>", "<color=#F0FFFF>[ Você tem 200 de vida e corre um pouco mais rápido. ]</color>");
	}

	public static void TanqueSpawn(Player player)
	{
		Util.SetVida(player, 800f);
		Util.Pasiva(player, EffectType.Slowness, 15);
		Util.Bc(player, 8, "<color=#675645>[ Eres un: <color=#EC2121>Zombie</color> <color=#6D704F>tanque.</color> ]</color>", "<color=#675645>[ Você é um: <color=#EC2121>Zumbi</color> <color=#6D704F>tanque.</color> ]</color>");
		Util.Bc(player, 10, "<color=#675645>[ Tienes 800 de vida, pero caminas un poco más lento. ]</color>", "<color=#675645>[ Você tem 800 de vida, mas anda um pouco mais devagar. ]</color>");
	}

	public static void MilitarSpawn(Player player)
	{
		Util.SetVida(player, 300f);
		player.CurrentItem = player.AddItem(ItemType.GunCOM15);
		player.AddItem(ItemType.KeycardResearchCoordinator);
		player.AddItem(ItemType.ArmorHeavy);
		player.AddAmmo(AmmoType.Nato9, 60);
		Util.Bc(player, 8, "<color=#675645>[ Eres un: <color=#EC2121>Zombie</color> <color=#4A6F22>militar.</color> ]</color>", "<color=#675645>[ Você é um: <color=#EC2121>Zumbi</color> <color=#4A6F22>militar.</color> ]</color>");
		Util.Bc(player, 10, "<color=#675645>[ Tienes una COM-15 (60 balas), un chaleco pesado y 300 de vida. ]</color>", "<color=#675645>[ Você tem uma COM-15, um colete pesado e 300 de vida. ]</color>");
	}

	public static void MilitarEscopeteroSpawn(Player player)
	{
		Util.SetVida(player, 150f);
		player.CurrentItem = player.AddItem(ItemType.GunShotgun);
		player.AddItem(ItemType.KeycardResearchCoordinator);
		player.AddItem(ItemType.ArmorHeavy);
		player.AddAmmo(AmmoType.Ammo12Gauge, 10);
		Util.Bc(player, 8, "<color=#675645>[ Eres un: <color=#EC2121>Zombie</color> <color=#4A6F22>militar escopetero.</color> ]</color>", "<color=#675645>[ Você é um: <color=#EC2121>Zumbi</color> <color=#4A6F22>militar com escopeta.</color> ]</color>");
		Util.Bc(player, 10, "<color=#675645>[ Tienes una escopeta (10 balas extra), un chaleco pesado y 150 de vida. ]</color>", "<color=#675645>[ Você tem uma escopeta (10 balas extras), um colete pesado e 150 de vida. ]</color>");
	}

	public static void SilenciosoSpawn(Player player)
	{
		Util.Pasiva(player, EffectType.SilentWalk, byte.MaxValue);
		Util.Bc(player, 5, "<color=#EC2121>[ Eres un: <color=#addfba>Silencioso</color> ]</color>", "<color=#EC2121>[ Você é um: <color=#addfba>Silencioso</color> ]</color>");
		Util.Bc(player, 8, "<color=#EC2121>[ Eres un SCP-049-2 normal, pero tus pasos son totalmente silenciosos. ]</color>", "<color=#EC2121>[ Você é um SCP-049-2 normal, mas seus passos são totalmente silenciosos. ]</color>");
	}
}
