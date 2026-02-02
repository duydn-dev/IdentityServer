namespace IdentityServerHost.Extentions
{
    public static class CspExtentions
    {
        public static void ConfigureCspAllowHeaders(this WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.Use(async (context, next) =>
                {
                    context.Response.Headers["Content-Security-Policy"] =
                        "default-src 'self'; " +
                        "script-src 'self' 'unsafe-inline'; " +
                        "style-src 'self' 'unsafe-inline'; " +
                        "img-src 'self' data:; " +
                        "connect-src 'self' http://localhost:* ws://localhost:*;";
                    await next();
                });
            }
            else
            {
                app.Use(async (context, next) =>
                {
                    context.Response.Headers["Content-Security-Policy"] =
                        "default-src 'self'; " +
                        "script-src 'self'; " +
                        "style-src 'self'; " +
                        "img-src 'self' data:; " +
                        "font-src 'self'; " +
                        "connect-src 'self'; " +
                        "frame-ancestors 'none'; " +
                        "base-uri 'self'; " +
                        "form-action 'self';";
                    await next();
                });
            }
        }
    }
}
