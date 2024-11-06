namespace WebApplication1;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate? _next;

    public ErrorHandlingMiddleware(RequestDelegate? next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext? context)
    {
        await _next(context);

        if(context.Response.StatusCode == 401) await context.Response.WriteAsync("You're unauthorized user :(");
        if (context.Response.StatusCode == 403) await context.Response.WriteAsync("Access denied.");
        if (context.Response.StatusCode == 404) await context.Response.WriteAsync("Bad request :(");
    }
}
