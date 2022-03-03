using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;


namespace RemoteHostConnection
{
    class Program
    {
        static List<int> array = new List<int>();
        
        static void Main(string[] args)
        {
            string host = "88.212.241.115";
            double unixTimestamp = 1380619079;
            

            int port = UnixTimeStampToYear(unixTimestamp);
            IPAddress iPAddress = IPAddress.Parse(host);

            SendOne(iPAddress, port, "Greetings\n");
            SendBlock(iPAddress, port);
            
            Console.ReadKey();

        }

        static void SendOne(IPAddress iPAddress, int port, string message)
        {
            Socket socket = ConnectSocket(iPAddress, port);
            Console.WriteLine("connected");
            SendMessage(socket, message);
            Console.WriteLine("message sended");
            string answer = ReciveAnswer(socket);
            socket.Close();
            int res = GetInteger(answer);
            Console.WriteLine(answer);
            Console.WriteLine(res);
        }

        static void SendBlock(IPAddress iPAddress, int port)
        {
            DateTime start = DateTime.Now;
            int count = 10;
            Task[] tasks = new Task[count];
            for (int i = 0; i < count; ++i)
            {
                int message = i + 1;
                tasks[i] = Task.Run(() => SocketSendReceive(iPAddress, port, message));
            }

            Task.WaitAll(tasks);

            array.Sort();
            WrightToFile(array);

            foreach (var n in array)
            {
                Console.WriteLine(n);
            }

            DateTime finish = DateTime.Now;
            Console.WriteLine(finish.Subtract(start));
            float res = GetMediana(array);
            Console.WriteLine("Array lenght: {0}. Mediana: {1}", array.Count, res);
        }

        private static void WrightToFile(List<int> array)
        {
            string path = "1.txt";
            using (FileStream fstream = new FileStream(path, FileMode.OpenOrCreate | FileMode.Append))
            {
                foreach(int n in array)
                {
                    byte[] arr = Encoding.Default.GetBytes(n.ToString() + "\n");
                    fstream.Write(arr, 0, arr.Length);
                }
                
                Console.WriteLine("Текст записан в файл");
            }
        }

        public static int UnixTimeStampToYear(double unixTimeStamp)
        {
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dateTime.Year;
        }

        static Socket ConnectSocket(IPAddress iPAddress, int port)
        {
            Socket socket = null;
            do
            {
                try
                {
                    IPEndPoint ipEndPoint = new IPEndPoint(iPAddress, port);
                    socket = new Socket(iPAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    socket.Connect(ipEndPoint);
                    return socket;
                }
                catch { }
            } while (!socket.Connected);

            return socket;
        }

        static void CloseConnection(Socket socket)
        {
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
        }

        static void SocketSendReceive(IPAddress iPAddress, int port, int message)
        {
            string answer = null;
            while (answer == null)
            {
                Socket socket = ConnectSocket(iPAddress, port);
                Console.WriteLine("create socket for message: {0}", message);
                if (!SendMessage(socket, message + "\n"))
                {
                    CloseConnection(socket);
                    continue;
                }

                Console.WriteLine("send message: {0}", message);
                answer = ReciveAnswer(socket);
                CloseConnection(socket);
            }

            int res = GetInteger(answer);
            Console.WriteLine("answer for message {0}: {1}", message, res);
            Console.WriteLine("full answer: {0}", answer);
            array.Add(res);
        }

        static bool SendMessage(Socket socket, string message)
        {
            try
            {
                Byte[] bytesSent = Encoding.ASCII.GetBytes(message);
                socket.Send(bytesSent);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string ReciveAnswer(Socket socket)
        {
            Byte[] bytesReceived = new Byte[256];
            StringBuilder builder = new StringBuilder();
            Encoding encoding = Encoding.GetEncoding(20866);
            int bytes;
            do
            {
                try
                {
                    bytes = socket.Receive(bytesReceived, bytesReceived.Length, 0);
                }
                catch
                {
                    Console.WriteLine("answer wasn't receive");
                    return null;
                }

                builder.Append(encoding.GetString(bytesReceived, 0, bytes));
            } while (bytes > 0);

            string answer = builder.ToString();
            if (!answer.Contains("\n"))
            {
                Console.WriteLine("answer not ended");
                return null;
            }

            return answer;
        }

        static int GetInteger(string text)
        {
            if (text == null)
                return 0;

            int value;
            int.TryParse(string.Join("", text.Where(c => char.IsDigit(c))), out value);
            return value;
        }

        static float GetMediana(List<int> list)
        {
            int length = list.Count();
            if (length == 0)
                return 0;

            if (length % 2 == 1)
                return list.ElementAt(length / 2);
            else
                return 0.5f * (list.ElementAt(length / 2 - 1) + list.ElementAt(length / 2));
        }

        static int QuickSelect(IEnumerable<int> list, int k)
        {
            int length = list.Count();
            if (length == 1)
            {
                if (k != 0)
                    return 0;
                return list.ElementAt(0);
            }

            int pivot = new Random().Next(1, length + 1);
            IEnumerable<int> lows = list.Where(n => n < pivot);
            IEnumerable<int> highs = list.Where(n => n > pivot);
            IEnumerable<int> pivots = list.Where(n => n == pivot);

            if (k < lows.Count())
                return QuickSelect(lows, k);
            else if (k < (lows.Count() + pivots.Count()))
                return pivots.ElementAt(0);
            else
                return QuickSelect(highs, k - lows.Count() - pivots.Count());

        }
    }
}
