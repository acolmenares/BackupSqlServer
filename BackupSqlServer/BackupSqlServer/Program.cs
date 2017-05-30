using System;
using System.Configuration;
using System.Data.SqlClient;
using System.IO.Compression;
using Duplicati.Library.Backend;
using System.Collections.Generic;

namespace BackupSqlServer
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			Console.WriteLine("Hello World!");

            var backend = CrearConexionOneDrive();
			DoIt(backend);
			Console.WriteLine("This is The End!");

		}

        static OneDriveForBusinessBackend CrearConexionOneDrive()
        {
            var servidor = ConfigurationManager.AppSettings["Servidor"]; // "odb4://[]-my.sharepoint.com";
            var rutaEnElServidor = ConfigurationManager.AppSettings["RutaEnElServidor"];// "personal/[]_[]/Documents/duplicati";
            var username = ConfigurationManager.AppSettings["Username"];// "";
            var password = ConfigurationManager.AppSettings["Password"]; // "";
            var url = $"{servidor}/{rutaEnElServidor}";  //?auth-username={username}&auth-password={password}";

            var options = new Dictionary<string, string>();
            options.Add("auth-username", username);
            options.Add("auth-password", password);
            var server = new OneDriveForBusinessBackend(url, options);
            return server;
        }

		static void DoIt(OneDriveForBusinessBackend backend )
		{
			var connectionString = ConfigurationManager.ConnectionStrings["ConnString"].ConnectionString;

            // read backup folder from config file ("C:/temp/")

            var tempBackupfolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"tmp")+ System.IO.Path.DirectorySeparatorChar.ToString(); 
            if(! System.IO.Directory.Exists(tempBackupfolder))
            {
                System.IO.Directory.CreateDirectory(tempBackupfolder);
            }


            var crashPlanFolder = ConfigurationManager.AppSettings["CrashPlanFolder"];
            var crashPlanFileName = ConfigurationManager.AppSettings["CrashPlanFileName"];

            var backupFolder = ConfigurationManager.AppSettings["BackupFolder"];

			var sqlConStrBuilder = new SqlConnectionStringBuilder(connectionString);

			// set backupfilename (you will get something like: "C:/temp/MyDatabase-2013-12-07.bak")
			var tempBackupFileName = String.Format("{0}{1}-{2}.bak",
				tempBackupfolder, sqlConStrBuilder.InitialCatalog,
				DateTime.Now.ToString("yyyyMMdd-HHmmss"));

            Console.WriteLine("Creando backup desde la BD...");
			using (var connection = new SqlConnection(sqlConStrBuilder.ConnectionString))
			{
				var query = String.Format("BACKUP DATABASE {0} TO DISK='{1}'",
					sqlConStrBuilder.InitialCatalog, tempBackupFileName);

				using (var command = new SqlCommand(query, connection))
				{
					connection.Open();
					command.ExecuteNonQuery();
				}
			}

            Console.WriteLine("bak file creado {0}", tempBackupFileName);

            Console.WriteLine("comprimiendo...");
            var tmpZipfile = string.Format("{0}.zip", tempBackupFileName);

            using (ZipArchive zip = ZipFile.Open(tmpZipfile, ZipArchiveMode.Create))
            {
                zip.CreateEntryFromFile(tempBackupFileName, System.IO.Path.GetFileName(tempBackupFileName));
            }

            Console.WriteLine("zip file creado {0}", tmpZipfile);

            Console.WriteLine("borrando bak file");
            System.IO.File.Delete(tempBackupFileName);
            Console.WriteLine("bak file borrado {0}", tempBackupFileName);

            //
            try
            {
                Console.WriteLine("subiendo {0}", tmpZipfile);
                backend.Put(System.IO.Path.GetFileName(tmpZipfile), tmpZipfile);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            //

            var crashPlanFileZip = String.Format("{0}{1}-{2}.sql.zip",
                                             crashPlanFolder, crashPlanFileName,
                                             DateTime.Now.ToString("yyyyMM"));

            try
            {
                Console.WriteLine("copiando  el zip file {0} :  {1} ", tmpZipfile, crashPlanFileZip);
                System.IO.File.Copy(tmpZipfile, crashPlanFileZip, overwrite: true);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            //

            var backupFileName = String.Format("{0}{1}-{2}.bak",
				backupFolder, sqlConStrBuilder.InitialCatalog,
				DateTime.Now.ToString("yyyyMMdd-HHmmss"));

            var zipfile = string.Format("{0}.zip", backupFileName);
                        
            // movi
            Console.WriteLine("moviendo el zip file {0} :  {1} ", tmpZipfile, zipfile);

            System.IO.File.Move(tmpZipfile, zipfile);

            Console.WriteLine("Fin");

           

        }
	}
}


//https://github.com/duplicati/duplicati/issues/2062
/*
 If you open OneDrive in your Browser you have something like this:
https://[xxx]-my.sharepoint.com/personal/[your_name]/_layouts/15/onedrive.aspx

And in Duplicati you use this as the target:
od4b://[xxx]-my.sharepoint.com/personal/[your_name]/Documents/[Duplicati]?auth-username=[username]&auth-password=[pw]

[ ] marks the things you have to change. The last "Duplicati" in the second link is a folder I created in OneDrive.

Office 365 is "Microsoft OneDrive for Business" in Duplicat

https://[]-my.sharepoint.com/personal/[]_[]_[]/_layouts/15/onedrive.aspx
[]-my.sharepoint.com/personal/[]_[]_[]/Documents/duplicati?auth-username=[username]&auth-password=[pw]

 */
