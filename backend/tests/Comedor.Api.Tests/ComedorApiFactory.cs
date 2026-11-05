using Comedor.Api.Tests.Persistencia;
using Comedor.Application.Abstracciones;
using Comedor.Application.Usuarios;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Data.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Comedor.Api.Tests;

/// <summary>API en memoria sobre el contenedor de SQL Server, con una base de datos propia por instancia.</summary>
public sealed class ComedorApiFactory : WebApplicationFactory<Program>
{
    public const string AdminUsuario = "admin";
    public const string AdminContrasena = "Admin-Prueba-123";
    public const string ClaveJwt = "clave-jwt-de-pruebas-con-mas-de-32-caracteres";

    private readonly string _cadena;

    public ComedorApiFactory(SqlServerFixture sql)
    {
        var b = new DbConnectionStringBuilder { ConnectionString = sql.CadenaConexion };
        b.Remove("Database");
        b.Remove("Initial Catalog");
        b["Database"] = "Comedor_" + Guid.NewGuid().ToString("N");
        _cadena = b.ConnectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        // UseSetting es visible para Program.cs antes de Build(); ConfigureAppConfiguration no.
        builder.UseSetting("Jwt:Clave", ClaveJwt);
        builder.UseSetting("Qr:ClaveFirma", "clave-firma-qr-de-pruebas-con-mas-de-32-caracteres");
        builder.UseSetting("ConnectionStrings:Comedor", _cadena);
        builder.UseSetting("Semilla:Admin:Usuario", AdminUsuario);
        builder.UseSetting("Semilla:Admin:Contrasena", AdminContrasena);
        builder.UseSetting("Sincronizacion:Habilitada", "false");

        // Solo por el entorno de desarrollo (runtime 10 con proyectos net8.0): el PipeWriter del TestHost 8 no
        // implementa UnflushedBytes, que exige System.Text.Json 10. Con un writer estándar el problema desaparece
        // y en un runtime 8 el cambio es inocuo.
        builder.ConfigureServices(s => s.AddTransient<IStartupFilter, EscritorEstandarFilter>());
    }

    /// <summary>Crea (o reemplaza) un usuario del sistema con la contraseña dada.</summary>
    public async Task CrearUsuarioAsync(string usuario, string contrasena, string rol, bool activo = true)
    {
        using var scope = Services.CreateScope();
        var hasher = scope.ServiceProvider.GetRequiredService<IHasherContrasena>();
        var repo = scope.ServiceProvider.GetRequiredService<IUsuarioSistemaRepositorio>();
        var u = new UsuarioSistema { Usuario = usuario, Rol = rol, Activo = activo };
        u.HashContrasena = hasher.Hash(u, contrasena);
        await repo.GuardarAsync(u);
    }
}

internal sealed class EscritorEstandarFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> siguiente) => app =>
    {
        app.Use(async (contexto, next) =>
        {
            var cuerpo = new StreamResponseBodyFeature(contexto.Response.Body);
            contexto.Features.Set<IHttpResponseBodyFeature>(cuerpo);
            await next(contexto);
            await cuerpo.CompleteAsync();
        });
        siguiente(app);
    };
}
