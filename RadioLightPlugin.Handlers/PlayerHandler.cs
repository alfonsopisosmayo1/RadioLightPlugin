using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CustomPlayerEffects;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.API.Features.Pickups.Projectiles;
using Exiled.CustomRoles.API;
using Exiled.Events.EventArgs.Player;
using LabApi.Events.Arguments.Scp079Events;
using MEC;
using PlayerRoles;
using RadioLightPlugin.Command;
using RadioLightPlugin.Misc;
using RadioLightPlugin.Subclases;
using UnityEngine;

namespace RadioLightPlugin.Handlers;

public class PlayerHandler
{
	private static readonly Dictionary<int, float> _ultimoSonidoItem = new Dictionary<int, float>();

	private const int ProbCivilGulag = 65;

	private static bool Br
	{
		get
		{
			Plugin singleton = Plugin.Singleton;
			return singleton != null && singleton.Config?.Brazuca == true;
		}
	}

	private static bool Vivo(Player p)
	{
		return p != null && p.IsConnected && p.IsAlive;
	}

	public void OnPickingItem(PickingUpItemEventArgs ev)
	{
		if (!(ev.Player == null) && ev.Pickup != null)
		{
			if (Vip.FantasmaTemp.Contains(ev.Player.UserId))
			{
				ev.IsAllowed = false;
			}
			switch ((ushort)(Var.EsCompuestoC(ev.Pickup.Serial) ? 6942 : ev.Pickup.Serial))
			{
			case 6941:
				ev.Player.Broadcast(8, Br ? "<color=#FFFF00>Você obteve um 'Super Kit de Primeiros Socorros', do qual você pode aleatoriamente obter uma de suas duas habilidades ao usá-lo.</color>" : "<color=#859CC7>Conseguiste un 'Super Kit de Primeros Auxilios', del cual puedes obtener aleatoriamente una de sus dos habilidades al usarlo.</color>");
				break;
			case 6942:
				Log.Info(ev.Player.Nickname + " agarró el Compuesto C");
				ev.Player.Broadcast(8, Br ? "<color=#FFFF00>Você obteve Composto C, use-o para adquirir algum poder.</color>" : "<color=#859CC7>Conseguiste <color=#00d9ff>Compuesto C</color>, úsalo para conseguir algún poder.</color>");
				break;
			}
		}
	}

	public void OnItemPick(PickingUpItemEventArgs ev)
	{
		if (ev.Player == null || ev.Pickup == null)
		{
			return;
		}
		ItemType type = ev.Pickup.Type;
		if (type.IsWeapon())
		{
			if (Var.Demoman.Contains(ev.Player))
			{
				ev.IsAllowed = false;
				ev.Player.ShowHint("<color=#e32433>No puedes usar otra arma que no sea tu lanzagranadas.</color>");
			}
			else if (Var.HammerDown.Contains(ev.Player.ReferenceHub))
			{
				ev.IsAllowed = false;
				ev.Player.ShowHint("<color=#e32433>No puedes recoger armas siendo Hammer Down.</color>");
			}
			else if (Var.SCP073.Contains(ev.Player) && type.IsWeapon(checkNonFirearm: false))
			{
				ev.IsAllowed = false;
				ev.Player.ShowHint("<color=#e32433>No puedes recoger armas de fuego siendo SCP-073.</color>");
			}
		}
		else if (Var.SCP073.Contains(ev.Player) && type.IsAmmo())
		{
			ev.IsAllowed = false;
			ev.Player.ShowHint("<color=#e32433>No puedes recoger munición siendo SCP-073.</color>");
		}
		float time = Time.time;
		if (!_ultimoSonidoItem.TryGetValue(ev.Player.Id, out var value) || time - value >= 0.6f)
		{
			_ultimoSonidoItem[ev.Player.Id] = time;
			Sonido.Para(ev.Player.Id, ev.IsAllowed ? "Item.ogg" : "Error.ogg");
		}
	}

	public void OnItemDrop(DroppingItemEventArgs ev)
	{
	}

	public void OnItemDropped(DroppedItemEventArgs ev)
	{
		Player p = ev?.Player;
		if (p == null || ev.Pickup == null || !p.IsAlive)
		{
			return;
		}
		bool taser = Var.Taser.Contains(p) && ev.Pickup.Type == ItemType.ParticleDisruptor;
		bool flag = Var.Demoman.Contains(p) && ev.Pickup.Type == ItemType.GunRevolver;
		if (!taser && !flag)
		{
			return;
		}
		ItemType tipo = ev.Pickup.Type;
		try
		{
			ev.Pickup.Destroy();
		}
		catch
		{
		}
		Timing.CallDelayed(0.1f, delegate
		{
			if (!(p == null) && p.IsConnected && p.IsAlive && p.Items.Count < 8)
			{
				Item item = p.AddItem(tipo);
				if (taser && item is Firearm firearm)
				{
					firearm.MagazineAmmo = Math.Max((int)firearm.MaxMagazineAmmo, 2);
				}
				p.ShowHint((!taser) ? "<color=#e32433>No puedes tirar tu lanzagranadas.</color>" : (Br ? "<color=#e32433>Você não pode soltar seu <b>Taser.</b></color>" : "<color=#e32433>No puedes soltar tu <b>Taser.</b></color>"));
			}
		});
	}

	public void OnUsedItem(UsedItemEventArgs ev)
	{
		Player p = ev.Player;
		if (p == null || ev.Item == null)
		{
			return;
		}
		if (Var.CientEjec == p.UserId && ev.Item.Type == ItemType.Medkit)
		{
			p.Hurt(32f, "Reacción alérgica al botiquín.");
		}
		if (Var.Heavys.Contains(p.UserId) && ev.Item.Type == ItemType.Medkit && Var.HeavyCooldown.Add(p.UserId))
		{
			Util.MarcarCooldown(p, "Botiquín reforzado", 30f);
			p.AddAhp(100f, 100f, 10f, 1f, 10f);
			string id = p.UserId;
			Var.Coroutines.Add(Timing.CallDelayed(30f, delegate
			{
				Var.HeavyCooldown.Remove(id);
			}));
		}
		if (ev.Item.Type == ItemType.SCP500)
		{
			p.AddAhp(125f, 125f);
		}
		if (ev.Item.Type == ItemType.Adrenaline && !p.IsEffectActive<MovementBoost>())
		{
			p.EnableEffect(EffectType.MovementBoost, 18, 5f);
		}
		switch ((ushort)(Var.EsCompuestoC(ev.Item.Serial) ? 6942 : ev.Item.Serial))
		{
		case 6941:
			p.Broadcast(5, "<color=#859CC7>Usaste el '<color=green>Super Kit de Primeros Auxilios</color>'.</color>");
			if (!p.IsEffectActive<MovementBoost>())
			{
				p.EnableEffect(EffectType.MovementBoost, 20, 40f);
			}
			if (Var.random.NextDouble() < 0.5)
			{
				p.AddAhp(125f, 125f, 20f, 0.8f, 45f);
			}
			else
			{
				p.EnableEffect(EffectType.AntiScp207, 35f);
			}
			break;
		case 6942:
		{
			foreach (Player item in Player.List)
			{
				if (item != p)
				{
					item.Broadcast(5, "<color=#e32433>Una persona se inyectó <color=green>compuesto C</color>.</color>");
				}
			}
			p.Broadcast(5, "<color=#859CC7>Te inyectaste <color=green>compuesto C</color>.</color>");
			RoleTypeId rol = p.Role.Type;
			Var.Coroutines.Add(Timing.CallDelayed(10f, delegate
			{
				if (Vivo(p) && p.Role.Type == rol)
				{
					switch (Var.random.Next(5))
					{
					case 0:
						CompuestoC.ATrain(p);
						break;
					case 1:
						CompuestoC.Starlight(p);
						break;
					case 2:
						CompuestoC.SoldierBoy(p);
						break;
					case 3:
						CompuestoC.BlackNoir(p);
						break;
					default:
						CompuestoC.Homelander(p);
						break;
					}
				}
			}));
			break;
		}
		}
	}

