using System.Collections.Generic;
using System.Text;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Doors;
using MEC;
using UnityEngine;
using PlayerRoles;
using RadioLightPlugin.Misc;

namespace RadioLightPlugin.Subclases;

public static class Guardia
{
	private const float CarceleroRadio = 10f;

	private const float CarceleroDuracion = 7f;

	private const float CarceleroCooldown = 60f;

	private const float VigiaDuracion = 8f;

	private const float VigiaCooldown = 90f;

	public static void TraidorSpawn(Player player)
	{
		Var.Traidor.Add(player.UserId);
		player.ClearInventory();
		player.AddItem(ItemType.ArmorLight);
		player.AddItem(ItemType.KeycardGuard);
		player.AddItem(ItemType.Medkit);
		player.AddItem(ItemType.GunFSP9);
		player.AddItem(ItemType.Flashlight);
		player.AddItem(ItemType.GrenadeFlash);
		player.AddItem(ItemType.Radio);
		player.AddAmmo(AmmoType.Nato9, 50);
		Util.Bc(player, 8, "[ Eres un: <color=#7F0A00>Traidor.</color> ]", "[ Você é um: <color=#7F0A00>Traidor.</color> ]");
		Util.Bc(player, 10, "[ <color=#9E4401>Puedes empezar a traicionar a tu equipo de guardias cuando uses tu linterna, lo que cambiará tu rol a Insurgente del Caos.</color> ]", "[ <color=#9E4401>Você pode começar a trair sua equipe de guardas ao usar sua lanterna, o que mudará seu papel para Insurgente do Caos.</color> ]");
	}

	public static void HeadguardSpawn(Player player)
	{
		Var.QuitarSubclases(player);
		Var.YaSpawneó = true;
		Var.HeadGuard.Add(player);
		Var.CooldownHeadguard.Remove(player);
		player.ClearInventory();
		Util.SetVida(player, 130f);
		player.AddItem(ItemType.KeycardMTFOperative);
		player.AddItem(ItemType.GunE11SR);
		player.AddItem(ItemType.Medkit);
		player.AddItem(ItemType.Adrenaline);
		player.AddItem(ItemType.Radio);
		player.AddItem(ItemType.ArmorCombat);
		player.AddItem(ItemType.GrenadeFlash);
		player.AddItem(ItemType.Flashlight);
		player.AddAmmo(AmmoType.Nato556, 120);
		player.AddAmmo(AmmoType.Nato9, 90);
		Util.Bc(player, 5, "<color=#002B66>[ <color=#C4C4C4>Eres el</color> <color=#666666>Cabecilla de los Guardias.</color> ]</color>", "<color=#002B66>[ <color=#C4C4C4>Você é o</color> <color=#666666>Líder dos Guardas.</color> ]</color>");
		Util.Bc(player, 8, "<color=#002B66>[ <color=#C4C4C4>Puedes darle un subidón de moral a ti y a tus compañeros cercanos al presionar 'ALT' (cooldown de 1 minuto y medio).</color> ]</color>", "<color=#002B66>[ <color=#C4C4C4>Você pode dar um impulso de moral a você e aos seus colegas pressionando 'ALT' (cooldown de 1 minuto e meio).</color> ]</color>");
		Var.Coroutines.Add(Timing.RunCoroutine(Coroutines.HeadGuardBC(player)));
		Habilidades.Asignar(player, "Impulso de moral", 0f, delegate(Player p)
		{
			if (Var.CooldownHeadguard.Contains(p))
			{
				EffectsHUD.Notify(p, "<color=#e32433>Tu impulso de moral todavía se está recargando.</color>", 2f);
				return false;
			}
			Var.Coroutines.Add(Timing.RunCoroutine(Coroutines.ImpulsoMoral(p)));
			return true;
		});
	}

