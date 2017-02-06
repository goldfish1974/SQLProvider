#r @"../../../bin/FSharp.Data.SqlProvider.dll"

open System
open System.Linq
open FSharp.Data.Sql
open FSharp.Data.Sql.Common.QueryEvents

// Install this:
// http://www.microsoft.com/download/en/confirmation.aspx?id=23734

[<Literal>]
let connectionString = @"Provider=Microsoft.ACE.OLEDB.12.0; Data Source= " + __SOURCE_DIRECTORY__ + @"\..\..\..\docs\files\msaccess\Northwind.mdb"

type mdb = SqlDataProvider<Common.DatabaseProviderTypes.MSACCESS, connectionString, @"" , @"", 100, true>
let ctx = mdb.GetDataContext()

SqlQueryEvent.Add (printfn "%s")

/// Normal query
let mattisOrderDetails =
    query { for c in ctx.Northwind.Customers do
            // you can directly enumerate relationships with no join information
            //for o in ctx.Northwind.Customers.FK_Orders_Customers do
            // or you can explicitly join on the fields you choose
            join od in ctx.Northwind.Orders on (c.CustomerId = od.CustomerId)
            //  the (!!) operator will perform an outer join on a relationship
            //for prod in (!!) od.``FK_Order Details_Products`` do 
            // nullable columns can be represented as option types. The following generates IS NOT NULL
            where c.Country.IsSome
            // standard operators will work as expected; the following shows the like operator and IN operator
            where (c.ContactName.Value =% ("Matti%") && od.ShipCountry.Value |=| [|"Finland";"England"|] )
            sortBy od.ShippedDate.Value
            // arbitrarily complex projections are supported
            select (c.ContactName, od.ShipAddress, od.ShipCountry, od.ShipName, od.ShippedDate.Value.Date) } 
    |> Seq.toArray

/// Query with space in table name
let orderDetail =
    query { 
        for c in ctx.Northwind.OrderDetails do
        select c
        head }
//orderDetail.Discount <- 0.5f
//orderDetail.Delete()
//ctx.SubmitUpdates()


/// CRUD Test. To use CRUD you have to have a primary key in your table. 
let crudops =
    let neworder = ctx.Northwind.Customers.``Create(CompanyName)``("FSharp.org")
    neworder.CustomerId <- Some "MyId"
    neworder.City <- Some "London"
    ctx.SubmitUpdates()
    let fetched =
        query { 
            for c in ctx.Northwind.Customers do
            where (c.CustomerId = Some "MyId")
            headOrDefault }
    fetched.Delete()
    ctx.SubmitUpdates()

/// Async contains query
let asyncContainsQuery =
    let contacts = ["Matti Karttunen"; "Maria Anders"]
    let r =
        async {
            let! res =
                query { 
                    for c in ctx.Northwind.Customers do
                    where (contacts.Contains(c.ContactName.Value))
                    select (c.CustomerId.Value, c.ContactName.Value)
                }|> Seq.executeQueryAsync
            return res |> Seq.toArray
        } |> Async.StartAsTask
    r.Wait()
    r.Result