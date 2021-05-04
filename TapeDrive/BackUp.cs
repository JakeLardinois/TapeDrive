using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;


namespace TapeDrive
{
    /*Writing a file that was 2389600KB from position 0 on the tape put the tapes position to 37338; that equates to 63KB per position
    *FUTURE IMPROVEMENTS - catch the 'out of memory' exception and decrease the MaxMemoryChunk property
    */
    public class BackUp
    {
        #region Public Properties

        public string TapeName { get; set; }
        #endregion

        #region Private Properties

        private TapeOperator TapeOperator { get; set; }
        private TapeOperator.MediaInfo MediaInfo { get; set; }
        private FileStream FileStreamToWrite { get; set; }
        private byte[] WriteBytes { get; set; }
        private byte[] ReadBytes { get; set; }
        #endregion

        public BackUp()
        {
        }

        public BackUp(string strTapeName)
        {
            TapeName = strTapeName;
        }

        #region Public Methods
        public void WriteFileToTape(BackupFile objBackupFile)
        {
            BackupFiles objBackupFiles = new BackupFiles();
            objBackupFiles.Add(objBackupFile);
            WriteFilesToTape(objBackupFiles, true);
        }
        //public void WriteFileToTape(BackupFile objBackupFile)
        //{
        //    TapeOperator objTapeOperator;
        //    TapeOperator.MediaInfo objMediaInfo;
        //    FileStream objFileStreamToWrite;
        //    byte[] objWriteBytes;


        //    Console.WriteLine("WriteFileToTape Method");
        //    Console.WriteLine("Initializing the tape drive...");
        //    objTapeOperator = new TapeOperator();
        //    objTapeOperator.Load(TapeName);

        //    Console.WriteLine("Setting Tape Position...");
        //    objTapeOperator.SetTapePosition(objBackupFile.StartTapeLocation);
        //    Console.WriteLine("Tape Position is " + objTapeOperator.GetTapePosition());

        //    //for some reason, I kept getting an error until I added the below statements
        //    Console.WriteLine("Getting the Tape Info...");
        //    objMediaInfo = objTapeOperator.GetTapeMediaParameters();
        //    //Console.WriteLine(objMediaInfo.BlockSize);
        //    //Console.WriteLine(objTapeOperator.BlockSize);

        //    Console.WriteLine("Opening the file...");
        //    objFileStreamToWrite = new FileStream(objBackupFile.FileNameAndPath, FileMode.Open, FileAccess.Read);
        //    objBackupFile.Size = objFileStreamToWrite.Length;

        //    //This code is where modification will be required to accomodate consecutive reads and writes to the tape for large file sizes
        //    if (objBackupFile.isLargeFile)
        //    {
        //        Console.WriteLine("This file will take " + objBackupFile.FileChunkCount + " Passes...");
        //        for (int intCounter = 1; intCounter <= objBackupFile.FileChunkCount; intCounter++)
        //        {
        //            if (intCounter < objBackupFile.FileChunkCount)
        //            {
        //                Console.WriteLine("This is pass " + intCounter);
        //                //Console.WriteLine("Initilizing Array");
        //                objWriteBytes = new byte[BackupFile.MaxMemoryChunk];
        //                //Console.WriteLine("Done Initilizing Array");

        //                Console.WriteLine("Reading file to Memory...");
        //                //Console.WriteLine(objFileStreamToWrite.Position);
        //                //lngPosition = objFileStreamToWrite.Position;
        //                objFileStreamToWrite.Read(objWriteBytes, 0, BackupFile.MaxMemoryChunk);

        //                //Console.WriteLine(objTapeOperator.GetTapePosition());
        //                Console.WriteLine("Writing Filestream to Tape");
        //                objTapeOperator.Write(objTapeOperator.GetTapePosition(), objWriteBytes);
        //                //Console.WriteLine("Finished Writing to Tape");
        //                //Console.WriteLine(objTapeOperator.GetTapePosition());
        //            }
        //            else
        //            {
        //                int intLeftOverBytes = (int)(objBackupFile.Size - BackupFile.MaxMemoryChunk * (intCounter - 1));

