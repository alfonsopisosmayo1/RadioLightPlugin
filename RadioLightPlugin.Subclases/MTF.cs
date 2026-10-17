using System.Collections.Generic;
using System.Globalization;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Doors;
using Exiled.Events.EventArgs.Server;
using Exiled.Events.Features;
using Exiled.Events.Handlers;
using MEC;
using PlayerRoles;
using RadioLightPlugin.Misc;
using UnityEngine;

namespace RadioLightPlugin.Subclases;

public static class MTF
{
	private const float MedicHealRadiusSqr = 36f;

	private const float MedicHealPerSecond = 0.5f;

	private const float TripodeDuracion = 6f;

	private const float TripodeCooldown = 60f;

	internal const string NombreTripode = "Trípode";

	internal const float ArtilleroDañoScp = 0.55f;

	internal const float TripodeBonus = 1.25f;

	private const float BrechaAlcance = 3.5f;

	private const float BrechaCooldown = 50f;

	private static readonly HashSet<DoorType> PuertasImposibles = new HashSet<DoorType>
	{
		DoorType.Scp079First,
		DoorType.Scp079Second,
		DoorType.Scp079Armory,
		DoorType.Scp106Primary,
		DoorType.Scp106Secondary,
		DoorType.Scp106Checkpoint,
		DoorType.Scp096,
		DoorType.Scp049Armory,
		DoorType.Scp173Armory,
		DoorType.Scp173Gate,
		DoorType.Scp330Chamber,
		DoorType.Scp939Cryo,
		DoorType.GR18Inner,
		DoorType.HIDChamber,
		DoorType.HIDUpper,
		DoorType.HIDUpper,
		DoorType.HIDLower,
		DoorType.LczArmory,
		DoorType.HczArmory,
		DoorType.NukeArmory,
		DoorType.NukeSurface,
		DoorType.GateAArmory,
		DoorType.CheckpointArmoryA,
		DoorType.CheckpointArmoryB,
		DoorType.Intercom,
		DoorType.ServerRoomCloset,
		DoorType.Hcz127Lab,
		DoorType.TestRoom,
		DoorType.EscapePrimary,
		DoorType.EscapeSecondary,
		DoorType.EscapeFinal
	};

	private const float BlindajeRadio = 7f;

	private const float BlindajeCooldown = 70f;

	private const float MarcaAlcance = 60f;

	private const float MarcaAngulo = 8f;

	private const float MarcaDuracion = 10f;

	private const float MarcaCooldown = 40f;

	private const float RepliegueCooldown = 150f;

	public static void BulldozerSpawn(Exiled.API.Features.Player player)
	{
		player.ClearBroadcasts();
		Var.Bulldozers.Add(player.UserId);
		player.ClearInventory();
		player.AddItem(ItemType.ArmorHeavy);
		player.AddItem(ItemType.KeycardMTFCaptain);
		player.AddItem(ItemType.GunShotgun);
		player.AddItem(ItemType.Radio);
		player.AddItem(ItemType.Medkit);
		player.AddAmmo(AmmoType.Ammo12Gauge, 80);
		player.AddAmmo(AmmoType.Nato556, 80);
		player.AddAmmo(AmmoType.Nato9, 60);
		Util.Bc(player, 8, "<color=#0C0066>[ Eres un: <color=#1D5D1A>Bulldozer.</color> ]</color>", "<color=#0C0066>[ Você é um: <color=#1D5D1A>Bulldozer.</color> ]</color>");
		Util.Bc(player, 5, "<color=#9E4401>[ Se te han entregado grandes cantidades de munición, tienes más vida y escudo que el resto. ]</color>", "<color=#9E4401>[ Grandes quantidades de munição foram fornecidas a você, e você tem mais vida e escudo do que os outros. ]</color>");
		player.Scale = new Vector3(1.1f, 1.1f, 1.1f);
		Util.SetVida(player, 150f);
		Util.Pasiva(player, EffectType.Exhausted, 1);
		player.AddAhp(1250f, 1250f, 0f, 0.8f);
	}

