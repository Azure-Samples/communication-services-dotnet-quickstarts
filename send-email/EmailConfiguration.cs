using System.Collections.Generic;
using Newtonsoft.Json;

namespace EmailSample
{
    class EmailConfiguration
    {
        public EmailConfiguration(string connectionString, string emailTemplatesData)
        {
            this.EmailTemplates = JsonConvert.DeserializeObject<List<EmailTemplate>>(emailTemplatesData);
            this.ConnectionString = connectionString;
        }

        public List<EmailTemplate> EmailTemplates { get; set; }

        /// <summary>
        /// The connectionstring of Azure Communication Service resource.
        /// </summary>
        public string ConnectionString { get; private set; }
    }

    class EmailTemplate
    {
        /// <summary>
        /// Email Template Name.
        /// </summary>
        public string TemplateName { get; set; }

        /// <summary>
        /// The Email Content to send.
        /// </summary>
        public string Subject { get; set; }

        public string PlainText { get; set; }

        public string HTMLText { get; set; }

        /// <summary>
        /// Email Importance High, Low, or Normal.
        /// <summary>
        public string Importance { get; set; }

        /// <summary>
        /// Email Sender.
        /// <summary>
        public string Sender { get; set; }
        /// <summary>
        /// Email Recipients.
        /// <summary>
        public string Recipients { get; set; }
        /// <summary>
        /// Email Attachments.
        /// <summary>
        public string Attachments { get; set; }
    }
}