        //                Console.WriteLine("This is pass " + intCounter);
        //                //Console.WriteLine("Initilizing Array");
        //                objWriteBytes = new byte[intLeftOverBytes];
        //                //Console.WriteLine("Done Initilizing Array");

        //                Console.WriteLine("Reading file to Memory...");
        //                //lngPosition = objFileStreamToWrite.Position;
        //                objFileStreamToWrite.Read(objWriteBytes, 0, intLeftOverBytes);

        //                Console.WriteLine("Writing Filestream to Tape");
        //                objTapeOperator.Write(objTapeOperator.GetTapePosition(), objWriteBytes);
        //                //Console.WriteLine("Finished Writing to Tape");
        //            }

        //        }
        //    }
        //    else
        //    {
        //        //Console.WriteLine("Initilizing Array");
        //        objWriteBytes = new byte[objFileStreamToWrite.Length];//This one generates an IOException if the file size is greater than 2GB
        //        //Console.WriteLine("Done Initilizing Array");

        //        Console.WriteLine("Reading file to Memory...");
        //        objFileStreamToWrite.Read(objWriteBytes, 0, (int)objFileStreamToWrite.Length);


        //        Console.WriteLine("Writing Filestream to Tape");
        //        objTapeOperator.Write(objBackupFile.StartTapeLocation, objWriteBytes);
        //        Console.WriteLine("Finished Writing to Tape");
        //    }

        //    objBackupFile.EndTapeLocation = objTapeOperator.GetTapePosition();
        //    Console.WriteLine("Tape Position is " + objBackupFile.EndTapeLocation);

        //    Console.WriteLine("Now I'm Cleaning up resources...");
        //    objTapeOperator.Close();
        //    objFileStreamToWrite.Close();
        //    Console.WriteLine("Now I'm Done!");
        //    Console.WriteLine();
        //}