	public static void SpySpawn(Exiled.API.Features.Player player)
	{
		player.ClearBroadcasts();
		player.ClearInventory();
		player.AddItem(ItemType.GunRevolver);
		player.AddItem(ItemType.Medkit);
		player.AddItem(ItemType.ArmorCombat);
		player.AddItem(ItemType.Radio);
		player.AddItem(ItemType.KeycardMTFOperative);
		player.AddItem(ItemType.GrenadeFlash);
		player.AddItem(ItemType.SCP268);
		player.AddItem(ItemType.Flashlight);
		player.AddAmmo(AmmoType.Ammo44Cal, 30);
		Util.Bc(player, 5, "<color=#00ffff>[ Eres un: </color><color=#e32433>Spy.</color> <color=#00ffff>]</color>", "<color=#00ffff>[ Você é um: </color><color=#e32433>Spy.</color> <color=#00ffff>]</color>");
		Util.Bc(player, 8, "<color=#00ffff>[ Puedes disfrazarte de Insurgente del Caos al usar la linterna (con un cooldown de 1 minuto). Pero al disparar, tu disfraz se quita. También puedes volverte invisible al ponerte el sombrero. ]</color>", "<color=#00ffff>[ Você pode se disfarçar de Insurgente do Caos ao usar a lanterna (com um cooldown de 1 minuto). Mas ao disparar, seu disfarce será removido. Você também pode ficar invisível ao colocar o chapéu. ]</color>");
		Util.Bc(player, 8, "<color=#00ffff>[ Al presionar ALT, podrás activar o desactivar tu Dead Ringer, el cual simula tu muerte. ]</color>", "<color=#00ffff>[ Ao pressionar ALT, você pode ativar ou desativar seu Dead Ringer, que simula sua morte. ]</color>");
		Var.DeadRinger.Remove(player);
		Var.DeadRingerUsado.Remove(player);
		Var.Spy.Add(player);
		Habilidades.Asignar(player, "Dead Ringer", 0f, DeadRinger.Alternar, linterna: false);
	}

	public static void TaserSpawn(Exiled.API.Features.Player player)
	{
		player.ClearBroadcasts();
		player.ClearInventory();
		player.AddItem(ItemType.ArmorCombat);
		player.AddItem(ItemType.Radio);
		player.AddItem(ItemType.Painkillers);
		player.AddItem(ItemType.ParticleDisruptor);
		player.AddItem(ItemType.KeycardMTFOperative);
		player.AddItem(ItemType.GunE11SR);
		player.AddAmmo(AmmoType.Nato556, 40);
		Util.Bc(player, 5, "<color=#FFF300>[ Eres un <color=#0A0A0A>Taser</color>. ]</color>", "<color=#FFF300>[ Você é um <color=#0A0A0A>Taser</color>. ]</color>");
		Util.Bc(player, 8, "<color=#B5C800>[ Puedes inmovilizar a la gente disparándoles con tu Disruptor de Partículas. Tiene un cooldown de 30 segundos, así que úsalo sabiamente. ]</color>", "<color=#B5C800>[ Você pode imobilizar pessoas atirando nelas com seu Disruptor de Partículas. Tem um tempo de recarga de 30 segundos, então use com sabedoria. ]</color>");
		Var.CooldownTaser.Remove(player);
		Var.Taser.Add(player);
	}

	public static void MedicSpawn(Exiled.API.Features.Player player)
	{
		player.ClearBroadcasts();
		player.ClearInventory();
		player.AddItem(ItemType.GunRevolver);
		player.AddItem(ItemType.KeycardMTFOperative);
		player.AddItem(ItemType.SCP500);
		player.AddItem(ItemType.Medkit);
		player.AddItem(ItemType.Medkit);
		player.AddItem(ItemType.Adrenaline);
		player.AddAmmo(AmmoType.Ammo44Cal, 30);
		string text = 0.5f.ToString("0.##", CultureInfo.InvariantCulture);
		Util.Bc(player, 5, "<color=#385399>[ Eres un <color=#ed1b24>Medic</color>. ]</color>", "<color=#385399>[ Você é um <color=#ed1b24>Médico</color>. ]</color>");
		Util.Bc(player, 8, "<color=#385399>[ Curas automáticamente " + text + " HP/s a todos los aliados cerca de ti mientras estés vivo. ]</color>", "<color=#385399>[ Você cura automaticamente " + text + " HP/s a todos os aliados perto de você enquanto estiver vivo. ]</color>");
		Var.Medic.Add(player);
		Var.Coroutines.Add(Timing.RunCoroutine(MedicHealAura(player)));
	}

