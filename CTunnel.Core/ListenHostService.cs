//using System.Net;
//using System.Net.Security;
//using System.Net.Sockets;
//using System.Text;
//using Microsoft.Extensions.Hosting;

//namespace CTunnel.Core
//{
//    public class ListenHostService : IHostedService
//    {
//        private TcpListener _tcpListener = null!;

//        public Task StartAsync(CancellationToken cancellationToken)
//        {
//            _tcpListener = new TcpListener(IPAddress.Any, 443);
//            _tcpListener.Start();

//            while (true)
//            {
//                try
//                {
//                    var tcpClient = _tcpListener.AcceptTcpClient();
//                    var networkStream = tcpClient.GetStream();
//                    //var sslStream = new SslStream(
//                    //    networkStream,
//                    //    false,
//                    //    new RemoteCertificateValidationCallback(ValidateClientCertificate),
//                    //    null
//                    //);

//                    // 设置 ServerCertificateSelectionCallback 来根据 SNI 选择证书
//                    sslStream.ServerCertificateSelectionCallback = SelectCertificate;

//                    // 开始握手
//                    sslStream.AuthenticateAsServer(cert1); // 默认使用 cert1，后续会根据 SNI 进行切换

//                    // 读取客户端发送的消息
//                    StreamReader reader = new StreamReader(sslStream, Encoding.UTF8);
//                    string message = reader.ReadLine();
//                    Console.WriteLine("Received: " + message);

//                    // 发送响应消息
//                    StreamWriter writer = new StreamWriter(sslStream, Encoding.UTF8)
//                    {
//                        AutoFlush = true
//                    };
//                    writer.WriteLine("Hello from server!");

//                    tcpClient.Close();
//                }
//                catch (Exception ex)
//                {
//                    Console.WriteLine("Exception: " + ex.Message);
//                }
//            }
//        }

//        public async Task StopAsync(CancellationToken cancellationToken)
//        {
//            _tcpListener.Stop();
//            await Task.CompletedTask;
//        }
//    }
//}
