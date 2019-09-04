# Example in using DbEntityService for querying data

**The service is still be tested and developed.  Thus the exact API may change but the features presented in this tutorial would stay.**

*Assumption.  For a child set reference such as IList<SomeChildObject> on a parent entity object, the service __always assume__ that the reference __is not null__.  That means it is the application responsibility to make sure that a __child list is always instantiated__ in the parent entity constructor.*

## Retrieving Entity Object from Database

For example, we have an object that looks like the following.

```c#
	[Table("t_ceded_contract")]
    public class TCededContract : ITrackable
    {
        public TCededContract()
        {
            CededContractLayers = new List<TCededContractLayer>();
        }

		[Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("ceded_contract_pkey")]
        public int CededContractPkey { get; set; }

        [Column("uw_year")]
        public int UwYear { get; set; }

        [Column("xl_company_fkey")]
        public int XlCompanyFkey { get; set; }

		...
		...

        [ForeignKey(nameof(XlCompanyFkey))]
        public TCompany XlCompany { get; set; }

		[InverseProperty(nameof(TCededContractLayer.CededContract))]
		public IList<TCededContractLayer> CededContractLayers { get; set; }
	}
```

And we would like to find contract of year 2008 and of XL company with guid 1234.  Then we can query the entity using the following code.

```c#
var service = serviceProvider
				.GetService<IDbService>();
var contracts = service.Query<TCedecContract>()
				.Where(c => c.UwYear == 2008 && c.XlCompanyFkey == 1234)
				.ToArray();
```

Because foreign key has been setup between `TCededContract` and `TCompany`, the query will also retrieve the corresponding `TCompany` object for this contract, and it can be access via this `XlCompany` property on `TCededContract`.

## Running Query with some child set excluded

Assuming we have a structure like this

`TCededContract.CededContractLayers` &rarr; `TCededContractLayer.CededContractLayerDocs` &rarr; `TCededContractContractLayerDoc`

Then to retrieve a contract, say with contract number being 100, but leaving out the `CededContractLayerDocs` of `TCededContractLayer`, the code to do that will look like this:

```c#
var contract = _dbService
				.Query<TCededContract>()
				.Where(c => c.CededContractNum == 100)
				.Exclude<TCededContractLayer>(l => l.CededContractLayerDocs)
				.ToArray()
				.FirstOrDefault()
				;
```

## Running Raw SQL against Database

To run plain query and fetch result by the service, the code can look like this.

```c#
IDictionary<string, object> output;
var resultSet = serviceProvider
			.GetService<IDbService>();
			.ExecuteCommand()
			.SetCommand("Select c.company_name, ct.description from t_company c inner join t_lookups ct on c.company_type_fkey = ct.lookups_pkey")
			.Execute(out output)
			.ToArray();
```

In this example, the `resultSet` will be an array of `ExpandoObject` object with property `company_name` and `description`.  To access the result, one can just do this, for example,

```c#
foreach(var eachRow in resultSet)
{
	Console.WriteLine("Company = {0}, Type = {1}", eachRow.company_name, eachRow.description);
}
```

For details of this C# type supporting this feature, please read [ExpandoObject Class](https://docs.microsoft.com/en-us/dotnet/api/system.dynamic.expandoobject?view=netcore-2.2).

## Running stored procedure, such as `spu_getguid`

To execute, say, spu_getguid in an independent transaction, one can do this:

```c#
IDictionary<string, object> outputParameters;
var resultSet = this.DbService.ExecuteCommand()
							.SetStoredProcedure("[dbo].[spu_getguid]")
							.SetParameters(
								(@"guid_id", type, ParameterDirection.Input),
								(@"add2guid", range, ParameterDirection.Input),
								(@"next_guid", -1, ParameterDirection.Output)
							)
							.SetTransactionScopeOption(TransactionScopeOption.Suppress)
							.Execute(out outputParameters);
var nextGuid = (int)outputParameters["next_seq"];
```