	internal static IEnumerator<float> MedicHealAura(Exiled.API.Features.Player medic)
	{
		while (Util.Vivo(medic) && Var.Medic.Contains(medic))
		{
			yield return Timing.WaitForSeconds(1f);
			if (!Util.Vivo(medic) || !Var.Medic.Contains(medic))
			{
				break;
			}
			Vector3 pos = medic.Position;
			Side lado = medic.Role.Side;
			foreach (Exiled.API.Features.Player ally in Exiled.API.Features.Player.List)
			{
				if (!(ally == medic) && ally.IsAlive && ally.Role.Side == lado && (ally.Position - pos).sqrMagnitude <= 36f)
				{
					ally.Heal(0.5f);
				}
			}
		}
	}

	public static void HammerDown(Exiled.API.Features.Player player)
	{
		player.ClearBroadcasts();
		player.ClearInventory();
		player.AddItem(ItemType.Jailbird);
		player.AddItem(ItemType.Medkit);
		player.AddItem(ItemType.KeycardMTFOperative);
		player.AddItem(ItemType.ArmorCombat);
		player.AddItem(ItemType.GrenadeHE);
		Util.Pasiva(player, EffectType.MovementBoost, 20);
		Util.Bc(player, 5, "<color=#3b3737>[ Eres un <color=#3bb8d1>Hammer Down</color>. ]</color>", "<color=#3b3737>[ Você é um <color=#3bb8d1>Hammer Down</color>. ]</color>");
		Util.Bc(player, 8, "<color=#3b3737>[ Eres más rápido y tienes un Jailbird con el cual recuperas 10 HP al matar, pero no puedes agarrar otra arma. ]</color>", "<color=#3b3737>[ Você é mais rápido e tem um Jailbird que recupera 10 HP ao matar, mas não pode pegar outra arma. ]</color>");
		Var.HammerDown.Add(player.ReferenceHub);
	}

	public static void MachetazoSpawn(Exiled.API.Features.Player player)
	{
		player.ClearBroadcasts();
		Var.MachetazoSpawned = true;
		player.ClearInventory();
		player.AddItem(ItemType.SCP1509);
		player.AddItem(ItemType.ArmorCombat);
		player.AddItem(ItemType.KeycardMTFOperative);
		player.AddItem(ItemType.Radio);
		player.AddItem(ItemType.Medkit);
		player.AddItem(ItemType.Medkit);
		Util.SetVida(player, 125f);
		Util.Pasiva(player, EffectType.MovementBoost, 10);
		Util.Bc(player, 5, "<color=#0C0066>[ Eres un: <color=#1D5D1A>Machetazo.</color> ]</color>", "<color=#0C0066>[ Você é um: <color=#1D5D1A>Machetazo.</color> ]</color>");
		Util.Bc(player, 8, "<color=#9E4401>[ Eres más rápido que los demás y cuentas con un machete. ]</color>", "<color=#9E4401>[ Você é mais rápido que os outros e tem um facão. ]</color>");
	}

