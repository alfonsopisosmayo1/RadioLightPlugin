using System.Collections.Generic;
using System.ComponentModel;
using Exiled.API.Enums;
using Exiled.API.Interfaces;
using PlagueNotifier;
using RadioLightPlugin.Misc;

namespace RadioLightPlugin;

public class Config : IConfig
{
	public string[] TextosLlorona = new string[26]
	{
		"<b><color=#e32433>Ahí lo tenés al, PELOTUDO...</color></b>", "<b><color=#e32433>JAJA miren, se murió.</color></b>", "<b><color=#e32433>Pal gulag.</color></b>", "<b><color=#e32433>gg es</color></b>", "<b><color=#e32433>gg ez</color></b>", "<b><color=#e32433>Prepará el orto.</color></b>", "<b><color=#e32433>Prepará el culo.</color></b>", "<b><color=#e32433>Auf wiedersehen!</color></b>", "<b><color=#e32433>Tschüss!</color></b>", "<b><color=#e32433>JAJA MIREN, SE MURIÓ./color></b>",
		"<b><color=#e32433>Bienvenido al Gulag.</color></b>", "<b><color=#e32433>Por porteño.</color></b>", "<b><color=#e32433>Causa me van a matar pipipi</color></b>", "<b><color=#e32433>Piipipiiiii</color></b>", "<b><color=#e32433>Guam</color></b>", "<b><color=#e32433>Guampudo.</color></b>", "<b><color=#e32433>Aiuto</color></b>", "<b><color=#e32433>Cazzo</color></b>", "<b><color=#e32433>Sei stronso?</color></b>", "<b><color=#e32433>Por no seguir a BaiJavier.</color></b>",
		"<b><color=#e32433>Por no seguir a Carlos Estratega.</color></b>", "<b><color=#e32433>Por no seguir a LuisMiNegocios.</color></b>", "<b><color=#e32433>Por no seguir a Teniente Guam.</color></b>", "<b><color=#e32433>Rescatando al soldado Ryan sale mal.</color></b>", "<b><color=#e32433>bool IsDead = true;</color></b>", "<b><color=#e32433>bool Cagaste = true;</color></b>"
	};

	public string[] TextosCarameloRosa = new string[4] { "<b><color=#e32433>*Insertar frase funable*</color></b>", "<b><color=#e32433>الله أكبر</color></b>", "<b><color=#e32433>Allāhu Akbar</color></b>", "<b><color=#e32433>AY DIOS MIO</color></b>" };

	public bool Debug { get; set; } = false;


	public bool IsEnabled { get; set; } = true;


	public bool Brazuca { get; set; } = false;


	[Description("Consola de los jugadores limpia: los anuncios (Broadcast) de este plugin salen en el HUD de avisos y no quedan escritos en su consola. Ponelo en false para volver a los Broadcast normales.")]
	public bool ConsolaLimpia { get; set; } = true;


	[Description("Avisos de muerte de zombies (antes era el plugin PlagueNotifier).")]
	public PlagueNotifier.Config Plaga { get; set; } = new PlagueNotifier.Config();


	[Description("Máximo de zombies infectados vivos a la vez (los que convierten al matar).")]
	public int MaxZombiesInfectados { get; set; } = 2;


	[Description("Si el juego repite un SCP al empezar la ronda (muchos jugadores), uno de los repetidos pasa a ser SCP-3114 (nunca en Light).")]
	public bool Scp3114EnVezDeRepetido { get; set; } = true;


	[Description("Pone una Aspiradora (schematic de MapEditorReborn) en Surface, del lado de Gate B, en cada ronda.")]
	public bool AspiradoraGateB { get; set; } = true;


	[Description("Arenas del gulag y la práctica: cada ronda se elige una al azar. Posiciones como 'x, y, z'. Schematic (opcional) = mapa de ProjectMER que se pone en posicion_schematic al empezar la ronda.")]
	public List<ArenaGulag> GulagArenas { get; set; } = new List<ArenaGulag>
	{
		new ArenaGulag(),
		new ArenaGulag
		{
			Nombre = "Contenedores",
			Schematic = "ArenaContenedores",
			PosicionSchematic = "150, 3000, 0",
			SpawnA = "138, 3001, 0",
			SpawnB = "162, 3001, 0",
			Espera = "150, 3001, -18.25"
		},
		new ArenaGulag
		{
			Nombre = "Pilares",
			Schematic = "ArenaPilares",
			PosicionSchematic = "-150, 3000, 0",
			SpawnA = "-162, 3001, 0",
			SpawnB = "-138, 3001, 0",
			Espera = "-150, 3001, -18.25"
		},
		new ArenaGulag
		{
			Nombre = "Plaza",
			Schematic = "ArenaPlaza",
			PosicionSchematic = "0, 3000, 150",
			SpawnA = "-12, 3001, 150",
			SpawnB = "12, 3001, 150",
			Espera = "0, 3001, 131.75"
		}
	};


	[Description("Pone una luz de color en CADA botón de puerta y lector de tarjeta del mapa (cientos de luces). Se ve lindo pero baja los FPS de los jugadores: por eso viene apagado. Requiere reiniciar el servidor.")]
	public bool LucesBotonesPuertas { get; set; } = false;


