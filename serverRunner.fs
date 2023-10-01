open System
open System.Net
open System.Net.Sockets
open System.Text


let serverPort = 12345

// Function to handle communication with a client
let handleClient (csocket: Socket) =
    async {
        try
        let clientEndPoint = csocket.RemoteEndPoint.ToString()
        printfn "Client connected: %s" clientEndPoint
        let byteArrayLength = 1024 // Specify the length of the byte array
        let buffer = Array.create<byte> byteArrayLength 0uy   
        let mutable continueListening = true
        while continueListening do
            let bytesRead = csocket.Receive(buffer)
            if bytesRead = 0 then
                printfn "Client disconnected: %s" clientEndPoint
                continueListening <- false
            else
                let message = Encoding.ASCII.GetString(buffer, 0, bytesRead)
                printfn "Received: %s" message
                let separator = [| ' ' |] 
                let substrings = message.Split(separator, StringSplitOptions.RemoveEmptyEntries)
                let mutable result = 0
                let mutable exceptionOccurred = false

                if substrings.[0].Equals("bye",StringComparison.OrdinalIgnoreCase) then
                    continueListening <- false
                    printfn "Client saying bye"
                elif substrings.[0].Equals("terminate",StringComparison.OrdinalIgnoreCase) then
                    printfn "Client saying bye"
                elif not (substrings.[0].Equals("Add", StringComparison.OrdinalIgnoreCase) ||  substrings.[0].Equals("Substract", StringComparison.OrdinalIgnoreCase) ||    substrings.[0].Equals("Multiply", StringComparison.OrdinalIgnoreCase)) then
                    result <- -1
                    printfn "Incorrect operation provided ERR CODE: %d" result
                elif substrings.Length > 4 then
                    result <- -3
                    printfn "More than 4 inputs have been passed ERR CODE: %d" result
                elif substrings.Length < 2 then
                    result <- -2
                    printfn "Less than 2 inputs have been passed ERR CODE: %d" result
                elif substrings.[0].Equals("Add", StringComparison.OrdinalIgnoreCase) then
                    let numbersLen = substrings.Length - 1
                    for i = 0 to numbersLen - 1 do
                        if not exceptionOccurred then
                        try
                             result <- result + int substrings.[i + 1]
                         with
                        | :? System.FormatException as e ->
                            result <- -2
                            printfn "Exception occurred during casting: %s" e.Message
                            exceptionOccurred <- true       
                    printfn "result add: %d" result
                elif substrings.[0].Equals("Substract", StringComparison.OrdinalIgnoreCase) then
                        // Code for "Subtract" case
                        printfn  "substracting"
                elif substrings.[0].Equals("Multiply", StringComparison.OrdinalIgnoreCase) then
                    let mutable product = 1
                    let numbersLen = substrings.Length - 1
                    exceptionOccurred <- false
                    for i = 0 to numbersLen - 1 do
                        if not exceptionOccurred then
                            try
                                product <- product * int substrings.[i + 1]
                            with
                            | :? System.FormatException as e ->
                                product <- -2
                                printfn "Exception occurred during casting: %s" e.Message
                                exceptionOccurred <- true 
                    result <- product    
                    printfn "result: %d" result
                else
                    result <- -1
                    printfn "Incorrect command given"
            
                // Echo the message back to the client
                let smsg = sprintf "Result: %d" result
                csocket.Send(Encoding.ASCII.GetBytes(smsg)) |> ignore
        with
        | :? SocketException as se ->
            printfn "SocketException: %s" se.Message
        | :? ObjectDisposedException ->
            printfn "Client socket closed."
    }

// Main server function
let async main () =
    let ipAddress = IPAddress.Parse("127.0.0.1")
    let endPoint = IPEndPoint(ipAddress, serverPort)
    let serverSocket = new Socket(SocketType.Stream, ProtocolType.Tcp)
    
    try
        serverSocket.Bind(endPoint)
        serverSocket.Listen(4) // Listen for one client connection

        printfn "Server listening on port %d..." serverPort

        let rec acceptClients () =
            async {
                while true do
                    // Accept a client connection asynchronously
                    let! clientSocket = Async.AwaitTask(serverSocket.AcceptAsync())

                    // Start a new asynchronous operation to handle the client
                    Async.StartAsTask(handleClient clientSocket)

            }
        // Start accepting clients asynchronously
        do! acceptClients()

        // clientSocket.Close()
        // serverSocket.Close()
    with
    | :? SocketException as se ->
        printfn "SocketException: %s" se.Message
    | :? ArgumentException as ae ->
        printfn "ArgumentException: %s" ae.Message
    | :? InvalidOperationException as ioe ->
        printfn "InvalidOperationException: %s" ioe.Message
    | :? ObjectDisposedException as ode ->
        printfn "ObjectDisposedException: %s" ode.Message
    | ex ->
        printfn "An unexpected exception occurred: %s" ex.Message

// Call the main function
Async.RunSynchronously (main ())
