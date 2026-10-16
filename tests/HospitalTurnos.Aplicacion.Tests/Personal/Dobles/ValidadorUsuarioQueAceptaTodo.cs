using HospitalTurnos.Infraestructura.Identidad;
using Microsoft.AspNetCore.Identity;

namespace HospitalTurnos.Aplicacion.Tests.Personal.Dobles;

/// <summary>
/// Identity sin su revisión de nombres repetidos: así el alta llega a la base con un nombre que ya existe,
/// como pasa cuando dos altas iguales llegan a la vez (T4). Lo rechaza el índice único user_name_index.
/// </summary>
public sealed class ValidadorUsuarioQueAceptaTodo : IUserValidator<UsuarioPersonal>
{
    public Task<IdentityResult> ValidateAsync(UserManager<UsuarioPersonal> manager, UsuarioPersonal user) =>
        Task.FromResult(IdentityResult.Success);
}
