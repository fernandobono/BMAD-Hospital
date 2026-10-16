namespace HospitalTurnos.Aplicacion.Tests;

// Un solo contenedor para todas las clases de la colección, y ninguna de ellas en paralelo sobre
// la misma base (contrato técnico, PostgresFixture).
[CollectionDefinition("Postgres")]
public sealed class ColeccionPostgres : ICollectionFixture<PostgresFixture>;
