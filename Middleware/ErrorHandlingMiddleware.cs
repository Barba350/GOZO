using Microsoft.EntityFrameworkCore;
namespace GOZO;
public class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context) {
        try { await next(context); }
        catch (RuleException e) { await Error(context, 400, e.Message); }
        catch (BadHttpRequestException) { await Error(context, 400, "Solicitud inválida. Revisa los datos."); }
        catch (DbUpdateException e) { logger.LogWarning(e, "Restricción de datos"); await Error(context, 409, "Registro duplicado o referencia inválida. Actualiza e intenta otra vez."); }
        catch (Exception e) { logger.LogError(e, "Error inesperado"); await Error(context, 500, "No se pudo completar la operación. Consulta la consola del servidor."); }
    }
    private static async Task Error(HttpContext context, int status, string message) {
        if (context.Response.HasStarted) throw new InvalidOperationException(message);
        context.Response.StatusCode = status; await context.Response.WriteAsJsonAsync(new { error = message });
    }
}
