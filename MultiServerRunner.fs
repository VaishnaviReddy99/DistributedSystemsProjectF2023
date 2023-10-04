open System
open System.Net
open System.Net.Sockets
open System.Text
open System.Threading
open System.Collections.Concurrent

// Define IP and port
let ip = IPAddress.Parse("127.0.0.1")
let port = 12345


let mutable shouldTerminate = false
let shouldTerminateRef = ref shouldTerminate // Create a ref cell

let terminationLock = obj()  // An object to lock on

let safeSetTerminate () = 
    lock terminationLock (fun () -> shouldTerminate <- true)

let safeGetTerminate () = 
    lock terminationLock (fun () -> shouldTerminate)

let asyncPrint message = async {
    printfn "%s" message
}

// Thread-safe collection to hold all connected client sockets
let connectedClients = ConcurrentBag<Socket>() 

// Function to broadcast "terminate" message to all connected clients
let broadcastTerminate () = 
    for client in connectedClients do
        try
            let terminateMsg = "-5" // Error code for Terminate
            
            client.Send(Encoding.ASCII.GetBytes(terminateMsg)) |> ignore
        with
        | _ -> printfn "Error broadcasting terminate message to a client."
    printfn "done"

let handleClient (csocket: Socket) =
    async{
        try
        connectedClients.Add(csocket)
        let clientEndPoint = csocket.RemoteEndPoint.ToString()
        let currentThreadId = Thread.CurrentThread.ManagedThreadId
        asyncPrint (sprintf "ThreadId#%d Client connected: %s" currentThreadId clientEndPoint)
        let byteArrayLength = 1024 // Specify the length of the byte array
        let buffer = Array.create<byte> byteArrayLength 0uy   
        let mutable continueListening = true
        while continueListening && not (safeGetTerminate ()) do
            let termFlag = safeGetTerminate()
            let bytesRead = csocket.Receive(buffer)
            if bytesRead = 0 then
                printfn "ThreadId#%d Client disconnected: %s" currentThreadId clientEndPoint
                continueListening <- false
            else
                let message = Encoding.ASCII.GetString(buffer, 0, bytesRead)
                printfn "ThreadId#%d Received: %s" currentThreadId, message
                let separator = [| ' ' |] 
                let substrings = message.Split(separator, StringSplitOptions.RemoveEmptyEntries)
                let mutable result = 0
                let mutable exceptionOccurred = false

                if substrings.[0].Equals("bye",StringComparison.OrdinalIgnoreCase) then
                    continueListening <- false
                    result <- -5
                    printfn "ThreadId#%d Client saying bye" currentThreadId
                elif substrings.[0].Equals("terminate",StringComparison.OrdinalIgnoreCase) then
                    safeSetTerminate ()
                    broadcastTerminate ()
                    continueListening <- false
                    result <- -5
                    printfn "ThreadId#%d Received termination signal" currentThreadId

                elif not (substrings.[0].Equals("Add", StringComparison.OrdinalIgnoreCase) ||  substrings.[0].Equals("Substract", StringComparison.OrdinalIgnoreCase) ||    substrings.[0].Equals("Multiply", StringComparison.OrdinalIgnoreCase)) then
                    result <- -1
                    printfn "ThreadId#%d Incorrect operation provided ERR CODE: %d" currentThreadId result
                elif substrings.Length > 4 then
                    result <- -3
                    printfn "ThreadId#%d More than 4 inputs have been passed ERR CODE: %d" currentThreadId result
                elif substrings.Length < 2 then
                    result <- -2
                    printfn "ThreadId#%d Less than 2 inputs have been passed ERR CODE: %d" currentThreadId result
                elif substrings.[0].Equals("Add", StringComparison.OrdinalIgnoreCase) then
                    let numbersLen = substrings.Length - 1
                    for i = 0 to numbersLen - 1 do
                        if not exceptionOccurred then
                        try
                             result <- result + int substrings.[i + 1]
                         with
                        | :? System.FormatException as e ->
                            result <- -2
                            printfn "ThreadId#%d Exception occurred during casting: %s" currentThreadId e.Message
                            exceptionOccurred <- true       
                    printfn "ThreadId#%d result add: %d" currentThreadId result
                elif substrings.[0].Equals("Substract", StringComparison.OrdinalIgnoreCase) then
                    let numbersLen = substrings.Length - 1
                    for i = 0 to numbersLen - 1 do
                        if not exceptionOccurred then
                        try
                             result <-  int substrings.[i + 1] - result
                         with
                        | :? System.FormatException as e ->
                            result <- -2
                            printfn "ThreadId#%d Exception occurred during casting: %s" currentThreadId e.Message
                            exceptionOccurred <- true 
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
                                printfn "ThreadId#%d Exception occurred during casting: %s" currentThreadId e.Message
                                exceptionOccurred <- true 
                    result <- product    
                    printfn "ThreadId#%d result: %d" currentThreadId result
                else
                    result <- -1
                    printfn "ThreadId#%d Incorrect command given" currentThreadId
            
                // Echo the message back to the client
                let smsg = sprintf "%d" result
                csocket.Send(Encoding.ASCII.GetBytes(smsg)) |> ignore
        
        if safeGetTerminate() then
            printf "Termination signal being sent to %d \n" currentThreadId
            let smsg = sprintf "%d" -5
            csocket.Send(Encoding.ASCII.GetBytes(smsg)) |> ignore
        
        printfn "ThreadId#%d Closing connection for client %s" currentThreadId clientEndPoint

        with
        | :? SocketException as se ->
            printfn "SocketException: %s" se.Message
        | :? ObjectDisposedException ->
            printfn "Client socket closed."
    } |> Async.Start
// Start server
let startServer (ipAddress: IPAddress) (port: int) =
    // Create a TCP/IP socket
    let listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)

    try
        // Bind the socket to the local endpoint and listen for incoming connections
        listener.Bind(IPEndPoint(ipAddress, port)) |> ignore
        listener.Listen(100)

        printfn "Server is running on %O:%d" ipAddress port
        while not (safeGetTerminate ()) do
            printfn "Waiting for a connection..."
            let handler = listener.Accept()
            printfn "Client connected: %O" handler.RemoteEndPoint
            if not (safeGetTerminate ()) then
                Thread(ThreadStart(fun _ -> handleClient handler |> ignore)) |> (fun t -> t.Start())
       
        printfn "Terminating Server"
    with 
    | ex -> printfn "Exception: %s" (ex.Message)
    
    listener.Close()

// Start the server
let main() = 
    startServer ip port

main()
