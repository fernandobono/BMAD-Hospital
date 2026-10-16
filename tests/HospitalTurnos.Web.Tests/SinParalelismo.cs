// Por qué sin paralelismo (story 2.1): las fábricas de 1.1, 1.2 y 2.1 comparten el mismo PostgreSQL y
// aplican migraciones y siembra al arrancar. En paralelo, dos arranques simultáneos pelean por la misma
// base y los conteos se vuelven inestables. Equivale a una colección única sin tocar las clases de
// prueba de otras stories.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