	public void OnThrowingGrenade(ThrownProjectileEventArgs ev)
	{
		if (ev.Pickup == null || ev.Pickup.Type != ItemType.GrenadeHE || ev.Pickup.Serial == 10000)
		{
			return;
		}
		ExplosiveGrenade explosiveGrenade = ev.Item?.As<ExplosiveGrenade>();
		if (explosiveGrenade == null)
		{
			return;
		}
		float fuseTime = explosiveGrenade.FuseTime;
		Projectile proyectil = ev.Projectile;
		Player lanzador = ev.Player;
		explosiveGrenade.FuseTime = 100f;
		Timing.CallDelayed(fuseTime, delegate
		{
			if (!(proyectil?.Base == null))
			{
				Map.Explode(proyectil.Position, ProjectileType.FragGrenade, lanzador);
				proyectil.Destroy();
			}
		});
	}

	public void OnCoin(FlippingCoinEventArgs ev)
	{
		Player p = ev.Player;
		bool cruz = ev.IsTails;
		ev.Player.AddAhp(5f, 5f, 2f);
		Timing.CallDelayed(0.8f, delegate
		{
			if (Vivo(p))
			{
				p.ShowHint(Br ? ("<size=30>Caiu " + (cruz ? "<color=#000080>coroa</color>" : "<color=#C0C0C0>cara</color>") + "</size>") : ("<color=#859CC7><size=30>Cayó " + (cruz ? "<color=#000080>cruz</color>" : "<color=#C0C0C0>cara</color>") + "</size></color>"), 1f);
				if (Var.random.Next(100) < 20)
				{
					Item currentItem = p.CurrentItem;
					if (currentItem != null && currentItem.Type == ItemType.Coin)
					{
						p.DropItem(p.CurrentItem);
						p.ShowHint(Br ? "<size=30><color=#e32433>Você deixou cair a moeda...</color></size>" : "<size=30><color=#FF3131>Se te cayó la moneda...</color></size>", 1f);
					}
				}
			}
		});
	}

	public void OnRadioPreset(ChangingRadioPresetEventArgs ev)
	{
		StringBuilder stringBuilder = null;
		foreach (Player currentSpectatingPlayer in ev.Player.CurrentSpectatingPlayers)
		{
			if (currentSpectatingPlayer.Role.Type != RoleTypeId.Overwatch)
			{
				if (stringBuilder == null)
				{
					stringBuilder = new StringBuilder("<size=70%><align=right><color=#2C378A>Lista de espectadores:</color><color=#859CC7>\n");
				}
				stringBuilder.AppendLine(currentSpectatingPlayer.Nickname);
			}
		}
		ev.Player.ShowHint((stringBuilder == null) ? "<size=70%><align=right><color=#2C378A>Nadie te está especteando.</color></align></size>" : stringBuilder.Append("</color></align></size>").ToString(), 5f);
	}

	public void OnEscape(EscapingEventArgs ev)
	{
		Player player = ev.Player;
		if (player == null)
		{
			return;
		}
		if (Var.Nukerun)
		{
			ev.IsAllowed = false;
			Var.Nukerun = false;
			foreach (Player item in Player.List.ToList())
			{
				if (item != player && item.IsAlive)
				{
					item.Kill("Evento terminado.");
				}
			}
			player.Role.Set(RoleTypeId.Tutorial);
			Map.ClearBroadcasts();
			Map.Broadcast(10, "<color=#859CC7>" + (player.DisplayNickname ?? player.Nickname) + " ganó el evento Nukerun.</color>");
			Round.IsLocked = false;
		}
		else if (Var.Machetero == player.UserId)
		{
			ev.IsAllowed = false;
		}
		else if (player.IsCuffed)
		{
			RoleTypeId roleTypeId = player.Role.Team switch
			{
				Team.FoundationForces => GetChaosEquivalent(player.Role.Type), 
				Team.ChaosInsurgency => GetMTFEquivalent(player.Role.Type), 
				_ => RoleTypeId.None, 
			};
			if (roleTypeId != RoleTypeId.None)
			{
				ev.NewRole = roleTypeId;
				ev.IsAllowed = true;
				ev.EscapeScenario = EscapeScenario.CustomEscape;
			}
		}
	}

	private static RoleTypeId GetChaosEquivalent(RoleTypeId role)
	{
		return role switch
		{
			RoleTypeId.NtfCaptain => RoleTypeId.ChaosMarauder, 
			RoleTypeId.NtfSergeant => RoleTypeId.ChaosRepressor, 
			RoleTypeId.NtfPrivate => RoleTypeId.ChaosConscript, 
			_ => RoleTypeId.ChaosRifleman, 
		};
	}

	private static RoleTypeId GetMTFEquivalent(RoleTypeId role)
	{
		return role switch
		{
			RoleTypeId.ChaosMarauder => RoleTypeId.NtfCaptain, 
			RoleTypeId.ChaosRepressor => RoleTypeId.NtfSergeant, 
			RoleTypeId.ChaosRifleman => RoleTypeId.NtfSpecialist, 
			_ => RoleTypeId.NtfPrivate, 
		};
	}

	private static void ActualizarInfoVida(Player p)
	{
		int num = Mathf.CeilToInt(p.Health);
		float maxHealth = p.MaxHealth;
		p.CustomInfo = (((float)num > maxHealth * 0.75f) ? $"{num} HP" : (((float)num > maxHealth * 0.5f) ? $"<color=#FAFF86>{num} HP</color>" : $"<color=#C50000>{num} HP</color>"));
	}

	public void OnHurt(HurtEventArgs ev)
	{
		if (!(ev.Player == null) && ev.Player.IsAlive)
		{
			float time = Time.time;
			string key = ev.Player.RawUserId ?? ("id" + ev.Player.Id);
			if (!Var.LastHpInfoUpdate.TryGetValue(key, out var value) || !(time - value < 0.1f))
			{
				Var.LastHpInfoUpdate[key] = time;
				ActualizarInfoVida(ev.Player);
			}
		}
	}

	public void OnHealed(HealedEventArgs ev)
	{
		if (ev.Player != null && ev.Player.IsAlive)
		{
			ActualizarInfoVida(ev.Player);
		}
	}

