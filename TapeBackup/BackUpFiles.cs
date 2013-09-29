using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml;
using System.Xml.Serialization;
using System.IO;


namespace TapeBackup
{
    [Serializable()]
    public class BackupFile
    {
        public string FileName { get; set; }
        public long Size { get; set; }
        public long StartTapeLocation { get; set; }
        public long EndTapeLocation { get; set; }

        [XmlIgnore]
        public static string DefaultOutPutDirectory { get; set; }

        [XmlIgnore]
        private string mNewFileName { get; set; }
        [XmlIgnore]
        private string mFilePath { get; set; }
        [XmlIgnore]
        public string NewFileName { 
            get 
            {
                if (string.IsNullOrEmpty(mNewFileName))
                    return FileName;
                else
                    return
                        mNewFileName;
            } 
            set 
            {
                mNewFileName = value; 
            } 
        }
        [XmlIgnore]
        public string FilePath { 
            get 
            {
                if (string.IsNullOrEmpty(mFilePath))
                    return DefaultOutPutDirectory;
                else
                    return mFilePath;
            } 
            set 
            {
                mFilePath = value;
            } 
        }
        [XmlIgnore]
        public string FileNameAndPath { get { return FilePath + FileName; } }
        [XmlIgnore]
        public string NewFileNameAndPath { get { return FilePath + NewFileName; } }
        //This is the maximum amount of memory that will be used to read the file; I use this value to determine if the file needs to be broken up when being written to tape
        [XmlIgnore]
        public static int MaxMemoryChunk { get { return 536870911; } } //working value = 536870911; int.MaxValue = 2147483647;
        //This is the number of pieces that the file must be broken into in order to writ it onto tape
        [XmlIgnore]
        public int FileChunkCount {
            get
            {
                return (int)((this.Size / MaxMemoryChunk) + 1);
            }
        }
        [XmlIgnore]
        public bool isLargeFile {
            get
            {
                return this.FileChunkCount > 1;
            }
        }
    }

    public class BackupFiles : List<BackupFile>
    {
        public XmlDocument XMLDoc { get; set; }


        public BackupFiles()
            : base() { }

        public BackupFiles(string strFilePath, string strFileName)
            : base() 
        {
            XMLDoc = new System.Xml.XmlDocument();

            XMLDoc.Load(strFilePath + strFileName);
            Populate();
        }

        public long NextAvailableTapeLocation
        {
            get
            {
                try { return this.Max(b => b.EndTapeLocation); }
                catch { return 0; }//returns 0 if there are no elements in the collection; usually occurs on the first file being backed up.
                
            }
        }

        public void SaveToXMLFile(string strFilePath, string strFileName)
        {
            //create the serialiser to create the xml
            XmlSerializer serialiser = new XmlSerializer(typeof(List<BackupFile>));

            // Create the TextWriter for the serialiser to use
            TextWriter Filestream = new StreamWriter(strFilePath + strFileName);

            //write to the file
            serialiser.Serialize(Filestream, this);

            // Close the file
            Filestream.Close();
        }

        private void Populate()
        {
            XmlNodeList objXMLNodes;
            BackupFile objBackupFile;
            long lngTemp;


            objXMLNodes = XMLDoc.SelectNodes("ArrayOfBackupFile");//Inevitably this is case sensitive when it was "ArrayOfBackUpFile", objXMLNodes[0] would be null... 

            foreach (XmlNode objXMLNode in objXMLNodes[0])
            {
                objBackupFile = new BackupFile();

                //objOrder.Type = objXMLNode.Attributes["type"].Value;
                foreach (XmlNode objChildNode in objXMLNode.ChildNodes)
                {
                    switch (objChildNode.Name)
                    {
                        case "FileName":
                            objBackupFile.FileName = objChildNode.InnerText;
                            break;
                        case "Size":
                            objBackupFile.Size = long.TryParse(objChildNode.InnerText, out lngTemp) ? lngTemp : 0;
                            break;
                        case "StartTapeLocation":
                            objBackupFile.StartTapeLocation = long.TryParse(objChildNode.InnerText, out lngTemp) ? lngTemp : 0;
                            break;
                    }
                }
                this.Add(objBackupFile);
            }
        }

        
    }
}
