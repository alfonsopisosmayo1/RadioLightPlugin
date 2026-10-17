using CustomPlayerEffects;
using Exiled.API.Enums;
using Exiled.API.Features;
using MEC;
using PlayerRoles;
using RadioLightPlugin.Misc;

namespace RadioLightPlugin.Subclases;

public static class CompuestoC
{
	private static bool Sigue(Player p, RoleTypeId rol)
	{
		return Util.Vivo(p) && p.Role.Type == rol;
	}

	public static void ATrain(Player player)
	{
		Pasivas.Exento(player);
		RoleTypeId rol = player.Role.Type;
		player.AddAhp(500f, 500f, 0f, 0.4f);
		Util.SubirVida(player, 200f);
		player.EnableEffect(EffectType.MovementBoost);
		player.ChangeEffectIntensity(EffectType.MovementBoost, 220);
		Util.Bc(player, 5, "<color=#008f4a>[ <color=#4D4D4B>Tren-Roca</color> ]</color>", "<color=yellow>Você vê um casal se beijando enquanto corre e...</color>");
		Var.Coroutines.Add(Timing.CallDelayed(100f, delegate
		{
			if (Sigue(player, rol))
			{
				player.DisableAllEffects();
				player.EnableEffect<Sinkhole>();
				player.EnableEffect<Exhausted>();
				player.EnableEffect<CardiacArrest>();
				player.Health = 60f;
				Util.Bc(player, 5, "<color=#e32433>Tu corazón se aceleró demasiado, no puede soportarlo más.</color>", "<color=#e32433>Seu coração acelerou demais, não consegue mais suportar.</color>");
			}
		}));
	}

	public static void Starlight(Player player)
	{
		Pasivas.Exento(player);
		RoleTypeId rol = player.Role.Type;
		Util.Bc(player, 5, "<color=#008f4a>[ <color=#4D4D4B>Lamparita</color> ]</color>", "<color=yellow>Você sente o poder da eletricidade em você.</color>");
		Var.Coroutines.Add(Timing.CallDelayed(10f, delegate
		{
			if (Sigue(player, rol))
			{
				player.EnableEffect<Traumatized>();
				player.AddAhp(500f, 500f, 0f, 0.4f);
				Util.SubirVida(player, 200f);
				Util.Bc(player, 8, "<color=yellow>Tienes toda la electricidad en tu cuerpo.</color>", "<color=yellow>Você tem toda a eletricidade em seu corpo.</color>");
			}
		}));
	}

	public static void SoldierBoy(Player player)
	{
		Pasivas.Exento(player);
		Util.Bc(player, 8, "<color=yellow>Los verdaderos héroes, no juegan Blue Archive.</color>", "<color=yellow>Os verdadeiros heróis não jogam Genshin Impact.</color>");
		player.EnableEffect<Traumatized>();
		player.EnableEffect<DamageReduction>();
		player.EnableEffect<Exhausted>();
		player.EnableEffect<AntiScp207>();
		player.AddAhp(800f, 800f, 0f);
		Util.SubirVida(player, 800f);
	}

	public static void Homelander(Player player)
	{
		Pasivas.Exento(player);
		RoleTypeId rol = player.Role.Type;
		Util.Bc(player, 5, "<color=#0C0066>[ <color=#4D4D4B>Casa Aterrizador</color> ]</color>", "<color=yellow>Seu narcisismo se amplifica.</color>");
		Var.Coroutines.Add(Timing.CallDelayed(10f, delegate
		{
			if (Sigue(player, rol))
			{
				player.EnableEffect<Traumatized>();
				player.EnableEffect<DamageReduction>();
				player.AddAhp(600f, 600f, 0f);
				Util.SubirVida(player, 800f);
				Util.Bc(player, 8, "<color=yellow>Oye papu soy más fuerte, más astuto, soy mejor.</color>", "<color=yellow>Sou mais forte, mais astuto, sou melhor, EU SOU MELHOR.</color>");
			}
		}));
	}

	public static void BlackNoir(Player player)
	{
		Pasivas.Exento(player);
		RoleTypeId rol = player.Role.Type;
		player.Broadcast(8, "<color=#008f4a>[ <color=#4D4D4B>Negro Oscuro</color> ]</color>");
		Var.Coroutines.Add(Timing.CallDelayed(10f, delegate
		{
			if (Sigue(player, rol))
			{
				if (!player.IsInventoryFull)
				{
					player.AddItem(ItemType.Jailbird);
				}
				player.EnableEffect<Traumatized>();
				player.EnableEffect<DamageReduction>();
				player.EnableEffect<AntiScp207>();
				player.EnableEffect(EffectType.Scp1853);
				player.ChangeEffectIntensity(EffectType.Scp1853, byte.MaxValue);
				player.EnableEffect(EffectType.SilentWalk);
				player.ChangeEffectIntensity(EffectType.SilentWalk, byte.MaxValue);
				player.EnableEffect(EffectType.MovementBoost);
				player.ChangeEffectIntensity(EffectType.MovementBoost, 30);
				Util.SubirVida(player, 500f);
				player.Broadcast(8, "<color=yellow>...</color>");
			}
		}));
	}
}