        public void WriteFilesToTape(BackupFiles objBackupFiles, bool isSingleFile = false)
        {
            Console.WriteLine("WriteFileToTape Method");
            Console.WriteLine("Initializing the tape drive...");
            TapeOperator = new TapeOperator();
            TapeOperator.Load(TapeName);

            Console.WriteLine("Setting Tape Position...");
            //If it's a single file than I use the tape location specified on the BackupFile object, else I parse the collection for the next available location
            TapeOperator.SetTapePosition(isSingleFile ? objBackupFiles[0].StartTapeLocation : objBackupFiles.NextAvailableTapeLocation);
            Console.WriteLine("Tape Position is " + TapeOperator.GetTapePosition());

            //for some reason, I kept getting an error until I added the below statements
            Console.WriteLine("Getting the Tape Info...");
            MediaInfo = TapeOperator.GetTapeMediaParameters();
            //Console.WriteLine(objMediaInfo.BlockSize);
            //Console.WriteLine(objTapeOperator.BlockSize);

            foreach (BackupFile objBackupFile in objBackupFiles)
            {
                Console.WriteLine("Opening the file...");
                FileStreamToWrite = new FileStream(objBackupFile.FileNameAndPath, FileMode.Open, FileAccess.Read);
                objBackupFile.Size = FileStreamToWrite.Length;
                objBackupFile.StartTapeLocation = TapeOperator.GetTapePosition();

                //This code is where modification will be required to accomodate consecutive reads and writes to the tape for large file sizes
                if (objBackupFile.isLargeFile)
                {
                    Console.WriteLine("This file will take " + objBackupFile.FileChunkCount + " Passes...");
                    for (int intCounter = 1; intCounter <= objBackupFile.FileChunkCount; intCounter++)
                    {
                        if (intCounter < objBackupFile.FileChunkCount)
                        {
                            WriteBytes = null;//clears the memory...
                            GC.Collect();

                            Console.WriteLine("This is pass " + intCounter);
                            //Console.WriteLine("Initilizing Array");
                            WriteBytes = new byte[BackupFile.MaxMemoryChunk];
                            //Console.WriteLine("Done Initilizing Array");

                            Console.WriteLine("Reading file to Memory...");
                            //Console.WriteLine(objFileStreamToWrite.Position);
                            //lngPosition = objFileStreamToWrite.Position;
                            FileStreamToWrite.Read(WriteBytes, 0, BackupFile.MaxMemoryChunk);

                            //Console.WriteLine(objTapeOperator.GetTapePosition());
                            Console.WriteLine("Writing Filestream to Tape");
                            TapeOperator.Write(TapeOperator.GetTapePosition(), WriteBytes);
                            //Console.WriteLine("Finished Writing to Tape");
                            //Console.WriteLine(objTapeOperator.GetTapePosition());
                        }
                        else
                        {
                            int intLeftOverBytes = (int)(objBackupFile.Size - BackupFile.MaxMemoryChunk * (intCounter - 1));

                            WriteBytes = null;//clears the memory...
                            GC.Collect();

                            Console.WriteLine("This is pass " + intCounter);
                            //Console.WriteLine("Initilizing Array");
                            WriteBytes = new byte[intLeftOverBytes];
                            //Console.WriteLine("Done Initilizing Array");

                            Console.WriteLine("Reading file to Memory...");
                            //lngPosition = objFileStreamToWrite.Position;
                            FileStreamToWrite.Read(WriteBytes, 0, intLeftOverBytes);

                            Console.WriteLine("Writing Filestream to Tape");
                            TapeOperator.Write(TapeOperator.GetTapePosition(), WriteBytes);
                            //Console.WriteLine("Finished Writing to Tape");
                        }

                    }
                }
                else
                {
                    WriteBytes = null;//clears the memory...
                    GC.Collect();

                    //Console.WriteLine("Initilizing Array");
                    WriteBytes = new byte[FileStreamToWrite.Length];//This one generates an IOException if the file size is greater than 2GB
                    //Console.WriteLine("Done Initilizing Array");

                    Console.WriteLine("Reading file to Memory...");
                    FileStreamToWrite.Read(WriteBytes, 0, (int)FileStreamToWrite.Length);


                    Console.WriteLine("Writing Filestream to Tape");
                    //TapeOperator.Write(objBackupFile.StartTapeLocation, WriteBytes);
                    TapeOperator.Write(isSingleFile ? objBackupFile.StartTapeLocation : objBackupFiles.NextAvailableTapeLocation, WriteBytes);
                    //Console.WriteLine("Finished Writing to Tape");
                }

                objBackupFile.EndTapeLocation = TapeOperator.GetTapePosition();
                Console.WriteLine("Tape Position is " + objBackupFile.EndTapeLocation);
            }

            Console.WriteLine("Now I'm Cleaning up resources...");
            TapeOperator.Close();
            FileStreamToWrite.Close();
            Console.WriteLine("Now I'm Done!");
            Console.WriteLine();
        }

        public void ReadFileFromTape(BackupFile objBackupFile)
        {
            BackupFiles objBackupFiles;

            objBackupFiles = new BackupFiles();
            objBackupFiles.Add(objBackupFile);
            ReadFilesFromTape(objBackupFiles);
        }
        //public void ReadFileFromTape(BackupFile objBackupFile)
        //{
        //    TapeOperator objTapeOperator;
        //    FileStream objFileStream;
        //    byte[] objReadBytes;


        //    Console.WriteLine("ReadFileFromTape Method");
        //    Console.WriteLine("Initializing the tape drive...");
        //    objTapeOperator = new TapeOperator();
        //    objTapeOperator.Load(TapeName);

        //    Console.WriteLine("I'm Setting the tape position...");
        //    objTapeOperator.SetTapePosition(objBackupFile.StartTapeLocation);
        //    Console.WriteLine("Tape Position is " + objTapeOperator.GetTapePosition());