	public void OnHurting(HurtingEventArgs ev)
	{
		if (ev?.Player != null && ev.DamageHandler != null && ev.DamageHandler.Type == DamageType.Falldown && Util.InmuneCaida(ev.Player))
		{
			ev.IsAllowed = false;
			return;
		}
		Player player = ev.Player;
		Player atacante = ev.Attacker;
		if (atacante == null || player == null || player == atacante)
		{
			return;
		}
		if (Coroutines.EnCuentaRegresiva(atacante))
		{
			ev.IsAllowed = false;
			return;
		}
		if (EnPeleaGulag(atacante) && EnPeleaGulag(player) && !Coroutines.EnCuentaRegresiva(player))
		{
			if (player.IsEffectActive<SpawnProtected>())
			{
				player.DisableEffect(EffectType.SpawnProtected);
			}
			if (player.IsEffectActive<Invisible>())
			{
				player.DisableEffect(EffectType.Invisible);
			}
			if (atacante.IsEffectActive<Invisible>())
			{
				atacante.DisableEffect(EffectType.Invisible);
			}
			ev.IsAllowed = true;
		}
		DamageType type = ev.DamageHandler.Type;
		if (((uint)(type - 2) <= 1u || type == DamageType.Poison || type == DamageType.Scp207) ? true : false)
		{
			return;
		}
		float amount = ev.Amount;
		bool flag = !Round.IsEnded;
		bool flag2 = atacante.Role.Side == player.Role.Side;
		bool flag3 = type.IsWeapon();
		bool flag4 = type == DamageType.Explosion;
		bool flag5 = type == DamageType.ParticleDisruptor;
		try
		{
			if (flag)
			{
				if (atacante.IsNPC && atacante.DisplayNickname != null && atacante.DisplayNickname.Contains("void"))
				{
					ev.Amount = 2f;
				}
				if (atacante.RawUserId == "76561198722987850" && flag2)
				{
					ev.Amount = 0f;
				}
				if (flag2 && player.Role.Type != RoleTypeId.Tutorial)
				{
					ev.Amount *= 0.3f;
				}
				if (player.Lift != null && !player.IsScp)
				{
					ev.Amount *= 0.5f;
				}
				if (player.IsCuffed && player.Cuffer != atacante && !atacante.IsScp)
				{
					if (CanShowHurtingHint(atacante))
					{
						atacante.ShowHint("<color=#e32433>ATENCIÓN</color>\n<color=#859CC7>Le estás disparando a un arrestado: '<color=#e32433>" + (player.DisplayNickname ?? player.Nickname) + "</color>'.</color>");
					}
					if (CanShowHurtingHint(player))
					{
						player.ShowHint("<color=#e32433>ATENCIÓN</color>\n<color=#859CC7>Te está disparando '<color=#e32433>" + (atacante.DisplayNickname ?? atacante.Nickname) + "</color>' estando arrestado.</color>");
					}
					ev.Amount *= 0.25f;
				}
			}
			if (flag5 && Var.Taser.Contains(atacante))
			{
				if (flag2 || Var.CooldownTaser.Contains(atacante))
				{
					// [fix] el Taser inmoviliza, no mata (impactos extra de la ráfaga o en cooldown no hacen daño)
					ev.IsAllowed = false;
				}
				else if (Var.CooldownTaser.Add(atacante))
				{
					Util.MarcarCooldown(atacante, "Taser", 30f);
					ev.Amount = 0f;
					TaserCargador(atacante, lleno: false);
					Var.Coroutines.Add(Timing.RunCoroutine(Coroutines.Tasing(player, atacante)));
					Var.Coroutines.Add(Timing.CallDelayed(30f, delegate
					{
						TaserListo(atacante);
					}));
				}
			}
			if (flag4)
			{
				if (Var.Taser.Contains(atacante))
				{
					ev.IsAllowed = false;
				}
				else if (Var.Demoman.Contains(atacante))
				{
					if (flag2)
					{
						ev.IsAllowed = false;
					}
					else
					{
						ev.Amount *= 0.25f;
					}
				}
			}
			bool value = default(bool);
			if (flag3 && Var.Spy.Contains(player) && Var.DeadRinger.TryGetValue(player, out value) && value && Var.DeadRingerUsado.Add(player))
			{
				ev.Amount /= 4f;
				ActivarDeadRinger(player);
			}
			if (Var.Spy.Contains(player) && flag2)
			{
				if (CanShowHurtingHint(atacante))
				{
					atacante.ShowHint("<color=#e32433><b>ATENCIÓN</b></color>\n<color=#859CC7><b>Estás atacando a un Spy de tu equipo.</b></color>");
				}
				ev.Amount = 2f;
			}
			if (!flag || player.Role.Side == Side.Tutorial || !Server.FriendlyFire)
			{
				return;
			}
			if (flag2)
			{
				if (CanShowHurtingHint(atacante))
				{
					atacante.ShowHint("<color=#e32433>ATENCIÓN</color>\n<color=#859CC7>Le estás disparando a tu compañero '<color=#ff0000>" + (player.DisplayNickname ?? player.Nickname) + "</color>'.</color>");
				}
				if (CanShowHurtingHint(player))
				{
					player.ShowHint("<color=#e32433>ATENCIÓN</color>\n<color=#859CC7>Te está disparando tu compañero '<color=#ff0000>" + (atacante.DisplayNickname ?? atacante.Nickname) + "</color>'.</color>");
				}
				ev.Amount /= 2f;
				return;
			}
			if (Var.Scout.Contains(atacante.UserId))
			{
				Item currentItem = atacante.CurrentItem;
				if (currentItem != null && currentItem.Type == ItemType.GunCOM18)
				{
					player.EnableEffect(EffectType.Exhausted, 2f);
					player.EnableEffect(EffectType.SinkHole, 2f);
					return;
				}
			}
			if (Var.CientEjec == player.UserId && player.Health >= 30f)
			{
				player.EnableEffect(EffectType.MovementBoost, 25, 2f);
				player.EnableEffect(EffectType.AntiScp207, 5, 2f);
			}
		}
		catch (Exception ex)
		{
			Log.Error(ex.ToString());
		}
		finally
		{
			if (atacante.IsScp && ev.IsAllowed)
			{
				ev.Amount = amount;
			}
			if (ev.IsAllowed)
			{
				AplicarBonosDeHabilidad(ev, atacante, player, flag3);
			}
		}
	}

	private static void AplicarBonosDeHabilidad(HurtingEventArgs ev, Player atacante, Player victima, bool esArma)
	{
		if (esArma)
		{
			Item currentItem = atacante.CurrentItem;
			if (currentItem != null && currentItem.Type == ItemType.GunLogicer && Habilidades.Nombre(atacante) == "Trípode" && victima.IsScp)
			{
				ev.Amount *= 0.55f;
			}
		}
		if (esArma && Var.Activo(Var.Tripode, atacante))
		{
			ev.Amount *= 1.25f;
		}
		if (atacante.Role.Type == RoleTypeId.Scp106 && Var.Activo(Var.Hambre106, atacante))
		{
			ev.Amount *= 1.5f;
		}
		if (Var.Activo(Var.Marcados, victima) && atacante.Role.Side == Side.Mtf)
		{
			ev.Amount *= 1.25f;
		}
		if (victima.Role.Type == RoleTypeId.Scp096 && Var.Activo(Var.Coraza096, victima))
		{
			ev.Amount *= 0.65f;
		}
	}

	private static void ActivarDeadRinger(Player victima)
	{
		RoleTypeId role = victima.Role.Type;
		string nick = victima.Nickname;
		Vector3 pos = victima.Position;
		int playerId = victima.Id;
		Timing.CallDelayed(0.05f, delegate
		{
			if (Vivo(victima))
			{
				Ragdoll.CreateAndSpawn(role, nick, "Causa de muerte desconocida.", pos);
				victima.EnableEffect(EffectType.Invisible, 8f);
				victima.EnableEffect(EffectType.MovementBoost, 35, 4f);
				Timing.CallDelayed(8f, delegate
				{
					if (victima != null && victima.IsConnected)
					{
						Sonido.Para(playerId, "DR.ogg");
					}
				});
			}
		});
	}

	private static bool CanShowHurtingHint(Player player)
	{
		if (player == null)
		{
			return false;
		}
		if (player.IsNPC)
		{
			return false;
		}
		string key = player.RawUserId ?? ("id" + player.Id);
		float time = Time.time;
		if (Var.HurtingHintCooldown.TryGetValue(key, out var value) && time - value < 0.2f)
		{
			return false;
		}
		Var.HurtingHintCooldown[key] = time;
		return true;
	}

