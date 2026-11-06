using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Comedor.Api.Tareas;
using Comedor.Application.Abstracciones;
using Comedor.Application.Accesos;
using Comedor.Application.Administracion;
using Comedor.Application.Qr;
using Comedor.Application.Reportes;
using Comedor.Application.Sesion;
using Comedor.Application.Sincronizacion;
using Comedor.Application.Usuarios;
using Comedor.Infrastructure;
using Comedor.Infrastructure.Exportacion;
using Comedor.Infrastructure.Externos;
using Comedor.Infrastructure.Persistencia;
using Comedor.Infrastructure.Repositorios;
using Comedor.Infrastructure.Seguridad;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Comedor.Api.Configuracion;

public static class ServiciosComedor
{
    private const string PrefijoCadenas = "ConnectionStrings:";

    /// <summary>Lee un valor obligatorio de configuración; falla al iniciar con un mensaje claro si falta.</summary>
    public static string Requerido(this IConfiguration config, string clave)
    {
        var valor = clave.StartsWith(PrefijoCadenas, StringComparison.Ordinal)
            ? config.GetConnectionString(clave[PrefijoCadenas.Length..])
            : config[clave];
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new InvalidOperationException(
                $"Falta la configuración obligatoria '{clave}'. Defínela con user-secrets o variables de entorno (ver README, \"Configuración local\").");
        }

        return valor;
    }

    /// <summary>
    /// Revocación de la sesión del chef y del administrador (D-34): en cada petición el usuario debe existir,
    /// estar activo y conservar el rol del token. Los tokens de empleado no consultan UsuariosSistema.
    /// </summary>
    private static async Task ValidarSesionDeSistemaAsync(TokenValidatedContext contexto)
    {
        var rol = contexto.Principal?.FindFirst(ClaimTypes.Role)?.Value;
        if (rol != Roles.Chef && rol != Roles.Administrador)
        {
            return;
        }

        var usuario = contexto.Principal!.FindFirst("sub")?.Value;
        var repo = contexto.HttpContext.RequestServices.GetRequiredService<IUsuarioSistemaRepositorio>();
        var u = string.IsNullOrEmpty(usuario) ? null : await repo.ObtenerAsync(usuario);
        if (u is null || !u.Activo || u.Rol != rol)
        {
            contexto.Fail("Sesión revocada");
        }
    }

    public static IServiceCollection AddServiciosComedor(this IServiceCollection servicios, IConfiguration config)
    {
        var claveJwt = config.Requerido("Jwt:Clave");
        var claveQr = config.Requerido("Qr:ClaveFirma");
        var cadena = config.Requerido("ConnectionStrings:Comedor");

        servicios.AddDbContext<ComedorDbContext>(o => o.UseSqlServer(cadena));
        servicios.AddScoped<IEmpleadoRepositorio, EmpleadoRepositorio>();
        servicios.AddScoped<IReglaRepositorio, ReglaRepositorio>();
        servicios.AddScoped<IAccesoRepositorio, AccesoRepositorio>();
        servicios.AddScoped<IPuntoRepositorio, PuntoRepositorio>();
        servicios.AddScoped<IBitacoraSincronizacionRepositorio, BitacoraSincronizacionRepositorio>();
        servicios.AddScoped<IUsuarioSistemaRepositorio, UsuarioSistemaRepositorio>();
        servicios.AddScoped<IConsultaReportes, ConsultaReportes>();
        servicios.AddSingleton<IExportador, Exportador>();

        servicios.AddSingleton(TimeProvider.System);
        var zona = config["Comedor:ZonaHoraria"] ?? "America/Hermosillo";
        servicios.AddSingleton<IReloj>(sp => new RelojZonaHoraria(zona, sp.GetRequiredService<TimeProvider>()));
        servicios.AddSingleton<IFirmaToken>(new FirmaTokenHmac(claveQr));
        servicios.AddSingleton<IEmisorJwt>(sp => new EmisorJwt(claveJwt, sp.GetRequiredService<TimeProvider>()));
        servicios.AddSingleton<IHasherContrasena, HasherContrasena>();

        var usuariosEmpresa = config.GetSection("Autenticacion:Simulada:Usuarios")
            .GetChildren().ToDictionary(c => c.Key, c => c.Value ?? "");
        servicios.AddSingleton<IAutenticadorEmpresa>(new AutenticadorEmpresaSimulado(usuariosEmpresa));

        var ruta = Path.Combine(AppContext.BaseDirectory, config["Fuente:RutaEmpleados"] ?? "datos/empleados-simulados.json");
        servicios.AddSingleton<IFuentePersonalRRHH>(new FuenteRRHHSimulada(ruta));

        servicios.AddSingleton(new OpcionesSesion { DiasEmpleado = config.GetValue("Sesion:DiasEmpleado", 30) });
        servicios.AddSingleton(new OpcionesJwt
        {
            DiasChef = config.GetValue("Jwt:DiasChef", 3650),
            HorasAdministrador = config.GetValue("Jwt:HorasAdministrador", 2),
        });

        var sincronizacion = new OpcionesSincronizacion();
        var hora = config["Sincronizacion:Hora"];
        if (!string.IsNullOrWhiteSpace(hora))
        {
            sincronizacion.Hora = TimeOnly.Parse(hora, CultureInfo.InvariantCulture);
        }

        var turnos = config.GetSection("Sincronizacion:Turnos").Get<string[]>();
        if (turnos is { Length: > 0 })
        {
            sincronizacion.Turnos = turnos;
        }

        servicios.AddSingleton(sincronizacion);

        // El estado de bloqueo debe vivir entre solicitudes: singleton, no scoped.
        servicios.AddMemoryCache();
        servicios.AddSingleton<ControlIntentos>();

        servicios.AddScoped<EmitirTokenQr>();
        servicios.AddScoped<ValidadorTokenQr>();
        servicios.AddScoped<ValidarAcceso>();
        servicios.AddScoped<IniciarSesionEmpleado>();
        servicios.AddScoped<IniciarSesionSistema>();
        servicios.AddScoped<MarcarServido>();
        servicios.AddScoped<ListarSolicitudes>();
        servicios.AddScoped<SincronizarPersonal>();
        servicios.AddScoped<ConsultarEstadoSincronizacion>();
        servicios.AddScoped<GenerarReporte>();
        servicios.AddScoped<AdministrarReglas>();
        servicios.AddScoped<AdministrarPuntos>();
        servicios.AddScoped<AdministrarUsuarios>();
        servicios.AddScoped<RegistrarContingencia>();

        if (config.GetValue("Sincronizacion:Habilitada", true))
        {
            servicios.AddHostedService<SincronizacionNocturna>();
        }

        servicios.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.MapInboundClaims = false;
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(claveJwt)),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = "sub",
                    RoleClaimType = ClaimTypes.Role,
                };
                o.Events = new JwtBearerEvents { OnTokenValidated = ValidarSesionDeSistemaAsync };
            });
        servicios.AddAuthorization();

        var origenes = config.GetSection("Cors:Origenes").Get<string[]>() ?? [];
        servicios.AddCors(o => o.AddDefaultPolicy(p =>
            p.WithOrigins(origenes).AllowAnyHeader().AllowAnyMethod()));

        servicios.AddControllers().AddJsonOptions(o =>
            o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        servicios.AddProblemDetails();
        return servicios;
    }
}
