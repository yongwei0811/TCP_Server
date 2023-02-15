using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace TCP_Server
{
    internal class Program
    {
        static int count = 0;
        static List<string> boardList = new List<string>();
        static void Main(string[] args)
        {
            TcpListener server = new TcpListener(IPAddress.Parse("127.0.0.1"), 8080);
            server.Start();

            Console.WriteLine("Waiting for a client to connect...");
            sendSignal("on", 1234);
         
            while (true)
            {
                TcpClient client = server.AcceptTcpClient();
                Console.WriteLine("Client connected.");
                Thread t = new Thread(HandleClient);
                t.Start(client);
            }
        }

        static void HandleClient(object obj)
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
                            Console.WriteLine("Client disconnected.");
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

                        if (res == "Ok")
                        {
                            count++;
                            Console.WriteLine("Count value: " + count);
                            sendSignal("Ok", int.Parse(getIP(request)));
                            boardList.Add(request);
                        }

                        Console.WriteLine("Received: " + request);
                        Console.WriteLine("Sent: " + Encoding.ASCII.GetString(response));

                        if (count == 3)
                        {
                            count = 0;
                            boardList.Clear();
                            System.Threading.Thread.Sleep(1000);
                            sendSignal("on", 1234);
                        }
                        
                    }
                }

            }
            catch (Exception)
            {
                Console.WriteLine("Client disconnected.");
            }
            finally
            {
                client.Close();
            }
        }

        static void sendSignal(string msg, int port)
        {
            TcpClient client = new TcpClient("127.0.0.1", port);
            NetworkStream stream = client.GetStream();

            byte[] buffer = Encoding.ASCII.GetBytes(msg);
            stream.Write(buffer, 0, buffer.Length);
            Console.WriteLine("The request ("+ msg + ") already sent to: " + port);

            client.Close();
        }

        static string getResponse(string req)
        {
            string res = "";

            if (req.Contains("BoardReady") && !boardList.Contains(req))
            {
                res = "Ok";
            }
            else if (boardList.Contains(req))
            {
                res = "Same board signal";
                createLogFile(req);
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
            string currentDirectory = Directory.GetCurrentDirectory();
            //Console.WriteLine("The current directory is: " + currentDirectory);
            Console.WriteLine("A log file is created.");

            string logMessage = req + " is sent more than 1 signal at " + DateTime.Now;
          
            if (!File.Exists(logFile))
            {
                File.Create(logFile).Close();
            }

            File.AppendAllText(logFile, logMessage + Environment.NewLine);
        }

        static string getIP(string req)
        {
            string result = "No IP Address Found";
            string searchString = "BoardReady";
            int startIndex = req.IndexOf(searchString) + searchString.Length;

            if (startIndex >= 0)
            {
               result = req.Substring(startIndex);
            }

            return result;
        }

        static void helo()
        {
            Console.WriteLine("heloooo");
        }
    }
}
