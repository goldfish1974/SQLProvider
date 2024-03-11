module MsDataSqliteTransactions

open System
open System.IO
open FSharp.Data.Sql
open System.Linq
open NUnit.Framework
open System
open System.Transactions

[<Literal>]
let resolutionPath = __SOURCE_DIRECTORY__ + "/libs"

[<Literal>]
let connectionString =  @"Data Source=" + __SOURCE_DIRECTORY__ + @"/db/northwindEF.db;foreign keys=true"

type sql = SqlDataProvider<Common.DatabaseProviderTypes.SQLITE, connectionString, CaseSensitivityChange=Common.CaseSensitivityChange.ORIGINAL, ResolutionPath = resolutionPath, SQLiteLibrary=Common.SQLiteLibrary.MicrosoftDataSqlite>
FSharp.Data.Sql.Common.QueryEvents.SqlQueryEvent |> Event.add (printfn "Executing SQL: %O")

let createCustomer (dc:sql.dataContext) = 
    let newCustomer = dc.Main.Customers.Create()
    newCustomer.CustomerId <- "SQLPROVIDER"
    newCustomer.Address <- "FsPRojects"
    newCustomer.City <- "Fsharp"
    newCustomer.CompanyName <- "FSProjects"
    newCustomer.ContactName <- "A DB"
    newCustomer.ContactTitle <- "Mr"
    newCustomer.Country <- "England"
    newCustomer.Fax <- "Fax Number"
    newCustomer.Phone <- "Phone Number"
    newCustomer.PostalCode <- "PostCode"
    newCustomer.Region <- "London"
    newCustomer

[<Test>]
let ``If Error during transactions, database should rollback to the initial state``() = 
    let dc = sql.GetDataContext()
    
    let originalCustomers = 
        query { for cust in dc.Main.Customers do
                select cust }
        |> Seq.toList
     
    createCustomer dc |> ignore
    createCustomer dc |> ignore

    try     
        dc.SubmitUpdates()
    with
    | _ -> 
        ()

    let newCustomers = 
        query { for cust in dc.Main.Customers do
                select cust  }
        |> Seq.toList
    
    Assert.AreEqual(originalCustomers.Length, newCustomers.Length)
    // Clean up in case of test failure - does not work correctly for now.
    newCustomers 
    |> List.tryFind (fun x -> x.CustomerId = "SQLPROVIDER")
    |> Option.iter(fun created -> printfn "Cleaning up"; created.Delete(); dc.SubmitUpdates())