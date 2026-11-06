using Comedor.Api.Configuracion;
using Comedor.Application.Abstracciones;
using Comedor.Application.Usuarios;
using Comedor.Infrastructure.Persistencia;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddServiciosComedor(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler(salida => salida.Run(async contexto =>
{
    var error = contexto.Features.Get<IExceptionHandlerFeature>();
    if (error is not null)
    {
        app.Logger.LogError(error.Error, "Error inesperado en la API en {Endpoint}: {Detalle}",
            error.Endpoint?.DisplayName ?? contexto.Request.Path.Value, error.Error.Message);
    }

    contexto.Response.StatusCode = StatusCodes.Status500InternalServerError;
    await contexto.Response.WriteAsJsonAsync(new ProblemDetails
    {
        Status = StatusCodes.Status500InternalServerError,
        Title = "Ocurrió un error inesperado. Intenta de nuevo más tarde.",
    }, options: null, contentType: "application/problem+json");
}));
app.UseCors();
app.UseSoloRedLocal();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var proveedor = scope.ServiceProvider;
    await proveedor.GetRequiredService<ComedorDbContext>().Database.MigrateAsync();
    await SembrarAdministradorAsync(proveedor, app.Configuration, app.Logger);
}

app.Run();

static async Task SembrarAdministradorAsync(IServiceProvider sp, IConfiguration config, ILogger logger)
{
    var usuario = config["Semilla:Admin:Usuario"]?.Trim();
    var contrasena = config["Semilla:Admin:Contrasena"];
    if (string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(contrasena))
    {
        return;
    }

    var repo = sp.GetRequiredService<IUsuarioSistemaRepositorio>();
    if (await repo.ExisteAlgunoAsync())
    {
        return;
    }

    var admin = new UsuarioSistema { Usuario = usuario, Rol = Roles.Administrador, Activo = true };
    admin.HashContrasena = sp.GetRequiredService<IHasherContrasena>().Hash(admin, contrasena);
    await repo.GuardarAsync(admin);
    logger.LogInformation("Se sembró el administrador inicial {Usuario}", usuario);
}

public partial class Program
{
}