	public static void GrupoDeRespuestaSpawn(Exiled.API.Features.Player player)
	{
		Var.QuitarSubclases(player);
		player.ClearBroadcasts();
		Var.GrupoDeRespuestaSpawned = true;
		player.ClearInventory();
		Util.SetVida(player, 120f);
		player.AddItem(ItemType.ArmorCombat);
		player.AddItem(ItemType.KeycardMTFOperative);
		player.AddItem(ItemType.GunE11SR);
		player.AddItem(ItemType.Radio);
		player.AddItem(ItemType.Medkit);
		player.AddItem(ItemType.GrenadeHE);
		player.AddAmmo(AmmoType.Nato556, 120);
		Util.Bc(player, 5, "<color=#0C0066>[ Eres el: <color=#1D5D1A>Grupo de Respuesta Rápida.</color> ]</color>", "<color=#0C0066>[ Você é o: <color=#1D5D1A>Grupo de Resposta Rápida.</color> ]</color>");
		Util.Bc(player, 8, "<color=#9E4401>[ Apareces para detener a los Chaos. Tienes 120 de vida, chaleco de combate, E-11, una granada y un botiquín. ]</color>", "<color=#9E4401>[ Você apareceu para deter os Chaos. Tem 120 de vida, colete de combate, E-11, granada e kit médico. ]</color>");
	}

	public static void ArtilleroSpawn(Exiled.API.Features.Player player)
	{
		Util.Reemplazar(player, ItemType.GunE11SR, ItemType.GunLogicer);
		player.AddAmmo(AmmoType.Nato762, 100);
		Util.Bc(player, 6, "<color=#0096FF>[ Eres un: <color=#C9A227>Artillero</color> ]</color>", "<color=#0096FF>[ Você é um: <color=#C9A227>Artilheiro</color> ]</color>");
		Util.Bc(player, 10, $"<color=#0096FF>[ Tienes una Logicer (hace menos daño a los SCPs). Con tu habilidad montas el trípode: {6f:0} s sin poder moverte, pero haces +25% de daño y recibes menos. ]</color>", $"<color=#0096FF>[ Você tem uma Logicer (causa menos dano aos SCPs). Com sua habilidade monta o tripé: {6f:0} s sem se mover, mas causa +25% de dano e recebe menos. ]</color>");
		Habilidades.Asignar(player, "Trípode", 60f, delegate(Exiled.API.Features.Player p)
		{
			p.EnableEffect(EffectType.Ensnared, 6f);
			p.EnableEffect(EffectType.DamageReduction, 20, 6f);
			Var.Tripode[p] = Time.time + 6f;
			EffectsHUD.Notify(p, "<color=#C9A227><b>Trípode montado.</b></color> <color=#859CC7>+25% de daño.</color>");
			return true;
		});
	}

	public static void RompebrechasSpawn(Exiled.API.Features.Player player)
	{
		Util.Reemplazar(player, ItemType.GunE11SR, ItemType.GunShotgun);
		player.AddAmmo(AmmoType.Ammo12Gauge, 28);
		Util.Bc(player, 6, "<color=#0096FF>[ Eres un: <color=#e32433>Rompebrechas</color> ]</color>", "<color=#0096FF>[ Você é um: <color=#e32433>Arrombador</color> ]</color>");
		Util.Bc(player, 10, "<color=#0096FF>[ Tienes una escopeta. Con tu habilidad pones una carga en la puerta más cercana: las normales revientan y las blindadas se abren y quedan trabadas 8 s. ]</color>", "<color=#0096FF>[ Você tem uma escopeta. Com sua habilidade coloca uma carga na porta mais próxima: as normais explodem e as blindadas abrem e ficam travadas 8 s. ]</color>");
		Habilidades.Asignar(player, "Carga de brecha", 50f, CargaDeBrecha);
	}

	private static bool CargaDeBrecha(Exiled.API.Features.Player p)
	{
		float distance;
		Door closest = Door.GetClosest(p.Position, out distance);
		if (closest == null || distance > 3.5f || closest.IsElevator)
		{
			EffectsHUD.Notify(p, "<color=#e32433>Acercate a una puerta para poner la carga.</color>", 2f);
			return false;
		}
		if (PuertaImposible(closest))
		{
			EffectsHUD.Notify(p, "<color=#e32433>Esta puerta es demasiado reforzada para una carga.</color>", 2f);
			return false;
		}
		if (closest is BreakableDoor { IsDestroyed: not false })
		{
			EffectsHUD.Notify(p, "<color=#e32433>Esa puerta ya está destruida.</color>", 2f);
			return false;
		}
		Exiled.API.Features.Map.ExplodeEffect(closest.Position + Vector3.up, ProjectileType.FragGrenade);
		if (!Util.EsCheckpoint(closest) && closest is BreakableDoor { IsBreakable: not false } breakableDoor2 && breakableDoor2.Break())
		{
			EffectsHUD.Notify(p, "<color=#e32433><b>¡Brecha abierta!</b></color>");
			return true;
		}
		closest.IsOpen = true;
		closest.Lock(8f, DoorLockType.AdminCommand);
		EffectsHUD.Notify(p, "<color=#e32433><b>¡Puerta forzada!</b></color> <color=#859CC7>Queda abierta 8 s.</color>");
		return true;
	}

