using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml.Serialization;
using System.Xml;
using System.IO;


namespace TapeDrive
{
    [Serializable()]
    public class FileToBackUp
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        [XmlIgnore]
        public string FileNameAndPath { get { return FilePath + FileName; } }
    }

    public class SingleFileToBackUp : FileToBackUp
    {
        public long StartTapeLocation { get; set; }

        public void Serialize(string strFilePathAndName)
        {
            StreamWriter objStreamWriter;


            System.Xml.Serialization.XmlSerializer objXmlSerializer = new System.Xml.Serialization.XmlSerializer(this.GetType());
            objStreamWriter = new StreamWriter(strFilePathAndName, false);
            objXmlSerializer.Serialize(objStreamWriter, this);
            objStreamWriter.Close();
        }

        public static SingleFileToBackUp DeSerialize(string strFilePathAndName)
        {
            SingleFileToBackUp objSingleFileToBackup;
            System.Xml.Serialization.XmlSerializer objXmlSerializer;


            using (StreamReader objStreamReader = new StreamReader(strFilePathAndName))
            {
                objSingleFileToBackup = new SingleFileToBackUp();
                objXmlSerializer = new System.Xml.Serialization.XmlSerializer(objSingleFileToBackup.GetType());

                return (SingleFileToBackUp)objXmlSerializer.Deserialize(objStreamReader);
            }
        }
    }

    public class FilesToBackup : List<FileToBackUp>
    {
        public XmlDocument XMLDoc { get; set; }

        public FilesToBackup()
            : base() { }

        public FilesToBackup(string strFilePath, string strFileName)
            : base() 
        {
            XMLDoc = new System.Xml.XmlDocument();

            XMLDoc.Load(strFilePath + strFileName);
            Populate();
        }

        public void SaveToXMLFile(string strFilePath, string strFileName)
        {
            //create the serialiser to create the xml
            XmlSerializer serialiser = new XmlSerializer(typeof(List<FileToBackUp>));

            // Create the TextWriter for the serialiser to use
            TextWriter Filestream = new StreamWriter(strFilePath + strFileName);

            //write to the file
            serialiser.Serialize(Filestream, this);

            // Close the file
            Filestream.Close();
        }

        public BackupFiles ToBackupFiles()
        {
            BackupFiles objBackupFiles;
            BackupFile objBackupFile;

            objBackupFiles = new BackupFiles();
            foreach (FileToBackUp objFileToBackup in this)
            {
                objBackupFile = new BackupFile();
                objBackupFile.FileName = objFileToBackup.FileName;
                objBackupFile.FilePath = objFileToBackup.FilePath;
                objBackupFile.StartTapeLocation = 0;

                objBackupFiles.Add(objBackupFile);
            }

            return objBackupFiles;
        }

        private void Populate()
        {
            XmlNodeList objXMLNodes;
            FileToBackUp objFileToBackUp;


            objXMLNodes = XMLDoc.SelectNodes("ArrayOfFileToBackUp");

            foreach (XmlNode objXMLNode in objXMLNodes[0])
            {
                objFileToBackUp = new FileToBackUp();

                //objOrder.Type = objXMLNode.Attributes["type"].Value;
                foreach (XmlNode objChildNode in objXMLNode.ChildNodes)
                {
                    switch (objChildNode.Name)
                    {
                        case "FileName":
                            objFileToBackUp.FileName = objChildNode.InnerText;
                            break;
                        case "FilePath":
                            objFileToBackUp.FilePath = objChildNode.InnerText;
                            break;
                    }
                }
                this.Add(objFileToBackUp);
            }
        }

    }
}
