namespace HieuNga.Web.Middleware;

public class SeoMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/admin"))
        {
            context.Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
        }

        await next(context);
    }
}
