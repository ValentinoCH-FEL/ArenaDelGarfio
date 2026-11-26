#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

// Voz en Windows. Escucha de forma continua y entrega cada frase al hacer una pausa.
// No usa DictationRecognizer de Unity (su documentación solo lo garantiza en Windows 10):
// un proceso de PowerShell usa el reconocedor de Windows y, si no se puede, el clásico en español.
// Para el reconocedor de Windows: Configuración > Privacidad y seguridad > Voz > Reconocimiento de voz en línea.
public class VozWindows : IServicioVoz
{
    public const string AvisoPermiso = "Activa en Windows el \"Reconocimiento de voz en línea\" y vuelve a pulsar Hablar";

    public event Action<string> AlReconocer;
#pragma warning disable 67 // Estos motores no entregan texto parcial
    public event Action<string> AlOir;
#pragma warning restore 67
    public event Action<string> AlFallar;

    Process listener;
    readonly ConcurrentQueue<string> lines = new ConcurrentQueue<string>();

    public bool Disponible => true;
    public bool Escuchando => listener != null && !listener.HasExited;

    public void EmpezarAEscuchar()
    {
        if (Escuchando) return;
        DejarDeEscuchar();
        while (lines.TryDequeue(out _)) { }
        try
        {
            ProcessStartInfo info = PowerShell(ListenScript);
            info.RedirectStandardOutput = true;
            info.StandardOutputEncoding = Encoding.UTF8;
            // El proceso se cierra solo si el juego se cierra
            info.EnvironmentVariables["VOZ_PADRE"] = Process.GetCurrentProcess().Id.ToString();

            var process = new Process { StartInfo = info };
            process.OutputDataReceived += (sender, e) => { if (e.Data != null && sender == listener) lines.Enqueue(e.Data); };
            listener = process;
            process.Start();
            process.BeginOutputReadLine();
            BombaDeVoz.Crear(this);
            Debug.Log("[VOZ] escuchando");
        }
        catch (Exception e)
        {
            Debug.LogWarning(e.Message);
            DejarDeEscuchar();
            AlFallar?.Invoke("No se pudo iniciar el reconocimiento de voz");
        }
    }

    public void DejarDeEscuchar()
    {
        Process old = listener;
        listener = null;
        if (old == null) return;
        try { if (!old.HasExited) old.Kill(); }
        catch (Exception e) { Debug.LogWarning(e.Message); }
        old.Dispose();
    }

    // Los mensajes llegan en otro hilo: aquí se atienden en el hilo del juego
    public void Atender()
    {
        while (lines.TryDequeue(out string line))
        {
            if (!line.Contains("|")) continue; // Ruido de PowerShell
            string[] parts = line.Split(new[] { '|' }, 2);
            Debug.Log("[VOZ] " + line);
            if (parts[0] == "TEXTO") AlReconocer?.Invoke(parts[1]);
            else if (parts[0] == "PERMISO") AlFallar?.Invoke(AvisoPermiso);
            else if (parts[0] == "ERROR") AlFallar?.Invoke("No se pudo escuchar: " + parts[1]);
        }
    }

    public void Hablar(string texto, Sexo voz)
    {
        if (string.IsNullOrWhiteSpace(texto)) return;
        try
        {
            // El texto viaja como variable de entorno: nunca se mezcla con el código del script
            ProcessStartInfo info = PowerShell(SpeakScript);
            info.EnvironmentVariables["VOZ_TEXTO"] = texto.Length > 400 ? texto.Substring(0, 400) : texto;
            info.EnvironmentVariables["VOZ_SEXO"] = voz == Sexo.Mujer ? "Female" : "Male";
            Process.Start(info)?.Dispose();
        }
        catch (Exception e)
        {
            AlFallar?.Invoke("No se pudo reproducir la voz");
            Debug.LogWarning(e.Message);
        }
    }

    static ProcessStartInfo PowerShell(string script) => new ProcessStartInfo
    {
        FileName = "powershell.exe",
        Arguments = "-NoProfile -NonInteractive -EncodedCommand " + Convert.ToBase64String(Encoding.Unicode.GetBytes(script)),
        UseShellExecute = false,
        CreateNoWindow = true,
    };

