using DirectoryStructure;
using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SupermarketOpenAccess.Model;

namespace ReadZipFiles
{
    class Program
    {
        private static StringBuilder errorLog = new StringBuilder();
        private static string TEMP_DIRECTORY = string.Empty;
        private static Folder ROOT;

        static void Main(string[] args)
        {
            SetTempDirectory();

            UnZipFiles();

            InitializeRootDirectory();

            // Console.WriteLine(ROOT.ChildFolders[0].Files[0].Path);

            //string pathToFile = ROOT.ChildFolders[0].Files[0].Path;

            // PrintFileData(pathToFile);

            // PrintFolderFilesInfo(ROOT);

            using (var superMarketDb = new SupermarketModel())
            {
                var measure = superMarketDb.Measures.FirstOrDefault(m => m.ID == 1);

                var b = 5;
            }

            // PrintFileData("..\\..\\Temp-20130722021046\\22-Jul-2013\\Plovdiv-Stolipinovo-Sales-Report-22-Jul-2013.xls");
            var a = 5;
        }

        /// <summary>
        /// Gets the size of the folder.
        /// </summary>
        /// <returns>The size of the folder in bytes.</returns>
        public static long PrintFolderFilesInfo(Folder folder)
        {
            long sum = 0;

            // Get files sizes for current directory.
            foreach (var file in folder.Files)
            {
                PrintFileData(file.Path);
            }

            // Get childs folders size recuresively.
            foreach (var childFolder in folder.ChildFolders)
            {
                PrintFolderFilesInfo(childFolder);
            }

            return sum;
        }

        private static void PrintFileData(string pathToFile)
        {
            var reportPosition = pathToFile.IndexOf("Report");
            var fileData = pathToFile.Substring(reportPosition + 7, pathToFile.Length - reportPosition - 11);
            //DateTime date = DateTime.Parse(fileData);

            string provider = "Microsoft.ACE.OLEDB.12.0";
            string properties = "Excel 12.0;HDR=Yes;IMEX=1";
            string connectionString = String.Format("Provider = {0}; Data Source={1}; Extended Properties = \"{2}\"", provider, pathToFile, properties);
            OleDbConnection excelConnection = new OleDbConnection(connectionString);

            excelConnection.Open();

            // Open 03++ xlsx
            // It provides backward compatibility and reads old excel files.
            using (excelConnection)
            {
                // DataTable dtExcelSchema = excelConnection.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
                var readHeader = new OleDbCommand("select * from [Sales$B1:E]", excelConnection);
                OleDbDataReader headerData = readHeader.ExecuteReader();
                headerData.Read();
                var header = headerData[0];

                var readTable = new OleDbCommand("select * from [Sales$B3:E]", excelConnection);
                OleDbDataReader data = readTable.ExecuteReader();
                Console.WriteLine("----------------------------------");
                Console.WriteLine(fileData);
                Console.WriteLine(header);

                int counter = 0;
                while (data.Read())
                {
                    //if (counter == 1)
                    //{
                    //    // skip headers
                    //    counter++;
                    //    continue;
                    //}

                    //if (data[0].ToString() == "…")
                    //{
                    //    counter++;
                    //    continue;
                    //}

                    //if (data[0].ToString() == "Total sum:")
                    //{
                    //    Console.WriteLine("----------------------------------");
                    //    Console.WriteLine("Total Sum:" + data[3]);
                    //    counter++;
                    //    continue;
                    //}

                    // Skip empty rows
                    if (data[0].ToString() == "…" )
                    {
                        continue;
                    }

                    // Get footer
                    if (data[0].ToString() == "Total sum:" )
                    {
                        Console.WriteLine("Total Sum:" + data[3]);
                        counter++;
                        continue;
                    }

                    Console.WriteLine("Product:" + data["ProductId"]);
                    Console.WriteLine("Quantity:" + data["Quantity"]);
                    Console.WriteLine("Unit Pric:" + data["Unit Price"]);
                    Console.WriteLine("Sum:" + data["Sum"]);


                    //Console.WriteLine("----------------------------------");
                    //Console.WriteLine("ProductId: " + data[0]);
                    //Console.WriteLine("Quantity: " + data[1]);
                    //Console.WriteLine("UnitPrice: " + data[2]);
                    //Console.WriteLine("Sum: " + data[3]);
                }

            }
        }

        /// <summary>
        /// Initializes the root directory.
        /// </summary>
        private static void InitializeRootDirectory()
        {
            ROOT = new Folder(TEMP_DIRECTORY);
            GenerateFolders(ROOT);
        }

        /// <summary>
        /// Unzips files in temp dir.
        /// </summary>
        private static void UnZipFiles()
        {
            using (ZipFile zip = ZipFile.Read("..\\..\\Sample-Sales-Reports.zip"))
            {
                foreach (ZipEntry e in zip)
                {
                    e.Extract(TEMP_DIRECTORY, ExtractExistingFileAction.OverwriteSilently);  // overwrite == true  
                }
            }
        }

        /// <summary>
        /// Sets the temp dir name.
        /// </summary>
        private static void SetTempDirectory()
        {
            DateTime currentTime = DateTime.Now;
            TEMP_DIRECTORY = "..\\..\\Temp-" + String.Format("{0:yyyyMMddhhmmss}", currentTime);
        }

        /// <summary>
        /// Generates the folders.
        /// </summary>
        /// <param name="folder">The folder.</param>
        public static void GenerateFolders(Folder folder)
        {
            try
            {
                var dirs = Directory.GetDirectories(folder.Path);
                var fileNames = Directory.GetFiles(folder.Path);
                var files = GenerateFiles(fileNames);
                folder.Files.AddRange(files);

                // Walk recuresively through all sub folders.
                foreach (var dir in dirs)
                {
                    Folder currentFolder = new Folder(dir);
                    folder.ChildFolders.Add(currentFolder);
                    GenerateFolders(currentFolder);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                errorLog.AppendLine(ex.Message);
                return;
            }
        }

        /// <summary>
        /// Generates file object by given array of fileNames.
        /// </summary>
        /// <param name="filesNames">The files names.</param>
        /// <returns>List of file oblects.</returns>
        private static List<ReportFile> GenerateFiles(string[] filesNames)
        {
            List<ReportFile> files = new List<ReportFile>();

            for (int i = 0; i < filesNames.Length; i++)
            {
                files.Add(new ReportFile(filesNames[i]));
            }

            return files;
        }
    }
}
