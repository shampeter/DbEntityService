# Welcome to Tutorial

This tutorial will explain the main design goals of this component and thus the many functions and features provided.

## Design Goal

> To provide a simple yet powerful component that will replace the database access component from Framework 5.0.
> The design of the component has been focused on the following aspects.

__POCO__ : Plan Old C# Object

- The minimum requirement for an entity object is just the implementation of `ITraceable` interface with which also requires just status property to indicate of the entity is `New`, `Updated`, `Deleted` or `Unchanged`.
- For retrieving entity object from database, the component does not even require `ITraceable`.  The `ITraceable` interface is just required for persisting entity class.
- All `object-to-relational` mappings and attributes are accomplished by `CustomAttribute`, such as `Table`, `Column`, etc.

## Object Annotation

As said, most of the mapping functionalities will be accomplished by `CustomAttribute`. Attributes found in [System.ComponentModel.DataAnnotations](https://docs.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations?view=netcore-2.2) and [System.ComponentModel.DataAnnotations.Schema](https://docs.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.schema?view=netcore-2.2) are not only being used but the intended usage are being followed also.  New custom attributes are added only to address features specific to AXA XL legacy application requirements.

Following are the attributes employed or developed.

| Attribute  | Source   | Description                             |
|------------|--------------------|--------------------|
| Table      | System.Component  | Identify the underlying database table. Replacing the same from Framework 5.0|
| Column     | System.Component  | Identify the underlying database column.  Boolean property for identifying key or searchable columns as provided in Framework 5.0 will be retired. |
| Key        | System.Component  | Identify the primary key of the underlying database table. |
| DatabaseGenerated | System.Component  | Identify if framework should skip the concerned object property as the underlying database column value is being taken care by database, such as database `Default` constraint |
| ForeignKey | System.Component | Identify object property which correspond to the foreign key of a child set. |
| InverseProperty | System.Component |  Identify the navigation points between a parent-child relation |
| ConcurrencyCheck | System.Component | Identify the database column which will be used for optimistic record locking. |
| Connection | AXAXL | Same as designed in Framework 5.0        |
| ValueInjection |AXAXL   | Provide a `C#` script that will return a value for the corresponding object property during insertion, update or both.  |
| ActionInjection |AXAXL | Provide a `C#` script that will update the corresponding entity object during insertion, update or both.  |

### Note Worthy Changes

1. Compound primary key or foreign key are supported.  When mapping compound primary key to compound foreign key, the [Order](https://docs.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.schema.columnattribute.order?view=netframework-4.8#System_ComponentModel_DataAnnotations_Schema_ColumnAttribute_Order) attribute will be observed to map the keys accordingly.  If in any case, the number of columns between the compound primary and foreign keys does not match, say 2 columns for compound primary key but 3 columns for compound foreign keys, then only 2 columns between the keys will be mapped according to the `Order` attribute in ascending order.
1. When `DatabaseGenerated` attribute is used for column, such as `identity` key or `default` constraint, the new or updated value will be fetched into the entity object after the insert or update sql command.
1. `Version` column that used to be mandated in Framework 5.0 is also being phased out.  In fact, using optimistic record locking or not is up to the application design. Only when `ConcurrencyCheck` attribute is applied to a column will this service component attempts optimistic record locking. Indeed, [rowversion](https://docs.microsoft.com/en-us/sql/t-sql/data-types/rowversion-transact-sql?view=sql-server-2017) is preferred instead of an `int` column because `rowversion` is incremented automatically by the database in any situation.
