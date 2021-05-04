using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;

namespace TapeDrive
{
    class Program
    {
        public static BackUp Backup { get; set; }
        public static BackupFiles BackupFileList { get; set; }

        /*I was getting a System.OutOfMemoryException, so per the article http://www.codeproject.com/Articles/32912/Handling-of-Large-Byte-Arrays I switched the project from 'Debug' to 'Release' 
         * I also added 'WriteBytes = null;' and 'GC.Collect();' to the loop where I chunk files for writing and reading to/from tape in 'BackUp.cs' 
         * If this remains to be a future problem, I will need to add a mechanism which contols the BackupFile.MaxMemoryChunk property (this is what marshals the memory used for the byte arrays)
         * FUTURE IMPROVEMENTS 
         *      -Multi Thread the application so that while it is writing to the tape it is simultaneously reading the file; this will improve the amount of time to write the backups to tape
         *      -Marshall the memory consumption so that it can use the maximum amount of memory available; in essence dynamically adjust the amount of memory it uses either up or down to suit the 
         *          Server it is operating on.
         */
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                string strArgs = string.Join(" ", args).Remove(0, 1);//joins the initial argument array and removes the first space, else the space is also split into an argument
                string[] objArgs = strArgs.Split('/');
                
                for (int x = 0; x < objArgs.Length; x++)//loop through all the arguments
                {
                    switch (objArgs[x].ToUpper()[0])//examine the first character of the argument
                    {
                        case '?':
                            DisplayUsage();
                            Environment.Exit(0);
                            break;
                        case 'C': //Create a set of demo files that application uses
                            CreateDemoFiles();
                            break;
                        case 'T': //Tape Drive Name
                            if (objArgs[x][1] != ' ')
                                DisplayUsage();
                            else
                                Backup = new BackUp(objArgs[x].Substring(1).Trim());
                            break;
                        case 'I': //Get Tape Info
                            GetTapeInfo();
                            break;
                        case 'D': //Get Tape Info
                            GetTapeInfo();
                            break;
                        case 'O': //The output directory where the restored files default to
                            if (objArgs[x][1] != ' ')
                                DisplayUsage();
                            else
                                BackupFile.DefaultOutPutDirectory = objArgs[x].Substring(1);
                            break;
                        case 'E': //Eject the tape
                            EjectTape();
                            break;
                        case 'F': //Format the tape
                            FormatTape();
                            break;
                        case 'S': //Back up a single file
                            if (objArgs[x][1] != ' ')
                                DisplayUsage();
                            else
                                BackupSingleFile(objArgs[x].Substring(1));
                            break;
                        case 'B': //Backup multiple files
                            if (objArgs[x][1] != ' ')
                                DisplayUsage();
                            else
                                BackupFiles(objArgs[x].Substring(1));
                            break;
                        case 'R': //Restore files function
                            if (objArgs[x][1] != ' ')
                                DisplayUsage();
                            else
                                RestoreFiles(objArgs[x].Substring(1));
                            break;
                    }
                }

            }

        }

        private static void FormatTape()
        {
            try
            {
                Backup.FormatTape();
            }
            catch (Exception objEx)
            {
                using (StreamWriter w = File.AppendText("log.txt"))
                {
                    Log(string.Format("Error {0}", objEx.ToString()), w);
                    // Close the writer and underlying file.
                    w.Close();
                }
            }
            
        }

        private static void EjectTape()
        {
            try
            {
                Backup.EjectTape();
            }
            catch (Exception objEx)
            {
                using (StreamWriter w = File.AppendText("log.txt"))
                {
                    Log(string.Format("Error {0}", objEx.ToString()), w);
                    // Close the writer and underlying file.
                    w.Close();
                }
            }
        }

        private static void BackupSingleFile(string strFileToBackupFilePathAndName)
        {
            SingleFileToBackUp objSingleFileToBackup;
            //BackupFiles objBackupFiles;
            BackupFile objBackupFile;


            try
            {
                using (StreamWriter w = File.CreateText("log.txt"))
                {
                    Log(string.Format("Backup Operation Started at {0}", DateTime.Now), w);
                    // Close the writer and underlying file.
                    w.Close();
                }

                BackupFileList = new BackupFiles();
                objSingleFileToBackup = SingleFileToBackUp.DeSerialize(strFileToBackupFilePathAndName);
                objBackupFile = new BackupFile { FileName = objSingleFileToBackup.FileName, FilePath = objSingleFileToBackup.FilePath, StartTapeLocation = objSingleFileToBackup.StartTapeLocation };

                Backup.WriteFileToTape(objBackupFile);
                BackupFileList.Add(objBackupFile);
                BackupFileList.SaveToXMLFile(Directory.GetCurrentDirectory() + "\\", "BackedUpFile.xml");

                using (StreamWriter w = File.AppendText("log.txt"))
                {
                    Log(string.Format("Backup Operation Ended at {0}", DateTime.Now), w);
                    // Close the writer and underlying file.
                    w.Close();
                }
            }
            catch (Exception objEx)
            {
                using (StreamWriter w = File.AppendText("log.txt"))
                {
                    Log(string.Format("Error {0}", objEx.ToString()), w);
                    // Close the writer and underlying file.
                    w.Close();
                }
            }
        }

        private static void BackupFiles(string strFilesToBackupFilePathAndName)
        {
            FilesToBackup objFilesToBackup;

            try
            {
                using (StreamWriter w = File.CreateText("log.txt"))
                {
                    Log(string.Format("Backup Operation Started at {0}", DateTime.Now), w);
                    // Close the writer and underlying file.
                    w.Close();
                }

                objFilesToBackup = new FilesToBackup(Path.GetDirectoryName(strFilesToBackupFilePathAndName) + "\\", Path.GetFileName(strFilesToBackupFilePathAndName));
                BackupFileList = objFilesToBackup.ToBackupFiles();

                Backup.WriteFilesToTape(BackupFileList);
                BackupFileList.SaveToXMLFile(Directory.GetCurrentDirectory() + "\\", "BackedUpFiles.xml");

                using (StreamWriter w = File.AppendText("log.txt"))
                {
                    Log(string.Format("Backup Operation Ended at {0}", DateTime.Now), w);
                    // Close the writer and underlying file.
                    w.Close();
                }
            }
            catch (Exception objEx)
            {
                using (StreamWriter w = File.AppendText("log.txt"))
                {
                    Log(string.Format("Error {0}", objEx.ToString()), w);
                    // Close the writer and underlying file.
                    w.Close();
                }
            }
        }

        private static void RestoreFiles(string strFilesToRestoreFilePathAndName)
        {
            BackupFileList = new BackupFiles();
            try
            {
                using (StreamWriter w = File.CreateText("log.txt"))
                {
                    Log(string.Format("Restore Operation Started at {0}", DateTime.Now), w);
                    // Close the writer and underlying file.
                    w.Close();
                }

                BackupFileList = new BackupFiles(Path.GetDirectoryName(strFilesToRestoreFilePathAndName) + "\\", Path.GetFileName(strFilesToRestoreFilePathAndName));

                //Checks to make sure you have set a default ouput directory for the backupfiles to use.
                if (string.IsNullOrEmpty(BackupFile.DefaultOutPutDirectory) || !Directory.Exists(BackupFile.DefaultOutPutDirectory))
                    throw new DirectoryNotFoundException("Your Output Directory is invalid; use \"C:\\Temp\\\"");

                Backup.ReadFilesFromTape(BackupFileList);
                //BackupFileList.SaveToXMLFile(Directory.GetCurrentDirectory() + "\\", "BackedUpFiles.xml");

                using (StreamWriter w = File.AppendText("log.txt"))
                {
                    Log(string.Format("Restore Operation Ended at {0}", DateTime.Now), w);
                    // Close the writer and underlying file.
                    w.Close();
                }
            }
            catch (Exception objEx)
            {
                using (StreamWriter w = File.AppendText("log.txt"))
                {
                    Log(string.Format("Error {0}", objEx.ToString()), w);
                    // Close the writer and underlying file.
                    w.Close();
                }
            }
        }

        private static void CreateDemoFiles()
        {
            try
            {
                BackUp.CreateDemoRequiredFiles();
            }
            catch (Exception objEx)
            {
                using (StreamWriter w = File.AppendText("log.txt"))
                {
                    Log(string.Format("Error {0}", objEx.ToString()), w);
                    // Close the writer and underlying file.
                    w.Close();
                }
            }
        }

        private static void GetTapeDriveInfo()
        {
            Backup.WriteTapeDriveInfo();
        }

        private static void GetTapeInfo()
        {
            Backup.WriteTapeMediaInfo();
        }

        private static void DisplayUsage()
        {
            Console.WriteLine(BackUp.DisplayUsage());
        }

        /// <summary>
        /// Usage:
        ///using (StreamWriter w = File.AppendText("log.txt"))
        ///{
        ///   Log(string.Format("Successfully Updated MP2 Table {0} From EQNUM={1} To EQNUM={2}", 
        ///       strTable, strMP2Item, strMP2Item.Replace(objItem.WTFItemNumber, objItem.CustomerPartNumber)), w);
        ///   // Close the writer and underlying file.
        ///   w.Close();
        ///}
        /// </summary>
        /// <param name="logMessage"></param>
        /// <param name="w"></param>
        public static void Log(string logMessage, StreamWriter w)
        {
            w.Write("\r\nLog Entry : ");
            w.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(),
                DateTime.Now.ToLongDateString());
            w.WriteLine("  :");
            w.WriteLine("  :{0}", logMessage);
            w.WriteLine("-------------------------------");
            // Update the underlying file.
            w.Flush();
        }

        //static void Main(string[] args)
        //{
        //    BackupFiles objBackupFiles;
        //    BackUp objBackup;
        //    BackUpFile objBackupFile;


        //    //objBackup = new BackUp(@"\\.\Tape0");

        //    FilesToBackup objFilesToBackup;
        //    FileToBackUp objFileToBackup;

        //    objFilesToBackup = new FilesToBackup();
        //    objFileToBackup = new FileToBackUp { FileName = "test1.txt", FilePath = @"\\wiretechfab.com\CompanyFolders\IT\Backups\SQL\" };
        //    objFilesToBackup.Add(objFileToBackup);
        //    objFileToBackup = new FileToBackUp { FileName = "test2.txt", FilePath = @"\\wiretechfab.com\CompanyFolders\IT\Backups\SQL\" };
        //    objFilesToBackup.Add(objFileToBackup);
        //    objFilesToBackup.SaveToXMLFile(@"C:\Temp\", "FilesToBackup.xml");


        //    //objBackup.WriteTapeDriveInfo();
        //    //objBackup.SetTapeDriveInfo(new TapeOperator.DriveInfoSet { Compression = 1, DataPadding = 0, ECC = 1, EOTWarningZoneSize = 0, ReportSetMarks = 0 });
        //    //objBackup.WriteTapeDriveInfo();

        //    //Procedure for writing file to tape; Backup files must have a StartTapeLocation, FilePath and FileName
        //    //objBackupFiles = new BackupFiles();
        //    //objBackupFile = new BackUpFile { StartTapeLocation = objBackupFiles.NextAvailableTapeLocation, FilePath = @"\\wiretechfab.com\CompanyFolders\IT\Backups\SQL\", FileName = "Server2008.zip" };
        //    //objBackup.WriteFileToTape(objBackupFile);
        //    //objBackupFiles.Add(objBackupFile);
        //    //objBackupFile = new BackUpFile { StartTapeLocation = objBackupFiles.NextAvailableTapeLocation, FilePath = @"\\wiretechfab.com\CompanyFolders\IT\Backups\SQL\", FileName = "ERPSQLSVR.zip" };
        //    //objBackup.WriteFileToTape(objBackupFile);
        //    //objBackupFiles.Add(objBackupFile);
        //    //objBackupFile = new BackUpFile { StartTapeLocation = objBackupFiles.NextAvailableTapeLocation, FilePath = @"C:\Temp\", FileName = "en_visual_studio_2010_ultimate_x86_dvd_509116.iso" };
        //    //objBackup.WriteFileToTape(objBackupFile);
        //    //objBackupFiles.Add(objBackupFile);
        //    //objBackupFile = new BackUpFile { StartTapeLocation = objBackupFiles.NextAvailableTapeLocation, FilePath = @"C:\Temp\", FileName = "VIP-102B_Reference_Manual_v2.4.0.0[1].pdf" };
        //    //objBackup.WriteFileToTape(objBackupFile);
        //    //objBackupFiles.Add(objBackupFile);
        //    //objBackupFile = new BackUpFile { StartTapeLocation = objBackupFiles.NextAvailableTapeLocation, FilePath = @"C:\Temp\", FileName = "SDelete.zip" };
        //    //objBackup.WriteFileToTape(objBackupFile);
        //    //objBackupFiles.Add(objBackupFile);

        //    //Procedure for saving the collection of backed up files to XML for later retrieval
        //    //objBackupFiles.SaveToXMLFile(@"C:\Temp\", "output.xml");

        //    ////Procedure to read in a collection of backupfiles from xml
        //    //BackupFiles objBackupFiles = new BackupFiles(@"C:\Temp\", "output.xml");

        //    //Procedure for reading file from tape; Backup files must have a Size, NewFileName, FilePath and StartTapeLocation
        //    //objBackupFiles[0].NewFileName = "Server2008FromTape.zip";
        //    //objBackupFiles[0].NewFileName = "en_visual_studio_2010_ultimate_x86_dvd_509116FromTape.iso";
        //    //objBackupFiles[1].NewFileName = "VIP-102B_Reference_Manual_v2.4.0.0[1]FromTape.pdf";
        //    //objBackupFiles[2].NewFileName = "SDeleteFromTape.zip";
        //    //objBackup.ReadFileFromTape(objBackupFiles[0]);
        //    //objBackup.ReadFileFromTape(objBackupFiles[1]);
        //    //objBackup.ReadFileFromTape(objBackupFiles[2]);
        //}

    }
}
