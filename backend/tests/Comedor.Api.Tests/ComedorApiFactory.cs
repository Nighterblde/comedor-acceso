using Comedor.Api.Tests.Persistencia;
using Comedor.Application.Abstracciones;
using Comedor.Application.Usuarios;
using Comedor.Domain.Empleados;
using Comedor.Domain.Puntos;
using Comedor.Domain.Reglas;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using System.Data.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Comedor.Api.Tests;

/// <summary>API en memoria sobre el contenedor de SQL Server, con una base de datos propia por instancia.</summary>
public sealed class ComedorApiFactory : WebApplicationFactory<Program>
{
    public const string AdminUsuario = "admin";
    public const string AdminContrasena = "Admin-Prueba-123";
    public const string ClaveJwt = "clave-jwt-de-pruebas-con-mas-de-32-caracteres";

    public const string EmpleadoMatricula = "E1001";
    public const string EmpleadoContrasena = "contrasena-empleado-prueba";
    public const string PuntoId = "P1";

    private readonly string _cadena;

    /// <summary>Si se asigna, se usa como <c>Fuente:RutaExcel</c> (carpeta temporal de la prueba).</summary>
    public string? RutaExcel { get; init; }

    /// <summary>Reloj de las pruebas; arranca un martes dentro del horario de comida.</summary>
    public RelojFijo Reloj { get; } = new(new DateTime(2026, 11, 10, 13, 30, 0));

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
        builder.UseSetting("Autenticacion:Simulada:Usuarios:" + EmpleadoMatricula, EmpleadoContrasena);
        if (RutaExcel is not null)
        {
            builder.UseSetting("Fuente:RutaExcel", RutaExcel);
        }

        builder.UseSetting("App:UrlPublica", "http://localhost:4200/");
        builder.ConfigureTestServices(s => s.Replace(ServiceDescriptor.Singleton<IReloj>(Reloj)));

        // Solo por el entorno de desarrollo (runtime 10 con proyectos net8.0): el PipeWriter del TestHost 8 no
        // implementa UnflushedBytes, que exige System.Text.Json 10. Con un writer estándar el problema desaparece
        // y en un runtime 8 el cambio es inocuo.
        builder.ConfigureServices(s => s.AddTransient<IStartupFilter, EscritorEstandarFilter>());
    }

    /// <summary>Empleado Matutino lunes a viernes con contrato vigente, regla de comida 13:00-15:00 y un punto activo.</summary>
    public async Task SembrarAsync()
    {
        using var scope = Services.CreateScope();
        var sp = scope.ServiceProvider;
        await sp.GetRequiredService<IEmpleadoRepositorio>().GuardarAsync([
            new Empleado(EmpleadoMatricula, "Ana Pérez", "Producción", "Matutino",
                new HashSet<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday },
                new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), null, true)]);
        await sp.GetRequiredService<IReglaRepositorio>().GuardarAsync(
            new ReglaDerecho("Matutino", "Comida", new TimeOnly(13, 0), new TimeOnly(15, 0)));
        await sp.GetRequiredService<IPuntoRepositorio>().GuardarAsync(new PuntoEntrada(PuntoId, "Entrada principal", true));
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

/// <summary>Reloj con hora ajustable para las pruebas.</summary>
public sealed class RelojFijo(DateTime inicial) : IReloj
{
    public DateTime Valor { get; set; } = inicial;

    public DateTime Ahora() => Valor;
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
