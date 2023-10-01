open System
open System.Net
open System.Net.Sockets
open System.Text

let serverIP = "127.0.0.1"  // Replace with the IP address of your server
let serverPort = 12345      // Replace with the port your server is listening on

let main () =
    try
        let serverEndPoint = IPEndPoint(IPAddress.Parse(serverIP), serverPort)
        let clientSocket = new Socket(SocketType.Stream, ProtocolType.Tcp)

        // Connect to the server
        clientSocket.Connect(serverEndPoint)
        printfn "Connected to the server at %s:%d" serverIP serverPort

        // Send a message to the server
        let mutable message = "Multiply 2 2"
        let mutable messageBytes = Encoding.ASCII.GetBytes(message)
        clientSocket.Send(messageBytes)

        // Receive a response from the server
        let mutable bufferLength = 1024
        let mutable buffer = Array.create<byte> bufferLength 0uy
        let mutable bytesRead = clientSocket.Receive(buffer)
        let mutable response = Encoding.ASCII.GetString(buffer, 0, bytesRead)
        printfn "Server response: %s" response

        message <- "Add 45 15 30"
        messageBytes <- Encoding.ASCII.GetBytes(message)
        clientSocket.Send(messageBytes)
        bytesRead <- clientSocket.Receive(buffer)
        response <- Encoding.ASCII.GetString(buffer, 0, bytesRead)
        printfn "Server response: %s" response

        message <- "Add 45 15 "
        messageBytes <- Encoding.ASCII.GetBytes(message)
        clientSocket.Send(messageBytes)
        bytesRead <- clientSocket.Receive(buffer)
        response <- Encoding.ASCII.GetString(buffer, 0, bytesRead)
        printfn "Server response: %s" response

        message <- "Multiply 3 15 "
        messageBytes <- Encoding.ASCII.GetBytes(message)
        clientSocket.Send(messageBytes)
        bytesRead <- clientSocket.Receive(buffer)
        response <- Encoding.ASCII.GetString(buffer, 0, bytesRead)
        printfn "Server response: %s" response
        // Close the client socket
        clientSocket.Close()
    with
    | :? SocketException as se ->
        printfn "SocketException: %s" se.Message
    | ex ->
        printfn "An unexpected exception occurred: %s" ex.Message

// Call the main function to run the client
main()