	public static void NoobSpawn(Player player)
	{
		Util.SetVida(player, 130f);
		player.ClearInventory();
		player.AddItem(ItemType.ArmorCombat);
		player.AddItem(ItemType.KeycardGuard);
		player.AddItem(ItemType.GunE11SR);
		player.AddItem(ItemType.Medkit);
		player.AddItem(ItemType.Radio);
		player.AddAmmo(AmmoType.Nato556, 120);
		player.AddAmmo(AmmoType.Nato9, 90);
		Util.Bc(player, 8, "<color=#FF8E00>[ Eres un: <color=#fc5e03>Guardia Noob.</color> ]</color>", "<color=#FF8E00>[ Você é um: <color=#fc5e03>Guarda Noob.</color> ]</color>");
		Util.Bc(player, 10, "<color=#FF8E00>[ Tienes 130 de vida, un chaleco de combate, un rifle E-11 y un radio. ]</color>", "<color=#FF8E00>[ Você tem 130 de vida, um colete de combate, um rifle E-11 e um rádio. ]</color>");
	}

	public static void MilitarSpawn(Player player)
	{
		Util.SetVida(player, 100f);
		player.ClearInventory();
		player.AddItem(ItemType.ArmorCombat);
		player.AddItem(ItemType.GunAK);
		player.AddItem(ItemType.Medkit);
		player.AddItem(ItemType.KeycardMTFPrivate);
		player.AddAmmo(AmmoType.Nato762, 30);
		Var.Militar = player.UserId;
		Util.Bc(player, 8, "<color=#FF8E00>[ Eres un: <color=#fc5e03>Militar.</color> ]</color>", "<color=#FF8E00>[ Você é um: <color=#fc5e03>Militar.</color> ]</color>");
		Util.Bc(player, 10, "<color=#FF8E00>[ Tienes un chaleco de combate, un AK y un botiquín. ]</color>", "<color=#FF8E00>[ Você tem um colete de combate, um AK e um kit médico. ]</color>");
	}

	public static void ExpertoSpawn(Player player)
	{
		player.ClearInventory();
		player.AddItem(ItemType.ArmorCombat);
		player.AddItem(ItemType.GunCrossvec);
		player.AddItem(ItemType.Medkit);
		player.AddItem(ItemType.KeycardMTFPrivate);
		player.AddAmmo(AmmoType.Nato9, 30);
		Var.Militar = player.UserId;
		Util.Bc(player, 8, "<color=#FF8E00>[ Eres un: <color=#fc5e03>Experto en contención de SCP.</color> ]</color>", "<color=#FF8E00>[ Você é um: <color=#fc5e03>Especialista em contenção de SCP.</color> ]</color>");
		Util.Bc(player, 10, "<color=#FF8E00>[ Tienes un chaleco de combate, una Crossvec y un botiquín. ]</color>", "<color=#FF8E00>[ Você tem um colete de combate, uma Crossvec e um kit médico. ]</color>");
	}

	public static void ExpertoEnScpSpawn(Player player)
	{
		player.ClearInventory();
		player.AddItem(ItemType.ArmorCombat);
		player.AddItem(ItemType.GunSCP127);
		player.AddItem(ItemType.Medkit);
		player.AddItem(ItemType.KeycardMTFPrivate);
		Var.Militar = player.UserId;
		Util.Bc(player, 8, "<color=#FF8E00>[ Eres un: <color=#fc5e03>Experto en contención de SCP.</color> ]</color>", "<color=#FF8E00>[ Você é um: <color=#fc5e03>Especialista em contenção de SCP.</color> ]</color>");
		Util.Bc(player, 10, "<color=#FF8E00>[ Tienes un chaleco de combate, un SCP-127 y un botiquín. ]</color>", "<color=#FF8E00>[ Você tem um colete de combate, uma SCP-127 e um kit médico. ]</color>");
	}

	public static void CarceleroSpawn(Player player)
	{
		if (!player.IsInventoryFull)
		{
			player.AddItem(ItemType.GrenadeFlash);
		}
		player.AddAmmo(AmmoType.Nato9, 30);
		Util.Bc(player, 6, "<color=#5B6370>[ Eres un: <color=#C9A227>Carcelero</color> ]</color>", "<color=#5B6370>[ Você é um: <color=#C9A227>Carcereiro</color> ]</color>");
		Util.Bc(player, 10, $"<color=#5B6370>[ Con tu habilidad cierras y bloqueas todas las puertas a {10f:0} m durante {7f:0} s. Ideal para atrapar Clase-D fugados. ]</color>", $"<color=#5B6370>[ Com sua habilidade você fecha e tranca todas as portas a {10f:0} m por {7f:0} s. ]</color>");
		Habilidades.Asignar(player, "Encierro", 60f, Encierro);
	}

