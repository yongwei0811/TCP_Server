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
            TcpListener server = new TcpListener(IPAddress.Parse("10.11.12.11"), 51236);
            server.Start();

            Console.WriteLine("Waiting for a client to connect...");

            sendSignal("ON1", "10.11.12.24", 51236);
            sendSignal("ON2", "10.11.12.24", 51236);
            sendSignal("ON3", "10.11.12.24", 51236);

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

                        if (res == "good")
                        {
                            count++;
                            Console.WriteLine("Count value: " + count);
                            sendSignal("good", getIP(request), 51236);
                            boardList.Add(request);
                        }

                        Console.WriteLine("Received: " + request);
                        Console.WriteLine("Sent: " + Encoding.ASCII.GetString(response));

                        if (count == 3)
                        {
                            // check if the board ready signal's sequence correct
                            if(boardList[0] == "10.11.12.21 BOARDREADY" && boardList[1] == "10.11.12.22 BOARDREADY" && boardList[2] == "10.11.12.23 BOARDREADY")
                            {
                                count = 0;
                                boardList.Clear();
                                System.Threading.Thread.Sleep(1000);
                                sendSignal("ON1", "10.11.12.24", 1234);
                                sendSignal("ON2", "10.11.12.24", 1234);
                                sendSignal("ON3", "10.11.12.24", 1234);
                            }
                            else
                            {
                                Console.WriteLine("Incorrect sequence of board");
                            }
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

        static void sendSignal(string msg, string ip, int port)
        {
            try
            {
                TcpClient client = new TcpClient(ip, port);
                NetworkStream stream = client.GetStream();

                byte[] buffer = Encoding.ASCII.GetBytes(msg);
                stream.Write(buffer, 0, buffer.Length);
                Console.WriteLine("The request (" + msg + ") already sent to: " + port);

                client.Close();
            }
            catch (Exception ex) {
                Console.WriteLine("No connection could be made to the port " + port);
                //Console.WriteLine(ex.ToString());
            }
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