        //    Console.WriteLine("Creating the file on disk...");
        //    objFileStream = new FileStream(objBackupFile.NewFileNameAndPath, FileMode.OpenOrCreate, FileAccess.Write);


        //    if (objBackupFile.isLargeFile)
        //    {
        //        Console.WriteLine("This file will take " + objBackupFile.FileChunkCount + " Passes to read...");
        //        for (int intCounter = 1; intCounter <= objBackupFile.FileChunkCount; intCounter++)
        //        {
        //            if (intCounter < objBackupFile.FileChunkCount)
        //            {
        //                Console.WriteLine("This is pass " + intCounter);
        //                Console.WriteLine("Now I'm Reading file from tape into memory...");
        //                objReadBytes = objTapeOperator.Read(objTapeOperator.GetTapePosition(), BackupFile.MaxMemoryChunk);
        //                //objReadBytes = objTapeOperator.Read(objBackupFile.StartTapeLocation * intCounter, BackUpFile.MaxMemoryChunk);

        //                Console.WriteLine("Now I'm Writing file to disk...");
        //                objFileStream.Write(objReadBytes, 0, objReadBytes.Length);
        //            }
        //            else
        //            {
        //                Console.WriteLine("This is pass " + intCounter);
        //                Console.WriteLine("Now I'm Reading file from tape into memory...");
        //                objReadBytes = objTapeOperator.Read(objTapeOperator.GetTapePosition(), (int)(objBackupFile.Size - BackupFile.MaxMemoryChunk * (intCounter - 1)));

        //                Console.WriteLine("Now I'm Writing file to disk...");
        //                objFileStream.Write(objReadBytes, 0, objReadBytes.Length);
        //            }

        //        }
        //    }
        //    else
        //    {
        //        Console.WriteLine("Now I'm Reading file from tape into memory...");
        //        objReadBytes = objTapeOperator.Read(objBackupFile.StartTapeLocation, objBackupFile.Size);

        //        Console.WriteLine("Now I'm Writing file to disk...");
        //        objFileStream.Write(objReadBytes, 0, objReadBytes.Length);
        //    }

        //    Console.WriteLine("Tape Position is " + objTapeOperator.GetTapePosition());
        //    Console.WriteLine("Now I'm Cleaning up resources...");
        //    objFileStream.Close();
        //    objTapeOperator.Close();

        //    Console.WriteLine("Now I'm Done!");
        //    Console.WriteLine();
        //}

