# API de Pólizas: proyecto de aprendizaje en .NET 10

API REST mínima pero completa, pensada para **leer, ejecutar, romper y modificar** mientras se hace el curso de .NET.
Cada archivo tiene comentarios `APRENDIZAJE:` que explican el concepto y dicen en qué sección del curso se ve.

El dominio es de seguros (pólizas) a propósito: las reglas de negocio son reconocibles y alcanzan para
mostrar todo lo importante sin distraer.

## Requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (`dotnet --version` debe mostrar 10.x)
- Visual Studio 2022+ / VS Code / Rider

## Cómo empezar

```bash
dotnet run --project src/Polizas.Api     # levanta la API en http://localhost:5080
dotnet test                              # corre los tests de integración
```

Con la API corriendo, abrí `requests.http` (Visual Studio, VS Code con la extensión *REST Client*, o Rider)
y ejecutá los requests uno por uno. Al arrancar en Development se carga data de ejemplo en una base SQLite local (`polizas.db`).

> Si algo no compila o algún test falla al primer intento, avisá: el proyecto se escribió sin poder compilarlo en el entorno de creación.

## Endpoints

| Método | Ruta | Qué muestra |
|---|---|---|
| GET | `/api/polizas` | Filtros (`estado`, `tomador`) y paginación (`pagina`, `tamanio`) |
| GET | `/api/polizas/{id}` | 200 vs 404 |
| POST | `/api/polizas` | Validación (400), duplicados (409), 201 + `Location` |
| PUT | `/api/polizas/{id}` | Actualización con reglas de negocio |
| POST | `/api/polizas/{id}/anular` | Acción de negocio que vive en el dominio |
| DELETE | `/api/polizas/{id}` | 204 y borrado directo con `ExecuteDeleteAsync` |
| GET | `/api/cotizacion/dolar` | `HttpClientFactory` + servicio externo + degradación a 503 |
| GET | `/api/demo/ciclo-de-vida` | Singleton vs Scoped vs Transient |
| GET | `/health` | Health check |
| GET | `/openapi/v1.json` | Documento OpenAPI generado (solo en Development) |

## Estructura

```
src/Polizas.Api/
├── Program.cs                  Registro de servicios, pipeline de middleware y endpoints
├── Domain/                     Entidad Poliza, enum, excepción de negocio
├── Dtos/                       Lo que entra y sale de la API (nunca se expone la entidad)
├── Data/                       DbContext (EF Core + SQLite) y datos de ejemplo
├── Services/                   Lógica de aplicación detrás de interfaces
├── Endpoints/                  Minimal APIs agrupadas por recurso
├── Infrastructure/             Manejo global de errores y middleware propio
├── Configuracion/              Clases tipadas para appsettings.json (patrón Options)
└── Demo/                       Demostración de tiempos de vida de DI
tests/Polizas.Api.Tests/        Tests de integración con WebApplicationFactory
```

## Por dónde seguir

La guía paso a paso, con ejercicios y la relación con las secciones del curso, está en
[`docs/RUTA-DE-APRENDIZAJE.md`](docs/RUTA-DE-APRENDIZAJE.md).
