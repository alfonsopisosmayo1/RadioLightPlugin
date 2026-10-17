using System.Collections.Generic;

namespace RadioLightPlugin.Misc;

public sealed class FichaJugador
{
	public string UserId;

	public string Nick = "";

	public int Bajas;

	public int Muertes;

	public int Escapes;

	public int RondasScp;

	public int ScpsMatados;

	public int Gulags;

	public int Practicas;

	public int UltimoEnPie;

	public int MejorRachaPractica;

	public int PracticasSemana;

	public int Semana;

	public long Primera;

	public long Ultima;

	public int Visitas;

	public HashSet<string> Logros = new HashSet<string>();

	public string Equipado = "";

	public int Mes;

	public int PracticasMes;

	public int BajasMes;

	public List<string> Temporadas = new List<string>();
}