        public void ReadFilesFromTape(BackupFiles objBackupFiles)
        {
            Console.WriteLine("ReadFilesFromTape Method");
            Console.WriteLine("Initializing the tape drive...");
            TapeOperator = new TapeOperator();
            TapeOperator.Load(TapeName);

            foreach (BackupFile objBackupFile in objBackupFiles)
            {
                Console.WriteLine("I'm Setting the tape position...");
                TapeOperator.SetTapePosition(objBackupFile.StartTapeLocation);
                Console.WriteLine("Tape Position is " + TapeOperator.GetTapePosition());

                Console.WriteLine("Creating the file on disk...");
                FileStreamToWrite = new FileStream(objBackupFile.NewFileNameAndPath, FileMode.OpenOrCreate, FileAccess.Write);

                if (objBackupFile.isLargeFile)
                {
                    Console.WriteLine("This file will take " + objBackupFile.FileChunkCount + " Passes to read...");
                    for (int intCounter = 1; intCounter <= objBackupFile.FileChunkCount; intCounter++)
                    {
                        if (intCounter < objBackupFile.FileChunkCount)
                        {
                            ReadBytes = null;//clears the memory...
                            GC.Collect();

                            Console.WriteLine("This is pass " + intCounter);
                            Console.WriteLine("Now I'm Reading file from tape into memory...");
                            ReadBytes = TapeOperator.Read(TapeOperator.GetTapePosition(), BackupFile.MaxMemoryChunk);
                            //objReadBytes = objTapeOperator.Read(objBackupFile.StartTapeLocation * intCounter, BackUpFile.MaxMemoryChunk);

                            Console.WriteLine("Now I'm Writing file to disk...");
                            FileStreamToWrite.Write(ReadBytes, 0, ReadBytes.Length);
                        }
                        else
                        {
                            ReadBytes = null;//clears the memory...
                            GC.Collect();

                            Console.WriteLine("This is pass " + intCounter);
                            Console.WriteLine("Now I'm Reading file from tape into memory...");
                            ReadBytes = TapeOperator.Read(TapeOperator.GetTapePosition(), (int)(objBackupFile.Size - BackupFile.MaxMemoryChunk * (intCounter - 1)));

                            Console.WriteLine("Now I'm Writing file to disk...");
                            FileStreamToWrite.Write(ReadBytes, 0, ReadBytes.Length);
                        }

                    }
                }
                else
                {
                    ReadBytes = null;//clears the memory...
                    GC.Collect();

                    Console.WriteLine("Now I'm Reading file from tape into memory...");
                    ReadBytes = TapeOperator.Read(objBackupFile.StartTapeLocation, objBackupFile.Size);

                    Console.WriteLine("Now I'm Writing file to disk...");
                    FileStreamToWrite.Write(ReadBytes, 0, ReadBytes.Length);
                }
                Console.WriteLine("Tape Position is " + TapeOperator.GetTapePosition());
            }

            Console.WriteLine("Now I'm Cleaning up resources...");
            FileStreamToWrite.Close();
            TapeOperator.Close();

            Console.WriteLine("Now I'm Done!");
            Console.WriteLine();
        }

        public TapeOperator.MediaInfo GetTapeMediaInfo()
        {
            TapeOperator objTapeOperator;
            TapeOperator.MediaInfo objMediaInfo;


            Console.WriteLine("Initializing the tape drive...");
            objTapeOperator = new TapeOperator();
            objTapeOperator.Load(TapeName);
            objMediaInfo = objTapeOperator.GetTapeMediaParameters();
            objTapeOperator.Close();

            return objMediaInfo;
        }

        public void WriteTapeMediaInfo()
        {
            Console.WriteLine("Initializing the tape drive...");
            TapeOperator = new TapeOperator();
            TapeOperator.Load(TapeName);

            Console.WriteLine("Tape Position is " + TapeOperator.GetTapePosition());

            //for some reason, I kept getting an error until I added the below statements
            Console.WriteLine("Getting the Tape Info...");
            MediaInfo = TapeOperator.GetTapeMediaParameters();
            Console.WriteLine("Tape Block Size is: " + MediaInfo.BlockSize);
            Console.WriteLine("Tape Capacity is: " + MediaInfo.Capacity);
            Console.WriteLine("The Remaining Space is: " + MediaInfo.Remaining);
            Console.WriteLine("Is Write Protected (1=True & 0=False): " + MediaInfo.IsWriteProtected);
            Console.WriteLine("The Tape's partition Count is: " + MediaInfo.PartitionCount);

            TapeOperator.Close();
        }

        public void SetTapeMediaInfo(TapeOperator.MediaInfoSet objMediaInfoSet)//1=True and 0=False
        {
            TapeOperator objTapeOperator;

            
            Console.WriteLine("Initializing the tape drive...");
            objTapeOperator = new TapeOperator();
            objTapeOperator.Load(TapeName);

            objTapeOperator.SetTapeMediaParameters(objMediaInfoSet);
            objTapeOperator.Close();
        }

        public TapeOperator.DriveInfo GetTapeDriveInfo()
        {
            TapeOperator objTapeOperator;
            TapeOperator.DriveInfo objDriveInfo;


            Console.WriteLine("Initializing the tape drive...");
            objTapeOperator = new TapeOperator();
            objTapeOperator.Load(TapeName);
            objDriveInfo = objTapeOperator.GetTapeDriveParameters();
            objTapeOperator.Close();

            return objDriveInfo;
        }