	private static bool PuertaImposible(Door door)
	{
		return EsPuertaReforzada(door) || door.IsLocked;
	}

	internal static bool EsPuertaReforzada(Door door)
	{
		return door != null && PuertasImposibles.Contains(door.Type);
	}

	public static void IngenieroSpawn(Exiled.API.Features.Player player)
	{
		Util.Bc(player, 6, "<color=#0096FF>[ Eres un: <color=#4CAF50>Ingeniero de campo</color> ]</color>", "<color=#0096FF>[ Você é um: <color=#4CAF50>Engenheiro de campo</color> ]</color>");
		Util.Bc(player, 10, $"<color=#0096FF>[ Con tu habilidad despliegas blindaje: tú y tus aliados a {7f:0} m reciben 50 de escudo. ]</color>", $"<color=#0096FF>[ Com sua habilidade você implanta blindagem: você e seus aliados a {7f:0} m recebem 50 de escudo. ]</color>");
		Habilidades.Asignar(player, "Blindaje de campo", 70f, delegate(Exiled.API.Features.Player p)
		{
			int num = 0;
			foreach (Exiled.API.Features.Player item in Util.Cercanos(p.Position, 7f, (Exiled.API.Features.Player x) => x.IsHuman && x.Role.Side == p.Role.Side))
			{
				item.AddAhp(50f, 50f, 0.5f, 0.7f, 10f);
				if (item != p)
				{
					EffectsHUD.Notify(item, "<color=#4CAF50>" + p.Nickname + " te dio blindaje.</color>");
				}
				num++;
			}
			EffectsHUD.Notify(p, $"<color=#4CAF50><b>Blindaje desplegado</b></color> <color=#859CC7>({num} protegidos).</color>");
			return true;
		});
	}

	public static void TiradorSpawn(Exiled.API.Features.Player player)
	{
		player.AddAmmo(AmmoType.Nato556, 60);
		Util.Bc(player, 6, "<color=#0096FF>[ Eres un: <color=#e32433>Tirador</color> ]</color>", "<color=#0096FF>[ Você é um: <color=#e32433>Atirador</color> ]</color>");
		Util.Bc(player, 10, $"<color=#0096FF>[ Apunta a un enemigo y usa tu habilidad para marcarlo {10f:0} s: la Fundación ve dónde está y le hace +25% de daño. ]</color>", $"<color=#0096FF>[ Mire em um inimigo e use sua habilidade para marcá-lo {10f:0} s: a Fundação vê onde ele está e causa +25% de dano. ]</color>");
		Habilidades.Asignar(player, "Marcar objetivo", 40f, MarcarObjetivo);
	}

	private static bool MarcarObjetivo(Exiled.API.Features.Player p)
	{
		Vector3 position = p.CameraTransform.position;
		Vector3 forward = p.CameraTransform.forward;
		Exiled.API.Features.Player player = null;
		float num = 8f;
		foreach (Exiled.API.Features.Player item in Exiled.API.Features.Player.List)
		{
			if (!Util.EsEnemigo(p, item))
			{
				continue;
			}
			Vector3 to = item.Position - position;
			if (!(to.sqrMagnitude > 3600f))
			{
				float num2 = Vector3.Angle(forward, to);
				if (num2 < num)
				{
					num = num2;
					player = item;
				}
			}
		}
		if (player == null)
		{
			EffectsHUD.Notify(p, "<color=#e32433>No hay ningún enemigo en la mira.</color>", 2f);
			return false;
		}
		Var.Marcados[player] = Time.time + 10f;
		player.ShowHint("<color=#e32433><b>¡Te marcaron!</b></color> <color=#859CC7>La Fundación sabe dónde estás.</color>", 4f);
		string text = "<color=#e32433>OBJETIVO MARCADO:</color> <color=#859CC7>" + player.Nickname + " (" + Util.NombreRol(player.Role.Type) + ") en " + Util.Sala(player) + "</color>";
		foreach (Exiled.API.Features.Player item2 in Exiled.API.Features.Player.List)
		{
			if (item2.IsAlive && item2.Role.Side == p.Role.Side)
			{
				EffectsHUD.Notify(item2, text, 5f);
			}
		}
		return true;
	}