	public void OnDying(DyingEventArgs ev)
	{
		Player victima = ev.Player;
		Player attacker = ev.Attacker;
		if (victima == null)
		{
			return;
		}
		try
		{
			Var.GenteGulag.Remove(victima);
			if (!Var.Estuvo.Contains(victima.UserId))
			{
				Timing.CallDelayed(0.1f, delegate
				{
					if (victima.IsConnected && !victima.IsAlive)
					{
						victima.Broadcast(10, "\n<color=#859CC7>Hey, tú, ¿quieres pelear con alguien para reaparecer?\nEntonces abre la consola (<color=#2C378A>Ñ</color>) y escribe: '<b><color=#2C378A>.gulag</color></b>'.</color>");
					}
				});
			}
			Extra.AlMorir(victima);
			if (Var.PeleaPractica.Contains(victima))
			{
				Coroutines.ResolverPractica(ev);
			}
			else if (Var.Gulag2v2 && (Var.Gulag1.Contains(victima) || Var.Gulag2.Contains(victima)))
			{
				ResolverGulag2v2(ev, victima);
			}
			else
			{
				if (Var.Gulag1.Contains(victima) || Var.Gulag2.Contains(victima))
				{
					ResolverGulag(ev, victima, attacker);
				}
				if (attacker == null || attacker == victima)
				{
					return;
				}
				if (attacker.Role.Side != victima.Role.Side)
				{
					Sonido.Para(attacker.Id, "Kill.ogg");
				}
				if (Var.HammerDown.Contains(attacker.ReferenceHub) && attacker.Role.Side != victima.Role.Side)
				{
					attacker.Heal(10f);
				}
				Extra.AlMatar(attacker, victima);
				if (!ev.IsAllowed)
				{
					return;
				}
				if (attacker.Role.Type == RoleTypeId.Scp0492 && Var.ZombiesInfectados.Contains(attacker) && victima.Role.Side != 0 && victima.Role.Side != Side.Tutorial)
				{
					Vector3 pos = victima.Position;
					Timing.CallDelayed(0.2f, delegate
					{
						if (victima.IsConnected && !victima.IsAlive && !EnPeleaGulag(victima))
						{
							Util.ConvertirEnZombie(victima, pos);
						}
					});
				}
				if (!Round.IsStarted || Round.IsEnded)
				{
					return;
				}
				Plugin singleton = Plugin.Singleton;
				if (singleton != null && singleton.Config?.KillFeedEstilizado == false && attacker.Role.Side != 0 && victima.Role.Side != attacker.Role.Side && !attacker.IsHost)
				{
					MostrarKillFeed(ev, victima, attacker);
				}
				bool flag = Var.railgun || Var.Nukerun;
				bool flag2 = flag;
				if (!flag2)
				{
					Side side = attacker.Role.Side;
					bool flag3 = ((side == Side.Scp || side == Side.Tutorial) ? true : false);
					flag2 = flag3;
				}
				bool flag4 = flag2;
				if (!flag4)
				{
					Side side = victima.Role.Side;
					bool flag3 = ((side == Side.Scp || side == Side.Tutorial) ? true : false);
					flag4 = flag3;
				}
				if (!flag4 && victima.Role.Side == attacker.Role.Side)
				{
					string text = (Br ? ("<color=#e32433>TK detectado</color> <color=#949494>(Lembre-se de denunciar)</color>: " + attacker.Nickname) : ("<color=#e32433>TK detectado</color> <color=#859CC7>(Recuerda reportar)</color>: " + attacker.Nickname));
					if (!attacker.DoNotTrack)
					{
						text = text + " " + attacker.UserId;
					}
					victima.ClearBroadcasts();
					victima.Broadcast(12, $"{text}\n{DateTime.UtcNow} UTC");
				}
			}
		}
		catch (Exception ex)
		{
			Log.Error(ex.ToString());
		}
	}

	private static void MostrarKillFeed(DyingEventArgs ev, Player victima, Player atacante)
	{
		Faction faction = atacante.Role.Team.GetFaction();
		Faction faction2 = victima.Role.Team.GetFaction();
		DamageType type = ev.DamageHandler.Type;
		string text = ((!type.IsWeapon()) ? ((type == DamageType.Explosion) ? "\ud83d\udca3" : "⚔\ufe0f") : (Br ? "matou" : "mató a"));
		string text2 = "<color=#859CC7> " + text + " </color>";
		string nickname = atacante.Nickname;
		string nickname2 = victima.Nickname;
		string text3 = "<color=#3553AE>" + nickname + "</color>" + text2 + "<color=#3553AE>" + nickname2 + "</color>";
		string text4 = "<color=#3553AE>" + nickname + "</color>" + text2 + "<color=#B23D37>" + nickname2 + "</color>";
		string text5 = "<color=#B23D37>" + nickname + "</color>" + text2 + "<color=#3553AE>" + nickname2 + "</color>";
		string text6 = "<color=#B23D37>" + nickname + "</color>" + text2 + "<color=#B23D37>" + nickname2 + "</color>";
		foreach (Player item in Player.List)
		{
			if (item.IsAlive && !(item == victima))
			{
				Faction faction3 = item.Role.Team.GetFaction();
				bool flag = faction3 == faction;
				bool flag2 = faction3 == faction2;
				EffectsHUD.Panel(item, "killfeed", (!flag) ? (flag2 ? text5 : text6) : (flag2 ? text3 : text4), 5f);
			}
		}
	}

	private static void ResolverGulag(DyingEventArgs ev, Player victima, Player atacante)
	{
		if (ev.DamageHandler.Type == DamageType.Warhead)
		{
			ev.IsAllowed = false;
			victima.Heal(50f);
			return;
		}
		// El ganador es SIEMPRE el rival que sigue en pie (aunque la muerte sea por caída, suicidio,
		// granada propia o sin atacante), no "el que pegó el último tiro".
		List<Player> rivales = (Var.Gulag1.Contains(victima) ? Var.Gulag2 : Var.Gulag1).Where((Player p) => p != null && p != victima && p.IsConnected && p.Role.Type == RoleTypeId.Tutorial).ToList();
		Var.Gulag1.Clear();
		Var.Gulag2.Clear();
		Var.Gulag2v2 = false;
		victima.ClearInventory();
		if (rivales.Count == 0)
		{
			return;
		}
		Player ganador = ((atacante != null && rivales.Contains(atacante)) ? atacante : rivales[0]);
		victima.ShowHint("<color=#003ECA>Líder MTF:</color> Te dieron una paliza, te llevaremos a casa.", 5f);
		ganador.ShowHint("<color=#003ECA>Líder MTF:</color> No mostraste piedad. Espera el redespliegue.", 5f);
		Sonido.Para(ganador.Id, "GulagW.ogg");
		float vida = ganador.Health;
		ganador.ClearInventory();
		RevivirGanadorGulag(ganador, victima, vida);
	}

	private static void ResolverGulag2v2(DyingEventArgs ev, Player victima)
	{
		if (ev.DamageHandler.Type == DamageType.Warhead)
		{
			ev.IsAllowed = false;
			victima.Heal(50f);
			return;
		}
		List<Player> list = (Var.Gulag1.Contains(victima) ? Var.Gulag1 : Var.Gulag2);
		List<Player> source = ((list == Var.Gulag1) ? Var.Gulag2 : Var.Gulag1);
		list.Remove(victima);
		victima.ClearInventory();
		if (list.Any((Player p) => p != null && p.IsConnected && p.Role.Type == RoleTypeId.Tutorial))
		{
			foreach (Player item in Var.Gulag1.Concat(Var.Gulag2))
			{
				if (item != null && item.IsConnected)
				{
					EffectsHUD.Notify(item, "<color=#859CC7>\ud83d\udc80 " + victima.Nickname + " cayó.</color>");
				}
			}
			victima.Broadcast(4, "<color=#e32433>Caíste.</color> <color=#859CC7>Si tu compañero gana, igual no volvés: suerte la próxima.</color>");
			return;
		}
		List<Player> list2 = source.Where((Player p) => p != null && p.IsConnected && p.Role.Type == RoleTypeId.Tutorial).ToList();
		Var.Gulag1.Clear();
		Var.Gulag2.Clear();
		Var.Gulag2v2 = false;
		Coroutines.LimpiarArena();
		foreach (Player g in list2)
		{
			float vida = g.Health;
			g.ClearInventory();
			g.ShowHint("<color=#003ECA>Líder MTF:</color> Buen trabajo en equipo. Vuelven los dos al combate.", 5f);
			Sonido.Para(g.Id, "GulagW.ogg");
			RevivirGanadorGulag(g, null, vida);
		}
	}

