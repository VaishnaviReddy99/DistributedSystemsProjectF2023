// open System
// open System.Net
// open System.Net.Sockets
// open System.Text
// open System.Threading

// let serverIP = "127.0.0.1"
// let serverPort = 12345

// let sendThreadFunction (clientSocket: Socket) = 
//     let rec loop () =
//         printf "Enter your command for the server to perform action: "
//         let message = Console.ReadLine()
//         let messageBytes = Encoding.ASCII.GetBytes(message)
//         clientSocket.Send(messageBytes)

//         match message.ToLower() with
//         | "bye" | "terminate" -> printfn "Exiting send loop."
//         | _ -> loop()
    
//     loop()

// let receiveThreadFunction (clientSocket: Socket) =
//     let bufferLength = 1024
//     let buffer = Array.zeroCreate<byte> bufferLength
    
//     let rec loop () =
//         let bytesRead = clientSocket.Receive(buffer)
//         let response = Encoding.ASCII.GetString(buffer, 0, bytesRead)
//         printfn "Server response: %s" response
        
//         // Close the client socket if server sends a termination message
//         if response.ToLower().Contains("terminate") then
//             printfn "Exiting receive loop."
//             clientSocket.Close()
//         else
//             loop()
    
//     loop()

// let main () =
//     try
//         let serverEndPoint = IPEndPoint(IPAddress.Parse(serverIP), serverPort)
//         let clientSocket = new Socket(SocketType.Stream, ProtocolType.Tcp)

//         // Connect to the server
//         clientSocket.Connect(serverEndPoint)
//         printfn "Connected to the server at %s:%d" serverIP serverPort
        
//         // Start a thread to send messages
//         let sendThread = Thread(ThreadStart(fun () -> sendThreadFunction clientSocket))
//         sendThread.Start()
        
//         // Start a thread to receive messages
//         let receiveThread = Thread(ThreadStart(fun () -> receiveThreadFunction clientSocket))
//         receiveThread.Start()
        
//         // Wait for both threads to finish
//         sendThread.Join()
//         receiveThread.Join()
//     with
//     | :? SocketException as se -> printfn "SocketException: %s" se.Message
//     | ex -> printfn "An unexpected exception occurred: %s" ex.Message

// // Call the main function to run the client
// main()


open System
open System.Net
open System.Net.Sockets
open System.Text


let serverIP = "127.0.0.1"  // Replace with the IP address of your server
let serverPort = 12345      // Replace with the port your server is listening on


// ##############################################
// Working code

let main () =
    try
        let serverEndPoint = IPEndPoint(IPAddress.Parse(serverIP), serverPort)
        let clientSocket = new Socket(SocketType.Stream, ProtocolType.Tcp)

        // Connect to the server
        clientSocket.Connect(serverEndPoint)
        printfn "Connected to the server at %s:%d" serverIP serverPort
        let mutable command = ""
        let bufferLength = 1024
        let buffer = Array.create<byte> bufferLength 0uy

        let rec loop () =
            printf "Enter your command for the server to perform : "
            let message = Console.ReadLine()
            let  messageBytes = Encoding.ASCII.GetBytes(message)
            clientSocket.Send(messageBytes)
            let bytesRead = clientSocket.Receive(buffer)
            let response = Encoding.ASCII.GetString(buffer, 0, bytesRead)
            printfn "Server response: %s" response
            match response with
            | "-5" -> printfn "Got Server Response as -5. Terminating the Client"
            | _ -> loop()
        
        loop()

        clientSocket.Close()
    with
    | :? SocketException as se ->
        printfn "SocketException: %s" se.Message
    | ex ->
        printfn "An unexpected exception occurred: %s" ex.Message

// Call the main function to run the client
main()