	public static void CoordinadorSpawn(Exiled.API.Features.Player player)
	{
		Util.Bc(player, 6, "<color=#003ECA>[ Eres el: <color=#C9A227>Coordinador</color> ]</color>", "<color=#003ECA>[ Você é o: <color=#C9A227>Coordenador</color> ]</color>");
		Util.Bc(player, 10, "<color=#003ECA>[ Una vez por vida puedes pedir un refuerzo: un espectador aparece a tu lado como cadete MTF. ]</color>", "<color=#003ECA>[ Uma vez por vida você pode pedir reforço: um espectador aparece ao seu lado como cadete MTF. ]</color>");
		Habilidades.Asignar(player, "Pedir refuerzo", 0f, PedirRefuerzo);
	}

	private static bool PedirRefuerzo(Exiled.API.Features.Player p)
	{
		if (Exiled.API.Features.Warhead.IsDetonated || p.Zone == ZoneType.Pocket || p.IsInPocketDimension)
		{
			EffectsHUD.Notify(p, "<color=#e32433>No hay forma de enviarte refuerzos aquí.</color>");
			return false;
		}
		List<Exiled.API.Features.Player> list = new List<Exiled.API.Features.Player>();
		foreach (Exiled.API.Features.Player item in Exiled.API.Features.Player.List)
		{
			if (item.Role.Type == RoleTypeId.Spectator && !item.IsOverwatchEnabled && !item.IsNPC && !Var.GenteGulag.Contains(item))
			{
				list.Add(item);
			}
		}
		if (list.Count == 0)
		{
			EffectsHUD.Notify(p, "<color=#e32433>No hay nadie disponible para enviarte.</color>");
			return false;
		}
		Exiled.API.Features.Player refuerzo = list[Var.random.Next(list.Count)];
		Vector3 pos = p.Position;
		refuerzo.Role.Set(RoleTypeId.NtfPrivate, SpawnReason.ForceClass, RoleSpawnFlags.AssignInventory);
		Timing.CallDelayed(0.3f, delegate
		{
			if (Util.Vivo(refuerzo))
			{
				refuerzo.Position = pos + Vector3.up * 0.2f;
				refuerzo.EnableEffect(EffectType.SpawnProtected, 5f);
				refuerzo.Broadcast(6, "<color=#003ECA>Fuiste enviado como refuerzo de " + p.Nickname + ".</color>");
			}
		});
		EffectsHUD.Notify(p, "<color=#C9A227><b>Refuerzo en camino:</b></color> <color=#859CC7>" + refuerzo.Nickname + "</color>", 4f);
		Habilidades.Quitar(p);
		return true;
	}

	public static void EstrategaSpawn(Exiled.API.Features.Player player)
	{
		Util.Bc(player, 6, "<color=#003ECA>[ Eres el: <color=#57e5f7>Estratega</color> ]</color>", "<color=#003ECA>[ Você é o: <color=#57e5f7>Estrategista</color> ]</color>");
		Util.Bc(player, 10, "<color=#003ECA>[ Con tu habilidad ordenas un repliegue: los aliados de tu zona con menos del 40% de vida son traídos a tu lado y curados 30 HP. ]</color>", "<color=#003ECA>[ Com sua habilidade você ordena uma retirada: aliados da sua zona com menos de 40% de vida vêm até você e curam 30 HP. ]</color>");
		Habilidades.Asignar(player, "Orden de repliegue", 150f, OrdenDeRepliegue);
	}

