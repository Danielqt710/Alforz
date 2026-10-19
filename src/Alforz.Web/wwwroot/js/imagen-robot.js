// Achica la foto en el navegador antes de subirla (lado mayor 1200 px, JPEG),
// así una foto de celular de 5 MB queda en ~200 KB y entra en Firestore.
(function () {
    var LADO_MAXIMO = 1200;
    var CALIDAD = 0.85;
    var MAX_BYTES = 900 * 1024;

    var campo = document.querySelector('input[data-achicar-imagen]');
    if (!campo || !window.DataTransfer || !window.URL) return;

    var vista = document.getElementById('vista-previa');
    var sinFoto = document.getElementById('sin-foto');
    var estado = document.getElementById('estado-foto');
    var quitar = document.querySelector('input[name="QuitarImagen"][type="checkbox"]');
    var formulario = campo.form;
    var procesando = false;

    function mostrar(archivo) {
        if (!vista) return;
        vista.src = URL.createObjectURL(archivo);
        vista.hidden = false;
        if (sinFoto) sinFoto.hidden = true;
        if (quitar) quitar.checked = false;
    }

    function avisar(texto) {
        if (estado) estado.textContent = texto;
    }

    campo.addEventListener('change', function () {
        var archivo = campo.files && campo.files[0];
        if (!archivo || !archivo.type.startsWith('image/')) return;

        procesando = true;
        avisar('Preparando la foto…');
        var img = new Image();
        var url = URL.createObjectURL(archivo);

        img.onload = function () {
            URL.revokeObjectURL(url);
            var escala = Math.min(1, LADO_MAXIMO / Math.max(img.naturalWidth, img.naturalHeight));
            var lienzo = document.createElement('canvas');
            lienzo.width = Math.round(img.naturalWidth * escala);
            lienzo.height = Math.round(img.naturalHeight * escala);
            var ctx = lienzo.getContext('2d');
            ctx.fillStyle = '#ffffff'; // fondo blanco para PNG con transparencia
            ctx.fillRect(0, 0, lienzo.width, lienzo.height);
            ctx.drawImage(img, 0, 0, lienzo.width, lienzo.height);

            lienzo.toBlob(function (blob) {
                procesando = false;
                if (!blob) { mostrar(archivo); avisar('No pude achicarla; se sube tal cual.'); return; }
                var nombre = (archivo.name.replace(/\.[^.]+$/, '') || 'foto') + '.jpg';
                var achicado = new File([blob], nombre, { type: 'image/jpeg' });
                var dt = new DataTransfer();
                dt.items.add(achicado);
                campo.files = dt.files;
                mostrar(achicado);
                avisar(achicado.size > MAX_BYTES
                    ? 'La foto sigue siendo muy pesada. Prueba con otra.'
                    : 'Foto lista (' + Math.round(achicado.size / 1024) + ' KB).');
            }, 'image/jpeg', CALIDAD);
        };

        img.onerror = function () {
            URL.revokeObjectURL(url);
            procesando = false;
            avisar('No pude leer esa foto. Prueba con un JPG o PNG.');
        };

        img.src = url;
    });

    // Si tocan Guardar mientras la foto se está achicando, esperar a que termine.
    if (formulario) {
        formulario.addEventListener('submit', function (e) {
            if (!procesando) return;
            e.preventDefault();
            var espera = setInterval(function () {
                if (procesando) return;
                clearInterval(espera);
                formulario.requestSubmit ? formulario.requestSubmit() : formulario.submit();
            }, 150);
        });
    }
})();
