using Newtonsoft.Json;
using Serilog;
using System;

using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
namespace VGICancellazioneFile
{
    class Folders
    {
        public string Path { get; set; }
        public int NumDays { get; set; }
        public string DatiVerificare { get; set; } = "Creation Date";
        public bool SubFolders { get; set; } = false;
        public string Estensione { get; set; } 

        [JsonIgnore]
        public int DeletedFiles { get; set; } = 0;
        public delegate int DeleteFiles(DirectoryInfo dir);
        public void DeleteSubFolders(string path, DeleteFiles deleteFiles)
        {
            try
            {
                System.IO.DirectoryInfo di = new DirectoryInfo(path);
                Log.Information(String.Format("Searching subfolders: {0}", path));
                //Remove the files
                int deletedFiles = deleteFiles(di);
                DeletedFiles += deletedFiles;
                Log.Information(String.Format("Removed {0} files from subfolders: {1}", deletedFiles, path));
                Parallel.ForEach(di.EnumerateDirectories(), dir =>
                {
                    DeleteSubFolders(dir.FullName, deleteFiles);
                });
             }

            catch (Exception e)
            {
                Console.WriteLine("Error:" + e.Message);
                Log.Error(e, e.Message);
            }
        }

        public void DeleteDirectory()
        {
            try
            {               
                Log.Information(String.Format("Searching folders {0} for files with extension  {1} and {2} older than {3}", this.Path, this.Estensione, this.DatiVerificare, this.NumDays));
                //check if directory exist
                if (Directory.Exists(this.Path))
                {
                    DirectoryInfo di = new DirectoryInfo(this.Path);
                    //check if we are physically removing files from folders or just testing application              
                    DeleteFiles deleteFiles =null;
                    if (Program.TestMode)
                    {
                        deleteFiles = DeleteFilesTesting;
                    }
                    else
                    {
                        deleteFiles = DeleteFilesPysically;

                    }
                    //removing files from directory
                    DeletedFiles += deleteFiles(di);
                    //check if the app shpuld also search subfolders
                    if (this.SubFolders)
                    {
                        Log.Information("Searching files in subfolder:{0}",this.Path);

                        Parallel.ForEach(di.EnumerateDirectories(), dir =>
                        {
                            DeleteSubFolders(dir.FullName, deleteFiles);
                        });

                    }
                    Log.Information(String.Format("Removed {0} from folder: {1}", DeletedFiles, this.Path));
                }
                else
                {
                    Log.Warning(String.Format("Could not find this directory: {0}", this.Path));
                    Console.WriteLine(String.Format("Could not find this directory: {0}", this.Path));
               
                }

            }
            catch
            {
                throw;
            }
        }


        public int DeleteFilesPysically(DirectoryInfo dir)
        {
            try
            {
                 int deletedFiles = 0;
                ParallelOptions parallelOptions = new ParallelOptions();
                parallelOptions.MaxDegreeOfParallelism = Environment.ProcessorCount * 2;
                //schedule work across multiple threads based on your system environment
                Parallel.ForEach(dir.EnumerateFiles()
                    .Where(f => Delete(this, f)), parallelOptions, file =>
                    {

                        try
                        {
                            Log.Information(String.Format("Removed file: {0}  with creation date:{1} and modificcation date: {2}", file.FullName,file.CreationTime,file.LastWriteTime));
                            //eleminare il file                           
                                    file.Delete();   
                        }
                        catch (Exception e)
                        {
                            //Console.WriteLine("Error:" + e.Message);
                            Log.Error(e, e.Message);
                        }
                        deletedFiles++;
                    }

              );

                //return the number of deleted files
                return deletedFiles;
            }
            catch
            {
                throw;
            }

        }
        public int DeleteFilesTesting(DirectoryInfo dir)
        {
            try
            {
                int deletedFiles = 0;
                ParallelOptions parallelOptions = new ParallelOptions();
                parallelOptions.MaxDegreeOfParallelism = Environment.ProcessorCount * 2;
                //schedule work across multiple threads based on your system environment
                Parallel.ForEach(dir.EnumerateFiles()
                    .Where(f => Delete(this, f)), parallelOptions, file =>
                    {

                        try
                        {
                         
                            Log.Information(String.Format("Eliminare il file: {0}  la DATA DI CREAZIONE:{1} e la DATA DI MODIFICA: {2}", file.FullName, file.CreationTime, file.LastWriteTime));
                         
                        }
                        catch (Exception e)
                        {
                            //Console.WriteLine("Error:" + e.Message);
                            Log.Error(e, e.Message);
                        }
                        deletedFiles++;
                    }

              );

                //return the number of deleted files
                return deletedFiles;
            }
            catch
            {
                throw;
            }

        }

        public Func<Folders, FileInfo, bool> Delete = delegate (Folders fileInstance, FileInfo f) {
            bool shouldDelete = false;
            //check if the calculation should be done on creation date or modification date.
            if (fileInstance.DatiVerificare.Replace(" ", "").ToLower().Equals("modificationdate"))
            {
                shouldDelete = (DateTime.Now - f.LastWriteTime)
                .TotalDays >= fileInstance.NumDays;
            }
            else
            {
                shouldDelete = (DateTime.Now - f.CreationTime)
               .TotalDays >= fileInstance.NumDays;
            }
            //check the file extension
            if (!fileInstance.Estensione.Equals(".*"))
                shouldDelete = shouldDelete && f.Extension.Equals(fileInstance.Estensione);
            return shouldDelete;
        };

    }


}