        public void WriteTapeDriveInfo()
        {
            TapeOperator objTapeOperator;
            TapeOperator.DriveInfo objDriveInfo;


            Console.WriteLine("Initializing the tape drive...");
            objTapeOperator = new TapeOperator();
            objTapeOperator.Load(TapeName);

            objDriveInfo = objTapeOperator.GetTapeDriveParameters();

            Console.WriteLine("These Properties can be set...");
            Console.WriteLine("Compression is (1=True & 0=False): " + objDriveInfo.Compression);
            Console.WriteLine("DataPadding is: " + objDriveInfo.DataPadding);
            Console.WriteLine("ECC is: " + objDriveInfo.ECC);
            Console.WriteLine("EOTWarningZoneSite is: " + objDriveInfo.EOTWarningZoneSize);
            Console.WriteLine("ReportSetMarks is: " + objDriveInfo.ReportSetMarks);
            Console.WriteLine();
            Console.WriteLine("These Properties can not...");
            Console.WriteLine("Default Block Size is: " + objDriveInfo.DefaultBlockSize);
            Console.WriteLine("Maximum Block Size is: " + objDriveInfo.MaximumBlockSize);
            Console.WriteLine("Minimum Block Size is: " + objDriveInfo.MinimumBlockSize);

            objTapeOperator.Close();
        }

        public void SetTapeDriveInfo(TapeOperator.DriveInfoSet objDriveInfoSet)//1=True and 0=False
        {
            TapeOperator objTapeOperator;
            
            Console.WriteLine("SetTapeDriveInfo Method");
            Console.WriteLine("Initializing the tape drive...");
            objTapeOperator = new TapeOperator();
            objTapeOperator.Load(TapeName);

            objTapeOperator.SetTapeDriveParameters(objDriveInfoSet);
            objTapeOperator.Close();
        }

        public void FormatTape()
        {
            Console.WriteLine("FormatTape Method");
            Console.WriteLine("Initializing the tape drive...");
            TapeOperator = new TapeOperator();
            TapeOperator.Load(TapeName);
            MediaInfo = TapeOperator.GetTapeMediaParameters();

            TapeOperator.FormatTape();
            TapeOperator.Close();
        }

        public void EjectTape()
        {
            Console.WriteLine("EjectTape Method");
            Console.WriteLine("Initializing the tape drive...");
            TapeOperator = new TapeOperator();
            TapeOperator.Load(TapeName);

            MediaInfo = TapeOperator.GetTapeMediaParameters();

            TapeOperator.UnloadTape();
            TapeOperator.Close();
        }

        public static void CreateDemoRequiredFiles()
        {
            BackupFiles objBackupFiles;
            BackupFile objBackupFile;
            FilesToBackup objFilesToBackup;
            FileToBackUp objFileToBackup;
            SingleFileToBackUp objSingleFileToBackup;
            

            
            //Procedure for reading file from tape; Backup files must have a Size, NewFileName, FilePath and StartTapeLocation to be restored
            objBackupFiles = new BackupFiles();
            objBackupFile = new BackupFile { StartTapeLocation = 0, EndTapeLocation = 84673, FilePath = Directory.GetCurrentDirectory() + "\\", FileName = "Test1.txt", Size = 5549095658 };
            objBackupFiles.Add(objBackupFile);
            objBackupFile = new BackupFile { StartTapeLocation = 84673, EndTapeLocation = 84990, FilePath = Directory.GetCurrentDirectory() + "\\", FileName = "Test2.txt", Size = 20764304 };
            objBackupFiles.Add(objBackupFile);
            objBackupFiles.SaveToXMLFile(Directory.GetCurrentDirectory() + "\\", "BackedUpFilesExample.xml");

            //Backup files must have a FilePath and FileName in order to be written to the tape; StartTapeLocation is required when writing a single file, 
            //otherwise the app assigns locations as it's backing up multiple files
            objFilesToBackup = new FilesToBackup();
            objFileToBackup = new FileToBackUp { FilePath = Directory.GetCurrentDirectory() + "\\", FileName = "Test1.txt" };
            objFilesToBackup.Add(objFileToBackup);
            objFileToBackup = new FileToBackUp { FilePath = Directory.GetCurrentDirectory() + "\\", FileName = "Test2.txt" };
            objFilesToBackup.Add(objFileToBackup);
            objFilesToBackup.SaveToXMLFile(Directory.GetCurrentDirectory() + "\\", "FilesToBackupExample.xml");

            objSingleFileToBackup = new SingleFileToBackUp { StartTapeLocation = 84673, FilePath = Directory.GetCurrentDirectory() + "\\", FileName = "Test2.txt" };
            objSingleFileToBackup.Serialize(Directory.GetCurrentDirectory() + "\\FileToBackupExample.xml");

        }

