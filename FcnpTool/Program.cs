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
            if (args.Length > 0)
            {
                if (Path.GetExtension(args[0]) == ".fcnp")
                {
                    string outputPath = args[0] + ".xml";
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(Fcnp));

                    using (FileStream readStream = new FileStream(args[0], FileMode.Open))
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
                else if (Path.GetExtension(args[0]) == ".xml")
                {
                    string outputPath = args[0].Substring(0, args[0].LastIndexOf('.'));
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(Fcnp));

                    using (FileStream xmlStream = new FileStream(args[0], FileMode.Open))
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
            } //if
        } //Main
    } //class
} //FcnpParser
