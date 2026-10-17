using Exiled.API.Enums;
using Exiled.API.Features;
using RadioLightPlugin.Misc;
using UnityEngine;

namespace RadioLightPlugin.Subclases;

public static class IdC
{
	public static void HeavySpawn(Player player)
	{
		player.ClearBroadcasts();
		Var.Heavys.Add(player.UserId);
		player.ClearInventory();
		player.AddItem(ItemType.ArmorHeavy);
		player.AddItem(ItemType.KeycardChaosInsurgency);
		player.AddItem(ItemType.GunShotgun);
		player.AddItem(ItemType.SCP207);
		player.AddItem(ItemType.Adrenaline);
		player.AddAmmo(AmmoType.Ammo12Gauge, 100);
		Util.Bc(player, 8, "<color=#0C0066>[ Eres un: <color=#7F0A00>Heavy.</color> ]</color>", "<color=#0C0066>[ Você é um: <color=#7F0A00>Heavy.</color> ]</color>");
		Util.Bc(player, 5, "<color=#9E4401>[ Se te han entregado grandes cantidades de munición, y tienes más vida que el resto. Usar un botiquín te da escudo extra. ]</color>", "<color=#9E4401>[ Foram entregues grandes quantidades de munição para você, e você tem mais vida que os outros. ]</color>");
		player.Scale = new Vector3(1.1f, 1.1f, 1.1f);
		Util.SetVida(player, 500f);
		player.CustomInfo = $"Heavy | {(int)player.Health} HP";
	}

	public static void ScoutSpawn(Player player)
	{
		player.ClearBroadcasts();
		player.ClearInventory();
		player.AddAmmo(AmmoType.Nato9, 100);
		player.AddItem(ItemType.ArmorLight);
		player.AddItem(ItemType.GunCrossvec);
		player.AddItem(ItemType.KeycardChaosInsurgency);
		player.AddItem(ItemType.GrenadeFlash);
		player.AddItem(ItemType.GunCOM18);
		Util.SetVida(player, 90f);
		player.AddItem(ItemType.Painkillers);
		Util.Pasiva(player, EffectType.MovementBoost, 20);
		Util.Bc(player, 8, "<color=#F35355>[ Eres un: <color=#594D44>Scout.</color> ]</color>", "<color=#F35355>[ Você é um: <color=#594D44>Scout.</color> ]</color>");
		Util.Bc(player, 5, "<color=#F35355>[ Tienes 90 de vida, chaleco ligero y analgésicos, eres más rápido que el resto y puedes ralentizar a la gente disparándoles con tu COM-18. ]</color>", "<color=#F35355>[ Você tem 90 de vida, colete leve e analgésicos, é mais rápido que os outros e pode desacelerar as pessoas atirando nelas com seu COM-18. ]</color>");
		Var.Scout.Add(player.UserId);
	}

	public static void SpySpawn(Player player)
	{
		player.ClearBroadcasts();
		player.ClearInventory();
		player.AddItem(ItemType.GunRevolver);
		player.AddItem(ItemType.Medkit);
		player.AddItem(ItemType.ArmorCombat);
		player.AddItem(ItemType.KeycardChaosInsurgency);
		player.AddItem(ItemType.GrenadeFlash);
		player.AddItem(ItemType.SCP268);
		player.AddItem(ItemType.Flashlight);
		player.AddAmmo(AmmoType.Ammo44Cal, 30);
		Util.Bc(player, 5, "<color=#00ffff>[ Eres un: </color><color=#e32433>Spy</color><color=#00ffff> ]</color>", "<color=#00ffff>[ Você é um: </color><color=#e32433>Spy</color><color=#00ffff> ]</color>");
		Util.Bc(player, 8, "<color=#00ffff>[ Puedes disfrazarte de MTF al usar la linterna (con un cooldown de 1 minuto). Pero al disparar, tu disfraz se quita. También puedes volverte invisible al ponerte el sombrero. ]</color>", "<color=#00ffff>[ Você pode se disfarçar de MTF usando a lanterna (com um cooldown de 1 minuto). No entanto, disparar irá remover seu disfarce. Você também pode se tornar invisível ao usar o chapéu. ]</color>");
		Util.Bc(player, 8, "<color=#00ffff>[ Al presionar ALT, podrás activar o desactivar tu Dead Ringer, el cual simula tu muerte. ]</color>", "<color=#00ffff>[ Pressionando ALT, você pode ativar ou desativar seu Dead Ringer, que simula sua morte. ]</color>");
		Var.DeadRinger.Remove(player);
		Var.DeadRingerUsado.Remove(player);
		Var.Spy.Add(player);
		Habilidades.Asignar(player, "Dead Ringer", 0f, DeadRinger.Alternar, linterna: false);
	}

	public static void DemomanSpawn(Player player)
	{
		player.ClearBroadcasts();
		player.ClearInventory();
		player.AddItem(ItemType.GunRevolver);
		player.AddItem(ItemType.KeycardChaosInsurgency);
		Util.SetVida(player, 80f);
		Util.Bc(player, 5, "<color=#D1484D>[ Eres un: <color=#433939>Demoman.</color> ]</color>", "<color=#D1484D>[ Você é um: <color=#433939>Demoman.</color> ]</color>");
		Util.Bc(player, 8, "<color=#D1484D>[ Tu única arma es un lanzagranadas, por lo que no puedes agarrar otra arma. ]</color>", "<color=#D1484D>[ Sua única arma é um lançagranadas, então você não pode pegar outra arma. ]</color>");
		Var.Demoman.Add(player);
	}

	public static void SilentChaosSpawn(Player player)
	{
		player.ClearBroadcasts();
		player.ClearInventory();
		player.AddItem(ItemType.GunCOM15);
		player.AddItem(ItemType.SCP1853);
		player.AddItem(ItemType.ArmorCombat);
		player.AddItem(ItemType.Medkit);
		player.AddItem(ItemType.Adrenaline);
		player.AddItem(ItemType.GrenadeFlash);
		player.AddAmmo(AmmoType.Nato9, 60);
		Util.SetVida(player, 90f);
		Util.Pasiva(player, EffectType.MovementBoost, 30);
		Util.Pasiva(player, EffectType.SilentWalk, byte.MaxValue);
		Util.Bc(player, 5, "<color=#D1484D>[ Eres un: <color=#433939>Silent Chaos.</color> ]</color>", "<color=#D1484D>[ Você é um: <color=#433939>Silent Chaos.</color> ]</color>");
		Util.Bc(player, 8, "<color=#D1484D>[ Tus pasos son silenciosos y eres más rápido. ]</color>", "<color=#D1484D>[ Seus passos são silenciosos e você é mais rápido. ]</color>");
	}
}
