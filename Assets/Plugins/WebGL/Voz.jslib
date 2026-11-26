mergeInto(LibraryManager.library, {
  VozDisponible: function () {
    return (window.SpeechRecognition || window.webkitSpeechRecognition) ? 1 : 0;
  },

  VozEscuchar: function (receptorPtr) {
    var receptor = UTF8ToString(receptorPtr);
    var Reconocimiento = window.SpeechRecognition || window.webkitSpeechRecognition;
    if (!Reconocimiento) {
      SendMessage(receptor, 'AlError', 'Este navegador no reconoce voz. Usa Chrome o Edge.');
      SendMessage(receptor, 'AlTerminar');
      return;
    }
    if (window.arenaVoz) { window.arenaVoz.onend = null; window.arenaVoz.abort(); }
    window.arenaVozActiva = true;

    var r = new Reconocimiento();
    window.arenaVoz = r;
    r.lang = 'es-PE';
    r.interimResults = true;
    r.continuous = true;
    r.maxAlternatives = 1;
    r.onresult = function (e) {
      var resultado = e.results[e.results.length - 1];
      SendMessage(receptor, resultado.isFinal ? 'AlTexto' : 'AlParcial', resultado[0].transcript);
    };
    r.onerror = function (e) {
      var mensaje = e.error === 'not-allowed' ? 'Permite el uso del micrófono en el navegador'
        : e.error === 'no-speech' ? 'No escuché nada. Intenta otra vez.'
        : 'Error de voz: ' + e.error;
      if (e.error === 'no-speech') return; // Silencio: se sigue escuchando
      window.arenaVozActiva = false;
      SendMessage(receptor, 'AlError', mensaje);
    };
    r.onend = function () {
      // El navegador corta tras un silencio: se reanuda mientras el juego siga escuchando
      if (window.arenaVozActiva && window.arenaVoz === r) r.start();
      else SendMessage(receptor, 'AlTerminar');
    };
    r.start();
  },

  VozParar: function () {
    window.arenaVozActiva = false;
    if (window.arenaVoz) window.arenaVoz.stop();
  },

  VozHablar: function (textoPtr, mujer) {
    if (!window.speechSynthesis) return;
    var frase = new SpeechSynthesisUtterance(UTF8ToString(textoPtr));
    frase.lang = 'es-PE';

    // El navegador no dice el sexo de cada voz: se reconoce por su nombre
    var nombres = mujer
      ? /female|mujer|helena|laura|sabina|paulina|monica|mónica|dalia|elvira|elena|salome|camila|paloma/i
      : /\bmale\b|hombre|pablo|raul|raúl|jorge|diego|alvaro|álvaro|carlos|juan|alex|enrique|tomas|tomás|gonzalo/i;
    var españolas = speechSynthesis.getVoices().filter(function (v) { return v.lang.toLowerCase().indexOf('es') === 0; });
    var elegida = españolas.filter(function (v) { return nombres.test(v.name); })[0];

    if (elegida) {
      frase.voice = elegida;
    } else {
      // Sin una voz de ese sexo: se usa una en español y se cambia el tono
      if (españolas.length) frase.voice = españolas[0];
      frase.pitch = mujer ? 1.3 : 0.6;
    }
    speechSynthesis.cancel();
    speechSynthesis.speak(frase);
  },

  // ---- Detector de género por voz (mismo algoritmo que DetectorGenero.cs) ----
  // Escucha el micrófono, mide el tono (F0) cada 50 ms y guarda los valores en window.arenaTonos.
  TonoIniciar: function () {
    window.arenaTonos = [];
    window.arenaTonoPausa = false;
    if (window.arenaTonoCtx || !navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) return;
    navigator.mediaDevices.getUserMedia({ audio: true }).then(function (stream) {
      var Ctx = window.AudioContext || window.webkitAudioContext;
      var ctx = new Ctx();
      var fuente = ctx.createMediaStreamSource(stream);
      var analizador = ctx.createAnalyser();
      analizador.fftSize = 2048;
      fuente.connect(analizador);
      var datos = new Float32Array(analizador.fftSize);
      var fs = ctx.sampleRate;
      window.arenaTonoCtx = ctx;
      window.arenaTonoStream = stream;
      window.arenaTonoTimer = setInterval(function () {
        if (window.arenaTonoPausa) return;
        analizador.getFloatTimeDomainData(datos);
        var energia = 0;
        for (var i = 0; i < datos.length; i++) energia += datos[i] * datos[i];
        if (Math.sqrt(energia / datos.length) < 0.02) return;           // silencio
        var minLag = Math.floor(fs / 350), maxLag = Math.ceil(fs / 75);
        var n = datos.length - maxLag;
        if (n <= 0) return;
        var parecido = new Float32Array(maxLag + 2), mejor = 0;
        for (var lag = minLag; lag <= maxLag; lag++) {
          var s = 0, e1 = 0, e2 = 0;
          for (var j = 0; j < n; j++) { var a = datos[j], b = datos[j + lag]; s += a * b; e1 += a * a; e2 += b * b; }
          parecido[lag] = s / (Math.sqrt(e1 * e2) + 1e-9);
          if (parecido[lag] > mejor) mejor = parecido[lag];
        }
        if (mejor < 0.55) return;
        for (var l = minLag + 1; l < maxLag; l++) {                     // primer pico casi tan alto como el mejor
          if (parecido[l] >= mejor * 0.9 && parecido[l] >= parecido[l - 1] && parecido[l] >= parecido[l + 1]) {
            window.arenaTonos.push(fs / l);
            break;
          }
        }
      }, 50);
    }).catch(function () { window.arenaTonoError = true; });
  },

  TonoParar: function () {
    if (window.arenaTonoTimer) clearInterval(window.arenaTonoTimer);
    if (window.arenaTonoStream) window.arenaTonoStream.getTracks().forEach(function (t) { t.stop(); });
    if (window.arenaTonoCtx) window.arenaTonoCtx.close();
    window.arenaTonoTimer = window.arenaTonoStream = window.arenaTonoCtx = null;
  },

  // Mientras el juego habla no se mide (no confundir su propia voz con la del jugador)
  TonoPausar: function (pausa) { window.arenaTonoPausa = pausa !== 0; },

  TonoMuestras: function () { return window.arenaTonos ? window.arenaTonos.length : 0; },

  TonoMediana: function () {
    if (!window.arenaTonos || !window.arenaTonos.length) return 0;
    var c = window.arenaTonos.slice().sort(function (a, b) { return a - b; });
    return c[Math.floor(c.length / 2)];
  },

  TonoReiniciar: function () { window.arenaTonos = []; }
});
