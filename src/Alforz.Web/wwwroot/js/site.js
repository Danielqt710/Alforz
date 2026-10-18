// Botón de ojo en cada campo de clave: muestra u oculta lo que se escribe.
document.querySelectorAll('input[type="password"]').forEach(function (campo) {
    var contenedor = campo.closest('.form-floating') || campo.parentElement;
    contenedor.classList.add('alf-clave');

    var boton = document.createElement('button');
    boton.type = 'button';
    boton.className = 'alf-ver-clave';
    boton.setAttribute('aria-label', 'Mostrar clave');
    boton.setAttribute('aria-pressed', 'false');
    boton.innerHTML = '<i class="bi bi-eye" aria-hidden="true"></i>';

    boton.addEventListener('click', function () {
        var mostrar = campo.type === 'password';
        campo.type = mostrar ? 'text' : 'password';
        boton.querySelector('i').className = mostrar ? 'bi bi-eye-slash' : 'bi bi-eye';
        boton.setAttribute('aria-label', mostrar ? 'Ocultar clave' : 'Mostrar clave');
        boton.setAttribute('aria-pressed', String(mostrar));
        campo.focus();
    });

    // Al enviar vuelve a ocultarla, así el navegador la trata como clave (para guardarla, etc.).
    if (campo.form) {
        campo.form.addEventListener('submit', function () { campo.type = 'password'; });
    }

    campo.insertAdjacentElement('afterend', boton);
});
