open System
open System.Net
open System.Net.Sockets
open System.Text
open System.Threading

let serverIP = IPAddress.Parse("127.0.0.1")
let serverPort = 12345

let clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
clientSocket.Connect(IPEndPoint(serverIP, serverPort))

let terminationLock = obj()  // An object to lock on
let mutable shouldTerminate = false 

let safeSetTerminate () = 
    lock terminationLock (fun () -> shouldTerminate <- true)

let safeGetTerminate () = 
    lock terminationLock (fun () -> shouldTerminate)

let sendLoop () =
    let buffer = new System.Text.StringBuilder()
    printf "Enter your command: "
    while not (safeGetTerminate ()) do
        if Console.KeyAvailable then
            let key = Console.ReadKey(intercept=true) // Read the key but don't display it
            if key.Key = ConsoleKey.Enter then
                let message = buffer.ToString()
                let messageBytes = Encoding.ASCII.GetBytes(message)
                clientSocket.Send(messageBytes) |> ignore
                buffer.Clear()

                // Check if we should terminate
                if message.ToLower() = "bye" || message.ToLower() = "terminate" then
                    safeSetTerminate ()
                    printfn "\nEnding send loop."
            else
                buffer.Append(key.KeyChar) |> ignore
                Console.Write(key.KeyChar) // Display the character to the console

        Thread.Sleep(100) // Sleep for a short duration before checking again


let receiveLoop () =
    while not (safeGetTerminate ()) do
        let buffer = Array.zeroCreate<byte> 1024
        let bytesRead = clientSocket.Receive(buffer)
        let message = Encoding.ASCII.GetString(buffer, 0, bytesRead)
        printfn ""
        printfn "Server Response : %s" message
        printf "Enter your command :"

        // Check if we should terminate
        if message.ToLower() = "-5" then
            safeSetTerminate ()
            printfn "Recieved code -5. Terminating the Client"

let main() =
    printfn "Client starting..."

    let sendThread = Thread(ThreadStart(sendLoop))
    let receiveThread = Thread(ThreadStart(receiveLoop))

    sendThread.Start()
    receiveThread.Start()

    sendThread.Join()
    receiveThread.Join()

    clientSocket.Close()
    printfn "Client stopped."

main()