	private static bool Encierro(Player p)
	{
		if (Var.BetterOmegaWarheadActivated || Warhead.IsInProgress || Warhead.IsDetonated)
		{
			EffectsHUD.Notify(p, "<color=#e32433>Con la nuke activa las puertas no responden.</color>");
			return false;
		}
		float num = 100f;
		int num2 = 0;
		foreach (Door item in Door.List)
		{
			if (!item.IsElevator && !item.IsGate && !item.IsCheckpoint && !item.IsLocked && !(item is BreakableDoor { IsDestroyed: not false }) && !((item.Position - p.Position).sqrMagnitude > num))
			{
				item.IsOpen = false;
				item.Lock(7f, DoorLockType.AdminCommand);
				num2++;
			}
		}
		if (num2 == 0)
		{
			EffectsHUD.Notify(p, "<color=#e32433>No hay puertas cerca para bloquear.</color>", 2f);
			return false;
		}
		EffectsHUD.Notify(p, $"<color=#C9A227><b>¡Encierro!</b></color> <color=#859CC7>{num2} puertas bloqueadas por {7f:0} s.</color>", 4f);
		return true;
	}

	public static void VigiaSpawn(Player player)
	{
		if (!player.IsInventoryFull)
		{
			player.AddItem(ItemType.Adrenaline);
		}
		Util.Bc(player, 6, "<color=#5B6370>[ Eres un: <color=#57e5f7>Vigía</color> ]</color>", "<color=#5B6370>[ Você é um: <color=#57e5f7>Vigia</color> ]</color>");
		Util.Bc(player, 10, $"<color=#5B6370>[ Con tu habilidad revisas las cámaras durante {8f:0} s y ves en qué sala está cada Clase-D e Insurgente. ]</color>", $"<color=#5B6370>[ Com sua habilidade você verifica as câmeras por {8f:0} s e vê onde está cada Classe-D e Insurgente. ]</color>");
		Habilidades.Asignar(player, "Cámaras de seguridad", 90f, delegate(Player p)
		{
			Var.Coroutines.Add(Timing.RunCoroutine(Camaras(p, p.Role.Type)));
			return true;
		});
	}

	private static IEnumerator<float> Camaras(Player vigia, RoleTypeId rol)
	{
		StringBuilder sb = new StringBuilder(256);
		for (float t = 0f; t < 8f; t += 0.5f)
		{
			if (!Util.Vivo(vigia) || vigia.Role.Type != rol)
			{
				yield break;
			}
			sb.Clear();
			int total = 0;
			foreach (Player objetivo in Player.List)
			{
				if (objetivo.IsAlive && !objetivo.IsNPC && (objetivo.Role.Team == Team.ClassD || objetivo.Role.Team == Team.ChaosInsurgency))
				{
					total++;
					if (total <= 8)
					{
						sb.Append("<color=#FF8E00>").Append(Util.NombreRol(objetivo.Role.Type)).Append("</color> · ")
							.Append(Util.Sala(objetivo))
							.Append(" <color=#AAAAAA>· ")
							.Append(Mathf.RoundToInt(Vector3.Distance(vigia.Position, objetivo.Position)))
							.Append(" m</color>")
							.Append('\n');
					}
				}
			}
			if (total > 8)
			{
				sb.Append("<color=#AAAAAA>+").Append(total - 8).Append(" más</color>\n");
			}
			string cuerpo = ((total == 0) ? "<color=#4CAF50>Sin fugados a la vista.</color>" : sb.ToString());
			// [cambio] en vivo: se actualiza cada 0,5 s y se redibuja al instante (antes era una "foto" por segundo)
			EffectsHUD.Panel(vigia, "camaras", "<color=#57e5f7><b>CÁMARAS · EN VIVO</b></color> <color=#AAAAAA>" + Mathf.CeilToInt(8f - t) + " s</color>\n" + cuerpo.TrimEnd(new char[1] { '\n' }), 1.5f);
			EffectsHUD.RefrescarAhora(vigia);
			yield return Timing.WaitForSeconds(0.5f);
		}
		EffectsHUD.QuitarPanel(vigia, "camaras");
		EffectsHUD.RefrescarAhora(vigia);
	}
}
