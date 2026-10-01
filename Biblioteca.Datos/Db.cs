using System.Configuration;
using Microsoft.Data.SqlClient;
namespace Biblioteca.Datos;

internal static class Db
{
    public static string Cadena =>
        ConfigurationManager.ConnectionStrings["BibliotecaDB"].ConnectionString;

    public static SqlParameter P(string nombre, object? valor) => new(nombre, valor ?? DBNull.Value);

    public static async Task<List<T>> ListarAsync<T>(string sql, Func<SqlDataReader, T> mapa, params SqlParameter[] ps)
    {
        var lista = new List<T>();
        await using var cn = new SqlConnection(Cadena);
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddRange(ps);
        await cn.OpenAsync();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) lista.Add(mapa(r));
        return lista;
    }

    public static async Task<object?> EscalarAsync(string sql, params SqlParameter[] ps)
    {
        await using var cn = new SqlConnection(Cadena);
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddRange(ps);
        await cn.OpenAsync();
        return await cmd.ExecuteScalarAsync();
    }

    public static async Task<int> EjecutarAsync(string sql, params SqlParameter[] ps)
    {
        await using var cn = new SqlConnection(Cadena);
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddRange(ps);
        await cn.OpenAsync();
        return await cmd.ExecuteNonQueryAsync();
    }
}