	/// <summary>
	/// Devuelve al ganador del gulag al combate a los 2 s. Si usó un comando de tamaño (no el modificador
	/// Gigantes/Enanos de la pelea) va a espectador y, si hay perdedor, revive el perdedor.
	/// </summary>
	internal static void RevivirGanadorGulag(Player ganador, Player perdedor, float vida)
	{
		if (ganador == null)
		{
			return;
		}
		Timing.CallDelayed(2f, delegate
		{
			if (ganador == null || !ganador.IsConnected)
			{
				return;
			}
			Player quien = ganador;
			if (ganador.Scale != Vector3.one && !ModosGulag.EscalaDelModo(ganador))
			{
				ganador.Broadcast(5, "<color=#e32433>Fuiste enviado a espectador por modificar tamaño.</color>");
				ganador.Role.Set(RoleTypeId.Spectator);
				if (perdedor == null || !perdedor.IsConnected || perdedor.IsAlive)
				{
					return;
				}
				perdedor.Broadcast(5, "<color=green>Fuiste revivido.</color>");
				quien = perdedor;
			}
			DevolverAlCombate(quien, vida, premio: true, 0);
		});
	}

	/// <summary>Revive con reintentos: si el cambio de rol no se aplica (otro plugin lo bloqueó, etc.) lo vuelve a intentar.</summary>
	internal static void DevolverAlCombate(Player g, float vida, bool premio, int intento)
	{
		if (g == null || !g.IsConnected || Round.IsEnded)
		{
			return;
		}
		if (!g.IsAlive || g.Role.Type == RoleTypeId.Tutorial)
		{
			try
			{
				g.Role.Set(ElegirRolGulag());
			}
			catch (Exception ex)
			{
				Log.Warn("[Gulag] Error reviviendo a " + g.Nickname + ": " + ex.Message);
			}
		}
		if (!g.IsAlive || g.Role.Type == RoleTypeId.Tutorial)
		{
			if (intento < 4)
			{
				Timing.CallDelayed(0.5f, delegate
				{
					DevolverAlCombate(g, vida, premio, intento + 1);
				});
			}
			else
			{
				Log.Warn("[Gulag] No se pudo revivir a " + g.Nickname + " tras varios intentos.");
			}
			return;
		}
		if (premio)
		{
			VentajasVip.AlGanarGulag(g);
			Estadisticas.GanoGulag(g);
		}
		Timing.CallDelayed(1f, delegate
		{
			if (Vivo(g))
			{
				g.Health = Mathf.Clamp(vida, 1f, g.MaxHealth);
				g.EnableEffect<SpawnProtected>(7f);
			}
		});
	}

	internal static RoleTypeId ElegirRolGulag()
	{
		if (Round.ElapsedTime.TotalMinutes < 5.0 && !Map.IsLczDecontaminated && !Warhead.IsDetonated && Var.random.Next(100) < 65)
		{
			return (Var.random.Next(2) == 0) ? RoleTypeId.ClassD : RoleTypeId.Scientist;
		}
		RoleTypeId[] array = ((!Warhead.IsDetonated) ? new RoleTypeId[4]
		{
			RoleTypeId.ChaosRepressor,
			RoleTypeId.ChaosRifleman,
			RoleTypeId.FacilityGuard,
			RoleTypeId.NtfSergeant
		} : new RoleTypeId[4]
		{
			RoleTypeId.ChaosRepressor,
			RoleTypeId.ChaosRifleman,
			RoleTypeId.NtfSergeant,
			RoleTypeId.NtfPrivate
		});
		return array[Var.random.Next(array.Length)];
	}

	public void OnSpawn(SpawnedEventArgs ev)
	{
		Player p = ev.Player;
		if (p == null || Scp0871.Es(p))
		{
			return;
		}
		ReemplazoAfk.AlAparecer(p);
		if (p.Role.Type != RoleTypeId.Tutorial)
		{
			Timing.CallDelayed(2f, delegate
			{
				if (p.IsConnected)
				{
					p.IsGodModeEnabled = false;
				}
			});
		}
		p.Scale = Vector3.one;
		if (!p.IsHost)
		{
			p.ShowHint("", 0.1f);
		}
		if (p.Role.Type != RoleTypeId.Spectator && p.Role.Type != RoleTypeId.Overwatch)
		{
			Timing.CallDelayed(1f, delegate
			{
				if (Vivo(p))
				{
					ActualizarInfoVida(p);
				}
			});
		}
		if (p.Role.Type == RoleTypeId.FacilityGuard && !Var.YaSpawneó && Var.HeadGuard.Count == 0)
		{
			Timing.CallDelayed(3.5f, delegate
			{
				if (Vivo(p) && p.Role.Type == RoleTypeId.FacilityGuard && Var.HeadGuard.Count == 0 && !Var.YaSpawneó && !Var.Traidor.Contains(p.UserId) && Var.Machetero != p.UserId)
				{
					Guardia.HeadguardSpawn(p);
				}
			});
		}
		if (ev.Reason == SpawnReason.Escaped)
		{
			Sonido.Para(p.Id, "Escape.ogg");
		}
	}

	public void ChangingRole(ChangingRoleEventArgs ev)
	{
		Player p = ev.Player;
		if (p == null)
		{
			return;
		}
		if (p.IsNPC && p.UserId.Contains("7656119909114"))
		{
			ev.IsAllowed = false;
		}
		else
		{
			Escuadrones.AlCambiarRol(p);
			if (Scp0871.Es(p))
			{
				return;
			}
			bool flag = Var.HeadGuard.Contains(p);
			bool flag2 = Var.Disfrazados.Contains(p);
			ReemplazoAfk.AntesDeIrse(p, ev.NewRole, ev.Reason);
			Var.QuitarSubclases(p);
			Var.GenteGulag.Remove(p);
			if (ev.NewRole != RoleTypeId.Tutorial)
			{
				Var.ColaPractica.Remove(p);
				if (Var.PeleaPractica.Contains(p) && ev.NewRole != RoleTypeId.Spectator)
				{
					Coroutines.CancelarPractica(p);
				}
			}
			if (flag2)
			{
				p.DisplayNickname = null;
			}
			if (flag)
			{
				string text = p.Zone.ToString();
				foreach (Player item in Player.List)
				{
					if (!(item == p) && item.Role.Team == Team.FoundationForces)
					{
						item.Broadcast(10, Br ? ("<color=#FC9500>Atenção:</color>\n<color=#BB7878>O líder dos Guardas caiu em</color> <color=#1F618D>" + text + "</color>") : ("<color=#e32433>Atención:</color>\n<color=#859CC7>El cabecilla de los Guardias '" + p.Nickname + "' cayó en</color> <color=#1F618D>" + text + "</color>"));
					}
				}
			}
			RoleTypeId nuevo = ev.NewRole;
			Timing.CallDelayed(Util.Turno(0.2f), delegate
			{
				if (!Vivo(p) || p.Role.Type != nuevo || p.GetCustomRoles().Any())
				{
					return;
				}
				try
				{
					AsignarSubclase(p, nuevo);
				}
				catch (Exception arg)
				{
					Log.Error($"[Subclases] {p.Nickname} -> {nuevo}: {arg}");
				}
			});
			if (ev.Reason == SpawnReason.Escaped)
			{
				return;
			}
			Var.Coroutines.Add(Timing.CallDelayed(Util.Turno(3f), delegate
			{
				if (Vivo(p) && p.Role.Type == nuevo && !Var.EventoEnCurso && !p.GetCustomRoles().Any())
				{
					Util.KitMinimo(p);
				}
			}));
		}
	}

	private static bool Chance(int porcentaje)
	{
		return Var.random.Next(100) < porcentaje;
	}

