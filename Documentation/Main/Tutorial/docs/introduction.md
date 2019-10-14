# Introduction to AXAXL.DbEntity library

## What is DbEntity Library?

It is a database-first object-relational mapping support library.  The main goal is to handle database operation using plain C# objects.  Currently supported database is Microsoft SQL Database.
Setting up your project

There is only one Nuget package required to use this library, which is “AXAXL.DbEntity” and is available on our internal Nuget Repo http://xlre-nexus.r02.xlgs.local/repository/nuget-xlre/.  As of this writing, the latest version is 1.2.5.

## Designing your model classes

The DbEntity library has the following assumptions about a model class design.

1.	In a parent and child relationship, the parent model class will have only one primary key or one set of primary keys.  The library cannot support the relationship between parent and child if the parent key is not a primary key.
2.	The library will assume child set reference to be an IEnumerable, be it an Array or a List.
3.	The library will assume parent model class will instantiate child set reference in its class constructor.

## Annotating your model classes

Libraries required

-	System.ComponentModel.DataAnnotations
-	System.ComponentModel.DataAnnotations.Schema
-	AXAXL.DbEntity.Annotations

### Standard EF Core Data Annotation Supported

| Attribute	| Internet Reference |
|---|---|
| Table | [https://www.learnentityframeworkcore.com/configuration/data-annotation-attributes/table-attribute](https://www.learnentityframeworkcore.com/configuration/data-annotation-attributes/table-attribute) |
| Key | [https://www.learnentityframeworkcore.com/configuration/data-annotation-attributes/key-attribute](https://www.learnentityframeworkcore.com/configuration/data-annotation-attributes/key-attribute) |
| Column | [https://www.learnentityframeworkcore.com/configuration/data-annotation-attributes/column-attribute](https://www.learnentityframeworkcore.com/configuration/data-annotation-attributes/column-attribute) |
| DatabaseGenerated | [https://www.learnentityframeworkcore.com/configuration/data-annotation-attributes/databasegenerated-attribute](https://www.learnentityframeworkcore.com/configuration/data-annotation-attributes/databasegenerated-attribute) |
| ForeignKey | [https://www.learnentityframeworkcore.com/configuration/data-annotation-attributes/foreignkey-attribute](https://www.learnentityframeworkcore.com/configuration/data-annotation-attributes/foreignkey-attribute) |
| InverseProperty | [https://www.learnentityframeworkcore.com/configuration/data-annotation-attributes/inverseproperty-attribute](https://www.learnentityframeworkcore.com/configuration/data-annotation-attributes/inverseproperty-attribute) |
| ConcurrencyCheck | [https://www.learnentityframeworkcore.com/configuration/data-annotation-attributes/concurrencycheck-attribute](https://www.learnentityframeworkcore.com/configuration/data-annotation-attributes/concurrencycheck-attribute) |

### Standard EF Core Data Annotation Not Supported

-	ComplexType
-	Maxlength
-	NotMapped
-	StringLength
-	Timestamp

### Data Annotation Implemented by DbEntity Library

| Attribute | Apply To | Explanation |
|---|---|---|
| Connection | Class definition | Define the connection name used for accessing the database table behind this model class.  If absent or blank, the library will assume the intended connection is the default connection. |
| Constant | Property definition | Use this attribute on a model property, which holds a constant value. For example, as in the case of t_doc, when t_doc is associating with a certain table,  the owner_type in this specific relation is holding the same value for this particular table.  Owner_type will hold a different value when associating with another table. Therefore, when designing this kind of association between a model object a t_doc model, we can use `[Constant(“some value”)]` to tell the library to always use this assigned Constant value for database operation. |
| ValueInjection | Property definition | See explanation in the following section |
| ActionInjection | Property definition | See explanation in the following section |

#### Injection Annotation

Based on the [scripting capabilities offered by Roslyn compiler](https://github.com/dotnet/roslyn/wiki/Scripting-API-Samples), this annotation provides the capabilities to inject value into a model property using a Func delegate or update a model object with an Action delegate.  The c# script assigned to this annotation will be run by `CSharpScript` as like you are running interactive c# in visual studio.  This capability was designed to inject managed sequence number obtained from t_guid into primary key or to increment an integer version number.  Nevertheless, this capability can be extended to do much more.

| Attribute | Parameter | Explanation |
|---|---|---|
| ActionInjection | ActionScript | The c# script that will be compiled into an Action delegate
| ValueInjection | FunctionScript | The c# script that will be compiled into a Func delegate
| Both | When | When the script will be applied.  Valid values are `WhenInserted`, `WhenInsertedAndUpdated`, and `WhenUpdated`. The script will be applied to the model property before the database operation and on occasion, as specified. |
| Both | ScriptNamespaces | Other than System namespace and the namespace of the model object where this attribute is applied, use this parameter to add additional namespaces that your script may need. |
| Both | ServiceName | If you have a service injection prepared at startup and want to use it in your script, use the fully qualified interface name in this parameter, and the library will use it to get the service from service provider and inject it into the script engine. |

##### Dependency Injection Setup

Insert IDbService as one of the services in DI container.  The library has provided an extension method to do this under Microsoft.Extensions.DependencyInjection namespace.  Thus, when the DbEntity library NuGet package is installed, this extension will be available already.

Example.

The following will install IDbService into the DI container with the connection name “SQL_Connection” as the default connection.

```c#
services.AddSqlDbEntityService(
		config =>
		{
		config
			.AddOrUpdateConnection("SQL_Connection", …)
			.SetAsDefaultConnection("SQL_Connection")
			.PrintNodeMapToFile(…));
		})
```

Note that if you assign a file path to the `PrintNodeMapToFile` method, the model objects map called as `NodeMap` by the library, will be exported to the specified file in Markdown format.

After setting up the service, you will also need to `Bootstrap` the service.  This bootstrap can be done in the standard `Configure(IApplicationBuilder app, IHostingEnvironment env)` method under the ASP.NET Core startup convention.  A service provider will be available from the `IApplicationBuilder`.

By this bootstrap method, the library will scan through assemblies for model objects by looking for the `Table` attribute.

```c#
var service = app.ApplicationServices.GetService<IDbService>();
			Service.Bootstrap(
			assemblies: new[] { … },
			assemblyNamePrefixes: new[] { … });
```

Parameters

- assemblies. This is an array of assemblies you would want the DbEntity library to scan for your model objects.  A simple reflection call, like typeof(a model object).Assembly, will suffix.
If your model object is defined within the same web API project, such is not needed because your model object will be loaded in memory along with the web API.
- assemblyNamePrefixes. This parameter is optional.  By supplying the namespaces, you can narrow down to the search on your model objects.

## Functionalities

### Queries by IQuery

IDbService provides query capabilities via `IQuery` interface.  Methods are designed in a method chaining fashion with ToList, or ToArray terminate the chain and executes the query.  If concerned model objects have child sets, the child sets will be retrieved according to the parent-and-child relationship designed.

#### Methods on query conditions

```c#
IQuery<T> Where(Expression<Func<T, bool>> whereClause);

IQuery<T> Where<TParent, TChild>(Expression<Func<TChild, bool>> whereClause);

IQuery<T> And(Expression<Func<T, bool>> whereClause);

IQuery<T> And<TParent, TChild>(Expression<Func<TChild, bool>> whereClause);

IQuery<T> Or(params Expression<Func<T, bool>>[] orClauses);

IQuery<T> Or<TParent, TChild>(params Expression<Func<TChild, bool>>[] orClauses);

IQuery<T> OrderBy(Expression<Func<T, dynamic>> property, bool isAscending = true);
```

You can apply multiple `Where`, `And` and `Or` methods on one IQuery.  The service will treat them a filter conditions joined together by an `And` operator.

#### Methods on controlling navigation path

```c#
IQuery<T> Exclude(params Expression<Func<T, dynamic>>[] exclusions);

IQuery<T> Exclude<TObject>(params Expression<Func<TObject, dynamic>>[] exclusions) where TObject : class, new();
```

The `Exclude` methods are used to stop the service from walking down certain child sets.  If you would like to control the depth of your model object tree, use these methods to exclude certain child sets.

#### Methods on setting query behavior

```c#
IQuery<T> SetTimeout(int timeoutDurationInSeconds = 30);
```

#### Methods on executing the query and returns results

```c#
IList<T> ToList(int maxNumOfRow = -1);

T[] ToArray(int maxNumOfRow = -1);
```

Other than these two methods, all methods in IQuery are to set up the conditions and behavior of a query.  Only when you call `ToList` or `ToArray` methods will the service execute the query and returns the result with the maximum number of rows specified.  If a maximum number of rows is not set, zero, or less than zero, the full result set will be returned.

#### Operations supported in the where clause

- `In`.  Available as an extension method of string, int, bool, and long.
- `Like`. Available as an extension method of string.  Pattern in parameter follows the same rules as SQL operator `LIKE`.
- Regular Lambda logic operators

#### What is not supported in where clause

The IDbService cannot support methods being called on or by a model property, because such will require the service to transform the concerned method into a valid SQL function or operation.

#### What is supported in where clause

The IDbService can support methods or functions being used on non-model property or variables.  Such will be included in the Lambda evaluation and transformed into appropriate SQL parameters.

## Persisting Data

IPersist and IChangeSet work together to provide data persistence to the database.

### IPersist

`IPersist` is responsible for controlling the root transaction and carries out the database operation on `Commit`.

```c#
IPersist Submit(Func<IChangeSet, IChangeSet> submitChangeSet)
```

- Submitting IChangeSet with which you can use to save one or more model objects, and with different transaction scope option than the root transaction, if you need to.

```c#
IPersist SetRootTransactionSCopeOption(TransactionScopeOption scopeOption)
```

- Assign root transaction scope option.
- If you omit this call, the service will assume transaction scope option being “Required”

```c#
IPersist SetRootIsolationLevel(IsolationLevel isolation)
```

- Assign the isolation level to the root transaction.
- If you omit this call, the service will assume “ReadCommitted” as the isolation level.

```c#
int Commit();
```

- Execute the database operations associated with each changeset submitted.
- Returns the number of records affected.

### IChangeSet

`IChangeSet` allows you to save multiple model objects and with an option to set a different transaction scope option and isolation level than the root transaction as set in IPersist.  The IDbService will walk down the object tree from the model object you submitted.

```c#
IChangeSet Save(params ITrackable[] entities)
```

- Save multiple model objects.  The service will determine the database operation depending on the EntityStatus of each model object.

```c#
IChangeSet Insert(params ITrackable[] entities);

IChangeSet Update(params ITrackable[] entities);

IChangeSet Delete(params ITrackable[] entities);
```

- Perform database operation on the model objects submitted according to the method you choose, disregarding the EntityStatus of the model objects.

```c#
IChangeSet Exclude<TObject>(params Expression<Func<TObject, dynamic>>[] exclusions) where TObject : class;
```

- Exclude childsets from database operation.

```c#
IChangeSet SetTransactionScopeOption(TransactionScopeOption option);
```
- Assign a different transaction scope option then the root transaction.
- If omitted, IDbService will use the same transaction scope option as the root transaction.

```c#
IChangeSet SetIsolationLevel(IsolationLevel isolationLevel)
```

- Assign a different isolation level than the root transaction.
- If omitted, IDbService will use the same isolation level as the root transaction.

## Executing Raw SQL or Stored Procedure

`IExecuteCommand` is for executing raw SQL or stored procedures.

```c#
IExecuteCommand SetStoredProcedure(string storedProcedureName, string connectionName = null);
```

- Use this method to assign the stored procedure.
- If you omit connectionName, the service will assume default connection.

```c#
IExecuteCommand SetCommand(string command, string connectionName = null);
```

- Use this method to assign the raw SQL.
- If you omit connectionName, the service will assume default connection.

```c#
IExecuteCommand SetParameters(params (string Name, object Value, ParameterDirection Direction)[] parameters);
```
- Use this method to assign SQLParameters.

```c#
IExecuteCommand SetTransactionScopeOption(TransactionScopeOption option);
```

- Assign transaction scope option if required.

```c#
IExecuteCommand SetIsolationLevel(System.Transactions.IsolationLevel isolationLevel);
```

- Assign isolation level if required.

```c#
IExecuteCommand SetTimeout(int timeoutDurationInSeconds = 30);
```

- Assign timeout if required.  Assign 0 or -1 for no timeout.

```c#
IEnumerable<dynamic> Execute(out IDictionary<string, object> parameters);

IEnumerable<T> Execute<T>(out IDictionary<string, object> parameters) where T : class, new();
```

- Perform the assigned database operation.
- The service will consolidate the output `SQLParameters` into the output dictionary.
- If the raw SQL or stored procedure returns data rows, you can get it from the resulting IEnumerable.
