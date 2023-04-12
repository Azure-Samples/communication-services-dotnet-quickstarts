namespace CallAutomation.Scenarios
{
    public class EndpointFilter
    {

        public static EndpointFilterDelegate RequestLogger(EndpointFilterFactoryContext handlerContext, EndpointFilterDelegate next)
        {
            var loggerFactory = handlerContext.ApplicationServices.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<EndpointFilter>();
            return (invocationContext) =>
            {
                logger.LogInformation($"[>>] Recieved request for {invocationContext.HttpContext.Request.Path}");
                return next(invocationContext);
            };
        }
    }
}