	private static void AsignarSubclase(Player p, RoleTypeId rol)
	{
		if (Var.EventoEnCurso || Escuadrones.Es(p))
		{
			return;
		}
		switch (rol)
		{
		case RoleTypeId.NtfCaptain:
			Var.Coroutines.Add(Timing.RunCoroutine(Coroutines.ShowDisplay(p)));
			if (Chance(20))
			{
				MTF.CoordinadorSpawn(p);
			}
			else if (Chance(25))
			{
				MTF.EstrategaSpawn(p);
			}
			else if (Chance(30))
			{
				Extra.ComandanteSpawn(p);
			}
			break;
		case RoleTypeId.NtfSergeant:
			if (Chance(15))
			{
				MTF.ArtilleroSpawn(p);
			}
			else if (Chance(18))
			{
				MTF.RompebrechasSpawn(p);
			}
			else if (Chance(20))
			{
				Extra.GranaderoSpawn(p);
			}
			break;
		case RoleTypeId.NtfSpecialist:
			if (Chance(15))
			{
				MTF.IngenieroSpawn(p);
			}
			else if (Chance(18))
			{
				MTF.TiradorSpawn(p);
			}
			else if (Chance(20))
			{
				Extra.ExploradorSpawn(p);
			}
			break;
		case RoleTypeId.Scp173:
		case RoleTypeId.Scp106:
		case RoleTypeId.Scp096:
		case RoleTypeId.Scp939:
		case RoleTypeId.Scp3114:
			ScpMayores.Asignar(p);
			break;
		case RoleTypeId.Scp079:
			p.Broadcast(10, Br ? "<color=#6A00FF>Se você não tem microfone e quer falar com outros <color=#e32433>SCPs</color> sendo <color=#e32433>SCP-079</color>, abra o console (tecla Ñ) e digite:\n.079 chat \"uma mensagem\"</color>" : "<color=#859CC7>Si no tienes micrófono y quieres hablar con los demás <color=#e32433>SCPs</color> siendo <color=#e32433>SCP-079</color>, abre la consola (Ñ) y pon:\n.079 chat \"un mensaje\"</color>");
			p.Broadcast(6, Br ? "<color=#6A00FF>Veja os comandos especiais do <color=#e32433>SCP-079</color> com:\n.079 help</color>" : "<color=#859CC7>Puedes ver los comandos especiales de <color=#e32433>SCP-079</color> con:\n.079 help</color>");
			ScpMayores.Asignar(p);
			break;
		case RoleTypeId.Scp049:
			Var.Coroutines.Add(Timing.RunCoroutine(Coroutines.HealZombie(p)));
			ScpMayores.Asignar(p);
			break;
		case RoleTypeId.FacilityGuard:
			if (Var.Traidor.Count == 0 && !Var.TraidorTP && Server.PlayerCount >= 14 && Chance(30))
			{
				Guardia.TraidorSpawn(p);
			}
			else if (string.IsNullOrEmpty(Var.Machetero) && Chance(5))
			{
				Cientifico.MacheteroSpawn(p);
			}
			else if (Chance(15))
			{
				Guardia.CarceleroSpawn(p);
			}
			else if (Chance(15))
			{
				Guardia.VigiaSpawn(p);
			}
			else if (Chance(15))
			{
				Extra.AntidisturbiosSpawn(p);
			}
			break;
		case RoleTypeId.ClassD:
			if (Chance(30))
			{
				ClaseD.SmallSpawn(p);
			}
			else if (Chance(5))
			{
				ClaseD.CandymanSpawn(p);
			}
			else if (Chance(10))
			{
				ClaseD.VeteranoSpawn(p);
			}
			else if (Chance(5))
			{
				ClaseD.KamikazeSpawn(p);
			}
			else if (Chance(10))
			{
				ClaseD.SilenciosoSpawn(p);
			}
			else if (Chance(10))
			{
				ClaseD.ImpersonatorSpawn(p);
			}
			else if (Chance(10))
			{
				Extra.RateroSpawn(p);
			}
			else if (Chance(10))
			{
				Extra.EscapistaSpawn(p);
			}
			break;
		case RoleTypeId.Scientist:
			AsignarCientifico(p);
			break;
		case RoleTypeId.Scp0492:
		{
			p.DisableAllEffects();
			int num = Var.ZombiesInfectados.Count((Player z) => z != null && z.IsAlive && z.Role.Type == RoleTypeId.Scp0492);
			int num2 = Plugin.Singleton?.Config?.MaxZombiesInfectados ?? Var.MaxZombiesInfectados;
			if (num < num2 && Chance(15))
			{
				SCP.InfectadoSpawn(p);
			}
			else if (Chance(20))
			{
				SCP.AtletaSpawn(p);
			}
			else if (Chance(30))
			{
				SCP.TanqueSpawn(p);
			}
			else if (Chance(25))
			{
				SCP.MilitarSpawn(p);
			}
			else if (Chance(10))
			{
				SCP.MilitarEscopeteroSpawn(p);
			}
			else if (Chance(30))
			{
				SCP.SilenciosoSpawn(p);
			}
			else if (Chance(25))
			{
				Extra.ExplosivoSpawn(p);
			}
			else if (Chance(25))
			{
				Extra.PutrefactoSpawn(p);
			}
			break;
		}
		case RoleTypeId.Spectator:
		case RoleTypeId.ChaosConscript:
		case RoleTypeId.NtfPrivate:
		case RoleTypeId.Tutorial:
		case RoleTypeId.CustomRole:
		case RoleTypeId.ChaosRifleman:
		case RoleTypeId.ChaosMarauder:
		case RoleTypeId.ChaosRepressor:
		case RoleTypeId.Overwatch:
		case RoleTypeId.Filmmaker:
			break;
		}
	}

	private static void AsignarCientifico(Player p)
	{
		int num = Var.random.Next(1, 101);
		if (num <= 5)
		{
			Cientifico.Scp181Spawn(p);
		}
		else if (num <= 10)
		{
			Cientifico.FantasmaSpawn(p);
		}
		else if (num <= 15)
		{
			Cientifico.VidenteSpawn(p);
		}
		else if (num <= 18 && Var.Scp990.Count == 0)
		{
			Cientifico.SCP990Spawn(p);
		}
		else if (num <= 21 && Var.Scp507.Count == 0)
		{
			Cientifico.SCP507Spawn(p);
		}
		else if (num <= 24 && Var.Scp1915.Count == 0)
		{
			Cientifico.SCP1915Spawn(p);
		}
		else if (num <= 29)
		{
			Cientifico.ObservadorSpawn(p);
		}
		else if (num <= 32 && string.IsNullOrEmpty(Var.Bomba))
		{
			Cientifico.BombaSpawn(p);
		}
		else if (num <= 35 && Var.SCP073.Count == 0)
		{
			Cientifico.Scp073Spawn(p);
		}
		else if (num <= 45 && string.IsNullOrEmpty(Var.CientEjec))
		{
			Cientifico.CientEjecSpawn(p);
		}
		else if (num <= 55)
		{
			Extra.QuimicoSpawn(p);
		}
		else if (num <= 65)
		{
			Extra.HackerSpawn(p);
		}
	}

