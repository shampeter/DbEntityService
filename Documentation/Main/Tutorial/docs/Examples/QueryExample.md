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

## Running Raw SQL against Database.

To run plain query and fetch result by the service, the code can look like this.

```c#
var service = serviceProvider
			.GetService<IDbService>();
var someTable = service.FromRawSql(
			@"select t.some_field_1, t.some_field_2, t.some_field_3 from some_table t where t.some_key = @SomeKey",
			new Dictionary<string, object>{ ["@SomeKey"] = 123 }
			).ToArray();
```
Where the dictionary parameter is used to provide value to the sql parameter `@SomeKey` in query.

And the result can be accessed by, say `someTable[10].some_field_1`, for the `some_field_1` column of the eleventh row of the query resultset.

For details of this C# type supporting this feature, please read [ExpandoObject Class](https://docs.microsoft.com/en-us/dotnet/api/system.dynamic.expandoobject?view=netcore-2.2).