	private static bool OrdenDeRepliegue(Exiled.API.Features.Player p)
	{
		ZoneType zone = p.Zone;
		Vector3 position = p.Position;
		int num = 0;
		foreach (Exiled.API.Features.Player item in Exiled.API.Features.Player.List)
		{
			if (!(item == p) && item.IsAlive && !item.IsNPC && item.Role.Side == p.Role.Side && item.IsHuman && item.Zone == zone && !item.IsInPocketDimension && item.Lift == null && !(item.Health > item.MaxHealth * 0.4f))
			{
				item.Position = position + Vector3.up * 0.2f;
				Util.Curar(item, 30f);
				item.Broadcast(5, "<color=#57e5f7>" + p.Nickname + " ordenó tu repliegue.</color>");
				num++;
			}
		}
		if (num == 0)
		{
			EffectsHUD.Notify(p, "<color=#e32433>No hay aliados heridos en tu zona.</color>");
			return false;
		}
		EffectsHUD.Notify(p, $"<color=#57e5f7><b>Repliegue:</b></color> <color=#859CC7>{num} aliados a salvo.</color>", 4f);
		return true;
	}

	public static void RegistrarEventos()
	{
		Exiled.Events.Handlers.Server.RespawningTeam += new CustomEventHandler<RespawningTeamEventArgs>(OnRespawningTeam);
	}

	public static void DesregistrarEventos()
	{
		Exiled.Events.Handlers.Server.RespawningTeam -= new CustomEventHandler<RespawningTeamEventArgs>(OnRespawningTeam);
	}

	private static void OnRespawningTeam(RespawningTeamEventArgs ev)
	{
		if (ev.IsAllowed && ev.Players != null && ev.Players.Count != 0)
		{
			List<Exiled.API.Features.Player> jugadores = new List<Exiled.API.Features.Player>(ev.Players);
			Var.Coroutines.Add(Timing.CallDelayed(1.5f, delegate
			{
				GrupoDeRespuestaCheck(jugadores);
			}));
		}
	}

	public static void GrupoDeRespuestaCheck(IEnumerable<Exiled.API.Features.Player> reaparecidos)
	{
		HashSet<Exiled.API.Features.Player> hashSet = new HashSet<Exiled.API.Features.Player>(reaparecidos);
		int num = 0;
		foreach (Exiled.API.Features.Player item in hashSet)
		{
			if (item != null && item.IsConnected && item.Role.Team == Team.ChaosInsurgency)
			{
				num++;
			}
		}
		int num2 = num / 4;
		if (num2 <= 0)
		{
			return;
		}
		List<Exiled.API.Features.Player> list = new List<Exiled.API.Features.Player>();
		foreach (Exiled.API.Features.Player item2 in Exiled.API.Features.Player.List)
		{
			if (item2.Role.Type == RoleTypeId.Spectator && !item2.IsOverwatchEnabled && !item2.IsNPC && !hashSet.Contains(item2))
			{
				list.Add(item2);
			}
		}
		for (int num3 = list.Count - 1; num3 > 0; num3--)
		{
			int num4 = Var.random.Next(num3 + 1);
			List<Exiled.API.Features.Player> list2 = list;
			int index = num3;
			int index2 = num4;
			Exiled.API.Features.Player value = list[num4];
			Exiled.API.Features.Player value2 = list[num3];
			list2[index] = value;
			list[index2] = value2;
		}
		int num5 = Mathf.Min(num2, list.Count);
		Log.Info($"[GRR] Chaos: {num} -> GRR: {num5} (candidatos {list.Count})");
		for (int i = 0; i < num5; i++)
		{
			Exiled.API.Features.Player elegido = list[i];
			elegido.Role.Set(RoleTypeId.NtfSergeant);
			Timing.CallDelayed(0.5f, delegate
			{
				if (Util.Vivo(elegido) && elegido.Role.Type == RoleTypeId.NtfSergeant)
				{
					GrupoDeRespuestaSpawn(elegido);
				}
			});
		}
	}
}