	public void OnFlashlightToggle(TogglingFlashlightEventArgs ev)
	{
		Player player = ev.Player;
		if (player == null || !ev.NewState)
		{
			return;
		}
		if (Var.Traidor.Contains(player.UserId) && !Var.TraidorTP)
		{
			if (player.IsInPocketDimension)
			{
				player.Broadcast(5, "<b><color=#FF3131>No puedes transformarte en la dimensión de bolsillo.</color></b>");
				return;
			}
			if (player.Role.Type != RoleTypeId.FacilityGuard)
			{
				Var.Traidor.Remove(player.UserId);
				return;
			}
			Var.Traidor.Remove(player.UserId);
			Var.TraidorTP = true;
			player.ReferenceHub.roleManager.ServerSetRole(RoleTypeId.ChaosRifleman, RoleChangeReason.None, RoleSpawnFlags.None);
			player.AddAmmo(AmmoType.Nato9, 50);
			player.AddAmmo(AmmoType.Nato556, 50);
			return;
		}
		if (Var.Impersonator.Contains(player))
		{
			if (!Var.CooldownImp.Add(player))
			{
				player.ShowHint("<b><color=#e32433>Debes esperar a que termine el cooldown de 2 minutos.</color></b>");
				return;
			}
			Util.MarcarCooldown(player, "Disfraz", 120f);
			RoleTypeId rol = player.Role.Type;
			player.ChangeAppearance(RoleTypeId.Scientist, skipJump: true, 0);
			player.ShowHint("<color=#859CC7>Te disfrazaste de <color=#FFFF7C>Científico</color>.</color>", 5f);
			Var.Coroutines.Add(Timing.CallDelayed(120f, delegate
			{
				Var.CooldownImp.Remove(player);
			}));
			Var.Coroutines.Add(Timing.CallDelayed(20f, delegate
			{
				if (Vivo(player) && player.Role.Type == rol)
				{
					player.ShowHint("<color=#FF8E00>A tu disfraz le quedan 20 segundos.</color>", 5f);
				}
			}));
			Var.Coroutines.Add(Timing.CallDelayed(40f, delegate
			{
				if (Vivo(player) && player.Role.Type == rol)
				{
					player.ChangeAppearance(player.Role.Type, skipJump: true, 0);
					player.ShowHint("<color=#e32433>Perdiste tu disfraz.</color>", 5f);
				}
			}));
			return;
		}
		if (Var.Spy.Contains(player))
		{
			if (!Var.CooldownSpy.Add(player))
			{
				player.ShowHint("<b><color=#e32433>Debes esperar a que termine el cooldown de 1 minuto.</color></b>");
				return;
			}
			Util.MarcarCooldown(player, "Disfraz", 60f);
			bool flag = player.Role.Side == Side.ChaosInsurgency;
			int num = Var.random.Next(4);
			RoleTypeId item;
			string item2;
			if (flag)
			{
				(RoleTypeId, string) tuple = num switch
				{
					0 => (RoleTypeId.NtfCaptain, "<color=green>Te disfrazaste de</color> <color=#003ECA>Capitán de MTF.</color>"), 
					1 => (RoleTypeId.NtfPrivate, "<color=green>Te disfrazaste de</color> <color=#6FC3FF>Cadete de MTF.</color>"), 
					2 => (RoleTypeId.NtfSpecialist, "<color=green>Te disfrazaste de</color> <color=#0096FF>Especialista de MTF.</color>"), 
					_ => (RoleTypeId.NtfSergeant, "<color=green>Te disfrazaste de</color> <color=#0096FF>Sargento de MTF.</color>"), 
				};
				item = tuple.Item1;
				item2 = tuple.Item2;
				(RoleTypeId, string) tuple2 = (tuple.Item1, tuple.Item2);
			}
			else
			{
				(RoleTypeId, string) tuple3 = num switch
				{
					0 => (RoleTypeId.ChaosConscript, "<color=blue>Te disfrazaste de</color> <color=#008F1E>Recluta de la Insurgencia del Caos.</color>"), 
					1 => (RoleTypeId.ChaosMarauder, "<color=blue>Te disfrazaste de</color> <color=#0A7D34>Merodeador de la Insurgencia del Caos.</color>"), 
					2 => (RoleTypeId.ChaosRepressor, "<color=blue>Te disfrazaste de</color> <color=#006728>Represor de la Insurgencia del Caos.</color>"), 
					_ => (RoleTypeId.ChaosRifleman, "<color=blue>Te disfrazaste de</color> <color=#008F1E>Fusilero de la Insurgencia del Caos.</color>"), 
				};
				item = tuple3.Item1;
				item2 = tuple3.Item2;
				(RoleTypeId, string) tuple2 = (tuple3.Item1, tuple3.Item2);
			}
			player.ChangeAppearance(item, skipJump: false, (byte)(flag ? 1u : 0u));
			player.ShowHint(item2);
			List<Player> list = Player.List.Where(delegate(Player x)
			{
				bool flag2 = x != player && x.IsAlive && !x.IsNPC && x.Role.Side != player.Role.Side;
				bool flag3 = flag2;
				if (flag3)
				{
					Side side = x.Role.Side;
					bool flag4 = (uint)(side - 1) <= 1u;
					flag3 = flag4;
				}
				return flag3;
			}).ToList();
			if (list.Count == 0)
			{
				list = Player.List.Where((Player x) => x != player && !x.IsNPC).ToList();
			}
			Player victima = ((list.Count > 0) ? list[Var.random.Next(list.Count)] : player);
			player.DisplayNickname = victima.Nickname;
			Var.Disfrazados.Add(player);
			Var.Coroutines.Add(Timing.CallDelayed(1f, delegate
			{
				Var.Coroutines.Add(Timing.RunCoroutine(Coroutines.SpyBc(player, victima)));
			}));
			Var.Coroutines.Add(Timing.CallDelayed(60f, delegate
			{
				Var.CooldownSpy.Remove(player);
			}));
		}
		if (Habilidades.UsaLinterna(player) && Habilidades.DobleLinterna(player))
		{
			Habilidades.Usar(player);
		}
	}

	public void OnShoot(ShootingEventArgs ev)
	{
		Player player = ev.Player;
		Item item = player?.CurrentItem;
		if (item == null)
		{
			return;
		}
		if (Coroutines.EnCuentaRegresiva(player))
		{
			ev.IsAllowed = false;
			return;
		}
		if (Var.Gulag1.Contains(player) || Var.Gulag2.Contains(player) || Var.PeleaPractica.Contains(player))
		{
			MunicionInfinita(player, item);
		}
		try
		{
			if (item.Type == ItemType.GunRevolver && Var.Demoman.Contains(player))
			{
				player.ThrowGrenade(ProjectileType.FragGrenade);
			}
			if (item.Type == ItemType.ParticleDisruptor && Var.Taser.Contains(player) && item is Firearm firearm)
			{
				// [fix] el Taser siempre queda cargado: si llega a 1 bala el juego hace el "último disparo" y lo rompe.
				firearm.MagazineAmmo = Math.Max((int)firearm.MaxMagazineAmmo, 2);
				if (Var.CooldownTaser.Contains(player))
				{
					ev.IsAllowed = false;
					player.ShowHint(Br ? "<b>Você deve esperar 30 segundos até que seu Taser recarregue.</b>" : "<b><color=#859CC7>Debes esperar 30 segundos hasta que se recargue tu Taser.</color></b>");
				}
			}
			else if (item.Type == ItemType.ParticleDisruptor && item is Firearm disruptor && !EnPeleaGulag(player))
			{
				VigilarDisruptorRoto(player, disruptor);
			}
			if (Var.Disfrazados.Remove(player))
			{
				player.ChangeAppearance(player.Role.Type, skipJump: true, 1);
				player.DisplayNickname = null;
			}
			if (item.Serial == 5354)
			{
				player.ThrowGrenade(ProjectileType.FragGrenade);
			}
			if (!ev.IsAllowed)
			{
				Log.Warn($"[Disparo cancelado] {player.Nickname} | arma: {item.Type} | Taser: {Var.Taser.Contains(player)} | Cooldown Taser: {Var.CooldownTaser.Contains(player)} | Demoman: {Var.Demoman.Contains(player)}");
			}
		}
		catch (Exception ex)
		{
			Log.Error(ex.ToString());
		}
	}

	private static bool EnPeleaGulag(Player p)
	{
		return p != null && (Var.Gulag1.Contains(p) || Var.Gulag2.Contains(p) || Var.PeleaPractica.Contains(p));
	}

	private static void MunicionInfinita(Player p, Item item)
	{
		if (!(item is Firearm firearm))
		{
			return;
		}
		try
		{
			if (item.Type == ItemType.ParticleDisruptor)
			{
				// [fix] recargar ANTES del disparo: nunca es el "último disparo" y el disruptor no se rompe.
				firearm.MagazineAmmo = Math.Max((int)firearm.MaxMagazineAmmo, 2);
				return;
			}
			AmmoType ammoType = firearm.AmmoType;
			if (ammoType != 0)
			{
				ushort ammoLimit = p.GetAmmoLimit(ammoType);
				if (ammoLimit > 0 && p.GetAmmo(ammoType) < ammoLimit)
				{
					p.SetAmmo(ammoType, ammoLimit);
				}
			}
		}
		catch (Exception ex)
		{
			Log.Debug("[Gulag] Munición: " + ex.Message);
		}
	}

