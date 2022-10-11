// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Calling.RecognizeDTMF
{
    using Microsoft.Owin;
    using Microsoft.Owin.FileSystems;
    using Microsoft.Owin.StaticFiles;
    using Owin;
    using System.IO;
    using System.Reflection;
    using System.Web.Http;

    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Configure Web API for self-host. 
            HttpConfiguration config = new HttpConfiguration();
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{action}",
                defaults: new { id = RouteParameter.Optional }
            );

            var relativePath = string.Format(@"..{0}..{0}..{0}", Path.DirectorySeparatorChar);
            string contentPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), relativePath);
            app.UseStaticFiles(new StaticFileOptions()
            {
                RequestPath = new PathString("/audio"),
                FileSystem = new PhysicalFileSystem(Path.Combine(contentPath, @"audio"))
            });

            app.UseWebApi(config);
        }
    }
}
