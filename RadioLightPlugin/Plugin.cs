using System;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Item;
using Exiled.Events.EventArgs.Map;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Scp079;
using Exiled.Events.EventArgs.Scp096;
using Exiled.Events.EventArgs.Scp330;
using Exiled.Events.EventArgs.Server;
using Exiled.Events.EventArgs.Warhead;
using Exiled.Events.Features;
using Exiled.Events.Handlers;
using HarmonyLib;
using LabApi.Events.Handlers;
using PlagueNotifier;
using RadioLightPlugin.Handlers;
using RadioLightPlugin.Misc;
using RadioLightPlugin.Patches;
using RadioLightPlugin.Subclases;

namespace RadioLightPlugin;

public class Plugin : Plugin<Config>
{
	private readonly Harmony harmony = new Harmony("com.radiolight.patch");

	public static Plugin Singleton { get; private set; }

	public EventHandlers Handler { get; set; }

	public MapHandler MapHandler { get; set; }

	public PlayerHandler PlayerHandler { get; set; }

	public ServerHandler ServerHandler { get; set; }

	public override void OnEnabled()
	{
		Singleton = this;
		try
		{
			harmony.PatchAll();
		}
		catch (Exception ex)
		{
			Log.Error(ex.ToString());
		}
		RegisterEvents();
		Iniciar("hilo principal", HiloPrincipal.Iniciar);
		Iniciar("HUD de efectos", EffectsHUD.Start);
		Iniciar("HUD de efectos (respetar otros hints)", delegate
		{
			HudRespetaHints.Aplicar(harmony);
		});
		Iniciar("consola limpia (anuncios al HUD)", delegate
		{
			ConsolaLimpia.Aplicar(harmony);
		});
		Iniciar("tecla de habilidad", Habilidades.Registrar);
		Iniciar("avisos de arrestos", Arrestos.Iniciar);
		Iniciar("info de SCPs en el HUD", InfoScp.Iniciar);
		Iniciar("avisos de zombies (RueI)", PlagueNotifier.Plugin.Activar);
		Iniciar("timer de respawn", TimerRespawn.Iniciar);
		Iniciar("pasivas de subclases", Pasivas.Iniciar);
		Iniciar("medidor de rendimiento", delegate
		{
			Perfilador.Aplicar(harmony);
		});
		Log.Info("[RadioLight] Plugin activado (compilación 03/10 — anti-teamkill con fuego cruzado).");
		base.OnEnabled();
	}

	public override void OnDisabled()
	{
		Singleton = null;
		try
		{
			harmony.UnpatchAll("com.radiolight.patch");
		}
		catch (Exception ex)
		{
			Log.Error(ex.ToString());
		}
		UnregisterEvents();
		try
		{
			Estadisticas.Guardar();
		}
		catch (Exception ex2)
		{
			Log.Warn("[Estadísticas] " + ex2.Message);
		}
		EffectsHUD.Stop();
		Iniciar("tecla de habilidad (apagar)", Habilidades.Desregistrar);
		Iniciar("avisos de arrestos (apagar)", Arrestos.Detener);
		Iniciar("info de SCPs (apagar)", InfoScp.Detener);
		Iniciar("avisos de zombies (apagar)", PlagueNotifier.Plugin.Desactivar);
		Iniciar("timer de respawn (apagar)", TimerRespawn.Detener);
		Iniciar("pasivas de subclases (apagar)", Pasivas.Detener);
		Iniciar("medidor de rendimiento (apagar)", Perfilador.Detener);
		Iniciar("hilo principal (apagar)", HiloPrincipal.Detener);
		base.OnDisabled();
	}

	private static void Iniciar(string nombre, Action accion)
	{
		try
		{
			accion();
		}
		catch (Exception arg)
		{
			Log.Error($"[RadioLight] Falló '{nombre}': {arg}");
		}
	}

