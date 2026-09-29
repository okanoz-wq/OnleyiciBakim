using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace OnleyiciBakim.Infrastructure;

public sealed class ApiExceptionMiddleware(
    RequestDelegate next,
    ILogger<ApiExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            var (status, title, detail) = exception switch
            {
                ResourceNotFoundException => (StatusCodes.Status404NotFound, "Kaynak bulunamadı", exception.Message),
                ConflictException => (StatusCodes.Status409Conflict, "Çakışma", exception.Message),
                DomainValidationException => (StatusCodes.Status400BadRequest, "Geçersiz istek", exception.Message),
                ExternalServiceException => (StatusCodes.Status503ServiceUnavailable,
                    "Harici servis kullanılamıyor", exception.Message),
                DbUpdateException => (StatusCodes.Status409Conflict, "Veritabanı işlemi tamamlanamadı",
                    "Kayıt başka bir kayıtla çakışıyor veya ilişkisel bütünlük kuralını ihlal ediyor."),
                _ => (StatusCodes.Status500InternalServerError, "Sunucu hatası",
                    "İstek işlenirken beklenmeyen bir hata oluştu.")
            };

            if (status >= 500)
            {
                logger.LogError(exception, "İşlenmeyen API hatası. TraceId: {TraceId}", context.TraceIdentifier);
            }
            else
            {
                logger.LogWarning(exception, "API isteği reddedildi. TraceId: {TraceId}", context.TraceIdentifier);
            }

            var problem = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail,
                Instance = context.Request.Path
            };
            problem.Extensions["traceId"] = context.TraceIdentifier;

            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}
