using HospitalTurnos.Dominio.Auditoria;

namespace HospitalTurnos.Dominio.Tests.Auditoria;

public class RegistroAuditoriaTests
{
    private static readonly DateTimeOffset CuandoUtc = new(2026, 10, 12, 11, 0, 0, TimeSpan.Zero);

    // Un caso por campo obligatorio y por forma de "vacío"; null en "campo" = el caso válido.
    public static TheoryData<string?, string?> Casos()
    {
        var casos = new TheoryData<string?, string?> { { null, null } };
        foreach (var campo in new[] { "quien", "rol", "accion", "entidadTipo", "entidadId" })
            foreach (var vacio in new[] { null, "", "  " })
                casos.Add(campo, vacio);
        return casos;
    }

    [Theory]
    [MemberData(nameof(Casos))]
    [Trait("Story", "1.3")]
    [Trait("AC", "1")]
    [Trait("AC", "2")]
    public void Constructor_CamposObligatoriosVacios_LanzaArgumentException(string? campo, string? valor)
    {
        string Valor(string nombre, string valido) => campo == nombre ? valor! : valido;

        RegistroAuditoria Construir() => new(CuandoUtc, "u-1",
            Valor("quien", "Ana Pérez"), Valor("rol", "Recepcionista"), CanalAuditoria.Recepcion,
            Valor("accion", AccionesAuditoria.TurnoReservado), Valor("entidadTipo", "Turno"),
            Valor("entidadId", "T-123"), """{"turnoId":"T-123"}""");

        if (campo is null)
        {
            var registro = Construir();
            Assert.Equal(CuandoUtc, registro.Cuando);
            Assert.Equal("u-1", registro.UsuarioId);
            Assert.Equal("Ana Pérez", registro.Quien);
            Assert.Equal("Recepcionista", registro.Rol);
            Assert.Equal(CanalAuditoria.Recepcion, registro.Canal);
            Assert.Equal("turno.reservado", registro.Accion);
            Assert.Equal("Turno", registro.EntidadTipo);
            Assert.Equal("T-123", registro.EntidadId);
            Assert.Equal("""{"turnoId":"T-123"}""", registro.Detalle);
            // Sin setters públicos: nadie corrige un registro desde el código.
            Assert.All(typeof(RegistroAuditoria).GetProperties(),
                p => Assert.False(p.SetMethod?.IsPublic ?? false, $"{p.Name} tiene setter público"));
            return;
        }

        var error = Assert.ThrowsAny<ArgumentException>(Construir);
        Assert.Equal(campo, error.ParamName);
    }

    [Fact]
    [Trait("Story", "1.3")]
    [Trait("AC", "3")]
    public void Constructor_CuandoConOffsetDistintoDeCero_LanzaArgumentException()
    {
        var cuandoCordoba = new DateTimeOffset(2026, 10, 12, 8, 0, 0, TimeSpan.FromHours(-3));

        var error = Assert.Throws<ArgumentException>(() => new RegistroAuditoria(cuandoCordoba, "u-1",
            "Ana Pérez", "Recepcionista", CanalAuditoria.Recepcion, AccionesAuditoria.TurnoReservado,
            "Turno", "T-123", null));

        Assert.Equal("cuando", error.ParamName);
    }
}