	private void RegisterEvents()
	{
		Handler = new EventHandlers();
		MapHandler = new MapHandler();
		PlayerHandler = new PlayerHandler();
		ServerHandler = new ServerHandler();
		Exiled.Events.Handlers.Map.AnnouncingChaosEntrance += new CustomEventHandler<AnnouncingChaosEntranceEventArgs>(MapHandler.OnIDC);
		Exiled.Events.Handlers.Map.AnnouncingNtfEntrance += new CustomEventHandler<AnnouncingNtfEntranceEventArgs>(MapHandler.OnMTF);
		Exiled.Events.Handlers.Map.AnnouncingScpTermination += new CustomEventHandler<AnnouncingScpTerminationEventArgs>(MapHandler.OnScpCassie);
		Exiled.Events.Handlers.Map.Decontaminating += new CustomEventHandler<DecontaminatingEventArgs>(MapHandler.OnDecontaminating);
		Scp079.Recontained += new CustomEventHandler<RecontainedEventArgs>(GeneratorHandler.OnScp079Recontained);
		Scp079Events.Pinged += PlayerHandler.OnPing079;
		Exiled.Events.Handlers.Player.Dying += new CustomEventHandler<DyingEventArgs>(GeneratorHandler.OnPlayerDying);
		Exiled.Events.Handlers.Warhead.Detonated += new CustomEventHandler(GeneratorHandler.OnWarheadDetonated);
		Exiled.Events.Handlers.Server.RoundEnded += new CustomEventHandler<RoundEndedEventArgs>(ServerHandler.OnRoundEnd);
		Exiled.Events.Handlers.Server.RespawningTeam += new CustomEventHandler<RespawningTeamEventArgs>(ServerHandler.OnRespawn);
		MTF.RegistrarEventos();
		Exiled.Events.Handlers.Warhead.Detonating += new CustomEventHandler<DetonatingEventArgs>(ServerHandler.OnDetonating);
		Exiled.Events.Handlers.Warhead.Starting += new CustomEventHandler<StartingEventArgs>(ServerHandler.OnWarheadStart);
		Exiled.Events.Handlers.Server.RoundStarted += new CustomEventHandler(ServerHandler.OnRoundStarted);
		Exiled.Events.Handlers.Server.RestartingRound += new CustomEventHandler(ServerHandler.OnRoundRestart);
		Exiled.Events.Handlers.Server.WaitingForPlayers += new CustomEventHandler(ServerHandler.OnWaiting);
		Exiled.Events.Handlers.Server.WaitingForPlayers += new CustomEventHandler(Scp3114Natural.Reiniciar);
		Exiled.Events.Handlers.Player.Escaping += new CustomEventHandler<EscapingEventArgs>(PlayerHandler.OnEscape);
		Exiled.Events.Handlers.Player.Hurt += new CustomEventHandler<HurtEventArgs>(PlayerHandler.OnHurt);
		Exiled.Events.Handlers.Player.Hurting += new CustomEventHandler<HurtingEventArgs>(PlayerHandler.OnHurting);
		Exiled.Events.Handlers.Player.Hurting += new CustomEventHandler<HurtingEventArgs>(Extra.AlHerir);
		Exiled.Events.Handlers.Player.Hurting += new CustomEventHandler<HurtingEventArgs>(Escuadrones.AlHerir);
		Exiled.Events.Handlers.Player.Shooting += new CustomEventHandler<ShootingEventArgs>(Extra.AlDisparar);
		Exiled.Events.Handlers.Player.Healing += new CustomEventHandler<HealingEventArgs>(Extra.AlCurarse);
		Exiled.Events.Handlers.Player.ChangingRole += new CustomEventHandler<ChangingRoleEventArgs>(Pasivas.AlCambiarRol);
		Exiled.Events.Handlers.Player.Died += new CustomEventHandler<DiedEventArgs>(Pasivas.AlMorir);
		Exiled.Events.Handlers.Player.UsedItem += new CustomEventHandler<UsedItemEventArgs>(Pasivas.AlUsarItem);
		Exiled.Events.Handlers.Player.ChangingItem += new CustomEventHandler<ChangingItemEventArgs>(Extra.AlCambiarItem);
		Exiled.Events.Handlers.Player.PickingUpItem += new CustomEventHandler<PickingUpItemEventArgs>(Coroutines.AlLevantarItem);
		Exiled.Events.Handlers.Player.Died += new CustomEventHandler<DiedEventArgs>(AntiTeamKill.AlMorir);
		Exiled.Events.Handlers.Player.Hurting += new CustomEventHandler<HurtingEventArgs>(ModosGulag.AlHerir);
		Exiled.Events.Handlers.Map.ExplodingGrenade += new CustomEventHandler<ExplodingGrenadeEventArgs>(Otros.AlExplotarGranada);
		Exiled.Events.Handlers.Player.ChangingRole += new CustomEventHandler<ChangingRoleEventArgs>(ModosGulag.AlCambiarRolEv);
		Exiled.Events.Handlers.Player.Hurt += new CustomEventHandler<HurtEventArgs>(AntiTeamKill.AlRecibirDanio);
		Exiled.Events.Handlers.Player.Dying += new CustomEventHandler<DyingEventArgs>(AntiTeamKill.AntesDeMorir);
		Exiled.Events.Handlers.Player.Died += new CustomEventHandler<DiedEventArgs>(Dinamica.AlMorir);
		Exiled.Events.Handlers.Player.Hurt += new CustomEventHandler<HurtEventArgs>(Dinamica.AlRecibirDanio);
		Exiled.Events.Handlers.Player.ChangingRole += new CustomEventHandler<ChangingRoleEventArgs>(Dinamica.AlCambiarRol);
		Exiled.Events.Handlers.Player.ChangingRole += new CustomEventHandler<ChangingRoleEventArgs>(TimerRespawn.AlCambiarRol);
		Exiled.Events.Handlers.Player.ChangingRole += new CustomEventHandler<ChangingRoleEventArgs>(Presentaciones.AlCambiarRol);
		Exiled.Events.Handlers.Player.Verified += new CustomEventHandler<VerifiedEventArgs>(VentajasVip.AlVerificar);
		Exiled.Events.Handlers.Player.Verified += new CustomEventHandler<VerifiedEventArgs>(Estadisticas.AlVerificar);
		Exiled.Events.Handlers.Player.Verified += new CustomEventHandler<VerifiedEventArgs>(DiscordLog.AlVerificar);
		Exiled.Events.Handlers.Player.Died += new CustomEventHandler<DiedEventArgs>(Estadisticas.AlMorir);
		Exiled.Events.Handlers.Player.Died += new CustomEventHandler<DiedEventArgs>(KillFeed.AlMorir);
		Exiled.Events.Handlers.Player.Hurt += new CustomEventHandler<HurtEventArgs>(Estadisticas.AlHacerDanio);
		Exiled.Events.Handlers.Player.Escaping += new CustomEventHandler<EscapingEventArgs>(Estadisticas.AlEscapar);
		Exiled.Events.Handlers.Player.Escaped += new CustomEventHandler<EscapedEventArgs>(Estadisticas.AlEscapado);
		Exiled.Events.Handlers.Server.RoundEnded += new CustomEventHandler<RoundEndedEventArgs>(VentajasVip.AlTerminarRonda);
		Exiled.Events.Handlers.Player.Healed += new CustomEventHandler<HealedEventArgs>(PlayerHandler.OnHealed);
		Exiled.Events.Handlers.Player.Dying += new CustomEventHandler<DyingEventArgs>(PlayerHandler.OnDying);
		Exiled.Events.Handlers.Player.Shooting += new CustomEventHandler<ShootingEventArgs>(PlayerHandler.OnShoot);
		Exiled.Events.Handlers.Player.Spawned += new CustomEventHandler<SpawnedEventArgs>(PlayerHandler.OnSpawn);
		Exiled.Events.Handlers.Player.ChangingRole += new CustomEventHandler<ChangingRoleEventArgs>(Scp3114Natural.AlCambiarRol);
		Exiled.Events.Handlers.Player.ChangingRole += new CustomEventHandler<ChangingRoleEventArgs>(PlayerHandler.ChangingRole);
		Exiled.Events.Handlers.Player.TriggeringTesla += new CustomEventHandler<TriggeringTeslaEventArgs>(PlayerHandler.OnTeslaTrigger);
		Exiled.Events.Handlers.Player.TogglingFlashlight += new CustomEventHandler<TogglingFlashlightEventArgs>(PlayerHandler.OnFlashlightToggle);
		Exiled.Events.Handlers.Player.TogglingNoClip += new CustomEventHandler<TogglingNoClipEventArgs>(PlayerHandler.OnAlt);
		Exiled.Events.Handlers.Player.DroppedItem += new CustomEventHandler<DroppedItemEventArgs>(PlayerHandler.OnItemDropped);
		Exiled.Events.Handlers.Player.PickingUpItem += new CustomEventHandler<PickingUpItemEventArgs>(PlayerHandler.OnItemPick);
		Exiled.Events.Handlers.Player.ThrownProjectile += new CustomEventHandler<ThrownProjectileEventArgs>(PlayerHandler.OnThrowingGrenade);
		Exiled.Events.Handlers.Player.RemovedHandcuffs += new CustomEventHandler<RemovedHandcuffsEventArgs>(PlayerHandler.OnRemovedCuff);
		Exiled.Events.Handlers.Player.RemovedHandcuffs += new CustomEventHandler<RemovedHandcuffsEventArgs>(Arrestos.AlDesesposar);
		Exiled.Events.Handlers.Player.Handcuffing += new CustomEventHandler<HandcuffingEventArgs>(Arrestos.AlEsposar);
		Exiled.Events.Handlers.Player.EnteringPocketDimension += new CustomEventHandler<EnteringPocketDimensionEventArgs>(PlayerHandler.EnteringPocket);
		Exiled.Events.Handlers.Player.EscapingPocketDimension += new CustomEventHandler<EscapingPocketDimensionEventArgs>(PlayerHandler.OnPocketEscape);
		Exiled.Events.Handlers.Player.FailingEscapePocketDimension += new CustomEventHandler<FailingEscapePocketDimensionEventArgs>(PlayerHandler.OnPocketDie);
		Exiled.Events.Handlers.Player.FlippingCoin += new CustomEventHandler<FlippingCoinEventArgs>(PlayerHandler.OnCoin);
		Exiled.Events.Handlers.Player.Joined += new CustomEventHandler<JoinedEventArgs>(PlayerHandler.OnJoin);
		Exiled.Events.Handlers.Player.Destroying += new CustomEventHandler<DestroyingEventArgs>(PlayerHandler.OnLeft);
		Exiled.Events.Handlers.Player.UsedItem += new CustomEventHandler<UsedItemEventArgs>(PlayerHandler.OnUsedItem);
		Exiled.Events.Handlers.Player.ChangingRadioPreset += new CustomEventHandler<ChangingRadioPresetEventArgs>(PlayerHandler.OnRadioPreset);
		Item.ChargingJailbird += new CustomEventHandler<ChargingJailbirdEventArgs>(Handler.OnChargingJailbird);
		Scp330.EatingScp330 += new CustomEventHandler<EatingScp330EventArgs>(Handler.OnEatingCandy);
		Scp330.InteractingScp330 += new CustomEventHandler<InteractingScp330EventArgs>(Handler.OnCandy);
		Scp096.AddingTarget += new CustomEventHandler<AddingTargetEventArgs>(Handler.OnSee096);
	}

