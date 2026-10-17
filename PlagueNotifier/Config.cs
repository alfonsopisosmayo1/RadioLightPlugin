using System.ComponentModel;
using Exiled.API.Interfaces;

namespace PlagueNotifier;

public class Config : IConfig
{
	public bool IsEnabled { get; set; } = true;


	public bool Debug { get; set; } = false;


	[Description("Mensaje de tipo log que veran los miembros de la plaga cuando muera un zombie.")]
	public string HintMessage { get; set; } = "<size=26><color=#4EBA6F><b>[PLAGA LOG]</b></color> <color=#E74C3C>Zombie {zombie}</color> <color=#ECF0F1>ha muerto</color> | <color=#F1C40F>Causa:</color> <color=#3498DB>{cause}</color></size>";


	[Description("Duracion en segundos del aviso tipo log en pantalla.")]
	public float HintDuration { get; set; } = 5f;


	[Description("Mensaje broadcast opcional (dejar vacio si solo se desea hint tipo log).")]
	public string BroadcastMessage { get; set; } = "";


	[Description("Duracion en segundos del broadcast si esta activado.")]
	public ushort BroadcastDuration { get; set; } = 4;


	[Description("Si es true, avisa a SCP-049 de la muerte del zombie.")]
	public bool NotifyDoctor { get; set; } = true;


	[Description("Si es true, avisa al resto de zombies (SCP-049-2) vivos.")]
	public bool NotifyZombies { get; set; } = true;


	[Description("Si es true, avisa tambien a todos los demas SCPs vivos.")]
	public bool NotifyAllScps { get; set; } = false;


	[Description("Si es true, le muestra el aviso al propio zombie que acaba de morir.")]
	public bool NotifyDeadZombie { get; set; } = true;


	[Description("Si es true, registra en consola del servidor cada muerte de zombie.")]
	public bool LogToConsole { get; set; } = true;

}
