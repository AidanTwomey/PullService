module Main

open System.Reflection
[<assembly: AssemblyTitle("PullService")>]
()

open System.IO;
open System
open Topshelf
open Time
open FSharp.Configuration

type Settings = AppSettings<"app.config">

let lookupFiles =
    Directory.GetFiles(Settings.FtpSource) |> Seq.map (File.ReadAllLines)

[<EntryPoint>]
let main argv =

  let log = log4net.LogManager.GetLogger("pull service")
  let sleep (time : TimeSpan) = System.Threading.Thread.Sleep(time)
  
  log4net.Config.XmlConfigurator.Configure() |> ignore

  let start hc =
    log.InfoFormat("pull service starting")

    let rec loop () = async {
      do! Async.Sleep(1000 * Settings.FtpPullInterval)
      log.InfoFormat("check ftp")
      return! loop () }

    let ftpMonitor () = async {
      try 
        do! loop () 
      finally 
        printfn "finished checking ftp" }

    let cts = new System.Threading.CancellationTokenSource()
    Async.Start(ftpMonitor (), cts.Token)

    log.InfoFormat("pull service started")
    true 
    
  let stop hc =
    log.InfoFormat("pull service stopped")
    true
  
  Service.Default
  |> with_start start
  |> with_recovery (ServiceRecovery.Default |> restart (min 10))
  |> with_stop stop
  |> run