	private void UnregisterEvents()
	{
		Exiled.Events.Handlers.Map.AnnouncingChaosEntrance -= new CustomEventHandler<AnnouncingChaosEntranceEventArgs>(MapHandler.OnIDC);
		Exiled.Events.Handlers.Map.AnnouncingNtfEntrance -= new CustomEventHandler<AnnouncingNtfEntranceEventArgs>(MapHandler.OnMTF);
		Exiled.Events.Handlers.Map.AnnouncingScpTermination -= new CustomEventHandler<AnnouncingScpTerminationEventArgs>(MapHandler.OnScpCassie);
		Exiled.Events.Handlers.Map.Decontaminating -= new CustomEventHandler<DecontaminatingEventArgs>(MapHandler.OnDecontaminating);
		Scp079.Recontained -= new CustomEventHandler<RecontainedEventArgs>(GeneratorHandler.OnScp079Recontained);
		Scp079Events.Pinged -= PlayerHandler.OnPing079;
		Exiled.Events.Handlers.Player.Dying -= new CustomEventHandler<DyingEventArgs>(GeneratorHandler.OnPlayerDying);
		Exiled.Events.Handlers.Warhead.Detonated -= new CustomEventHandler(GeneratorHandler.OnWarheadDetonated);
		Exiled.Events.Handlers.Server.RoundEnded -= new CustomEventHandler<RoundEndedEventArgs>(ServerHandler.OnRoundEnd);
		Exiled.Events.Handlers.Server.RespawningTeam -= new CustomEventHandler<RespawningTeamEventArgs>(ServerHandler.OnRespawn);
		MTF.DesregistrarEventos();
		Exiled.Events.Handlers.Warhead.Detonating -= new CustomEventHandler<DetonatingEventArgs>(ServerHandler.OnDetonating);
		Exiled.Events.Handlers.Warhead.Starting -= new CustomEventHandler<StartingEventArgs>(ServerHandler.OnWarheadStart);
		Exiled.Events.Handlers.Server.RoundStarted -= new CustomEventHandler(ServerHandler.OnRoundStarted);
		Exiled.Events.Handlers.Server.RestartingRound -= new CustomEventHandler(ServerHandler.OnRoundRestart);
		Exiled.Events.Handlers.Server.WaitingForPlayers -= new CustomEventHandler(ServerHandler.OnWaiting);
		Exiled.Events.Handlers.Server.WaitingForPlayers -= new CustomEventHandler(Scp3114Natural.Reiniciar);
		Exiled.Events.Handlers.Player.Escaping -= new CustomEventHandler<EscapingEventArgs>(PlayerHandler.OnEscape);
		Exiled.Events.Handlers.Player.Hurt -= new CustomEventHandler<HurtEventArgs>(PlayerHandler.OnHurt);
		Exiled.Events.Handlers.Player.Hurting -= new CustomEventHandler<HurtingEventArgs>(PlayerHandler.OnHurting);
		Exiled.Events.Handlers.Player.Hurting -= new CustomEventHandler<HurtingEventArgs>(Extra.AlHerir);
		Exiled.Events.Handlers.Player.Hurting -= new CustomEventHandler<HurtingEventArgs>(Escuadrones.AlHerir);
		Exiled.Events.Handlers.Player.Shooting -= new CustomEventHandler<ShootingEventArgs>(Extra.AlDisparar);
		Exiled.Events.Handlers.Player.Healing -= new CustomEventHandler<HealingEventArgs>(Extra.AlCurarse);
		Exiled.Events.Handlers.Player.ChangingRole -= new CustomEventHandler<ChangingRoleEventArgs>(Pasivas.AlCambiarRol);
		Exiled.Events.Handlers.Player.Died -= new CustomEventHandler<DiedEventArgs>(Pasivas.AlMorir);
		Exiled.Events.Handlers.Player.UsedItem -= new CustomEventHandler<UsedItemEventArgs>(Pasivas.AlUsarItem);
		Exiled.Events.Handlers.Player.ChangingItem -= new CustomEventHandler<ChangingItemEventArgs>(Extra.AlCambiarItem);
		Exiled.Events.Handlers.Player.PickingUpItem -= new CustomEventHandler<PickingUpItemEventArgs>(Coroutines.AlLevantarItem);
		Exiled.Events.Handlers.Player.Died -= new CustomEventHandler<DiedEventArgs>(AntiTeamKill.AlMorir);
		Exiled.Events.Handlers.Player.Hurting -= new CustomEventHandler<HurtingEventArgs>(ModosGulag.AlHerir);
		Exiled.Events.Handlers.Map.ExplodingGrenade -= new CustomEventHandler<ExplodingGrenadeEventArgs>(Otros.AlExplotarGranada);
		Exiled.Events.Handlers.Player.ChangingRole -= new CustomEventHandler<ChangingRoleEventArgs>(ModosGulag.AlCambiarRolEv);
		Exiled.Events.Handlers.Player.Hurt -= new CustomEventHandler<HurtEventArgs>(AntiTeamKill.AlRecibirDanio);
		Exiled.Events.Handlers.Player.Dying -= new CustomEventHandler<DyingEventArgs>(AntiTeamKill.AntesDeMorir);
		Exiled.Events.Handlers.Player.Died -= new CustomEventHandler<DiedEventArgs>(Dinamica.AlMorir);
		Exiled.Events.Handlers.Player.Hurt -= new CustomEventHandler<HurtEventArgs>(Dinamica.AlRecibirDanio);
		Exiled.Events.Handlers.Player.ChangingRole -= new CustomEventHandler<ChangingRoleEventArgs>(Dinamica.AlCambiarRol);
		Exiled.Events.Handlers.Player.ChangingRole -= new CustomEventHandler<ChangingRoleEventArgs>(TimerRespawn.AlCambiarRol);
		Exiled.Events.Handlers.Player.ChangingRole -= new CustomEventHandler<ChangingRoleEventArgs>(Presentaciones.AlCambiarRol);
		Exiled.Events.Handlers.Player.Verified -= new CustomEventHandler<VerifiedEventArgs>(VentajasVip.AlVerificar);
		Exiled.Events.Handlers.Player.Verified -= new CustomEventHandler<VerifiedEventArgs>(Estadisticas.AlVerificar);
		Exiled.Events.Handlers.Player.Verified -= new CustomEventHandler<VerifiedEventArgs>(DiscordLog.AlVerificar);
		Exiled.Events.Handlers.Player.Died -= new CustomEventHandler<DiedEventArgs>(Estadisticas.AlMorir);
		Exiled.Events.Handlers.Player.Died -= new CustomEventHandler<DiedEventArgs>(KillFeed.AlMorir);
		Exiled.Events.Handlers.Player.Hurt -= new CustomEventHandler<HurtEventArgs>(Estadisticas.AlHacerDanio);
		Exiled.Events.Handlers.Player.Escaping -= new CustomEventHandler<EscapingEventArgs>(Estadisticas.AlEscapar);
		Exiled.Events.Handlers.Player.Escaped -= new CustomEventHandler<EscapedEventArgs>(Estadisticas.AlEscapado);
		Exiled.Events.Handlers.Server.RoundEnded -= new CustomEventHandler<RoundEndedEventArgs>(VentajasVip.AlTerminarRonda);
		Exiled.Events.Handlers.Player.Healed -= new CustomEventHandler<HealedEventArgs>(PlayerHandler.OnHealed);
		Exiled.Events.Handlers.Player.Dying -= new CustomEventHandler<DyingEventArgs>(PlayerHandler.OnDying);
		Exiled.Events.Handlers.Player.Shooting -= new CustomEventHandler<ShootingEventArgs>(PlayerHandler.OnShoot);
		Exiled.Events.Handlers.Player.Spawned -= new CustomEventHandler<SpawnedEventArgs>(PlayerHandler.OnSpawn);
		Exiled.Events.Handlers.Player.ChangingRole -= new CustomEventHandler<ChangingRoleEventArgs>(PlayerHandler.ChangingRole);
		Exiled.Events.Handlers.Player.TriggeringTesla -= new CustomEventHandler<TriggeringTeslaEventArgs>(PlayerHandler.OnTeslaTrigger);
		Exiled.Events.Handlers.Player.TogglingFlashlight -= new CustomEventHandler<TogglingFlashlightEventArgs>(PlayerHandler.OnFlashlightToggle);
		Exiled.Events.Handlers.Player.TogglingNoClip -= new CustomEventHandler<TogglingNoClipEventArgs>(PlayerHandler.OnAlt);
		Exiled.Events.Handlers.Player.DroppedItem -= new CustomEventHandler<DroppedItemEventArgs>(PlayerHandler.OnItemDropped);
		Exiled.Events.Handlers.Player.PickingUpItem -= new CustomEventHandler<PickingUpItemEventArgs>(PlayerHandler.OnItemPick);
		Exiled.Events.Handlers.Player.ThrownProjectile -= new CustomEventHandler<ThrownProjectileEventArgs>(PlayerHandler.OnThrowingGrenade);
		Exiled.Events.Handlers.Player.RemovedHandcuffs -= new CustomEventHandler<RemovedHandcuffsEventArgs>(PlayerHandler.OnRemovedCuff);
		Exiled.Events.Handlers.Player.RemovedHandcuffs -= new CustomEventHandler<RemovedHandcuffsEventArgs>(Arrestos.AlDesesposar);
		Exiled.Events.Handlers.Player.Handcuffing -= new CustomEventHandler<HandcuffingEventArgs>(Arrestos.AlEsposar);
		Exiled.Events.Handlers.Player.EnteringPocketDimension -= new CustomEventHandler<EnteringPocketDimensionEventArgs>(PlayerHandler.EnteringPocket);
		Exiled.Events.Handlers.Player.EscapingPocketDimension -= new CustomEventHandler<EscapingPocketDimensionEventArgs>(PlayerHandler.OnPocketEscape);
		Exiled.Events.Handlers.Player.FailingEscapePocketDimension -= new CustomEventHandler<FailingEscapePocketDimensionEventArgs>(PlayerHandler.OnPocketDie);
		Exiled.Events.Handlers.Player.FlippingCoin -= new CustomEventHandler<FlippingCoinEventArgs>(PlayerHandler.OnCoin);
		Exiled.Events.Handlers.Player.Joined -= new CustomEventHandler<JoinedEventArgs>(PlayerHandler.OnJoin);
		Exiled.Events.Handlers.Player.Destroying -= new CustomEventHandler<DestroyingEventArgs>(PlayerHandler.OnLeft);
		Exiled.Events.Handlers.Player.UsedItem -= new CustomEventHandler<UsedItemEventArgs>(PlayerHandler.OnUsedItem);
		Exiled.Events.Handlers.Player.ChangingRadioPreset -= new CustomEventHandler<ChangingRadioPresetEventArgs>(PlayerHandler.OnRadioPreset);
		Item.ChargingJailbird -= new CustomEventHandler<ChargingJailbirdEventArgs>(Handler.OnChargingJailbird);
		Scp330.EatingScp330 -= new CustomEventHandler<EatingScp330EventArgs>(Handler.OnEatingCandy);
		Scp330.InteractingScp330 -= new CustomEventHandler<InteractingScp330EventArgs>(Handler.OnCandy);
		Scp096.AddingTarget -= new CustomEventHandler<AddingTargetEventArgs>(Handler.OnSee096);
		Handler = null;
		MapHandler = null;
		PlayerHandler = null;
		ServerHandler = null;
	}
}