        public static string DisplayUsage()
        {
            StringBuilder objStrBldr = new StringBuilder();

            objStrBldr.AppendLine("Created By: Jake Lardinois");
            objStrBldr.AppendLine("Date Created: 7/01/2013");
            objStrBldr.AppendLine("TapeBackup.exe is an application that takes a list of files and stores them to a specified tape drive.");
            objStrBldr.AppendLine("Note: It does need around 620-630MB of memory to run; else you may recieve an OutOfMemoryException");
            objStrBldr.AppendLine();
            objStrBldr.AppendLine();
            objStrBldr.AppendLine("Backup Example:");
            objStrBldr.AppendLine("tapebackup.exe /T \"\\\\.\\Tape0\" /B \"C:\\TEMP\\FilesToBackup.xml\"");
            objStrBldr.AppendLine();
            objStrBldr.AppendLine("Restore Example where \\O is the directory where the restored files will be copied to:");
            objStrBldr.AppendLine("tapebackup.exe /T \"\\\\.\\Tape0\" /O \"C:\\TEMP\\\" /R \"C:\\TEMP\\BackedUpFiles.xml.xml\"");
            objStrBldr.AppendLine();
            objStrBldr.AppendLine("Create Demo Backup, Single Backup, Restore, and Single Restore file Examples:");
            objStrBldr.AppendLine("tapebackup.exe /C");
            objStrBldr.AppendLine();
            objStrBldr.AppendLine("Backup Single File example:");
            objStrBldr.AppendLine("tapebackup.exe /T \"\\\\.\\Tape0\" /S \"C:\\TEMP\\FileToBackup.xml\"");
            objStrBldr.AppendLine();
            objStrBldr.AppendLine("Get Tape Drive Info:");
            objStrBldr.AppendLine("tapebackup.exe /T \"\\\\.\\Tape0\" /D");
            objStrBldr.AppendLine();
            objStrBldr.AppendLine("Get Tape Info:");
            objStrBldr.AppendLine("tapebackup.exe /T \"\\\\.\\Tape0\" /I");
            objStrBldr.AppendLine();
            objStrBldr.AppendLine("Format the tape:");
            objStrBldr.AppendLine("tapebackup.exe /T \"\\\\.\\Tape0\" /F");
            objStrBldr.AppendLine();
            objStrBldr.AppendLine("Eject the tape:");
            objStrBldr.AppendLine("tapebackup.exe /T \"\\\\.\\Tape0\" /E");
            objStrBldr.AppendLine();
            objStrBldr.AppendLine("\t /T - The name of the Tape Drive (found in device manager under 'Tape Symbolic Name'");
            objStrBldr.AppendLine("\t /B - Indicates that a Backup is to occur. The files to be backup up are stored in an ");
            objStrBldr.AppendLine("\t /C - The name of the new file that will be created (complete with path)");
            objStrBldr.AppendLine("\t /S - If used then the Carriage Returns and Line Feeds from the original file will be stripped prior to folding.");
            return objStrBldr.ToString();
        }
        #endregion

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
    }
}
