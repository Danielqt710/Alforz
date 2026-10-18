using System.ComponentModel.DataAnnotations;

namespace Alforz.Web.Models.ViewModels;

public static class ReglasClave
{
    public const int LargoMinimo = 8;

    /// <summary>Al menos una letra y un número.</summary>
    public const string Patron = @"^(?=.*[A-Za-zÁÉÍÓÚÑáéíóúñ])(?=.*\d).+$";

    public const string Mensaje = "La clave debe tener al menos una letra y un número.";
}

public class IngresarViewModel
{
    [Required(ErrorMessage = "Escribe tu usuario o correo.")]
    [Display(Name = "Usuario o correo")]
    public string UsuarioOCorreo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escribe tu clave.")]
    [DataType(DataType.Password)]
    [Display(Name = "Clave")]
    public string Clave { get; set; } = string.Empty;

    [Display(Name = "Recordarme")]
    public bool Recordarme { get; set; }

    public string? ReturnUrl { get; set; }
}

public class RegistrarViewModel
{
    [Required(ErrorMessage = "Elige un nombre de usuario.")]
    [StringLength(30, MinimumLength = 3, ErrorMessage = "Entre 3 y 30 caracteres.")]
    [RegularExpression(@"^[A-Za-z0-9._-]+$", ErrorMessage = "Solo letras, números, punto, guion y guion bajo.")]
    [Display(Name = "Nombre de usuario")]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escribe tu correo.")]
    [EmailAddress(ErrorMessage = "El correo no es válido.")]
    [StringLength(254)]
    [Display(Name = "Correo")]
    public string Correo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escribe una clave.")]
    [StringLength(100, MinimumLength = ReglasClave.LargoMinimo, ErrorMessage = "La clave debe tener al menos 8 caracteres.")]
    [RegularExpression(ReglasClave.Patron, ErrorMessage = ReglasClave.Mensaje)]
    [DataType(DataType.Password)]
    [Display(Name = "Clave")]
    public string Clave { get; set; } = string.Empty;

    [Required(ErrorMessage = "Repite la clave.")]
    [Compare(nameof(Clave), ErrorMessage = "Las claves no coinciden.")]
    [DataType(DataType.Password)]
    [Display(Name = "Repetir clave")]
    public string ConfirmarClave { get; set; } = string.Empty;
}

public class VerificarCorreoViewModel
{
    [Required(ErrorMessage = "Escribe tu correo.")]
    [EmailAddress(ErrorMessage = "El correo no es válido.")]
    [Display(Name = "Correo")]
    public string Correo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escribe el código.")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "El código tiene 6 dígitos.")]
    [Display(Name = "Código")]
    public string Codigo { get; set; } = string.Empty;
}

public class OlvideClaveViewModel
{
    [Required(ErrorMessage = "Escribe tu correo.")]
    [EmailAddress(ErrorMessage = "El correo no es válido.")]
    [Display(Name = "Correo")]
    public string Correo { get; set; } = string.Empty;
}

public class RestablecerClaveViewModel
{
    [Required(ErrorMessage = "Escribe tu correo.")]
    [EmailAddress(ErrorMessage = "El correo no es válido.")]
    [Display(Name = "Correo")]
    public string Correo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escribe el código.")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "El código tiene 6 dígitos.")]
    [Display(Name = "Código")]
    public string Codigo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escribe la clave nueva.")]
    [StringLength(100, MinimumLength = ReglasClave.LargoMinimo, ErrorMessage = "La clave debe tener al menos 8 caracteres.")]
    [RegularExpression(ReglasClave.Patron, ErrorMessage = ReglasClave.Mensaje)]
    [DataType(DataType.Password)]
    [Display(Name = "Clave nueva")]
    public string NuevaClave { get; set; } = string.Empty;

    [Required(ErrorMessage = "Repite la clave.")]
    [Compare(nameof(NuevaClave), ErrorMessage = "Las claves no coinciden.")]
    [DataType(DataType.Password)]
    [Display(Name = "Repetir clave")]
    public string ConfirmarClave { get; set; } = string.Empty;
}
