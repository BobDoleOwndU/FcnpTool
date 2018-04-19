using System;
using System.IO;
using System.Reflection;

namespace FcnpParser
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                if (Path.GetExtension(args[0]) == ".fcnp")
                {
                    string outputPath = args[0] + ".txt";

                    using (FileStream readStream = new FileStream(args[0], FileMode.Open))
                    using (FileStream writeStream = new FileStream(outputPath, FileMode.Create))
                    {
                        try
                        {
                            Fcnp fcnp = new Fcnp();
                            fcnp.Read(readStream);
                            fcnp.WriteOutputToTextFile(writeStream);

                            readStream.Close();
                            writeStream.Close();
                        } //try
                        catch (Exception e)
                        {
                            readStream.Close();
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
                } //if
            } //if
        } //Main
    } //class
} //FcnpParser
