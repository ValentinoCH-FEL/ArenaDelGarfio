using System.Collections.Generic;
using UnityEngine;

// Detector de género por la voz: mide el tono (frecuencia fundamental, F0) mientras el jugador habla
// y estima si la voz es de hombre o de mujer.
//   - Voz típica de hombre: 85 a 165 Hz
//   - Voz típica de mujer: 165 a 255 Hz
//   - Entre 145 y 180 Hz las voces se cruzan: ahí el detector dice que no está seguro.
// Usa autocorrelación: busca cada cuántas muestras se repite la onda de la voz.
// Es una estimación (puede fallar con voces muy graves o muy agudas, ruido o susurros).
public class DetectorGenero
{
    public const float LimiteHombre = 145f; // Hz: por debajo, voz de hombre
    public const float LimiteMujer = 180f;  // Hz: por encima, voz de mujer (en medio: no se puede asegurar)
    const float MinF0 = 75f, MaxF0 = 350f; // Rango de voces humanas habladas
    const float MinVolumen = 0.02f;        // Por debajo es silencio o ruido de fondo
    const float MinParecido = 0.55f;       // Qué tan periódica debe ser la onda para contar como voz

    readonly List<float> tonos = new List<float>();
    readonly int frecuenciaMuestreo;

    public DetectorGenero(int frecuenciaMuestreo) { this.frecuenciaMuestreo = frecuenciaMuestreo; }

    public int Muestras => tonos.Count;
    public void Reiniciar() => tonos.Clear();

    // Analiza un trozo de audio (unos 64 ms). Si hay voz, guarda su tono.
    public void Analizar(float[] audio)
    {
        float energia = 0f;
        for (int i = 0; i < audio.Length; i++) energia += audio[i] * audio[i];
        float rms = Mathf.Sqrt(energia / audio.Length);
        if (rms < MinVolumen) return;

        int minLag = Mathf.FloorToInt(frecuenciaMuestreo / MaxF0);
        int maxLag = Mathf.CeilToInt(frecuenciaMuestreo / MinF0);
        int n = audio.Length - maxLag;
        if (n <= 0) return;

        // Autocorrelación normalizada: 1 = la onda se repite exactamente cada "lag" muestras
        var parecido = new float[maxLag + 2];
        float mejor = 0f;
        for (int lag = minLag; lag <= maxLag + 1 && lag + n <= audio.Length; lag++)
        {
            float suma = 0f, e1 = 0f, e2 = 0f;
            for (int i = 0; i < n; i++)
            {
                float a = audio[i], b = audio[i + lag];
                suma += a * b;
                e1 += a * a;
                e2 += b * b;
            }
            parecido[lag] = suma / (Mathf.Sqrt(e1 * e2) + 1e-9f);
            if (lag <= maxLag) mejor = Mathf.Max(mejor, parecido[lag]);
        }
        if (mejor < MinParecido) return;

        // El tono real es el PRIMER pico casi tan alto como el mejor
        // (los múltiplos del período también se parecen y harían creer que la voz es más grave)
        int periodo = 0;
        for (int lag = minLag + 1; lag < maxLag; lag++)
        {
            float r = parecido[lag];
            if (r >= mejor * 0.9f && r >= parecido[lag - 1] && r >= parecido[lag + 1]) { periodo = lag; break; }
        }
        if (periodo == 0) return;

        tonos.Add((float)frecuenciaMuestreo / periodo);
    }

    // Tono típico de la frase: la mediana (no se deja engañar por algunos valores raros)
    public float TonoMedio()
    {
        if (tonos.Count == 0) return 0f;
        var copia = new List<float>(tonos);
        copia.Sort();
        return copia[copia.Count / 2];
    }

    // null si no hay suficiente voz o si el tono está en la zona donde se cruzan las voces de hombre y mujer
    public Sexo? Estimar(int minimoMuestras = 6)
    {
        if (tonos.Count < minimoMuestras) return null;
        float tono = TonoMedio();
        if (tono < LimiteHombre) return Sexo.Hombre;
        if (tono > LimiteMujer) return Sexo.Mujer;
        return null;
    }

    public bool HayVozSuficiente(int minimoMuestras = 6) => tonos.Count >= minimoMuestras;
}