    // Escribe líneas "TIPO|dato": LISTO (motor en uso), TEXTO, PERMISO o ERROR
    const string ListenScript = @"
$ProgressPreference = 'SilentlyContinue'
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = New-Object System.Text.UTF8Encoding $false
function Emitir($linea) { [Console]::Out.WriteLine($linea); [Console]::Out.Flush() }
function Vivo { [bool](Get-Process -Id ([int]$env:VOZ_PADRE) -ErrorAction SilentlyContinue) }

try {
    Add-Type -AssemblyName System.Runtime.WindowsRuntime
    $null = [Windows.Media.SpeechRecognition.SpeechRecognizer, Windows.Media.SpeechRecognition, ContentType = WindowsRuntime]
    $null = [Windows.Globalization.Language, Windows.Globalization, ContentType = WindowsRuntime]
    $asTask = [System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object {
        $_.Name -eq 'AsTask' -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1'
    } | Select-Object -First 1
    function Esperar($op, [Type]$tipo) {
        $t = $asTask.MakeGenericMethod($tipo).Invoke($null, @($op))
        if (-not $t.Wait(60000)) { throw 'Sin respuesta' }
        $t.Result
    }
    $rec = New-Object Windows.Media.SpeechRecognition.SpeechRecognizer (New-Object Windows.Globalization.Language 'es-ES')
    $c = Esperar ($rec.CompileConstraintsAsync()) ([Windows.Media.SpeechRecognition.SpeechRecognitionCompilationResult])
    if ($c.Status -ne 'Success') { throw ""$($c.Status)"" }
    Emitir 'LISTO|reconocedor de Windows'
    while (Vivo) {
        $r = Esperar ($rec.RecognizeAsync()) ([Windows.Media.SpeechRecognition.SpeechRecognitionResult])
        if ($r.Status -ne 'Success' -and $r.Status -ne 'TimeoutExceeded') { throw ""$($r.Status)"" }
        if ($r.Text) { Emitir ""TEXTO|$($r.Text)"" }
    }
    exit 0
} catch {
    if (""$($_.Exception)"" -match '80045509|privacy') { Emitir 'PERMISO|'; exit 1 }
    Emitir ""AVISO|reconocedor de Windows no disponible: $($_.Exception.Message -replace '\s+', ' ')""
}

try {
    Add-Type -AssemblyName System.Speech
    $motor = New-Object System.Speech.Recognition.SpeechRecognitionEngine (New-Object System.Globalization.CultureInfo 'es-ES')
    $motor.LoadGrammar((New-Object System.Speech.Recognition.DictationGrammar))
    $motor.SetInputToDefaultAudioDevice()
    Emitir 'LISTO|reconocedor clasico'
    while (Vivo) {
        $r = $motor.Recognize([TimeSpan]::FromSeconds(8))
        if ($r -and $r.Confidence -ge 0.2) { Emitir ""TEXTO|$($r.Text)"" }
    }
} catch {
    Emitir ""ERROR|$($_.Exception.Message -replace '\s+', ' ')""
}
";

    // 1) Voz en español del sexo pedido entre las voces modernas de Windows (incluye las masculinas).
    // 2) Si no hay, las voces clásicas; si ninguna es de ese sexo, se cambia el tono.
    const string SpeakScript = @"
$ProgressPreference = 'SilentlyContinue'
$ErrorActionPreference = 'Stop'
$texto = $env:VOZ_TEXTO
$sexo = $env:VOZ_SEXO

try {
    Add-Type -AssemblyName System.Runtime.WindowsRuntime
    $null = [Windows.Media.SpeechSynthesis.SpeechSynthesizer, Windows.Media.SpeechSynthesis, ContentType = WindowsRuntime]
    $voz = [Windows.Media.SpeechSynthesis.SpeechSynthesizer]::AllVoices |
        Where-Object { $_.Language -like 'es*' -and ""$($_.Gender)"" -eq $sexo } | Select-Object -First 1
    if ($voz) {
        $motor = New-Object Windows.Media.SpeechSynthesis.SpeechSynthesizer
        $motor.Voice = $voz
        $asTask = [System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object {
            $_.Name -eq 'AsTask' -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1'
        } | Select-Object -First 1
        $tarea = $asTask.MakeGenericMethod([Windows.Media.SpeechSynthesis.SpeechSynthesisStream]).Invoke($null, @($motor.SynthesizeTextToStreamAsync($texto)))
        $null = $tarea.Wait(15000)
        $audio = [System.IO.WindowsRuntimeStreamExtensions]::AsStreamForRead($tarea.Result)
        (New-Object System.Media.SoundPlayer $audio).PlaySync()
        exit 0
    }
} catch { }

Add-Type -AssemblyName System.Speech
$s = New-Object System.Speech.Synthesis.SpeechSynthesizer
$voces = @($s.GetInstalledVoices() | Where-Object { $_.Enabled } | ForEach-Object { $_.VoiceInfo })
$es = @($voces | Where-Object { $_.Culture.Name -like 'es*' })
$v = $es | Where-Object { ""$($_.Gender)"" -eq $sexo } | Select-Object -First 1
$tono = 'default'
if (-not $v) {
    $v = @($es + $voces) | Select-Object -First 1
    if ($sexo -eq 'Male') { $tono = '-35%' } else { $tono = '+10%' }
}
$s.SelectVoice($v.Name)
$seguro = [System.Security.SecurityElement]::Escape($texto)
$ssml = ""<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='$($v.Culture.Name)'><prosody pitch='$tono'>$seguro</prosody></speak>""
$s.SpeakSsml($ssml)
";
}

// Pasa los mensajes del proceso de voz al juego, y lo cierra al salir
public class BombaDeVoz : MonoBehaviour
{
    static BombaDeVoz instance;
    VozWindows service;

    public static void Crear(VozWindows service)
    {
        if (instance == null)
        {
            var go = new GameObject("BombaDeVoz");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<BombaDeVoz>();
        }
        instance.service = service;
    }

    void Update() => service?.Atender();
    void OnDestroy() => service?.DejarDeEscuchar();
}
#endif
