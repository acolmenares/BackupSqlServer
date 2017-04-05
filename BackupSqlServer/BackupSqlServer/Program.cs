using System;
using System.Configuration;
using System.Data.SqlClient;
using System.IO.Compression;

namespace BackupSqlServer
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			Console.WriteLine("Hello World!");
			DoIt();
			Console.WriteLine("This is The End!");

		}

		static void DoIt()
		{
			var connectionString = ConfigurationManager.ConnectionStrings["ConnString"].ConnectionString;

			// read backup folder from config file ("C:/temp/")
			var tempBackupfolder = System.IO.Directory.GetCurrentDirectory();
			var backupFolder = ConfigurationManager.AppSettings["BackupFolder"];

			var sqlConStrBuilder = new SqlConnectionStringBuilder(connectionString);

			// set backupfilename (you will get something like: "C:/temp/MyDatabase-2013-12-07.bak")
			var tempBackupFileName = String.Format("{0}{1}-{2}.bak",
				tempBackupfolder, sqlConStrBuilder.InitialCatalog,
				DateTime.Now.ToString("yyyyMMdd-HHmmss"));

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

			var backupFileName = String.Format("{0}{1}-{2}.bak",
				backupFolder, sqlConStrBuilder.InitialCatalog,
				DateTime.Now.ToString("yyyyMMdd-HHmmss"));
            using(ZipArchive zip = ZipFile.Open(string.Format("{0}.zip", backupFileName), ZipArchiveMode.Create))
            {
                zip.CreateEntryFromFile(tempBackupFileName, System.IO.Path.GetFileName(backupFileName));
            }
			System.IO.File.Delete(tempBackupFileName);
		}
	}
}
