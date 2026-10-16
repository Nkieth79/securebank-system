using System.Net;
using System.Net.Sockets;
using SecureBank.Server.Services;

namespace SecureBank.Server.Network;

public class TcpServer
{
    private readonly int _port;
    private readonly AuthService _authService;
    private readonly TransactionService _transactionService;
    private TcpListener? _listener;
    private bool _running;

    public TcpServer(int port, AuthService authService, TransactionService transactionService)
    {
        _port = port;
        _authService = authService;
        _transactionService = transactionService;
    }

    public void Start()
    {
        _listener = new TcpListener(IPAddress.Any, _port);
        _listener.Start();
        _running = true;

        Console.WriteLine($"Server listening on port {_port}");

        while (_running)
        {
            TcpClient client = _listener.AcceptTcpClient();
            Console.WriteLine("Client connected!");

            ClientHandler handler = new ClientHandler(client, _authService, _transactionService);

            Thread thread = new Thread(handler.Handle);
            thread.Start();
        }
    }

    public void Stop()
    {
        _running = false;
        _listener?.Stop();
    }
}