open System
open System.Net
open System.Net.Sockets
open System.Text
open System.Threading

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

let handleClient (csocket: Socket) =
    async{
        try
        let clientEndPoint = csocket.RemoteEndPoint.ToString()
        let currentThreadId = Thread.CurrentThread.ManagedThreadId
        asyncPrint (sprintf "#%d Client connected: %s" currentThreadId clientEndPoint)
        let byteArrayLength = 1024 // Specify the length of the byte array
        let buffer = Array.create<byte> byteArrayLength 0uy   
        let mutable continueListening = true
        while continueListening && not (safeGetTerminate ()) do
            let bytesRead = csocket.Receive(buffer)
            if bytesRead = 0 then
                printfn "#%d Client disconnected: %s" currentThreadId clientEndPoint
                continueListening <- false
            else
                let message = Encoding.ASCII.GetString(buffer, 0, bytesRead)
                printfn "#%d Received: %s" currentThreadId, message
                let separator = [| ' ' |] 
                let substrings = message.Split(separator, StringSplitOptions.RemoveEmptyEntries)
                let mutable result = 0
                let mutable exceptionOccurred = false

                if substrings.[0].Equals("bye",StringComparison.OrdinalIgnoreCase) then
                    continueListening <- false
                    printfn "#%d Client saying bye" currentThreadId
                elif substrings.[0].Equals("terminate",StringComparison.OrdinalIgnoreCase) then
                    safeSetTerminate ()
                    continueListening <- false
                    printfn "#%d Received termination signal" currentThreadId

                elif not (substrings.[0].Equals("Add", StringComparison.OrdinalIgnoreCase) ||  substrings.[0].Equals("Substract", StringComparison.OrdinalIgnoreCase) ||    substrings.[0].Equals("Multiply", StringComparison.OrdinalIgnoreCase)) then
                    result <- -1
                    printfn "#%d Incorrect operation provided ERR CODE: %d" currentThreadId result
                elif substrings.Length > 4 then
                    result <- -3
                    printfn "#%d More than 4 inputs have been passed ERR CODE: %d" currentThreadId result
                elif substrings.Length < 2 then
                    result <- -2
                    printfn "#%d Less than 2 inputs have been passed ERR CODE: %d" currentThreadId result
                elif substrings.[0].Equals("Add", StringComparison.OrdinalIgnoreCase) then
                    let numbersLen = substrings.Length - 1
                    for i = 0 to numbersLen - 1 do
                        if not exceptionOccurred then
                        try
                             result <- result + int substrings.[i + 1]
                         with
                        | :? System.FormatException as e ->
                            result <- -2
                            printfn "#%d Exception occurred during casting: %s" currentThreadId e.Message
                            exceptionOccurred <- true       
                    printfn "#%d result add: %d" currentThreadId result
                elif substrings.[0].Equals("Substract", StringComparison.OrdinalIgnoreCase) then
                        // Code for "Subtract" case
                        printfn  "#%d substracting" currentThreadId
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
                                printfn "#%d Exception occurred during casting: %s" currentThreadId e.Message
                                exceptionOccurred <- true 
                    result <- product    
                    printfn "#%d result: %d" currentThreadId result
                else
                    result <- -1
                    printfn "#%d Incorrect command given" currentThreadId
            
                // Echo the message back to the client
                let smsg = sprintf "Result: %d" result
                csocket.Send(Encoding.ASCII.GetBytes(smsg)) |> ignore
        printfn "#%d Closing connection for client %s" currentThreadId clientEndPoint

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
