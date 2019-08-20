# Database Entity Service

This is the database access component designed for AXA XL Framework 6.0, replacing Desitny Framework 5.0.

## Directions

Following are the design directions for this component which addresses many of the shortcomings encountered over the years on the existing Framework 5.0 database access capabilities.

1. Avoid Redundant Implementation

    Framework 5.0 due to its age, has implemented features which were missing during its time of conception.  The framework or the underlying `Microsoft Enterprise Library` has implemented

    - SQL command retry in times of database access issue; and
    - Database connection to database transaction coupling so that it was guaranteed database connection was re-used under the same database transaction.

    These implementations will not be implemented again on this database access component in order to avoid redundant implementation and possible complication.

1. Will Not Expose Underlying Database Connection and Data Reader Used

    Framework 5.0 by using `Microsoft Enterprise Library` (or `EntLib` in abbreviation) exposed the `SQLDatabase` of the `EntLib` for application use.  Such feature helped guarantee that the underlying database connection was closed properly after simple query execution.  However, with respect to data reader, it was still left for application logics to create and close a data reader properly.  Over the years, it was observed that the usage of these 2 objects would varies widely and lead to hidden error, such as data reader left open indefinitely.

    In order to address this issue, Framework 6.0 will not expose any database connection, SQL command or data reader object.  If direct access to database connection or data reader is needed, developers would just code them directly following Microsoft references and guidelines.  Such arrangement, we hope, would help avoid confusion.

1. Simple `C#` Object

    Framework 5.0, when dealing with database persistence, required the concerned object to be designed according to base classes and interfaces.  Because of the features and capabilities of the language and runtime, such design was the only way.

    Nevertheless, with the advance of `.NET` runtime and `C#` language features, database persistence can be implemented on simple `C#` object without any interface or base class requirement.  If individual application implementation find it useful to have a common base class of objects participating in database operation, such still can be done, but it would not be a requirement imposed by the framework anymore.

1. Injection-based Automation

    Framework 5.0 carried a lot of automation on database columns, such as the audit columns like `added_by`, `modify_dt` and `version` etc.  The data assigned by these fields were hard-coded by framework to help automate the updates of these common fields.  Nevertheless, such implementation lead made enhancement much harder.

    With Framework 6.0, the update of these fields will still be automated but will be `injected` by applying annotation to the corresponding object properties.  Such implementation will open up a lot more possibilities for future enhancement.

1. Borrow a page from `Entity Framework Core`

    Since Framework 5.0 was implemented in the days where there was no defacto standard ORM framework found in the industry, thus the full database access convention and API were designed following the best practices of those days.

    Nevertheless, after 10 years, our unique implementation alienated new developers joining the time.  With that in mind, Framework 6.0 will be designed following the same design strategies of `Entity Framework Core`, hopefully new developers joining the team can draw on their experience with this industry defacto standard, making their adoption on Framework 6.0 easier.

## Features

Following sections will describe the features implemented in the Framework 6.0 database access component.

1. Object Annotation

    Many of the Framework 5.0 custom attributes will be replaced by the same found in [System.ComponentModel.DataAnnotations](https://docs.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations?view=netcore-2.2) and [System.ComponentModel.DataAnnotations.Schema](https://docs.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.schema?view=netcore-2.2).  Nevertheless, new custom attributes will be developed to address features not addressed by the attributes in these 2 namespaces.

    Following are the attributes, in mind.

    | Attribute  | System.Component   | Custom             | Description                             |
    |------------|--------------------|--------------------|-----------------------------------------|
    | Table      | :heavy_check_mark: |                    | Identify the underlying database table. |
    | Column     | :heavy_check_mark: |                    | Identify the underlying database column.  Boolean property for identifying key or searchable columns as provided in Framework 5.0 will be retired. |
    | Connection |                    | :heavy_check_mark: | ** Pending Design Decision **           |
    | Key        | :heavy_check_mark: |                    | Identify the primary key of the underlying database table. |
    | DatabaseGenerated | :heavy_check_mark: |                    | Identify if framework should skip the concerned object property as the underlying database column value is being taken care by database, such as database `Default` constraint |
    | ForeignKey | :heavy_check_mark: |                    | Identify object property which correspond to the foreign key of a child set. |
    | InverseProperty | :heavy_check_mark: |                    | Identify the navigation points between a parent-child relation |
    | ValueInjection |                    | :heavy_check_mark: | Provide a `C#` script that will return a value for the corresponding object property during insertion, update or both.  |
    | ActionInjection |                    | :heavy_check_mark: | Provide a `C#` script that will update the corresponding object property during insertion, update or both.  |
    | ConcurrencyCheck | :heavy_check_mark: |                    | Identify the database column which will be used for optimistic record locking |

## Overall Components / Sub-Systems Design

1. Annotation System.  Dictate the annotations being used.  For this implementation, as mentioned, it will be the following.  If required, a different annotation system can be design to provide the same database specific meta data.

    - `System.ComponentModel.DataAnnotations`
    - `System.ComponentModel.DataAnnotations.Schema`
    - Own custom annotation for `ValueInjection` and `ActionInjection`.

1. Node Graph.  This is the database neutral meta data model that help map a plain object into a database table.  It identifies

    - Table
    - Schema
    - Connection
    - Primary Key
    - Data Columns
    - Optimistic Record Locking Mechanism
    - Relationships between objects by means of Foreign Key and Parent/Child reference point in an object.

1. Director. By means of [Builder Pattern](https://en.wikipedia.org/wiki/Builder_pattern#Definition), this is the component that will walk through the node graph and call `Builder` to materialize the objects from database.  It controls

    - Which path to walk through and which to skip, like excluding some childset for the size of the object tree retrieved.
    - How far up the direction from a child to parent and how deep the navigation on the graph will go from parent to child.
    - Keep track of the path walked so that the navigation on the node graph won't go in circle.

1. Builder or Sql Database Driver.  By means of [Builder Pattern](https://en.wikipedia.org/wiki/Builder_pattern#Definition), this is the builder.  It is responsible to

    - Making us of information on node graph and build Sql Statement of a particular database vendor for `CRUD` operations.
    - Translate a `where` clause in `LINQ` expressions into actual `where` clause in Sql of a particular database vendor.

And would probably introduce a `DbContext-Like` facade object to provide one entry point.