	[Description("Mide qué partes del plugin usan más tiempo del servidor (para encontrar lag). Cada 5 min lo escribe en el log y avisa si algo tarda más de 30 ms. Comando: rlrendimiento.")]
	public bool Perfilador { get; set; } = true;


	[Description("Si es true, cada ronda el gulag y la práctica tienen un modo de armas al azar (Solo escopetas, Solo jailbird, Solo pistolas...).")]
	public bool GulagModosDeArmas { get; set; } = true;


	[Description("Si es true, cada ronda el gulag y la práctica tienen además un modificador (baja gravedad, 50 de vida, a oscuras, velocidad, gigantes, enanos, silencio, sangrado, muerte súbita, cámara lenta, ciego por momentos, armas cambiantes, rebote, la arena se cierra).")]
	public bool GulagModificadores { get; set; } = true;


	[Description("Gulag 2v2: con 4 o más en la cola del gulag, probabilidad (0 a 1) de que la pelea sea por parejas (vuelven los dos ganadores).")]
	public float Gulag2v2Probabilidad { get; set; } = 0.35f;


	[Description("Anti-teamkill: banea automáticamente al que mate a esta cantidad de compañeros en una ronda.")]
	public bool AntiTeamkill { get; set; } = true;


	public int AntiTeamkillBajas { get; set; } = 3;


	[Description("Duración del ban automático por teamkill, en minutos (1440 = 1 día).")]
	public int AntiTeamkillBanMinutos { get; set; } = 1440;


	[Description("Último en pie: con 4 o más en la cola de .practica, probabilidad (0 a 1) de que la pelea sea un todos contra todos (hasta 6).")]
	public float PracticaUltimoEnPieProbabilidad { get; set; } = 0.35f;


	[Description("Estadísticas personales (.stats), ranking de práctica (.top), logros e insignias, y mensaje de bienvenida.")]
	public bool Estadisticas { get; set; } = true;


	public bool Logros { get; set; } = true;


	public bool MensajeBienvenida { get; set; } = true;


	[Description("Webhook de Discord para los logs (resumen de ronda, servidor lleno, baneos del anti-teamkill, SCPs AFK). Vacío = desactivado.")]
	public string DiscordWebhook { get; set; } = "";

	[Description("Claves privadas: van SOLO acá, en el config del server (nunca en el código ni en GitHub). Vacío = esa función queda apagada.")]
	public string WebhookVotacion { get; set; } = "";

	[Description("Webhook de Discord para los logs de VIP.")]
	public string WebhookVip { get; set; } = "";

	[Description("Segundo webhook de Discord de VIP (compras / avisos).")]
	public string WebhookVipCompras { get; set; } = "";

	[Description("Webhook de Discord donde se avisa cuando un VIP usa el comando de CASSIE.")]
	public string WebhookVipCassie { get; set; } = "";

	[Description("Steam Web API key (comando de eventos que lee el perfil de Steam).")]
	public string SteamApiKey { get; set; } = "";

	[Description("API key de Google Gemini (respuestas con IA).")]
	public string GeminiApiKey { get; set; } = "";

	[Description("URL completa del proxy de GPT incluida la api_key, sin el prompt (ej: http://host/gpt/api.php?api_key=XXX&model=gpt-3.5-turbo).")]
	public string GptApiUrl { get; set; } = "";


	public string DiscordNombre { get; set; } = "RadioLight";


	public bool DiscordResumenRonda { get; set; } = true;


	public bool DiscordServidorLleno { get; set; } = true;


	public bool DiscordBaneos { get; set; } = true;


	public bool DiscordScpAfk { get; set; } = true;


	[Description("SCP AFK al empezar: si un SCP no se movió ni giró la cámara en estos segundos, se le ofrece a los espectadores (.tomarscp). 0 = desactivado.")]
	public int AfkScpSegundos { get; set; } = 60;


	[Description("SCP-087-1: aparece (con el modelo de SCP-106) en la sala de Heavy u Oficinas con más gente, corta la luz, traba las puertas 2 s y se lleva hasta 3 personas a la dimensión de bolsillo. Dura 10 s.")]
	public bool Scp0871Activado { get; set; } = true;


	[Description("SCP-087-1: cada cuántos minutos aparece (si mínimo y máximo son distintos, al azar entre los dos).")]
	public float Scp0871MinutosMin { get; set; } = 9f;


	public float Scp0871MinutosMax { get; set; } = 9f;


	[Description("SCP-087-1: mínimo de humanos en la sala para que aparezca.")]
	public int Scp0871MinimoGente { get; set; } = 2;


	[Description("Kill feed estilizado arriba a la derecha (debajo de los efectos) para todos.")]
	public bool KillFeedEstilizado { get; set; } = true;


	[Description("Anuncio de los MVP de la ronda cuando se elige ganador.")]
	public bool AnuncioMvp { get; set; } = true;


	[Description("Si un jugador con título equipado entra al servidor, se le anuncia a todos.")]
	public bool BienvenidaConTitulo { get; set; } = true;


