using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Comedor.Api.Tareas;
using Comedor.Application.Abstracciones;
using Comedor.Application.Accesos;
using Comedor.Application.Qr;
using Comedor.Application.Sesion;
using Comedor.Application.Sincronizacion;
using Comedor.Infrastructure;
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

        servicios.AddSingleton(new OpcionesQr
        {
            SegundosRotacion = config.GetValue("Qr:SegundosRotacion", 30),
            SegundosValidez = config.GetValue("Qr:SegundosValidez", 60),
        });
        servicios.AddSingleton(new OpcionesSesion { DiasEmpleado = config.GetValue("Sesion:DiasEmpleado", 30) });
        servicios.AddSingleton(new OpcionesJwt
        {
            HorasSistema = config.GetValue("Jwt:HorasSistema", 12),
            DiasPunto = config.GetValue("Jwt:DiasPunto", 30),
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
