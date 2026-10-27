namespace Polizas.Api.Dtos;

/// <summary>APRENDIZAJE: tipo genérico reutilizable para cualquier listado paginado.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Pagina, int Tamanio)
{
    public int TotalPaginas => Tamanio == 0 ? 0 : (int)Math.Ceiling(Total / (double)Tamanio);
}
