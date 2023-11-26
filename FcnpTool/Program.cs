using System;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;

namespace FcnpTool
{
    class Program
    {
        static void Main(string[] args)
        {
            foreach(string arg in args)
            {
                if (!File.Exists(arg))
                    continue;

                if (Path.GetExtension(arg) == ".fcnp")
                {
                    string outputPath = arg + ".xml";
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(Fcnp));

                    using (FileStream readStream = new FileStream(arg, FileMode.Open))
                    using (FileStream xmlStream = new FileStream(outputPath, FileMode.Create))
                    {
                        try
                        {
                            Fcnp fcnp = new Fcnp();
                            fcnp.Read(readStream);
                            xmlSerializer.Serialize(xmlStream, fcnp);

                            readStream.Close();
                            xmlStream.Close();
                        } //try
                        catch (Exception e)
                        {
                            readStream.Close();
                            xmlStream.Close();

                            using (FileStream errorStream = new FileStream($"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\\stacktrace.txt", FileMode.Create))
                            {
                                StreamWriter writer = new StreamWriter(errorStream);
                                writer.AutoFlush = true;
                                writer.WriteLine(e.Message);
                                writer.Write(e.StackTrace);
                                errorStream.Close();
                            } //using
                        } //catch
                    } //using
                } //if
                else if (Path.GetExtension(arg) == ".xml")
                {
                    string outputPath = arg.Substring(0, arg.LastIndexOf('.'));
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(Fcnp));

                    using (FileStream xmlStream = new FileStream(arg, FileMode.Open))
                    using (FileStream writeStream = new FileStream(outputPath, FileMode.Create))
                    {
                        try
                        {
                            Fcnp fcnp = (Fcnp)xmlSerializer.Deserialize(xmlStream);
                            fcnp.Write(writeStream);

                            xmlStream.Close();
                            writeStream.Close();
                        } //try
                        catch (Exception e)
                        {
                            xmlStream.Close();
                            writeStream.Close();

                            using (FileStream errorStream = new FileStream($"{Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}\\stacktrace.txt", FileMode.Create))
                            {
                                StreamWriter writer = new StreamWriter(errorStream);
                                writer.AutoFlush = true;
                                writer.WriteLine(e.Message);
                                writer.Write(e.StackTrace);
                                errorStream.Close();
                            } //using
                        } //catch
                    } //using
                } //else if
            }
        } //Main
    } //class
} //FcnpParser
