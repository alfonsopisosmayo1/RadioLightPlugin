# RadioLightPlugin

Plugin principal del servidor **RadioLight** de SCP: Secret Laboratory.
Hecho para **SCP:SL 14.2**, **EXILED 9.14** y **LabAPI 1.1**.

## Qué trae

- **Subclases** para Clase-D, científicos, guardias, MTF, Caos y SCPs, con habilidades y cooldowns.
- **Gulag** (1v1 y 2v2) con modificadores por ronda, y **práctica** libre.
- **Escuadrones especiales del MTF**: Nu-7 «Hammer Down» (muchos SCP y 30+ jugadores) y Beta-7 «Maz Hatters» (muchos zombis). Salen una vez por partida, con anuncio de CASSIE propio y música.
- **SCP-087-1**: aparece en la sala con más gente (máximo 3 veces por partida).
- **HUD propia**: zonas fijas en pantalla (paneles, efectos, kill feed, avisos y lista de SCPs aliados) que no se pisan ni dependen de RueI.
- **Lista de SCPs** para el equipo SCP: vida, escudo, ubicación, energía y cámara del 079, energía del 106, estado del 096 y zombis del 049.
- **Anti-lag seguro**: si el server cae de 6 TPS limpia solo basura soltada por jugadores (nunca vitrinas, armas especiales ni objetos del mapa).
- **Medidor de rendimiento**: avisa en consola cada trabón del server y qué código corrió (`rlrendimiento`).
- VIP, estadísticas, logros, votaciones, eventos, Omega Warhead y más.

## Compilar

1. Instalá el [.NET SDK](https://dotnet.microsoft.com/download) 8 o más nuevo.
2. Copiá las DLL del server a `lib/` como dice [`lib/README.md`](lib/README.md).
3. En la carpeta del proyecto:

   ```
   dotnet build -c Release
   ```

4. El plugin queda en `bin/Release/net48/RadioLightPlugin.dll`.

El proyecto usa [Krafs.Publicizer](https://github.com/krafs/Publicizer) (se baja solo con NuGet) para acceder a miembros privados del juego.

## Instalar en el server

1. Copiá `RadioLightPlugin.dll` a `.config/EXILED/Plugins/`.
2. Plugins que usa (opcionales pero recomendados): **AudioPlayer** (sonidos y música), **ProjectMER** + **MEROptimizer** (arenas del gulag), **SSMenuSystem** (tecla de habilidad).
3. Reiniciá el server. La primera vez se crea su sección `radio_light_plugin:` en `.config/EXILED/Configs/<puerto>-config.yml`.

## Claves privadas (webhooks y API keys)

**No van en el código.** Se ponen solo en el config del server, nunca se suben a GitHub.
Copiá las líneas de [`config-ejemplo.yml`](config-ejemplo.yml) dentro de `radio_light_plugin:` y completalas.
Si una queda vacía, esa función simplemente se apaga.

## Comandos principales

| Comando | Dónde | Qué hace |
|---|---|---|
| `escuadron nu7\|beta7 [cantidad]` | RA / consola | Spawnea un escuadrón especial con espectadores |
| `rl087` | RA | Hace aparecer al SCP-087-1 |
| `rlrendimiento [reset]` | RA / consola | Qué partes del server gastan más tiempo |
| `prenderomega` / `apagaromega` | RA / consola | Omega Warhead |
| `evento ...` | RA | Eventos (nukerun, tdm, railgunroyale…) |
| `.gulag` / `.practica` | Consola del jugador | Entrar al gulag o a la práctica |
| `.subclase` / `.habilidad` | Consola del jugador | Info de tu subclase / usar habilidad |

## Estructura

```
RadioLightPlugin/            Plugin.cs, Config.cs, Secretos.cs
RadioLightPlugin.Handlers/   eventos de jugador, servidor y mapa
RadioLightPlugin.Subclases/  subclases, escuadrones (Nu-7 / Beta-7), utilidades
RadioLightPlugin.Misc/       HUD propia, gulag, SCP-087-1, limpieza, sonido, info de SCPs…
RadioLightPlugin.Patches/    parches de Harmony (HUD, medidor de rendimiento…)
RadioLightPlugin.Command/    comandos
PlagueNotifier/              avisos de la plaga de zombis
```