	[Description("Si es true, el staff (acceso al RA) puede usar los comandos .vip sin el límite de una vez por partida.")]
	public bool VipStaffSinLimite { get; set; } = false;


	[Description("Si es true, el staff con acceso al RA no es baneado por el anti-teamkill.")]
	public bool AntiTeamkillIgnorarStaff { get; set; } = true;


	[Description("Al morir, te muestra quién te mató, con qué y cuánta vida le quedó.")]
	public bool PantallaDeMuerte { get; set; } = true;


	[Description("Último aliento: con 20 de vida o menos, un empujón de velocidad (una vez por vida).")]
	public bool UltimoAliento { get; set; } = true;


	[Description("Mensaje corto que ve cada jugador al entrar en el lobby (vacío = ninguno).")]
	public string MensajeLobby { get; set; } = "<size=30><color=#859CC7>Bienvenido a <b>RadioLight</b>.</color> <color=#884481>VIP</color> <color=#AAAAAA>y</color> <color=#c99203>VIP+</color> <color=#AAAAAA>en nuestro Discord.</color></size>";


	[Description("Mensaje corto al terminar la ronda (vacío = ninguno).")]
	public string MensajeFinRonda { get; set; } = "<size=28><color=#859CC7>¡Gracias por jugar en RadioLight!</color> <color=#AAAAAA>Bancá el server con</color> <color=#884481>VIP</color> <color=#AAAAAA>— info en Discord.</color></size>";


	[Description("Posición fija de la aspiradora de Surface ('x, y, z'). Vacío = automático del lado de Gate B. Usá el comando de RA 'aspiradora' parado donde la querés y te da estos valores.")]
	public string AspiradoraPosicion { get; set; } = "";


	[Description("Hacia dónde mira la aspiradora fija (grados).")]
	public float AspiradoraRotacionY { get; set; } = 0f;


	public List<DiseaseConfig> Diseases { get; set; } = new List<DiseaseConfig>
	{
		new DiseaseConfig
		{
			Name = "Schnellenheiz",
			Description = "El paro cardíaco es la detención súbita e inesperada del corazón, interrumpiendo el flujo sanguíneo y provocando la falta de oxígeno en el cuerpo.",
			Effect = EffectType.CardiacArrest
		},
		new DiseaseConfig
		{
			Name = "Hipotermia",
			Description = "La hipotermia es una urgencia médica que ocurre cuando el cuerpo pierde calor más rápido de lo que lo produce, lo que provoca una peligrosa disminución de la temperatura corporal.",
			Effect = EffectType.Hypothermia
		},
		new DiseaseConfig
		{
			Name = "Miopía magna",
			Description = "En la miopía magna existe un alargamiento excesivo del globo ocular que da lugar a una elongación o estiramiento anómalo de todas las estructuras, incluida la retina que puede quedar adelgazada.",
			Effect = EffectType.Blinded
		},
		new DiseaseConfig
		{
			Name = "Diabetes",
			Description = "La diabetes es una enfermedad en la que los niveles de glucosa (azúcar) de la sangre están muy altos.",
			Effect = EffectType.Scp207
		},
		new DiseaseConfig
		{
			Name = "Esquizofrenia",
			Description = "La esquizofrenia es un trastorno mental grave por el cual las personas interpretan la realidad de manera anormal.",
			Effect = EffectType.AmnesiaVision
		},
		new DiseaseConfig
		{
			Name = "Obesidad",
			Description = "La obesidad es una enfermedad compleja que consiste en tener demasiada grasa corporal.",
			Effect = EffectType.Stained
		},
		new DiseaseConfig
		{
			Name = "Sordera",
			Description = "La hipoacusia, sordera o deficiencia auditiva, es un trastorno sensorial que consiste en la incapacidad para escuchar sonidos, y que dificulta el desarrollo del habla, el lenguaje y la comunicación.",
			Effect = EffectType.Deafened
		},
		new DiseaseConfig
		{
			Name = "Ceguera",
			Description = "La ceguera o invidencia,\u200b es una discapacidad sensorial que consiste en la pérdida total o parcial del sentido de la vista.",
			Effect = EffectType.Flashed
		},
		new DiseaseConfig
		{
			Name = "Combustión espontánea humana",
			Description = "La combustión humana espontánea se refiere a la muerte por fuego que se origina sin una fuente externa aparente de ignición; la creencia consiste en que el fuego comienza dentro del cuerpo de la víctima.",
			Effect = EffectType.Burned
		},
		new DiseaseConfig
		{
			Name = "Síndrome de fatiga crónica",
			Description = "El síndrome de fatiga crónica refleja una situación de fatigabilidad persistente e inexplicada a pequeños esfuerzos tanto físicos como mentales que resulta claramente invalidante para el paciente.",
			Effect = EffectType.Exhausted
		},
		new DiseaseConfig
		{
			Name = "Perón",
			Description = "Sin dudas el caso del robo de las manos de Perón es uno de los más grandes misterios que encierra la historia judicial reciente.",
			Effect = EffectType.SeveredHands
		}
	};

}
