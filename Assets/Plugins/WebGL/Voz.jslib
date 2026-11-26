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
  }
});
