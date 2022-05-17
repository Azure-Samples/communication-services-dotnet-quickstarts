using System;
using System.Threading.Tasks;
using System.Configuration;
using System.Collections.Generic;
using System.IO;

namespace EmailSample
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Start running Email sample");

            try
            {
                EmailConfiguration emailConfig = InitializeConfiguration();
                var tasks = new List<Task>();

                foreach (EmailTemplate email in emailConfig.EmailTemplates)
                {
                    tasks.Add(Task.Run(async () => await new EmailSender(emailConfig.ConnectionString).SendEmailToRecipients(email)));
                }

                await Task.WhenAll(tasks);
                Console.WriteLine("Email Sample completed successfully");
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Email Sample Failed --> {ex.Message}");
            }
        }


        /// <summary>
        /// Fetch configurations from App Settings
        /// </summary>
        /// <returns>The EmailConfiguration object.</returns>
        private static EmailConfiguration InitializeConfiguration()
        {
            EmailConfiguration emailConfiguration = null;

            try
            {
                var connectionString = ConfigurationManager.AppSettings["Connectionstring"];
                var dataFileContent = ReadDataFile();
                emailConfiguration = new EmailConfiguration(connectionString, dataFileContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initiate configurations --> {ex.Message}");
            }

            return emailConfiguration;
        }

        private static string ReadDataFile()
        {
            string dataFileContent = null;
            try
            {
                using (StreamReader r = new StreamReader("data//data_file.json"))
                {
                    dataFileContent = r.ReadToEnd();
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Failed to read json File --> {ex.Message}");
            }

            return dataFileContent;
        }
    }
}
