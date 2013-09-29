using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TapeBackup
{
    public static class MyExtensionMethods
    {
        public static bool IsEmpty<T>(this IEnumerable<T> source)
        {
            if (source == null)
                return true;
            else
                return !source.Any();
        }

        //public static void ToXML<T>(this IEnumerable<T> source)
        //{
        //    //create the serialiser to create the xml
        //    XmlSerializer serialiser = new XmlSerializer(typeof(List<T>));

        //    // Create the TextWriter for the serialiser to use
        //    TextWriter Filestream = new StreamWriter(@"C:\temp\output.xml");

        //    //write to the file
        //    serialiser.Serialize(Filestream, source);

        //    // Close the file
        //    Filestream.Close();
        //}
    }
}
