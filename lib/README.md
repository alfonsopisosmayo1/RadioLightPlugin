# Librerías para compilar

Copiá acá las DLL de tu server. No se suben a GitHub (son del juego y de EXILED).

```
lib/
├── Managed/                 ← la carpeta entera SCPSL_Data/Managed del server
├── Exiled.API.dll           ← .config/SCP Secret Laboratory/LabAPI/dependencies/<puerto>/
├── Newtonsoft.Json.dll      ← .config/SCP Secret Laboratory/LabAPI/dependencies/<puerto>/
├── Exiled.Events.dll        ← .config/EXILED/Plugins/
├── Exiled.CustomRoles.dll   ← .config/EXILED/Plugins/
├── 0Harmony.dll             ← .config/EXILED/Plugins/dependencies/
└── RueI.dll                 ← .config/SCP Secret Laboratory/LabAPI/plugins/<puerto>/
```

Si no querés copiar `Managed/`, podés apuntar a la del server al compilar:

```
dotnet build -c Release -p:SLManaged="D:\Server scp\SCPSL_Data\Managed"
```
