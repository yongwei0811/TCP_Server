using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;

namespace TCP_Server
{
    internal class Program
    {
        static int count = 0;
        static List<string> boardList = new List<string>();
        static object myLock = new object();

        static void Main(string[] args)
        {
            TcpListener server = new TcpListener(IPAddress.Parse("10.11.12.11"), 51236);
            server.Start();

            Console.WriteLine("Waiting for a client to connect...");

            while (true)
            {
                Console.WriteLine();
                sendSignal("ON0", "10.11.12.24", 51236);


                for (int i = 0; i < 3; i++)
                {
                    TcpClient client = server.AcceptTcpClient();
                    Console.WriteLine("\nClient connected.");
                    HandleClient(client);

                    int num = i + 1;
                    if (i < 2)
                        sendSignal("ON" + num, "10.11.12.24", 51236);

                }
                if (count == 3)
                {
                    Console.WriteLine("Reset...");
                    count = 0;
                    boardList.Clear();
                    System.Threading.Thread.Sleep(20000);

                    Console.WriteLine("Reset done...");
                }
            }
        }

        static void HandleClient(object obj)
        {
            try
            {
                TcpClient client = (TcpClient)obj;
                NetworkStream stream = client.GetStream();

                try
                {
                    while (client.Connected)
                    {
                        // to check if the client disconnect to the server
                        if (client.Client.Poll(0, SelectMode.SelectRead))
                        {
                            byte[] buff = new byte[1];
                            if (client.Client.Receive(buff, SocketFlags.Peek) == 0)
                            {
                                Console.WriteLine("Client disconnected.\n");
                                break;
                            }
                        }

                        if (stream.DataAvailable)
                        {
                            // Read the request from the client.
                            byte[] buffer = new byte[1024];
                            int bytesRead = stream.Read(buffer, 0, buffer.Length);
                            string request = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                            string res = getResponse(request);

                            // Write the response back to the client.
                            byte[] response = Encoding.ASCII.GetBytes(res);
                            stream.Write(response, 0, response.Length);

                            if (res == "good")
                            {
                                sendSignal("good", getIP(request), 51236);
                                count++;
                                Console.WriteLine("Count value: " + count);
                                boardList.Add(request);
                            }

                            Console.WriteLine("Received: " + request);
                            Console.WriteLine("Sent: " + Encoding.ASCII.GetString(response) + " to " + getIP(request));
                        }
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: ");
                    Console.WriteLine(ex.ToString());
                }
                finally
                {
                    client.Close();
                }
            }
            catch (IOException ex)
            {
                // An error occurred while receiving or sending data
                Console.WriteLine($"Error 1: {ex.Message}");
            }
        }

        static void sendSignal(string msg, string ip, int port)
        {
            int t = 0;
            do
            {
                try
                {
                    TcpClient client = new TcpClient(ip, port);
                    NetworkStream stream = client.GetStream();

                    byte[] buffer = Encoding.ASCII.GetBytes(msg);
                    stream.Write(buffer, 0, buffer.Length);
                    Console.WriteLine("The request (" + msg + ") already sent to: " + ip);

                    client.Close();
                    break;
                }
                catch (Exception ex)
                {
                    t++;
                    Console.WriteLine("No connection could be made to " + ip);
                    //Console.WriteLine(ex.ToString());
                    continue;
                }
            } while (t < 4); // resend the signal if fail, maximum resend 4 times
        }

        static string getResponse(string req)
        {
            string res = "";

            if (req.Contains("BOARDREADY") && !boardList.Contains(req))
            {
                res = "good";
            }
            else if (boardList.Contains(req))
            {
                res = "Same board signal";
                createLogFile(req + " is sent more than 1 signal at ");
            }
            else
            {
                res = "Unknown request";
            }

            return res;
        }

        static void createLogFile(string req)
        {
            string logFile = "log.txt";
            //string currentDirectory = Directory.GetCurrentDirectory();
            //Console.WriteLine("The current directory is: " + currentDirectory);
            Console.WriteLine("A log file is created.");

            string logMessage = req + DateTime.Now;

            if (!File.Exists(logFile))
            {
                File.Create(logFile).Close();
            }

            File.AppendAllText(logFile, logMessage + Environment.NewLine);
        }

        static string getIP(string req)
        {
            string result = "No IP Address Found";
            string searchString = " BOARDREADY";
            int index = req.IndexOf(searchString);

            if (index >= 0)
            {
                result = req.Substring(0, index);
            }

            return result;
        }
    }
}
