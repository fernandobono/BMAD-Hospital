using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace HospitalTurnos.Infraestructura.Persistencia;

/// <summary>
/// Tablas, columnas, claves e índices en snake_case (contrato técnico). Se escribe a mano en vez de
/// usar EFCore.NamingConventions porque el contrato lo pide en OnModelCreating y es material de clase.
/// Con esto la story 1.3 declara DbSet&lt;RegistroAuditoria&gt; RegistrosAuditoria y obtiene la tabla
/// registros_auditoria sin configurar el nombre a mano.
/// </summary>
public static class ConvencionSnakeCase
{
    /// <summary>
    /// Inserta "_" antes de una mayúscula precedida por minúscula o dígito ("UsuarioId" → usuario_id),
    /// y antes de una mayúscula seguida de minúscula y precedida por mayúscula ("HTTPServidor" →
    /// http_servidor). Después pasa todo a minúsculas.
    /// </summary>
    public static string ASnakeCase(string nombre)
    {
        var resultado = new StringBuilder(nombre.Length + 8);
        for (var i = 0; i < nombre.Length; i++)
        {
            var actual = nombre[i];
            if (i > 0 && char.IsUpper(actual))
            {
                var anterior = nombre[i - 1];
                var siguienteEsMinuscula = i + 1 < nombre.Length && char.IsLower(nombre[i + 1]);
                if (char.IsLower(anterior) || char.IsDigit(anterior) || (char.IsUpper(anterior) && siguienteEsMinuscula))
                    resultado.Append('_');
            }
            resultado.Append(actual);
        }
        return resultado.ToString().ToLowerInvariant();
    }

    /// <summary>Aplica la convención a toda entidad del modelo. Con 0 entidades no hace nada.</summary>
    public static void Aplicar(ModelBuilder modelBuilder)
    {
        foreach (var entidad in modelBuilder.Model.GetEntityTypes())
        {
            // El nombre de tabla por defecto ya es el del DbSet (RegistrosAuditoria), no el de la clase.
            var tabla = entidad.GetTableName();
            if (tabla is null)
                continue;
            entidad.SetTableName(ASnakeCase(tabla));

            var tablaReal = StoreObjectIdentifier.Table(entidad.GetTableName()!, entidad.GetSchema());
            foreach (var propiedad in entidad.GetProperties())
            {
                var columna = propiedad.GetColumnName(tablaReal);
                if (columna is not null)
                    propiedad.SetColumnName(ASnakeCase(columna), tablaReal);
            }

            foreach (var clave in entidad.GetKeys())
            {
                var nombre = clave.GetName();
                if (nombre is not null)
                    clave.SetName(ASnakeCase(nombre));
            }

            foreach (var foranea in entidad.GetForeignKeys())
            {
                var nombre = foranea.GetConstraintName();
                if (nombre is not null)
                    foranea.SetConstraintName(ASnakeCase(nombre));
            }

            foreach (var indice in entidad.GetIndexes())
            {
                var nombre = indice.GetDatabaseName();
                if (nombre is not null)
                    indice.SetDatabaseName(ASnakeCase(nombre));
            }
        }
    }
}