	private static void VigilarDisruptorRoto(Player p, Firearm disruptor)
	{
		try
		{
			if (disruptor.MagazineAmmo > 1)
			{
				return;
			}
			ushort serial = disruptor.Serial;
			Timing.CallDelayed(2.5f, delegate
			{
				try
				{
					if (!(p == null) && p.IsConnected && p.IsAlive)
					{
						Item item = p.Items.FirstOrDefault((Item i) => i != null && i.Serial == serial);
						if (item is Firearm { MagazineAmmo: 0 } firearm)
						{
							if (firearm.Base is InventorySystem.Items.Firearms.ParticleDisruptor particleDisruptor)
							{
								particleDisruptor.ServerDestroyItem();
							}
							else
							{
								p.RemoveItem(item);
							}
						}
					}
				}
				catch (Exception ex2)
				{
					Log.Debug("[Disruptor] " + ex2.Message);
				}
			});
		}
		catch (Exception ex)
		{
			Log.Debug("[Disruptor] " + ex.Message);
		}
	}

	private static void TaserCargador(Player player, bool lleno)
	{
		try
		{
			if (!(player == null) && player.IsConnected)
			{
				Firearm firearm = player.Items.OfType<Firearm>().FirstOrDefault((Firearm i) => i.Type == ItemType.ParticleDisruptor);
				if (firearm != null)
				{
					firearm.MagazineAmmo = Math.Max((int)firearm.MaxMagazineAmmo, 2);
				}
			}
		}
		catch (Exception arg)
		{
			Log.Error($"[TaserCargador] {arg}");
		}
	}

	private static void TaserListo(Player player)
	{
		Var.CooldownTaser.Remove(player);
		TaserCargador(player, lleno: true);
	}

	public void OnPing079(Scp079PingedEventArgs ev)
	{
		try
		{
			Player player = ((ev?.Player?.ReferenceHub != null) ? Player.Get(ev.Player.ReferenceHub) : null);
			if (!(player == null) && Habilidades.Tiene(player) && !(Habilidades.Restante(player) > 0f))
			{
				Habilidades.Usar(player);
			}
		}
		catch (Exception arg)
		{
			Log.Error($"[Habilidades] Ping de 079: {arg}");
		}
	}

	public void OnAlt(TogglingNoClipEventArgs ev)
	{
		if (Vivo(ev.Player) && Habilidades.Tiene(ev.Player))
		{
			if (!Habilidades.UsaTecla(ev.Player))
			{
				Habilidades.Usar(ev.Player);
			}
			else
			{
				Habilidades.AltIgnorado(ev.Player);
			}
		}
	}

	public void OnTeslaTrigger(TriggeringTeslaEventArgs ev)
	{
		if (!(ev.Player == null) && ev.Player.Role.Type != RoleTypeId.Scp079)
		{
			bool isAllowed;
			switch (ev.Player.Role.Team)
			{
			case Team.SCPs:
			case Team.ChaosInsurgency:
			case Team.ClassD:
				isAllowed = true;
				break;
			default:
				isAllowed = false;
				break;
			}
			ev.IsAllowed = isAllowed;
			if (!Var.TeslaCassieBool && ev.Player.Role.Team == Team.FoundationForces && ev.Player.Role.Type != RoleTypeId.FacilityGuard)
			{
				Var.TeslaCassieBool = true;
				Sonido.Mapa($"Tesla{Var.random.Next(1, 4)}.ogg");
			}
		}
	}

	public void OnRemovedCuff(RemovedHandcuffsEventArgs ev)
	{
		if (ev.Target != null && ev.Player != null)
		{
			ev.Target.ShowHint("<color=#e32433>" + (ev.Player.DisplayNickname ?? ev.Player.Nickname) + "</color> <color=#859CC7>te ha desarrestado.</color>", 6f);
		}
	}

	public void EnteringPocket(EnteringPocketDimensionEventArgs ev)
	{
		ev.Player?.EnableEffect(EffectType.Flashed, 0.1f);
	}

	public void OnPocketEscape(EscapingPocketDimensionEventArgs ev)
	{
		if (ev.Player == null)
		{
			return;
		}
		Player escapado = ev.Player;
		Vector3 destino = ev.TeleportPosition;
		if (ev.IsAllowed)
		{
			Timing.CallDelayed(1.5f, delegate
			{
				Dimension106.Revisar(escapado, destino);
			});
		}
		string text = ev.Player.DisplayNickname ?? ev.Player.Nickname;
		foreach (Player item in Player.List)
		{
			if (item.Role.Type == RoleTypeId.Scp106)
			{
				item.ShowHint("<color=#e32433>" + text + "</color> <color=#859CC7>ha escapado de la dimensión de bolsillo.</color>");
			}
		}
	}

	public void OnPocketDie(FailingEscapePocketDimensionEventArgs ev)
	{
		string text = ev.Player.DisplayNickname ?? ev.Player.Nickname;
		foreach (Player item in Player.List)
		{
			if (item.Role.Type == RoleTypeId.Scp106)
			{
				item.ShowHint("<color=#e32433>" + text + "</color> <color=#859CC7>se perdió en la dimensión de bolsillo.</color>");
				item.Heal(100f);
			}
		}
	}

	public void OnJoin(JoinedEventArgs ev)
	{
		Player p = ev.Player;
		if (p == null || Scp0871.Es(p))
		{
			return;
		}
		if (p.IsNorthwoodStaff)
		{
			foreach (Player item in Player.List)
			{
				if (item.RemoteAdminAccess)
				{
					item.Broadcast(10, "<color=#e32433>Atención:</color>\n<color=#859CC7>Un Staff de NorthWood entró al servidor.</color>");
				}
			}
		}
		Timing.CallDelayed(3f, delegate
		{
			Habilidades.EnviarA(p);
		});
		Timing.CallDelayed(20f, delegate
		{
			if (!(p == null) && p.IsConnected && (p.IsMuted || p.IsGlobalMuted || p.IsIntercomMuted))
			{
				p.IsMuted = false;
				p.IsGlobalMuted = false;
				p.IsIntercomMuted = false;
				p.UnMute();
			}
		});
	}

	public void OnLeft(DestroyingEventArgs ev)
	{
		Player player = ev.Player;
		if (player == null || Scp0871.Es(player))
		{
			return;
		}
		ReemplazoAfk.AntesDeIrse(player, RoleTypeId.Spectator, SpawnReason.ForceClass);
		ReemplazoAfk.AlSalir(player);
		Var.ColaPractica.Remove(player);
		if (Var.PeleaPractica.Contains(player))
		{
			Coroutines.CancelarPractica(player);
		}
		Var.QuitarSubclases(player);
		Var.CooldownFantasma.Remove(player);
		Var.CooldownHeadguard.Remove(player);
		Var.CooldownSpy.Remove(player);
		Var.CooldownTaser.Remove(player);
		Var.CooldownImp.Remove(player);
		Var.CooldownScp507.Remove(player);
		Var.GenteGulag.Remove(player);
		Escuadrones.AlCambiarRol(player);
		Escuadrones.EnOleada.Remove(player);
		Var.HurtingHintCooldown.Remove(player.RawUserId ?? string.Empty);
		Var.LastHpInfoUpdate.Remove(player.RawUserId ?? string.Empty);
		if (Var.Gulag2v2)
		{
			Var.Gulag1.Remove(player);
			Var.Gulag2.Remove(player);
			return;
		}
		Player player2 = null;
		if (Var.Gulag1.Contains(player) && Var.Gulag2.Count > 0)
		{
			player2 = Var.Gulag2[0];
		}
		else if (Var.Gulag2.Contains(player) && Var.Gulag1.Count > 0)
		{
			player2 = Var.Gulag1[0];
		}
		else if (!Var.Gulag1.Contains(player) && !Var.Gulag2.Contains(player))
		{
			return;
		}
		Var.Gulag1.Clear();
		Var.Gulag2.Clear();
		if (player2 != null && player2.IsConnected)
		{
			player2.Role.Set(ElegirRolGulag());
		}
	}
}
