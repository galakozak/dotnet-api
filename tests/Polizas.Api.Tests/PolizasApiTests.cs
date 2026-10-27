using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Polizas.Api.Demo;
using Polizas.Api.Domain;
using Polizas.Api.Dtos;

namespace Polizas.Api.Tests;

/// <summary>
/// APRENDIZAJE: los tests están escritos como "cliente de la API": hacen requests HTTP y verifican
/// códigos de estado y cuerpos de respuesta. Así se prueba el contrato real, no detalles internos.
/// IClassFixture comparte una sola instancia de la API entre todos los tests de la clase,
/// por eso cada test usa números de póliza únicos para no pisarse con los demás.
/// </summary>
public sealed class PolizasApiTests(ApiFactory fabrica) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _cliente = fabrica.CreateClient();

    // El servidor serializa los enums como texto; el cliente de prueba tiene que saber leerlos.
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static string NumeroUnico() => "T-" + Guid.NewGuid().ToString("N")[..10];

    private static object PolizaValida(string numero, string tomador = "Tomador de Prueba") => new
    {
        numero,
        tomador,
        sumaAsegurada = 1_000_000m,
        prima = 15_000m,
        vigenciaDesde = "2026-01-01",
        vigenciaHasta = "2027-01-01"
    };

    private async Task<PolizaDto> CrearPolizaAsync(string? numero = null, string tomador = "Tomador de Prueba")
    {
        var respuesta = await _cliente.PostAsJsonAsync("/api/polizas", PolizaValida(numero ?? NumeroUnico(), tomador));
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        return (await respuesta.Content.ReadFromJsonAsync<PolizaDto>(Json))!;
    }

    [Fact]
    public async Task Health_responde_ok()
    {
        var respuesta = await _cliente.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact]
    public async Task Crear_devuelve_201_con_Location_y_la_poliza_vigente()
    {
        var numero = NumeroUnico();

        var respuesta = await _cliente.PostAsJsonAsync("/api/polizas", PolizaValida(numero));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var poliza = await respuesta.Content.ReadFromJsonAsync<PolizaDto>(Json);
        Assert.NotNull(poliza);
        Assert.Equal(numero, poliza.Numero);
        Assert.Equal(EstadoPoliza.Vigente, poliza.Estado);
        Assert.Equal($"/api/polizas/{poliza.Id}", respuesta.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Crear_con_datos_invalidos_devuelve_400_con_detalle_de_errores()
    {
        var cuerpoInvalido = new
        {
            numero = "A",            // demasiado corto
            tomador = "",            // requerido
            sumaAsegurada = 1000m,
            prima = 0m,              // debe ser mayor a cero
            vigenciaDesde = "2026-01-01",
            vigenciaHasta = "2027-01-01"
        };

        var respuesta = await _cliente.PostAsJsonAsync("/api/polizas", cuerpoInvalido);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        using var documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        Assert.True(documento.RootElement.TryGetProperty("errors", out _));
    }

    [Fact]
    public async Task Crear_con_vigencia_invertida_devuelve_400()
    {
        var cuerpo = new
        {
            numero = NumeroUnico(),
            tomador = "Tomador de Prueba",
            sumaAsegurada = 1_000_000m,
            prima = 15_000m,
            vigenciaDesde = "2027-01-01",
            vigenciaHasta = "2026-01-01"
        };

        var respuesta = await _cliente.PostAsJsonAsync("/api/polizas", cuerpo);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task Crear_con_numero_duplicado_devuelve_409()
    {
        var numero = NumeroUnico();
        await CrearPolizaAsync(numero);

        var respuesta = await _cliente.PostAsJsonAsync("/api/polizas", PolizaValida(numero));

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
    }

    [Fact]
    public async Task Obtener_inexistente_devuelve_404()
    {
        var respuesta = await _cliente.GetAsync("/api/polizas/999999");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Fact]
    public async Task Obtener_existente_devuelve_200()
    {
        var creada = await CrearPolizaAsync();

        var poliza = await _cliente.GetFromJsonAsync<PolizaDto>($"/api/polizas/{creada.Id}", Json);

        Assert.Equal(creada.Numero, poliza?.Numero);
    }

    [Fact]
    public async Task Anular_una_poliza_dos_veces_devuelve_409_la_segunda()
    {
        var creada = await CrearPolizaAsync();

        var primera = await _cliente.PostAsync($"/api/polizas/{creada.Id}/anular", content: null);
        var segunda = await _cliente.PostAsync($"/api/polizas/{creada.Id}/anular", content: null);

        Assert.Equal(HttpStatusCode.OK, primera.StatusCode);
        var anulada = await primera.Content.ReadFromJsonAsync<PolizaDto>(Json);
        Assert.Equal(EstadoPoliza.Anulada, anulada?.Estado);
        Assert.NotNull(anulada?.AnuladaEl);

        Assert.Equal(HttpStatusCode.Conflict, segunda.StatusCode);
    }

    [Fact]
    public async Task Eliminar_devuelve_204_y_luego_404()
    {
        var creada = await CrearPolizaAsync();

        var borrado = await _cliente.DeleteAsync($"/api/polizas/{creada.Id}");
        var consulta = await _cliente.GetAsync($"/api/polizas/{creada.Id}");

        Assert.Equal(HttpStatusCode.NoContent, borrado.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, consulta.StatusCode);
    }

    [Fact]
    public async Task Listar_filtra_por_estado_y_por_tomador()
    {
        var tomador = "Tomador-" + Guid.NewGuid().ToString("N")[..8];
        var vigente = await CrearPolizaAsync(tomador: tomador);
        var aAnular = await CrearPolizaAsync(tomador: tomador);
        await _cliente.PostAsync($"/api/polizas/{aAnular.Id}/anular", content: null);

        var resultado = await _cliente.GetFromJsonAsync<PagedResult<PolizaDto>>(
            $"/api/polizas?estado=Vigente&tomador={tomador}", Json);

        Assert.NotNull(resultado);
        Assert.Equal(1, resultado.Total);
        Assert.Equal(vigente.Id, resultado.Items.Single().Id);
    }

    [Fact]
    public async Task Listar_pagina_los_resultados()
    {
        var tomador = "Paginado-" + Guid.NewGuid().ToString("N")[..8];
        for (var i = 0; i < 3; i++) await CrearPolizaAsync(tomador: tomador);

        var resultado = await _cliente.GetFromJsonAsync<PagedResult<PolizaDto>>(
            $"/api/polizas?tomador={tomador}&pagina=1&tamanio=2", Json);

        Assert.NotNull(resultado);
        Assert.Equal(3, resultado.Total);
        Assert.Equal(2, resultado.Items.Count);
        Assert.Equal(2, resultado.TotalPaginas);
    }

    [Fact]
    public async Task Cotizacion_devuelve_el_dolar_del_servicio_reemplazado_en_los_tests()
    {
        var dolar = await _cliente.GetFromJsonAsync<CotizacionDolarRespuesta>("/api/cotizacion/dolar", Json);

        Assert.Equal(1050m, dolar?.Venta);
    }

    [Fact]
    public async Task CicloDeVida_singleton_se_repite_scoped_cambia_por_request_y_transient_siempre_cambia()
    {
        var a = await _cliente.GetFromJsonAsync<CicloDeVidaDto>("/api/demo/ciclo-de-vida", Json);
        var b = await _cliente.GetFromJsonAsync<CicloDeVidaDto>("/api/demo/ciclo-de-vida", Json);

        Assert.NotNull(a);
        Assert.NotNull(b);

        Assert.Equal(a.Singleton, b.Singleton);              // una sola instancia para toda la app
        Assert.NotEqual(a.Scoped, b.Scoped);                 // una instancia nueva por request...
        Assert.Equal(a.Scoped, a.ScopedEnOtroServicio);      // ...compartida dentro del mismo request
        Assert.NotEqual(a.Transient1, a.Transient2);         // una instancia nueva cada vez que se pide
    }

    private sealed record CotizacionDolarRespuesta(decimal Compra, decimal Venta);
}
